namespace OcrMyImage.Application.HocrElements
{
    internal class HCarea : HocrClass
    {
        public IList<HParagraph> Paragraphs { get; set; }
        internal HCarea()
        {
            Paragraphs = new List<HParagraph>();
        }
    }
}
