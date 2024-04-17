namespace OcrMyImage.Application.HocrElements
{
    internal class HLine : HocrClass
    {
        private readonly float _dpi;

        public IList<HWord> Words { get; set; }
        public IList<HLine> LinesInSameSentence { get; set; }
        public bool LineWasCombined { get; private set; }

        public HLine(float dpi)
        {
            Words = new List<HWord>();
            LinesInSameSentence = new List<HLine>();
            _dpi = dpi;
        }

        public void AlignTops()
        {
            if (Words.Count == 0)
                return;

            float maxHeight = Words.OrderByDescending(x => x.Bbox.Height).Take(1).Single().Bbox.Height;
            float top = Words.Select(x => x.Bbox.Top).Min(); ;

            foreach (HWord word in Words)
            {
                word.Bbox.Top = top;
                word.Bbox.Height = maxHeight;
            }

        }

        public HLine CombineLinesInSentence()
        {
            if (LinesInSameSentence == null || LinesInSameSentence.Count == 0)
                return this;

            HLine line = new HLine(_dpi)
            {
                Id = LinesInSameSentence.OrderBy(x => x.Bbox.Left).First().Id,
                Bbox = new BBox(_dpi)
                {
                    Top = LinesInSameSentence.Select(x => x.Bbox.Top).Min(),
                    Height = LinesInSameSentence.Last().Bbox.Height,
                    Left = LinesInSameSentence.Select(x => x.Bbox.Left).Min(),
                    Width = LinesInSameSentence.Select(x => x.Bbox.Width).Sum()
                }
            };

            foreach (HLine o in LinesInSameSentence.OrderBy(x => x.Bbox.Left))
                line.Text += o.Text;

            if (LinesInSameSentence.Count > 1)
                line.LineWasCombined = true;

            return line;

        }


    }
}
