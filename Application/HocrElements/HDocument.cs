namespace OcrMyImage.Application.HocrElements
{
    internal class HDocument : HocrClass
    {
        private readonly Parser _parser;

        public IList<HPage> Pages { get; set; }

        public HDocument(float dpi)
        {
            Pages = new List<HPage>();
            _parser = new Parser(dpi);
        }

        public void AddFile(string hocrFile)
        {
            _parser.ParseHocr(this, hocrFile, true);
        }


    }
}
