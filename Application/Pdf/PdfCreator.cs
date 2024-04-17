using iTextSharp.text;
using iTextSharp.text.pdf;
using OcrMyImage.Application;
using OcrMyImage.Application.Enums;
using OcrMyImage.Application.HocrElements;
using OcrMyImage.Application.ImageProcessors;
using System.Drawing;
using System.Drawing.Imaging;
using Rectangle = iTextSharp.text.Rectangle;

namespace OcrMyImage.Application.Pdf
{
    public delegate System.Drawing.Image ProcessImageForDisplay(System.Drawing.Image image);

    public delegate System.Drawing.Image ProcessImageForOcr(System.Drawing.Image image);
    internal class PdfCreator : IDisposable
    {
        private readonly float _dpi;
        private readonly OcrController _ocrController;
        private Document _doc;
        private HDocument _hDoc;
        private PdfWriter _writer;

        public PdfCreator(PdfCompressorSettings settings, string newPdf, PdfMeta meta, float dpi)
        {
            _ocrController = new OcrController();
            PdfSettings = settings;
            PdfFilePath = newPdf;
            SetupDocumentWriter(newPdf, meta.Author, meta.Title, meta.Subject, meta.KeyWords);
            _hDoc = new HDocument(dpi);
            _dpi = dpi;
        }
        public string PdfFilePath { get; }
        public PdfCompressorSettings PdfSettings { get; set; }

        public void Dispose()
        {
            try
            {
                _writer.Dispose();
                _doc = null;
                _writer = null;
            }
            catch
            {
                //
            }
        }



        /// <summary>
        ///     If adding an image directly, don't forget to call CreatePage
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sessionName"></param>
        /// <param name="dimension"></param>
        private void AddImage(System.Drawing.Image image, string sessionName, Rectangle dimension)
        {

            if (OnProcessImageForDisplay != null)
                image = OnProcessImageForDisplay(image);
            Bitmap bmp = ImageProcessor.GetAsBitmap(image, PdfSettings.Dpi);
            //image.Dispose();
            iTextSharp.text.Image i = GetImageForPdf(bmp, sessionName);
            AddImage(i, dimension);
            bmp.Dispose();

        }

        private void AddImage(iTextSharp.text.Image image, Rectangle dimension)
        {

            if (dimension == null)
            {
                if (PdfSettings.PdfPageSize == null)
                {
                    //Getting Width of the image width adding the page right & left margin
                    float width = image.Width / PdfSettings.Dpi * 72;

                    //Getting Height of the image height adding the page top & bottom margin
                    float height = image.Height / PdfSettings.Dpi * 72;

                    //Creating pdf rectangle with the specified height & width for page size declaration
                    dimension = new Rectangle(width, height);
                }
                else
                {
                    dimension = PdfSettings.PdfPageSize;
                }
            }
            /*you __MUST__ call SetPageSize() __BEFORE__ calling NewPage()
            * AND __BEFORE__ adding the image to the document
            */

            //Changing the page size of the pdf document based on the rectangle defined


            float dHeight = dimension.Height / _dpi;
            float dwidth = dimension.Width / _dpi;
            Rectangle newDim = new Rectangle(dwidth * 72.0f, dHeight * 72.0f);
            _doc.SetPageSize(newDim);
            image.SetAbsolutePosition(0, 0);
            image.ScaleAbsolute(_doc.PageSize.Width, _doc.PageSize.Height);
            _doc.NewPage();
            _doc.Add(image);

        }

        public string AddPage(string imagePath, PdfMode mode, string sessionName, Rectangle dimension)
        {
            System.Drawing.Image img = System.Drawing.Image.FromFile(imagePath);
            string result = AddPage(img, mode, sessionName, dimension);
            img.Dispose();
            return result;
        }

        public string AddPage(System.Drawing.Image image, PdfMode mode, string sessionName, Rectangle dimension)
        {
            Guid objGuid = image.FrameDimensionsList[0];
            FrameDimension frameDim = new FrameDimension(objGuid);
            int frameCount = image.GetFrameCount(frameDim);
            string pageBody = string.Empty;
            for (int i = 0; i < frameCount; i++)
            {
                image.SelectActiveFrame(frameDim, i);
                switch (mode)
                {
                    case PdfMode.Ocr:
                        {
                            AddImage(image, sessionName, dimension);

                            if (OnProcessImageForOcr != null)
                                image = OnProcessImageForOcr(image);

                            _ocrController.AddToDocument(PdfSettings.Language, image, ref _hDoc, sessionName);

                            if (_hDoc.Pages.Count > 0)
                            {
                                HPage page = _hDoc.Pages[_hDoc.Pages.Count - 1];
                                WriteUnderlayContent(page);
                                pageBody = pageBody + page.TextUnescaped;
                            }

                            break;
                        }

                }
            }

            return pageBody;
        }

