using OcrMyImage.Application;
using OcrMyImage.Application.Enums;
using OcrMyImage.Application.Exceptions;
using OcrMyImage.Application.ImageProcessors;
using System.Drawing;
using Rectangle = iTextSharp.text.Rectangle;

namespace OcrMyImage.Application.Pdf
{
    public delegate void CompressorExceptionOccurred(PdfCompressor c, Exception x);

    public delegate void CompressorEvent(string msg);

    public delegate string PreProcessImage(string bitmapPath);

    public class PdfCompressor
    {
        private string GhostScriptPath { get; set; } = string.Empty;
        public PdfCompressor(string ghostScriptPath, PdfCompressorSettings settings = null)
        {
            PdfSettings = settings ?? new PdfCompressorSettings();
            GhostScriptPath = ghostScriptPath;
        }

        public PdfCompressorSettings PdfSettings { get; }

        private string[] CompressAndOcr(string sessionName, string inputFileName, string outputFileName, PdfMeta meta)
        {

            string pageBody = "";

            OnCompressorEvent?.Invoke(sessionName + " Creating PDF Reader");
            PdfReader reader = new PdfReader(inputFileName, PdfSettings.Dpi, GhostScriptPath);

            var pgSize = reader.PageCount;
            List<string> pageBodyList = new List<string>(pgSize);

            OnCompressorEvent?.Invoke(sessionName + " Creating PDF Writer");
            PdfCreator writer =
                new PdfCreator(PdfSettings, outputFileName, meta, PdfSettings.Dpi) { PdfSettings = { WriteTextMode = PdfSettings.WriteTextMode } };

            try
            {
                for (int i = 1; i <= pgSize; i++)
                {
                    OnCompressorEvent?.Invoke(sessionName + " Processing page " + i + " of " + reader.PageCount);
                    string img = reader.GetPageImage(i, sessionName, this);
                    if (OnPreProcessImage != null) img = OnPreProcessImage(img);
                    Bitmap chk = new Bitmap(img);
                    Rectangle pageSize = new Rectangle(0, 0, chk.Width, chk.Height);
                    chk.Dispose();
                    var pageString = writer.AddPage(img, PdfMode.Ocr, sessionName, pageSize);
                    pageBodyList.Add(pageString);
                    pageBody = pageBody + pageString;
                }

                writer.SaveAndClose();
                writer.Dispose();
                reader.Dispose();
                return pageBodyList.ToArray();
            }
            catch (Exception x)
            {
                OnCompressorEvent?.Invoke(sessionName + " Image not supported in " + Path.GetFileName(inputFileName) + ". Skipping");
                writer.SaveAndClose();
                writer.Dispose();
                reader.Dispose();
                OnExceptionOccurred?.Invoke(this, x);
                throw;
            }
        }

        private string[] CompressAndOcrFromImages(string sessionName, List<byte[]> inputFiles, string outputFileName, PdfMeta meta)
        {

            List<string> pageBodyList = new List<string>(inputFiles.Count);

            OnCompressorEvent?.Invoke(sessionName + " Creating PDF Writer");
            PdfCreator writer =
                new PdfCreator(PdfSettings, outputFileName, meta, PdfSettings.Dpi) { PdfSettings = { WriteTextMode = PdfSettings.WriteTextMode } };

            try
            {
                for (int i = 1; i <= inputFiles.Count; i++)
                {
                    byte[] image = inputFiles.ElementAt(i - 1);
                    //ImageProcessor.GetAsBitmapFromArray(image, PdfSettings.Dpi);

                    Bitmap chk = ImageProcessor.GetAsBitmapFromArray(image, PdfSettings.Dpi);
                    if (chk.Width > 3000)
                    {
                        image = ImageProcessor.ScaleImage(image);
                        chk = ImageProcessor.GetAsBitmapFromArray(image, PdfSettings.Dpi);
                    }
                    Rectangle pageSize = new Rectangle(0, 0, chk.Width, chk.Height);
                    chk.Dispose();
                    string outputDataFilePath = TempData.Instance.CreateTempFile(sessionName, ".bmp");
                    File.WriteAllBytes(outputDataFilePath, image);
                    var pageString = writer.AddPage(outputDataFilePath, PdfMode.Ocr, sessionName, pageSize);
                    pageBodyList.Add(pageString);
                }

                writer.SaveAndClose();
                writer.Dispose();
                return pageBodyList.ToArray();
            }
            catch (IOException x)
            {
                Console.WriteLine("Hocr file used by another proccess!. " + x.Message);
                throw new IOException("Hocr file used by another proccess!.", x);
            }
            catch (OutOfMemoryException e)
            {
                Console.WriteLine("Not enough memory in system!. " + e.Message);
                throw new OutOfMemoryException("Not enough memory in system.");
            }
            catch (Exception x)
            {
                OnCompressorEvent?.Invoke(sessionName + " Image not supported in " + ". Skipping");
                OnExceptionOccurred?.Invoke(this, x);
                Console.WriteLine("Image not supported in!. " + sessionName + " - " + x.Message);
                throw;
            }
            finally
            {
                writer.Dispose();
                writer.SaveAndClose();
            }
        }

        public event CompressorExceptionOccurred OnExceptionOccurred;
        public event CompressorEvent OnCompressorEvent;
        public event PreProcessImage OnPreProcessImage;

