using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMJPEGStreamViewer {
    static class SimpleMJPEGDecoder {

        // JPEG delimiters
        const byte picMarker = 0xFF;
        const byte picStart = 0xD8;
        const byte picEnd = 0xD9;

        /// <summary>
        /// Start a MJPEG on a http stream
        /// </summary>
        /// <param name="action">Delegate to run at each frame</param>
        /// <param name="url">url of the http stream (only basic auth is implemented)</param>
        /// <param name="login">optional login</param>
        /// <param name="password">optional password (only basic auth is implemented)</param>
        /// <param name="token">cancellation token used to cancel the stream parsing</param>
        /// <param name="chunkMaxSize">Max chunk byte size when reading stream</param>
        /// <param name="frameBufferSize">Maximum frame byte size</param>
        /// <returns></returns>
        /// 
        public async static Task StartAsync(Action<Image> action, string url, string login = null, string password = null, CancellationToken? token = null, int chunkMaxSize = 1024, int frameBufferSize = 1024 * 1024) {

            var tok = token ?? CancellationToken.None;

            using(var cli = new HttpClient()) {
                if(!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
                    cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}")));

                using(var stream = await cli.GetStreamAsync(url).ConfigureAwait(false)) {

                    var streamBuffer = new byte[chunkMaxSize];      // Stream chunk read
                    var frameBuffer = new byte[frameBufferSize];    // Frame buffer

                    var frameIdx = 0;       // Last written byte location in the frame buffer
                    var inPicture = false;  // Are we currently parsing a picture ?
                    byte current = 0x00;    // The last byte read
                    byte previous = 0x00;   // The byte before

                    // Continuously pump the stream. The cancellationtoken is used to get out of there
                    while(true) {
                        var streamLength = await stream.ReadAsync(streamBuffer, 0, chunkMaxSize, tok).ConfigureAwait(false);
                        parseStreamBuffer(action, frameBuffer, ref frameIdx, streamLength, streamBuffer, ref inPicture, ref previous, ref current);
                    };
                }
            }
        }

        // Parse the stream buffer

        static void parseStreamBuffer(Action<Image> action, byte[] frameBuffer, ref int frameIdx, int streamLength, byte[] streamBuffer, ref bool inPicture, ref byte previous, ref byte current) {
            var idx = 0;
            while(idx < streamLength) {
                if(inPicture) {
                    parsePicture(action, frameBuffer, ref frameIdx, ref streamLength, streamBuffer, ref idx, ref inPicture, ref previous, ref current);
                }
                else {
                    searchPicture(frameBuffer, ref frameIdx, ref streamLength, streamBuffer, ref idx, ref inPicture, ref previous, ref current);
                }
            }
        }

        // While we are looking for a picture, look for a FFD8 (end of JPEG) sequence.

        static void searchPicture(byte[] frameBuffer, ref int frameIdx, ref int streamLength, byte[] streamBuffer, ref int idx, ref bool inPicture, ref byte previous, ref byte current) {
            do {
                previous = current;
                current = streamBuffer[idx++];

                // JPEG picture start ?
                if(previous == picMarker && current == picStart) {
                    frameIdx = 2;
                    frameBuffer[0] = picMarker;
                    frameBuffer[1] = picStart;
                    inPicture = true;
                    return;
                }
            } while(idx < streamLength);
        }

        // While we are parsing a picture, fill the frame buffer until a FFD9 is reach.

        static void parsePicture(Action<Image> action, byte[] frameBuffer, ref int frameIdx, ref int streamLength, byte[] streamBuffer, ref int idx, ref bool inPicture, ref byte previous, ref byte current) {
            do {
                previous = current;
                current = streamBuffer[idx++];
                frameBuffer[frameIdx++] = current;

                // JPEG picture end ?
                if(previous == picMarker && current == picEnd) {
                    Image img = null;

                    // Using a memorystream this way prevent arrays copy and allocations
                    using(var s = new MemoryStream(frameBuffer, 0, frameIdx)) {
                        try {
                            img = Image.FromStream(s);
                        }
                        catch {
                            // We dont care about badly decoded pictures
                        }
                    }

                    // Defer the image processing to prevent slow down
                    // The image processing delegate must dispose the image eventually.
                    Task.Run(() => action(img));
                    inPicture = false;
                    return;
                }
            } while(idx < streamLength);
        }
    }
}
