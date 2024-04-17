
using HtmlAgilityPack;
using System.Text;

namespace OcrMyImage.Application.HocrElements
{

    internal class Parser
    {
        private readonly float _dpi;
        private HLine _currentLine;
        private HPage _currentPage;
        private HCarea _currentCarea;
        private HHeader _currentHeader;
        private HTextFloat _currentTextFloat;
        private HOcrCaption _currentHocrCaption;
        private HParagraph _currentPara;
        private HtmlDocument _doc;
        private HDocument _hDoc;
        private string _hOcrFilePath;


        public Parser(float dpi) { _dpi = dpi; }

        private void ParseCharactersForLine(string title)
        {
            if (title == null)
                return;

            title = title.Replace("x_bboxes", "");

            string[] coords = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string bbox = "";
            string word = "";
            HWord w = new HWord();
            int charPos = 0;

            for (int i = 0; i < coords.Length; i++)
            {
                if (i % 4 == 0 && i != 0)
                {
                    HChar c = new HChar();
                    BBox b = new BBox(bbox, _dpi);
                    c.Bbox = b;

                    char[] chars = _currentLine.Text.ToCharArray();
                    c.Text = chars[charPos].ToString();

                    if (c.Text != " ")
                    {
                        word += c.Text;
                        if (w.Bbox == null)
                        {
                            w.Bbox = new BBox(_dpi);
                            {
                                w.Bbox.Height = c.Bbox.Height;
                                w.Bbox.Left = c.Bbox.Left;
                                w.Bbox.Top = _currentLine.Bbox.Top;
                            }
                        }
                    }
                    else
                    {
                        if (w.Characters.Count > 0)
                        {
                            HChar previouschar = w.Characters.OrderBy(x => x.ListOrder).Last();
                            w.Bbox.Width = previouschar.Bbox.Left + previouschar.Bbox.Width - w.Bbox.Left;
                            w.Text = word + " ";
                            w.Bbox.Height = w.Characters.Select(x => x.Bbox.Height).Max();
                            w.CleanText();
                            if (w.Characters.Count > 0 && w.Text != null && w.Text.Trim() != "")
                                _currentLine.Words.Add(w);
                            w = new HWord();
                            word = string.Empty;
                        }
                    }

                    bbox = string.Empty;
                    if ((int)c.Bbox.Left != -1)
                    {
                        c.ListOrder = charPos;
                        w.Characters.Add(c);
                    }

                    charPos += 1;
                }

                bbox += coords[i] + " ";
            }

            if (w.Characters.Count <= 0 || word.Trim() == string.Empty)
                return;
            w.Text = word;
            w.CleanText();
            _currentLine.Words.Add(w);
        }

        public HDocument ParseHocr(HDocument hOrcDoc, string hOcrFile, bool append)
        {
            _hDoc = hOrcDoc;

            if (_doc == null)
                _doc = new HtmlDocument();

            _hOcrFilePath = hOcrFile;
            if (File.Exists(hOcrFile) == false)
                throw new Exception("hocr file not found");

            _currentPage = null;
            _currentHeader = null;
            _currentPara = null;
            _currentCarea = null;
            _currentLine = null;
            _currentTextFloat = null;
            _currentHocrCaption = null;



            _doc.Load(hOcrFile, Encoding.UTF8);


            HtmlNode body = _doc.DocumentNode.SelectNodes("//body")[0];
            HtmlNodeCollection nodes1 = body.SelectNodes("//div");
            //#Issue #1 reported by Ryan-George
            IEnumerable<HtmlNode> divs = body.ChildNodes.Where(node => node.Name.ToLower() == "div");
            HtmlNodeCollection nodes = new HtmlNodeCollection(null);
            foreach (HtmlNode div in divs) nodes.Add(div);

            _hDoc.ClassName = "body";

            ParseNodes(nodes);
            return _hDoc;
        }

