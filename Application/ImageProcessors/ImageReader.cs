using System.Drawing;
using OcrMyImage.Application;
namespace OcrMyImage.Application.ImageProcessors
{
    internal class ImageReader
    {
        private List<byte[]> images;
        private int PageCount;

        public ImageReader(List<byte[]> images)
        {
            this.images = images;
            PageCount = images.Count;
        }

        public string GetPageImage(int pageNumber, string sessionName)
        {
            return GetPageImageWith(pageNumber, sessionName);
        }
        private string GetPageImageWith(int pageNumber, string sessionName)
        {
            try
            {
                var img = images.ElementAt(pageNumber);
                string outPut = GetOutPutFileName(sessionName, ".tiff");
                var imaj = GetImageFromByteArray(img);
                imaj.Save(outPut);
                return new FileInfo(outPut.Replace('"', ' ').Trim()).FullName;
            }
            catch (Exception e)
            {
                throw new Exception("Sayfa görüntüsü tiff olarak kaydedilemedi", e);
            }
        }
        private Image GetImageFromByteArray(byte[] image)
        {
            using (var ms = new MemoryStream(image))
            {
                return Image.FromStream(ms);
            }
        }

        private static string GetOutPutFileName(string sessionName, string extWithDot)
        {
            return "\"" + TempData.Instance.CreateTempFile(sessionName, extWithDot) + "\"";
        }
    }
}
