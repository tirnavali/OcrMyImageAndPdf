namespace OcrMyImage.Application.HocrElements
{
    internal class HParagraph : HocrClass
    {
        public IList<HLine> Lines { get; set; }
        public HParagraph()
        {
            Lines = new List<HLine>();
        }
    }
}
