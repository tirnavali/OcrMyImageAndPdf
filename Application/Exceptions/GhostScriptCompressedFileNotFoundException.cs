namespace OcrMyImage.Application.Exceptions
{
    public class GhostScriptCompressedFileNotFoundException : Exception
    {
        public GhostScriptCompressedFileNotFoundException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
    }
}
