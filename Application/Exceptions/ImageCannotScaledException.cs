namespace OcrMyImage.Application.Exceptions
{
    public class ImageCannotScaledException : Exception
    {
        public ImageCannotScaledException() : base() { }
        public ImageCannotScaledException(string message) : base(message) { }
        public ImageCannotScaledException(string message, Exception innerException) : base(message, innerException) { }
    }
}
