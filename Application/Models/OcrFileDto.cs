namespace OcrMyImage.Application.Models
{
    public class OcrFileDto
    {
        public string[] Text { get; set; }
        public byte[] FileData { get; set; }
        public string[]? Error { get; set; }
    }
}
