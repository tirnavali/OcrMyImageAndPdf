using OcrMyImage.Application.HocrElements;
using OcrMyImage.Application.ImageProcessors;
using System.Drawing;
using System.Reflection;
using Tesseract;

namespace OcrMyImage.Application
{
    internal class OcrController
    {
        internal void AddToDocument(string language, Image image, ref HDocument doc, string sessionName)
        {
            Bitmap b = ImageProcessor.GetAsBitmap(image, (int)Math.Ceiling(image.HorizontalResolution));
            string imageFile = TempData.Instance.CreateTempFile(sessionName, ".tif");
            b.Save(imageFile, System.Drawing.Imaging.ImageFormat.Tiff);
            string result = CreateHocr(language, imageFile, sessionName);
            doc.AddFile(result);
            b.Dispose();
            //image.Dispose();
        }

        public string CreateHocr(string language, string imagePath, string sessionName)
        {
            string dataFolder = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            string dataPath = TempData.Instance.CreateDirectory(sessionName, dataFolder);
            string outputFile = Path.Combine(dataPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            string enginePath = string.Empty;
            try
            {
                //var _enginePath = Path.Combine(Environment.CurrentDirectory, "Application", "tessdata");
                //var den = Path.Combine(Assembly.GetCallingAssembly().Location, "tessdata");
                enginePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "Application", "tessdata");
                //enginePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "tessdata");
            }
            catch (Exception e)
            {
                enginePath = Path.Combine(Environment.CurrentDirectory, "tessdata");
            }


            using (TesseractEngine engine = new TesseractEngine(enginePath, "tur"))
            {
                using (Pix img = Pix.LoadFromFile(imagePath))
                {


                    Orientation baseOrientation = Orientation.PageUp;
                    var blackAndWhiteImg = img.ConvertRGBToGray();

                    try
                    {
                        blackAndWhiteImg = blackAndWhiteImg.BinarizeSauvolaTiled(img.Width / 2, 0.17F, 140, 150);
                    }
                    catch
                    {
                        blackAndWhiteImg = blackAndWhiteImg.BinarizeOtsuAdaptiveThreshold(440, 192, 60, 60, 0.005F);
                    }
                    img.Dispose();

                    //var blackAndWhiteImg = img.ConvertRGBToGray().BinarizeOtsuAdaptiveThreshold(440, 192, 60, 60, 0.005F);
                    //blackAndWhiteImg = ScaleImage(blackAndWhiteImg);

                    //Detect orientation
                    if (true)
                    {
                        using (var page = engine.Process(blackAndWhiteImg, Tesseract.PageSegMode.AutoOsd))
                        {
                            using (var pageIter = page.AnalyseLayout())
                            {
                                var pageProps = pageIter.GetProperties();
                                var orientation = pageProps.Orientation;

                                if (Orientation.PageRight == orientation)
                                {
                                    baseOrientation = Orientation.PageRight;
                                    blackAndWhiteImg = page.Image.Rotate90(-1);
                                    blackAndWhiteImg = blackAndWhiteImg.Rotate(pageIter.GetProperties().DeskewAngle);
                                }
                                else if (Orientation.PageLeft == orientation)
                                {
                                    baseOrientation = Orientation.PageLeft;
                                    blackAndWhiteImg = page.Image.Rotate90(1);
                                    blackAndWhiteImg = blackAndWhiteImg.Rotate(pageIter.GetProperties().DeskewAngle);
                                }
                            }

                        }
                    }

                    using (var page = engine.Process(blackAndWhiteImg, Tesseract.PageSegMode.AutoOsd))
                    {
                        string hocrtext = page.GetHOCRText(0);
                        //Console.WriteLine(hocrtext);
                        File.WriteAllText(outputFile + ".hocr", hocrtext);
                        Console.WriteLine($"{imagePath} processed.");
                    }
                    blackAndWhiteImg.Dispose();
                }
            }
            return outputFile + ".hocr";
        }

        private Pix ScaleImage(Pix loadedImage)
        {
            using (var im = loadedImage.Deskew())
            {
                if (im.Width <= 2000)
                    return im.Scale(2.0F, 2.0F);
                else if (im.Width <= 3000 && im.Width > 2000)
                    return im.Scale(2.0F, 2.0F);
                else if (im.Width <= 4500 && im.Width > 3000)
                    return im.Scale(2.0F, 2.0F);
                else
                    return im.Scale(1.5F, 1.5F);
            }
        }
    }
}
