namespace OcrMyImage.Application.HocrElements
{
    internal class HPage : HocrClass
    {
        public IList<HParagraph> Paragraphs { get; set; }
        public int AverageWordCountPerLine { get; private set; }
        public string ImageFile { get; set; }
        public int ImageFrameNumber { get; set; }

        public HPage()
        {
            Paragraphs = new List<HParagraph>();
        }

        public IList<HLine> CombineSameRowLines()
        {
            IList<HLine> lines = new List<HLine>();
            foreach (HParagraph p in Paragraphs)
                foreach (HLine l in p.Lines)
                    if (lines.All(x => x.Id != l.Id))
                        lines.Add(l);

            IList<HLine> results = new List<HLine>();

            IOrderedEnumerable<HLine> sortedLines = lines.OrderBy(x => x.Bbox.Top);
            foreach (HLine l in sortedLines)
            {
                l.CleanText();

                List<HLine> linesOnThisLine = lines.Where(x => Math.Abs(x.Bbox.DefaultPointBBox.Top - l.Bbox.DefaultPointBBox.Top) <= 2)
                    .OrderBy(x => x.Bbox.Left)
                    .Distinct().ToList();

                if (linesOnThisLine.Select(x => x.Id.Trim()).Distinct().Count() > 1)
                    l.LinesInSameSentence = linesOnThisLine;

                HLine c = l.CombineLinesInSentence();

                if (results.All(x => x.Id != c.Id))
                    results.Add(c);
            }

            AverageWordCountPerLine = Convert.ToInt32(Math.Ceiling(results.Select(x => x.Words.Count).Average()));

            return results.OrderBy(x => x.Bbox.Top).Distinct().ToList();
        }
    }
}
