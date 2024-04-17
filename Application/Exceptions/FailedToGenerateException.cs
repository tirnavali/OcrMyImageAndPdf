namespace OcrMyImage.Application.Exceptions
{
    public class FailedToGenerateException : Exception
    {
        public FailedToGenerateException(string msg, Exception innerExcepiton) : base(msg, innerExcepiton)
        {

        }
    }
}
