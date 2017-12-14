using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace ImageTools
{

    public class ImageTransform
    {
        static public Bitmap rotateImage90(Bitmap b)
        {
            
            Bitmap returnBitmap = (Bitmap)b.Clone();
            returnBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

            //Altera o Exif se houver
            try
            {
                PropertyItem prop = returnBitmap.GetPropertyItem((int)PropertyTagId.Orientation);
                Orientation txt = (Orientation)PropertyTag.getValue(prop);

                prop.Value = new Byte[] { 1, 0 };

                returnBitmap.SetPropertyItem(prop);
            }
            catch { }

            /*
            Bitmap returnBitmap = new Bitmap(b.Width, b.Height);
            Graphics g = Graphics.FromImage(returnBitmap);
            g.DrawImage(b, new Point(0, 0));
            returnBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);*/
            return returnBitmap;
        }

        static public Bitmap rotateImage270(Bitmap b)
        {
            Bitmap returnBitmap = (Bitmap)b.Clone();
            returnBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);


            //Altera o Exif se houver
            try
            {
                PropertyItem prop = returnBitmap.GetPropertyItem((int)PropertyTagId.Orientation);
                Orientation txt = (Orientation)PropertyTag.getValue(prop);

                prop.Value = new Byte[] { 1, 0 };

                returnBitmap.SetPropertyItem(prop);
            }
            catch { }

            /*
            Bitmap returnBitmap = new Bitmap(b.Width, b.Height);
            Graphics g = Graphics.FromImage(returnBitmap);
            g.DrawImage(b, new Point(0, 0));
            returnBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);*/
            return returnBitmap;
        }

        static public Bitmap RotateImage(Bitmap b, float angle)
        {
            //create a new empty bitmap to hold rotated image
            Bitmap returnBitmap = new Bitmap(b.Width, b.Height);
            b.SetResolution(b.HorizontalResolution, b.VerticalResolution);

            //make a graphics object from the empty bitmap
            Graphics g = Graphics.FromImage(returnBitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            //move rotation point to center of image
            g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
            //rotate
            g.RotateTransform(angle);
            //move image back
            g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
            //draw passed in image onto graphics object
            g.DrawImage(b, new Point(0, 0));
            return returnBitmap;
        }

        /// <summary>
        /// Method for merge images.
        /// </summary>
        /// <param name="imgBase">the image to background</param>
        /// <param name="imgToMerge">the image to merge</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static public Bitmap MergeImage(Bitmap imgBase, Bitmap imgToMerge, int x, int y)
        {
            return MergeImage(imgBase, imgToMerge, new Point(x, y));
        }

        /// <summary>
        /// Method for merge images.
        /// </summary>
        /// <param name="imgBase">the image to background</param>
        /// <param name="imgToMerge">the image to merge</param>
        /// <param name="point"></param>
        /// <returns></returns>
        static public Bitmap MergeImage(Bitmap imgBase, Bitmap imgToMerge, Point point)
        {

            /*
            Bitmap b = (Bitmap)imgBase.Clone();
            Graphics g = Graphics.FromImage((Image)b);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;*/

            Bitmap b = new Bitmap(imgBase.Width, imgBase.Height);
            b.SetResolution(imgBase.HorizontalResolution, imgBase.VerticalResolution);

            Graphics g = Graphics.FromImage((Image)b);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            g.DrawImage(imgBase, 0, 0, imgBase.Width, imgBase.Height);
            g.DrawImage(imgToMerge, point.X, point.Y, imgToMerge.Width, imgToMerge.Height);

            g.Save();

            g.Dispose();

            return b;

        }

        /// <summary>
        /// method for cropping an image.
        /// </summary>
        /// <param name="img">the image to crop</param>
        /// <param name="width">new height</param>
        /// <param name="height">new width</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static public Bitmap Crop(Bitmap imgToCrop, int width, int height, int x, int y)
        {


            Bitmap b = new Bitmap(width, height);
            b.SetResolution(imgToCrop.HorizontalResolution, imgToCrop.VerticalResolution);

            Graphics g = Graphics.FromImage((Image)b);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            
            g.DrawImage(imgToCrop, new Rectangle(-1, -1, width + 1, height + 1), x, y, width, height, GraphicsUnit.Pixel);

            g.Dispose();

            return b;

            /*
            try
            {
                Image image = (Image)imgToCrop;
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                bmp.SetResolution(80, 60);

                Graphics gfx = Graphics.FromImage(bmp);
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gfx.DrawImage(image, new Rectangle(0, 0, width, height), x, y, width, height, GraphicsUnit.Pixel);
                // Dispose to free up resources
                image.Dispose();
                bmp.Dispose();
                gfx.Dispose();

                return bmp;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }*/
        }

        /// <summary>
        /// Resize image
        /// </summary>
        /// <param name="imgToResize"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap resizeImage(Bitmap imgToResize, SizeF size)
        {
            return resizeImage(imgToResize, size, true);
        }

        /// <summary>
        /// Resize image
        /// </summary>
        /// <param name="imgToResize"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap resizeImage(Bitmap imgToResize, Size size)
        {
            return resizeImage(imgToResize, new SizeF((float)size.Width, (float)size.Height), true);
        }

        /// <summary>
        /// Resize image
        /// </summary>
        /// <param name="imgToResize"></param>
        /// <param name="size"></param>
        /// <param name="maintainProportions"></param>
        /// <returns></returns>
        public static Bitmap resizeImage(Bitmap imgToResize, SizeF size, Boolean maintainProportions)
        {
            float sourceWidth = imgToResize.Width;
            float sourceHeight = imgToResize.Height;

            float destWidth = size.Width;
            float destHeight = size.Height;

            if (maintainProportions)
            {
                float nPercent = 0;
                float nPercentW = 0;
                float nPercentH = 0;

                nPercentW = ((float)size.Width / (float)sourceWidth);
                nPercentH = ((float)size.Height / (float)sourceHeight);

                if (nPercentH > nPercentW)
                    nPercent = nPercentH;
                else
                    nPercent = nPercentW;

                float calcWidth = (int)(sourceWidth * nPercent);
                float calcHeight = (int)(sourceHeight * nPercent);

                if (calcWidth > size.Width)
                {
                    destWidth = (Int32)size.Width;
                    destHeight = (Int32)(sourceHeight * nPercentH);
                }
                else if (calcHeight > size.Height)
                {
                    destWidth = (Int32)(sourceWidth * nPercentW);
                    destHeight = (Int32)size.Height;
                }
                else
                {
                    if (nPercentH < nPercentW)
                        nPercent = nPercentH;
                    else
                        nPercent = nPercentW;

                    destWidth = (int)(sourceWidth * nPercent);
                    destHeight = (int)(sourceHeight * nPercent);
                }
                
            }

            Bitmap b = new Bitmap((Int32)destWidth, (Int32)destHeight);
            b.SetResolution(imgToResize.HorizontalResolution, imgToResize.VerticalResolution);

            Graphics g = Graphics.FromImage((Image)b);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return b;
        }

        /// <summary>
        /// Prepare image to print
        /// </summary>
        /// <param name="imgToPrint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap resizeCropImage(Bitmap imgToPrint, Size destSize)
        {
            Size sourceSize = imgToPrint.Size;

            //Muda a orientação se necessário
            if (sourceSize.Height > sourceSize.Width)
                destSize = new Size(destSize.Height, destSize.Width);

            //Calcula a área a imagem de origem que será copiada
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)destSize.Width / (float)sourceSize.Width);
            nPercentH = ((float)destSize.Height / (float)sourceSize.Height);

            if (nPercentH > nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;


            Size tmpNewSize = new Size((int)(sourceSize.Width * nPercent), (int)(sourceSize.Height * nPercent));
            Point posNewPoint = new Point(0, 0);
            //Verifica se necessita fazer crop
            if ((tmpNewSize.Width > destSize.Width) || (tmpNewSize.Height > destSize.Height))
            {

                //Calcula a posição do crop
                if (tmpNewSize.Width > destSize.Width)
                {
                    posNewPoint.X = (Int32)(((float)tmpNewSize.Width - (float)destSize.Width) / 2);
                }

                if (tmpNewSize.Height > destSize.Height)
                {
                    posNewPoint.Y = (Int32)(((float)tmpNewSize.Height - (float)destSize.Height) / 2);
                }

            }


            //Calcula a área de corte com base no tamanho original da imagem
            RectangleF cropRect = new Rectangle();
            cropRect.X = ((float)posNewPoint.X / nPercent);
            if (cropRect.X < 0)
                cropRect.X = 0;

            cropRect.Y = (float)posNewPoint.Y / nPercent;
            if (cropRect.Y < 0)
                cropRect.Y = 0;

            cropRect.Width = (float)destSize.Width / nPercent;
            cropRect.Height = (float)destSize.Height / nPercent;


            //Cria a imagem base com fundo branco
            Bitmap baseBmp = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format24bppRgb);
            baseBmp.SetResolution(imgToPrint.HorizontalResolution, imgToPrint.VerticalResolution);

            //Cria o objeto de desenho e fundo branco
            Graphics gfx = Graphics.FromImage(baseBmp);
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            gfx.FillRectangle(new SolidBrush(Color.White), 0, 0, destSize.Width, destSize.Height);

            gfx.DrawImage(imgToPrint, new Rectangle(-1, -1, destSize.Width + 1, destSize.Height + 1), cropRect.X, cropRect.Y, cropRect.Width, cropRect.Height, GraphicsUnit.Pixel);

            gfx.Dispose();
            gfx = null;

            return baseBmp;
        }

        /// <summary>
        /// Prepare image to print
        /// </summary>
        /// <param name="imgToPrint"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Bitmap setPrintSize(Bitmap imgToPrint, PrintSize printSize)
        {
            Size sourceSize = imgToPrint.Size;
            Size destSize = new Size(0, 0);
            switch (printSize)
            {
                case PrintSize.cm10x15:
                    destSize = new Size(1772, 1181);
                    break;

                case PrintSize.cm13x18:
                    destSize = new Size(2126, 1535);
                    break;

                case PrintSize.cm15x21:
                    destSize = new Size(2480, 1772);
                    break;

                case PrintSize.cm20x25:
                    destSize = new Size(2953, 2362);
                    break;

                case PrintSize.cm20x30:
                    destSize = new Size(3543, 2362);
                    break;

            }

            //Muda a orientação se necessário
            if (sourceSize.Height > sourceSize.Width)
                destSize = new Size(destSize.Height, destSize.Width);

            //Calcula a área a imagem de origem que será copiada
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)destSize.Width / (float)sourceSize.Width);
            nPercentH = ((float)destSize.Height / (float)sourceSize.Height);

            if (nPercentH > nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            
            Size tmpNewSize = new Size((int)(sourceSize.Width * nPercent), (int)(sourceSize.Height * nPercent));
            Point posNewPoint = new Point(0, 0);
            //Verifica se necessita fazer crop
            if ((tmpNewSize.Width > destSize.Width) || (tmpNewSize.Height > destSize.Height))
            {

                //Calcula a posição do crop
                if (tmpNewSize.Width > destSize.Width)
                {
                    posNewPoint.X = (Int32)(((float)tmpNewSize.Width - (float)destSize.Width) / 2);
                }

                if (tmpNewSize.Height > destSize.Height)
                {
                    posNewPoint.Y = (Int32)(((float)tmpNewSize.Height - (float)destSize.Height) / 2);
                }

            }


            //Calcula a área de corte com base no tamanho original da imagem
            RectangleF cropRect = new Rectangle();
            cropRect.X = ((float)posNewPoint.X / nPercent);
            if (cropRect.X < 0)
                cropRect.X = 0;

            cropRect.Y = (float)posNewPoint.Y / nPercent;
            if (cropRect.Y < 0)
                cropRect.Y = 0;

            cropRect.Width = (float)destSize.Width / nPercent;
            cropRect.Height = (float)destSize.Height / nPercent;


            //Cria a imagem base com fundo branco
            Bitmap baseBmp = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format24bppRgb);
            baseBmp.SetResolution(300, 300);

            //Cria o objeto de desenho e fundo branco
            Graphics gfx = Graphics.FromImage(baseBmp);
            gfx.SmoothingMode = SmoothingMode.AntiAlias;
            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;

            gfx.FillRectangle(new SolidBrush(Color.White), 0, 0, destSize.Width, destSize.Height);

            gfx.DrawImage(imgToPrint, new Rectangle(0, 0, destSize.Width, destSize.Height), cropRect.X, cropRect.Y, cropRect.Width, cropRect.Height, GraphicsUnit.Pixel);

            gfx.Dispose();
            gfx = null;

            return baseBmp;
        }
    }
}