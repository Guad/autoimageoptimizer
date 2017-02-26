using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using nQuant;

namespace AutoImageOptimizer.Implementations
{
    public class PngOptimizer : IOptimizer
    {
        public bool Optimizable(string extension)
        {
            return extension.EndsWith(".png");
        }

        public void Optimize(string fullPath, Stream fileStream)
        {
            var quantizer = new WuQuantizer();

            //Bitmap bmp;

            using (Bitmap bmp = new Bitmap(fileStream))
            using (Bitmap bmp32 = ConvertBitmapTo32Bit(bmp))
            using (Image optimized = quantizer.QuantizeImage(bmp32, 0, 0))
            {
                //bmp.Dispose();

                using (fileStream)
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    optimized.Save(fileStream, ImageFormat.Png);
                    fileStream.SetLength(fileStream.Position);
                }
            }
        }

        private Bitmap ConvertBitmapTo32Bit(Bitmap input)
        {
            var tmp = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(tmp))
                g.DrawImageUnscaled(input, new Point(0, 0));

            return tmp;
        }
    }
}