        private void ParseNodes(HtmlNodeCollection nodes)
        {
            foreach (HtmlNode node in nodes.ToList())
            {
                if (node.HasAttributes)
                {
                    string className = string.Empty;
                    string title = string.Empty;
                    string id = string.Empty;

                    if (node.Attributes["class"] != null)
                        className = node.Attributes["class"].Value;
                    if (node.Attributes["title"] != null)
                        title = node.Attributes["title"].Value;
                    if (node.Attributes["Id"] != null)
                        id = node.Attributes["Id"].Value;

                    switch (className)
                    {
                        case "ocr_page":
                            _currentPage = new HPage();
                            _currentPage.ClassName = className;
                            _currentPage.Id = id;
                            ParseTitle(title, _currentPage);
                            _currentPage.Text = node.InnerText;
                            _hDoc.Pages.Add(_currentPage);
                            break;
                        //case "ocr_carea":
                        //    _currentCarea = new HCarea();
                        //    _currentCarea.ClassName = className;
                        //    _currentCarea.Id = id;
                        //    ParseTitle(title, _currentCarea);
                        //    _currentCarea.Text = node.InnerText;
                        //    _currentPage.Careas.Add(_currentCarea);
                        //    break;
                        case "ocr_par":
                            _currentPara = new HParagraph();
                            _currentPara.ClassName = className;
                            _currentPara.Id = id;
                            ParseTitle(title, _currentPara);
                            _currentPara.Text = node.InnerText;
                            _currentPage.Paragraphs.Add(_currentPara);
                            break;
                        case "ocr_caption":
                            _currentLine = new HOcrCaption(_dpi);
                            _currentLine.ClassName = className;
                            _currentLine.Id = id;
                            ParseTitle(title, _currentLine);
                            _currentLine.Text = node.InnerText;
                            if (_currentPage == null)
                                _currentPage = new HPage();
                            if (_currentPara == null)
                            {
                                _currentPara = new HParagraph();
                                _currentPage.Paragraphs.Add(_currentPara);
                            }
                            _currentPara.Lines.Add(_currentLine);
                            break;
                        case "ocr_header":
                            _currentLine = new HHeader(_dpi);
                            _currentLine.ClassName = className;
                            _currentLine.Id = id;
                            ParseTitle(title, _currentLine);
                            _currentLine.Text = node.InnerText;
                            if (_currentPage == null)
                                _currentPage = new HPage();
                            if (_currentPara == null)
                            {
                                _currentPara = new HParagraph();
                                _currentPage.Paragraphs.Add(_currentPara);
                            }
                            _currentPara.Lines.Add(_currentLine);
                            break;
                        case "ocr_textfloat":
                            _currentLine = new HTextFloat(_dpi);
                            _currentLine.ClassName = className;
                            _currentLine.Id = id;
                            ParseTitle(title, _currentLine);
                            _currentLine.Text = node.InnerText;
                            if (_currentPage == null)
                                _currentPage = new HPage();
                            if (_currentPara == null)
                            {
                                _currentPara = new HParagraph();
                                _currentPage.Paragraphs.Add(_currentPara);
                            }
                            _currentPara.Lines.Add(_currentLine);
                            break;
                        case "ocr_line":
                            _currentLine = new HLine(_dpi);
                            _currentLine.ClassName = className;
                            _currentLine.Id = id;
                            ParseTitle(title, _currentLine);
                            _currentLine.Text = node.InnerText;
                            if (_currentPage == null)
                                _currentPage = new HPage();
                            if (_currentPara == null)
                            {
                                _currentPara = new HParagraph();
                                _currentPage.Paragraphs.Add(_currentPara);
                            }
                            _currentPara.Lines.Add(_currentLine);
                            break;
                        //case "ocr_textfloat":
                        //    _currentLine = new HLine(_dpi);
                        //    _currentLine.ClassName = className;
                        //    _currentLine.Id = id;
                        //    ParseTitle(title, _currentLine);
                        //    _currentLine.Text = node.InnerText;
                        //    if (_currentPage == null)
                        //        _currentPage = new HPage();
                        //    if (_currentPara == null)
                        //    {
                        //        _currentPara = new HParagraph();
                        //        _currentPage.Paragraphs.Add(_currentPara);
                        //    }
                        //    // continue from there
                        //    _currentPara.Lines.Add(_currentLine);
                        //    break;

                        case "ocrx_word":
                            HWord w = new HWord();
                            w.ClassName = className;
                            w.Id = id;
                            ParseTitle(title, w);
                            w.Text = node.InnerText;
                            if (_currentHeader != null)
                            {
                                _currentHeader.Words.Add(w);
                                break;
                            }
                            if (_currentLine == null)
                            {
                                _currentLine = new HLine(_dpi);
                                _currentPara.Lines.Add(_currentLine);
                            }
                            _currentLine.Words.Add(w);
                            break;
                        case "ocr_word":
                            HWord w1 = new HWord();
                            w1.ClassName = className;
                            w1.Id = id;
                            ParseTitle(title, w1);
                            w1.Text = node.InnerText;
                            _currentLine.Words.Add(w1);
                            break;
                        case "ocr_cinfo": //cuneiform only
                            ParseCharactersForLine(title);
                            break;
                    }
                }

                ParseNodes(node.ChildNodes);
            }
        }

        private void ParseTitle(string title, HocrClass ocrclass)
        {
            if (title == null)
                return;

            string[] values = title.Split(';');
            foreach (string s in values)
            {
                if (s.Contains("image ") || s.Contains("file "))
                {
                    string filePath = s.Replace("image ", string.Empty).Replace("file ", string.Empty).Replace('"', ' ').Trim();

                    if (File.Exists(filePath))
                    {
                        if (ocrclass is HPage)
                            _currentPage.ImageFile = filePath;
                    }
                    else
                    {
                        filePath = _hOcrFilePath.Replace(Path.GetFileName(_hOcrFilePath), Path.GetFileName(filePath));
                        {
                            if (ocrclass is HPage)
                                _currentPage.ImageFile = filePath;
                        }
                    }
                }

                if (s.Contains("ppageno"))
                    if (int.TryParse(s.Replace("ppageno", ""), out int frame))
                        _currentPage.ImageFrameNumber = frame;
                if (!s.Contains("bbox"))
                    continue;
                string coords = s.Replace("bbox", "");
                BBox box = new BBox(coords, _dpi);
                ocrclass.Bbox = box;
            }
        }
    }

}
