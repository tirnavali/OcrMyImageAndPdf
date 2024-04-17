using Microsoft.AspNetCore.Mvc;
using OcrMyImage.Application;
using OcrMyImage.Application.Models;
using System.Web.Http.Description;

namespace OcrMyImage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class OcrController : ControllerBase
    {

        [HttpPost("PdfFromPdf")]
        [ResponseType(typeof(OcrFileDto))]
        public ActionResult<OcrFileDto> GeneratePdf(IFormFile file)
        {
            try
            {
                if (file.FileName.EndsWith(".pdf"))
                {
                    using MemoryStream ms = new MemoryStream();
                    file.CopyTo(ms);
                    ms.ToArray();
                    var _ocrGate = OcrGate.Instance();
                    var result = _ocrGate.ProccessPDF(ms.ToArray());
                    ms.Dispose();
                    OcrFileDto ocrFileDto = new OcrFileDto() { Text = result.Item2, FileData = result.Item1 };
                    return new OkObjectResult(ocrFileDto);
                }
                else
                {
                    throw new FileLoadException("PDF uzantılı dosya yükleyin.");
                }

            }

            catch
            {
                throw;
            }
        }

        [HttpPost("PdfFromImages")]
        [ResponseType(typeof(OcrFileDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<OcrFileDto> GeneratePdfFromImages(IList<IFormFile> files)
        {
            if (OcrDurumu.Pool == 0)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            try
            {
                OcrDurumu.Pool--;
                List<byte[]> images = new List<byte[]>();
                if (files.All(t =>
                t.ContentType.Contains("jpg") ||
                t.ContentType.Contains("jpeg") ||
                t.ContentType.Contains("tif") ||
                t.ContentType.Contains("tiff") ||
                t.ContentType.Contains("png")))
                {
                    foreach (var item in files)
                    {
                        var memstrm = new MemoryStream();
                        item.CopyTo(memstrm);
                        images.Add(memstrm.ToArray());
                        memstrm.Dispose();
                    }
                    files = null;


                    var _ocrGate = OcrGate.Instance();
                    var result = _ocrGate.ProccessImages(images);

                    OcrFileDto ocrFileDto = new OcrFileDto() { Text = result.Item2, FileData = result.Item1 };
                    OcrDurumu.DevamEdiyor = false;
                    return new OkObjectResult(ocrFileDto);
                }
                else
                {
                    throw new FileLoadException("Tüm dosya uzatıları jpg, jpeg, tif, png veya tiff olmalı.");
                }

            }
            catch (OutOfMemoryException ex)
            {
                var errorResult = new OcrFileDto()
                {
                    Error = new string[] { ex.Message },
                    FileData = new byte[] { },
                    Text = new string[] { "" },
                };

                return new BadRequestObjectResult(ex.Message);
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally { OcrDurumu.Pool++; }
        }
    }


    static class OcrDurumu
    {
        public static bool DevamEdiyor = false;
        public static int Pool = 10;
    }
}
