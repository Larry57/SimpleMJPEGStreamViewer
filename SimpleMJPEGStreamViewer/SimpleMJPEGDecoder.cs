using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMJPEGStreamViewer {
    static class SimpleMJPEGDecoder {

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
        public async static Task StartAsync(Action<Image> action, string url, string login = null, string password = null, CancellationToken? token = null, int chunkMaxSize = 1024, int frameBufferSize = 1024 * 1024) {
            var tok = token ?? CancellationToken.None;
            tok.ThrowIfCancellationRequested();

            using(var cli = new HttpClient()) {
                if(!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
                    cli.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{login}:{password}")));

                using(var stream = await cli.GetStreamAsync(url).ConfigureAwait(false)) {

                    var streamBuffer = new byte[chunkMaxSize];
                    var frameBuffer = new byte[frameBufferSize];

                    var frameIdx = 0;
                    var inPicture = false;
                    var previous = (byte)0;
                    var current = (byte)0;

                    while(true) {
                        var streamLength = await stream.ReadAsync(streamBuffer, 0, chunkMaxSize, tok).ConfigureAwait(false);
                        ParseBuffer(action, frameBuffer, ref frameIdx, ref inPicture, ref previous, ref current, streamBuffer, streamLength);
                    };
                }
            }
        }

        static void ParseBuffer(Action<Image> action, byte[] frameBuffer, ref int frameIdx, ref bool inPicture, ref byte previous, ref byte current, byte[] streamBuffer, int streamLength) {
            var idx = 0;
            loop:
            if(idx < streamLength) {
                if(inPicture) {
                    do {
                        previous = current;
                        current = streamBuffer[idx++];
                        frameBuffer[frameIdx++] = current;
                        if(previous == (byte)0xff && current == (byte)0xd9) {
                            Image img = null;
                            using(var s = new MemoryStream(frameBuffer, 0, frameIdx)) {
                                try {
                                    img = Image.FromStream(s);
                                }
                                catch {
                                    // dont care about errors while decoding bad picture
                                }
                            }
                            Task.Run(() => action?.Invoke(img));
                            inPicture = false;
                            goto loop;
                        }
                    } while(idx < streamLength);
                }
                else {
                    do {
                        previous = current;
                        current = streamBuffer[idx++];
                        if(previous == (byte)0xff && current == (byte)0xd8) {
                            frameIdx = 2;
                            frameBuffer[0] = (byte)0xff;
                            frameBuffer[1] = (byte)0xd8;
                            inPicture = true;
                            goto loop;
                        }
                    } while(idx < streamLength);
                }
            }
        }
    }
}
