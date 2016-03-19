using System.Drawing;
using System.Windows.Forms;

namespace SimpleMJPEGStreamViewer {
    public class SimplePictureBox : Control {

        public SimplePictureBox() {
            DoubleBuffered = true;
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (e, s) => {
                fps = frameCount.ToString();
                frameCount = 0;
                Invalidate();
            };
            timer.Start();
        }

        string fps;
        int frameCount;

        Image image;

        public Image Image {
            get {
                return image;
            }
            set {
                if(image != null)
                    image.Dispose();

                image = value;

                frameCount++;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(Image != null)
                e.Graphics.DrawImage(Image, this.ClientRectangle);
            e.Graphics.DrawString(fps, this.Font, Brushes.Red, 20, 20);
        }
    }
}