        private int GetPages(byte[] data)
        {
            try
            {
                using (iTextSharp.text.pdf.PdfReader pdfReader = new iTextSharp.text.pdf.PdfReader(data))
                    return pdfReader.NumberOfPages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }
        public Tuple<byte[], string[]> CreateSearchablePdfFromImages(List<byte[]> images, PdfMeta metaData)
        {
            string sessionName = "";
            try
            {
                sessionName = TempData.Instance.CreateNewSession();
                int PageCountStart = images.Count;
                OnCompressorEvent?.Invoke("Created Session:" + sessionName);
                //string inputDataFilePath = TempData.Instance.CreateTempFile(sessionName, ".pdf");
                string outputDataFilePath = TempData.Instance.CreateTempFile(sessionName, ".pdf");
                if (images == null || images.Count == 0)
                    throw new Exception("No data in images");
                //using (FileStream writer = new FileStream(inputDataFilePath, FileMode.Create, FileAccess.Write))
                //{
                //    writer.Write(fileData, 0, fileData.Length);
                //    writer.Flush(true);
                //}

                OnCompressorEvent?.Invoke(sessionName + " Wrote binary to file");
                OnCompressorEvent?.Invoke(sessionName + " Begin Compress and Ocr");
                //string pageBody = CompressAndOcrFromImages(sessionName, images, outputDataFilePath, metaData);
                string[] pageBodyList = CompressAndOcrFromImages(sessionName, images, outputDataFilePath, metaData);
                string outputFileName = outputDataFilePath;
                if (PdfSettings.CompressFinalPdf)
                {
                    OnCompressorEvent?.Invoke(sessionName + " Compressing output");
                    GhostScript gs = new GhostScript(GhostScriptPath, PdfSettings.Dpi);
                    outputFileName = gs.CompressPdf(outputDataFilePath, sessionName, PdfSettings.PdfCompatibilityLevel, PdfSettings.DistillerMode,
                        PdfSettings.DistillerOptions);
                }
                int PageCountEnd = images.Count;
                byte[] outFile = File.ReadAllBytes(outputFileName);
                OnCompressorEvent?.Invoke(sessionName + " Destroying session");
                TempData.Instance.DestroySession(sessionName);
                if (PageCountEnd != PageCountStart)
                {
                    throw new PageCountMismatchException("Page count is different", PageCountStart, PageCountEnd);

                }
                return new Tuple<byte[], string[]>(outFile, pageBodyList);

            }
            catch (OutOfMemoryException e)
            {
                throw;
            }
            catch (Exception e)
            {
                OnExceptionOccurred?.Invoke(this, e);
                throw new FailedToGenerateException("Error in: CreateSearchablePdfFromImages", e);
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionName))
                {
                    TempData.Instance.DestroySession(sessionName);
                }
            }
        }
        public Tuple<byte[], string[]> CreateSearchablePdf(byte[] fileData, PdfMeta metaData)
        {
            string sessionName = "";
            try
            {
                int PageCountStart = GetPages(fileData);
                sessionName = TempData.Instance.CreateNewSession();
                OnCompressorEvent?.Invoke("Created Session:" + sessionName);
                string inputDataFilePath = TempData.Instance.CreateTempFile(sessionName, ".pdf");
                string outputDataFilePath = TempData.Instance.CreateTempFile(sessionName, ".pdf");
                if (fileData == null || fileData.Length == 0)
                    throw new Exception("No Data in fileData");
                using (FileStream writer = new FileStream(inputDataFilePath, FileMode.Create, FileAccess.Write))
                {
                    writer.Write(fileData, 0, fileData.Length);
                    writer.Flush(true);
                }

                OnCompressorEvent?.Invoke(sessionName + " Wrote binary to file");
                OnCompressorEvent?.Invoke(sessionName + " Begin Compress and Ocr");
                //string pageBody = CompressAndOcr(sessionName, inputDataFilePath, outputDataFilePath, metaData);
                string[] pageBodyList = CompressAndOcr(sessionName, inputDataFilePath, outputDataFilePath, metaData);
                string outputFileName = outputDataFilePath;
                if (PdfSettings.CompressFinalPdf)
                {
                    OnCompressorEvent?.Invoke(sessionName + " Compressing output");
                    GhostScript gs = new GhostScript(GhostScriptPath, PdfSettings.Dpi);
                    outputFileName = gs.CompressPdf(outputDataFilePath, sessionName, PdfSettings.PdfCompatibilityLevel, PdfSettings.DistillerMode,
                        PdfSettings.DistillerOptions);
                }

                byte[] outFile = File.ReadAllBytes(outputFileName);
                int PageCountEnd = GetPages(outFile);
                OnCompressorEvent?.Invoke(sessionName + " Destroying session");
                TempData.Instance.DestroySession(sessionName);

                if (PageCountEnd != PageCountStart)
                    throw new PageCountMismatchException("Page count is different", PageCountStart, PageCountEnd);

                return new Tuple<byte[], string[]>(outFile, pageBodyList);
            }
            catch (Exception e)
            {
                OnExceptionOccurred?.Invoke(this, e);
                throw new FailedToGenerateException("Error in: CreateSearchablePdf", e);
            }
            finally
            {
                TempData.Instance.DestroySession(sessionName);
            }
        }
    }
}
