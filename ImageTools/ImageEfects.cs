using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

namespace ImageTools
{
    public class ImageEfects
    {
        static public Bitmap RoundCorners(Bitmap StartImage, int CornerRadius, Color BackgroundColor)
        {
            CornerRadius *= 2;
            Bitmap RoundedImage = new Bitmap(StartImage.Width, StartImage.Height);
            Graphics g = Graphics.FromImage(RoundedImage);
            g.Clear(BackgroundColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Brush brush = new TextureBrush(StartImage);
            GraphicsPath gp = new GraphicsPath();

            gp.AddArc(0, 0, CornerRadius, CornerRadius, 180, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0, CornerRadius, CornerRadius, 270, 90);
            gp.AddArc(0 + RoundedImage.Width - CornerRadius, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 0, 90);
            gp.AddArc(0, 0 + RoundedImage.Height - CornerRadius, CornerRadius, CornerRadius, 90, 90);
            g.FillPath(brush, gp);
            return RoundedImage;
        }

        static public Bitmap BarrelDistortion(Bitmap StartImage, double factor)
        {
            return BarrelDistortion(StartImage, factor, true, Color.Transparent);
        }

        static public Bitmap BarrelDistortion(Bitmap sourceImage, double factor, Boolean autoCrop, Color backgroundColor)
        {
            Bitmap StartImage = null;
            BitmapData srcBitmapData = null;
            Byte[] srcPixels = null;
            Byte[] dstPixels = null;
            Bitmap NewImage = null;
            BitmapData dstBitmapData = null;

            try
            {

                // Verifica se bpp (Bits Per Pixel) é 8, 24, ou 32
                int Depth = System.Drawing.Bitmap.GetPixelFormatSize(sourceImage.PixelFormat);
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Recupera a contagem dos componentes de cor
                int cCount = Depth / 8;

                Size baseSize = new Size(sourceImage.Width, sourceImage.Height);

                //verifica se é uma imagem de baixa e precisa redimencionar para melhorar a qualidade
                //e não gerar serrilhamento da imagem
                Int32 maxSize = Math.Max(sourceImage.Width, sourceImage.Height);
                if (maxSize < 3000)
                {
                    float percent = 3000F / (float)maxSize;
                    baseSize = new Size((Int32)((float)sourceImage.Width * percent), (Int32)((float)sourceImage.Height * percent));
                }

                StartImage = new Bitmap(baseSize.Width, baseSize.Height, sourceImage.PixelFormat);
                StartImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
                
                //Cria o objeto de desenho e fundo branco
                Graphics g = Graphics.FromImage(StartImage);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(sourceImage, new Rectangle(-1, -1, baseSize.Width + 1, baseSize.Height + 1), 0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel);
                g.Dispose();


                // Bloqueia a imagem de origem e copia para o array de bytes e libera a imagem de origem
                srcBitmapData = StartImage.LockBits(new Rectangle(0, 0, StartImage.Width, StartImage.Height), ImageLockMode.ReadOnly, StartImage.PixelFormat);
                srcPixels = new byte[StartImage.Width * StartImage.Height * (Depth / 8)];
                Marshal.Copy(srcBitmapData.Scan0, srcPixels, 0, srcPixels.Length);
                StartImage.UnlockBits(srcBitmapData);
                srcBitmapData = null;

                //Cria o array de bytes da imagem de destino
                dstPixels = new Byte[srcPixels.Length];

                //Preenche todo o quadro com a cor de fundo selecionada
                Int32 index = ((1 * StartImage.Width) + 1) * cCount; //index = ((Y * Width) + X) * cCount
                do
                {
                    if (Depth == 32) // Para 32 bpp define Red, Green, Blue e Alpha
                    {
                        dstPixels[index++] = backgroundColor.B;
                        dstPixels[index++] = backgroundColor.G;
                        dstPixels[index++] = backgroundColor.R;
                        dstPixels[index++] = backgroundColor.A; // a
                    }
                    if (Depth == 24) // Para 24 bpp define Red, Green e Blue
                    {
                        dstPixels[index++] = backgroundColor.B;
                        dstPixels[index++] = backgroundColor.G;
                        dstPixels[index++] = backgroundColor.R;
                    }
                    if (Depth == 8)
                    // Para 8 bpp define o valor da cor (Red, Green and Blue como sendo a mesma coisa)
                    {
                        dstPixels[index++] = backgroundColor.B;
                    }

                } while (index < srcPixels.Length);


                //Calcula a amplitude máxima possível para a imagem e multiplica pelo fator desejados
                double amp = 0;
                double ang = Math.PI * 0.5;
                for (Int32 a = 0; a < StartImage.Height; a++)
                {
                    int y = (int)((StartImage.Height / 2) - amp * Math.Sin(ang));
                    if ((y < 0) || (y > StartImage.Height))
                        break;
                    amp = a;
                }
                amp = (amp - 2) * (factor < -1 ? -1 : (factor > 1 ? 1 : factor));


                //Define variáveis que calcula os pontos de corte (se houver)
                Int32 x1, y1, x2, y2;
                x1 = StartImage.Width;
                y1 = StartImage.Height;
                x2 = 0;
                y2 = 0;

                //Copia pixel a pixel para as novas posições
                index = ((1 * StartImage.Width) + 1) * cCount;
                do
                {

                    Int32 y = (Int32)((index / cCount) / StartImage.Width);
                    Int32 x = (index / cCount) - (y * StartImage.Width);

                    Point pt = NewPoint(new Point(x, y), StartImage.Width, StartImage.Height, amp, factor < 0);

                    //Valores para crop
                    if (factor >= 0)
                    {
                        if (x == StartImage.Width / 2)
                        {
                            if (pt.Y < y1)
                                y1 = pt.Y;

                            if (pt.Y > y2)
                                y2 = pt.Y;
                        }

                        if (y == StartImage.Height / 2)
                        {
                            if (pt.X < x1)
                                x1 = pt.X;

                            if (pt.X > x2)
                                x2 = pt.X;
                        }
                    }
                    else
                    {
                        if ((x == 1) && (y == 1))
                        {
                            y1 = pt.Y;
                            x1 = pt.X;
                        }

                        if ((x == StartImage.Width - 1) && (y == StartImage.Height - 1))
                        {
                            y2 = pt.Y;
                            x2 = pt.X;
                        }
                    }
                
                    //Indice de bytes onde será aplicada o pixel
                    Int32 dstIndex = ((pt.Y * StartImage.Width) + pt.X) * cCount;

                    if (Depth == 32)
                    {
                        dstPixels[dstIndex] = srcPixels[index++];
                        dstPixels[dstIndex + 1] = srcPixels[index++];
                        dstPixels[dstIndex + 2] = srcPixels[index++];
                        dstPixels[dstIndex + 3] = srcPixels[index++]; // a
                    }
                    if (Depth == 24)
                    {
                        dstPixels[dstIndex] = srcPixels[index++];
                        dstPixels[dstIndex + 1] = srcPixels[index++];
                        dstPixels[dstIndex + 2] = srcPixels[index++];
                    }
                    if (Depth == 8)
                    {
                        dstPixels[dstIndex] = srcPixels[index++];
                    }

                } while (index < srcPixels.Length);

                //Cria a nova imagem com base no array de bytes previamente criado
                NewImage = new Bitmap(StartImage.Width, StartImage.Height, StartImage.PixelFormat);
                NewImage.SetResolution(StartImage.HorizontalResolution, StartImage.VerticalResolution);
                dstBitmapData = NewImage.LockBits(new Rectangle(0, 0, StartImage.Width, StartImage.Height), ImageLockMode.WriteOnly, StartImage.PixelFormat);
                Marshal.Copy(dstPixels, 0, dstBitmapData.Scan0, dstPixels.Length);
                NewImage.UnlockBits(dstBitmapData);

                /*
                // Para efeito de visualização, desenha o quadrado onde será realizado o corte
                Graphics g2 = Graphics.FromImage(NewImage);
                g2.SmoothingMode = SmoothingMode.AntiAlias;
                g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g2.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g2.DrawRectangle(new Pen(new SolidBrush(Color.Red), 3), new Rectangle(x1, y1, x2 - x1, y2 - y1));
                g2.Dispose();*/

                //Gera a imagem final, com crop ou coo o redimencionamento real
                Bitmap FinalImage = new Bitmap(sourceImage.Width, sourceImage.Height, StartImage.PixelFormat);
                NewImage.SetResolution(StartImage.HorizontalResolution, StartImage.VerticalResolution);

                Graphics g1 = Graphics.FromImage(FinalImage);
                g1.SmoothingMode = SmoothingMode.AntiAlias;
                g1.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g1.PixelOffsetMode = PixelOffsetMode.HighQuality;

                //Realiza o corte se estiver habilitado o corte automático e houver necessidade de corte
                if ((autoCrop) && ((x1 > 0) || (y1 > 0) || (x2 < NewImage.Height) || (y2 < NewImage.Height)))
                {
                    Rectangle cropRect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                    g1.DrawImage(NewImage, new Rectangle(-1, -1, FinalImage.Width + 1, FinalImage.Height + 1), cropRect.X, cropRect.Y, cropRect.Width, cropRect.Height, GraphicsUnit.Pixel);
                }
                else
                {
                    g1.DrawImage(NewImage, new Rectangle(-1, -1, FinalImage.Width + 1, FinalImage.Height + 1), 0, 0, NewImage.Width, NewImage.Height, GraphicsUnit.Pixel);
                }

                g1.Dispose();
                g1 = null;

                NewImage = null;
                return FinalImage;
            }
            finally
            {
                srcBitmapData = null;
                srcPixels = null;
                dstPixels = null;
                dstBitmapData = null;
            }

        }

        private static Point NewPoint(Point AtualPoint, Int32 Width, Int32 Height, double Aplitude, Boolean inverse)
        {
            Point uP = AtualPoint;

            int pY, pX;
            double aY, aX;

            aY = aX = 0;

            double angX = Math.PI * 1 * (double)uP.X / (double)Width;
            double caX = Aplitude * ((((double)Height / 2F) - (double)uP.Y) / ((double)Height / 2F));

            double angY = Math.PI * 1 * (double)uP.Y / (double)Height;
            double caY = Aplitude * ((((double)Width / 2F) - (double)uP.X) / ((double)Width / 2F));

            if (inverse)
            {
                double iAng = Math.PI * -1 * 0.5;
                aX = (caX * Math.Sin(iAng));
                aY = (caY * Math.Sin(iAng));
            }

            pY = (int)(uP.Y + aX + caX * Math.Sin(angX));
            pX = (int)(uP.X + aY + caY * Math.Sin(angY));

            return new Point(pX, pY);

        }

    }
}
