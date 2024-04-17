using OcrMyImage.Application.Enums;
using OcrMyImage.Application.Pdf;

namespace OcrMyImage.Application
{
    public class OcrGate
    {
        private static OcrGate _ocrGate;
        private static object lockObject = new object();
        private static PdfCompressor _comp;
        private List<string> distillerOptions;
        private PdfCompressorSettings pdfSettings;
        private PdfMeta pdfMeta;
        private OcrGate()
        {
            distillerOptions = new List<string>
            {
                "-dSubsetFonts=true",
                "-dFastWebView=false",
                "-dCompressFonts=true",
                "-sProcessColorModel=DeviceRGB",
                "-sColorConversionStrategy=sRGB",
                "-sColorConversionStrategyForImages=sRGB",
                "-dConvertCMYKImagesToRGB=true",
                "-dDetectDuplicateImages=true",
                "-dDownsampleColorImages=true",
                "-dDownsampleGrayImages=true",
                "-dDownsampleMonoImages=true",
                "-dColorImageResolution=265",
                "-dGrayImageResolution=265",
                "-dMonoImageResolution=265",
                "-dDoThumbnails=false",
                "-dCreateJobTicket=false",
                "-dPreserveEPSInfo=false",
                "-dPreserveOPIComments=false",
                "-dPreserveOverprintSettings=false",
                "-dUCRandBGInfo=/Remove"
            };
            pdfSettings = new PdfCompressorSettings
            {
                PdfCompatibilityLevel = PdfCompatibilityLevel.Acrobat_7_1_6,
                WriteTextMode = WriteTextMode.Word,
                Dpi = 250,
                ImageType = PdfImageType.Tif,
                ImageQuality = 60,
                CompressFinalPdf = true,
                DistillerMode = dPdfSettings.screen,
                DistillerOptions = string.Join(" ", distillerOptions.ToArray())
            };

            pdfMeta = new PdfMeta
            {
                Author = "TBMM Kütüphane ve Arşiv Hizmetleri",
                KeyWords = string.Empty,
                Subject = string.Empty,
                Title = string.Empty
            };

        }

        public static OcrGate Instance()
        {
            if (_ocrGate == null)
            {
                lock (lockObject)
                {
                    if (_ocrGate == null)
                    {
                        _ocrGate = new OcrGate();
                    }
                }
            }
            return _ocrGate;
        }

        public Tuple<byte[], string[]> ProccessPDF(byte[] pdf)
        {
            _comp = new PdfCompressor(@"gswin64c.exe", pdfSettings);
            Tuple<byte[], string[]>? processedData = _comp.CreateSearchablePdf(pdf, pdfMeta);
            return processedData;
        }


        public Tuple<byte[], string[]> ProccessImages(IList<byte[]> images)
        {
            _comp = new PdfCompressor(@"gswin64c.exe", pdfSettings);
            try
            {
                Tuple<byte[], string[]>? processedData = _comp.CreateSearchablePdfFromImages(images.ToList(), pdfMeta);
                return processedData;
            }
            catch (OutOfMemoryException ex)
            {
                throw new OutOfMemoryException(ex.Message);
            }
        }




    }
}
