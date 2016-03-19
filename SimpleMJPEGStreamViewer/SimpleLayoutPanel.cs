
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace SimpleMJPEGStreamViewer {
    public partial class SimpleLayoutPanel : Panel {
        public SimpleLayoutPanel() {
            DoubleBuffered = true;
        }

        readonly public int HorizontalSpace = 1;
        readonly public int VerticalSpace = 1;
        readonly public double Ratio = 4/3d;

        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);

            ReArrangeControls();
            Invalidate(true);
        }

        public void ReArrangeControls() {
            if(!Visible)
                return;
            this.SuspendLayout();
            arrange(this.Width, this.Height, HorizontalSpace, VerticalSpace, Ratio, this.Controls.OfType<Control>().Where(ctl => ctl.Visible));
            this.ResumeLayout(false);
        }

        class ItemSize {
            public int Cols;
            public int Rows;
            public int Width;
            public int Height;
        }

        static ItemSize calculateBestItemSize(int w, int h, int n, int horizontalGap, int verticalGap, double ratio) {
            if(n == 0)
                return null;

            return (

                // Try n rows
                Enumerable.Range(1, n).Select(testRowCount => {
                    var testItemHeight = (h - verticalGap * (testRowCount - 1)) / testRowCount;

                    return new ItemSize {
                        Cols = (int)Math.Ceiling((double)n / testRowCount),
                        Rows = testRowCount,
                        Height = (int)testItemHeight,
                        Width = (int)(testItemHeight * ratio)
                    };
                })

                // Try n columns
                .Concat(
                Enumerable.Range(1, n).Select(testColCount => {
                    var testItemWidth = (w - horizontalGap * (testColCount - 1)) / testColCount;

                    return new ItemSize {
                        Cols = testColCount,
                        Rows = (int)Math.Ceiling((double)n / testColCount),
                        Height = (int)(testItemWidth / ratio),
                        Width = (int)testItemWidth
                    };
                })))

                // Remove when it's too big
                .Where(item => item.Width * item.Cols + horizontalGap * (item.Cols - 1) <= w &&
                               item.Height * item.Rows + verticalGap * (item.Rows - 1) <= h)

                // Get the biggest area
                .OrderBy(item => item.Height * item.Width)
                .LastOrDefault();
        }

        static void arrange(int width, int height, int horizontalGap, int verticalGap, double aspectRatio, IEnumerable<Control> controls) {

            var count = controls.Count();
            if(count == 0)
                return;

            var bestSizedItem = calculateBestItemSize(width, height, count, horizontalGap, verticalGap, aspectRatio);
            if(bestSizedItem == null)
                return;

            // Centering
            var xCenter = (width - (bestSizedItem.Width * bestSizedItem.Cols + bestSizedItem.Cols * horizontalGap - horizontalGap)) / 2;
            var y = (height - (bestSizedItem.Height * bestSizedItem.Rows + bestSizedItem.Rows * verticalGap - verticalGap)) / 2;

            var x = xCenter;

            foreach(var control in controls) {
                control.SetBounds(x, y, bestSizedItem.Width, bestSizedItem.Height);
                x += bestSizedItem.Width + horizontalGap;
                if(x + bestSizedItem.Width - horizontalGap > width) {
                    x = xCenter;
                    y += bestSizedItem.Height + verticalGap;
                }
            }
        }
    }
}