        private iTextSharp.text.Image GetImageForPdf(Bitmap image, string sessionName)
        {
            iTextSharp.text.Image i = null;

            switch (PdfSettings.ImageType)
            {
                case PdfImageType.Tif:
                    i = iTextSharp.text.Image.GetInstance(ImageProcessor.ConvertToImage(image, "TIFF", PdfSettings.ImageQuality, PdfSettings.Dpi),
                        ImageFormat.Png);
                    break;
                case PdfImageType.Png:
                    i = iTextSharp.text.Image.GetInstance(ImageProcessor.ConvertToImage(image, "PNG", PdfSettings.ImageQuality, PdfSettings.Dpi),
                        ImageFormat.Png);
                    break;
                case PdfImageType.Jpg:
                    i = iTextSharp.text.Image.GetInstance(ImageProcessor.ConvertToImage(image, "JPEG", PdfSettings.ImageQuality, PdfSettings.Dpi),
                        ImageFormat.Jpeg);
                    break;
                case PdfImageType.Bmp:
                    i = iTextSharp.text.Image.GetInstance(ImageProcessor.ConvertToImage(image, "BMP", PdfSettings.ImageQuality, PdfSettings.Dpi),
                        ImageFormat.Bmp);
                    break;
            }

            return i;
        }


        public event ProcessImageForDisplay OnProcessImageForDisplay;
        public event ProcessImageForOcr OnProcessImageForOcr;

        public void SaveAndClose()
        {
            try
            {
                if (_doc.PageNumber == 0)
                    _doc.NewPage();
                _doc.Close();
            }
            catch (Exception)
            {
                //
                if (_doc != null)
                {
                    _doc.Close();
                }
            }
        }

        private void SetupDocumentWriter(string fileName, string author, string title, string subject, string keywords)
        {
            _doc = new Document();
            _doc.SetMargins(0, 0, 0, 0);

            _writer = PdfWriter.GetInstance(_doc, new FileStream(fileName, FileMode.Create));
            _writer.CompressionLevel = 100;
            _writer.SetFullCompression();

            _writer.SetMargins(0, 0, 0, 0);
            _doc.Open();
            if (PdfSettings == null)
                return;
            _doc.AddAuthor(author);
            _doc.AddTitle(title);
            _doc.AddSubject(subject);
            _doc.AddKeywords(keywords);
        }

        private void WriteUnderlayContent(HPage page)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            foreach (HParagraph para in page.Paragraphs.ToList())
                foreach (HLine line in para.Lines.ToList())
                    switch (PdfSettings.WriteTextMode)
                    {
                        case WriteTextMode.Word:
                            {
                                line.AlignTops();

                                foreach (HWord c in line.Words)
                                {
                                    c.CleanText();
                                    BBox b = BBox.ConvertBBoxToPoints(c.Bbox, PdfSettings.Dpi);

                                    //if (b.Height > 28)
                                    if (b.Height > 150)
                                        continue;

                                    PdfContentByte cb = _writer.DirectContentUnder;

                                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, "CP1254", false);
                                    if (!string.IsNullOrEmpty(PdfSettings.FontName))
                                    {
                                        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), PdfSettings.FontName);
                                        baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                                    }

                                    cb.BeginText();
                                    cb.SetFontAndSize(baseFont, b.Height > 0 ? b.Height : 2);
                                    cb.SetTextMatrix(b.Left, _doc.PageSize.Height - b.Top - b.Height + 2);

                                    float space;
                                    if (c.Text.Length != 0)
                                        space = Math.Abs((b.Left - b.Width) / c.Text.Length);
                                    else
                                        space = DocWriter.SPACE;
                                    cb.SetWordSpacing(space);
                                    cb.ShowText(c.Text.Trim() + " ");
                                    cb.EndText();
                                }

                                break;
                            }
                        case WriteTextMode.Line:
                            {
                                line.CleanText();
                                BBox b = BBox.ConvertBBoxToPoints(line.Bbox, PdfSettings.Dpi);

                                if (b.Height > 28)
                                    continue;
                                PdfContentByte cb = _writer.DirectContentUnder;

                                BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, false);
                                cb.BeginText();
                                cb.SetFontAndSize(baseFont, b.Height > 0 ? b.Height : 2);
                                cb.SetTextMatrix(b.Left, _doc.PageSize.Height - b.Top - b.Height + 2);

                                float space;
                                if (line.Text.Length != 0)
                                    space = Math.Abs((b.Left - b.Width) / line.Text.Length);
                                else
                                    space = DocWriter.SPACE;

                                cb.SetWordSpacing(space);
                                cb.ShowText(line.Text);
                                cb.EndText();
                                break;
                            }
                    }
        }

    }
}
