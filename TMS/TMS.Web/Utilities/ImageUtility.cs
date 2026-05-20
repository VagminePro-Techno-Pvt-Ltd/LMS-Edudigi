using System.Drawing;

namespace TMS.Web.Utilities
{
    public class ImageUtility
    {
        public static Stream ResizeImage(Stream processingImage, int resizeheight = 75, int resizewidth = 100)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            using (Image imgPhoto = Image.FromStream(processingImage)) //background image or big image
            {
                int sourceWidth = imgPhoto.Width;
                int sourceHeight = imgPhoto.Height;
                int sourceX = 0;
                int sourceY = 0;
                int destX = 0;
                int destY = 0;

                float nPercent = 0;
                float nPercentW = 0;
                float nPercentH = 0;

                nPercentW = ((float)resizewidth / (float)sourceWidth);
                nPercentH = ((float)resizeheight / (float)sourceHeight);
                if (nPercentH < nPercentW)
                {
                    nPercent = nPercentH;
                    destX = System.Convert.ToInt16((resizewidth - (sourceWidth * nPercent)) / 2);
                }
                else
                {
                    nPercent = nPercentW;
                    destY = System.Convert.ToInt16((resizeheight - (sourceHeight * nPercent)) / 2);
                }

                int destWidth = (int)(sourceWidth * nPercent);
                int destHeight = (int)(sourceHeight * nPercent);

                using (Bitmap bmPhoto = new Bitmap(resizewidth, resizeheight, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

                    using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
                    {
                        grPhoto.Clear(System.Drawing.Color.White);
                        grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        grPhoto.DrawImage(imgPhoto,
                            new Rectangle(destX, destY, destWidth, destHeight),
                            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                            GraphicsUnit.Pixel);
                    }
                    Stream memoryStream = new MemoryStream();
                    bmPhoto.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return memoryStream;
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}
