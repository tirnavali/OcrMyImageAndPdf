namespace OcrMyImage.Application.HocrElements
{
    internal class HWord : HocrClass
    {
        public IList<HChar> Characters { get; set; }
        public HWord() { Characters = new List<HChar>(); }

        public void AlignCharacters()
        {
            if (Characters.Count == 0)
                return;

            float maxHeight = Characters.OrderByDescending(x => x.Bbox.Height).Take(1).Single().Bbox.Height;
            float top = Characters.Select(x => x.Bbox.Top).Min();
            foreach (HChar c in Characters)
            {
                c.Bbox.Height = maxHeight;
                c.Bbox.Top = top;
            }
        }
    }
}
