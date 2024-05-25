using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OnvifService.Controllers
{
    [Authorize(AuthenticationSchemes = "Digest")]
    [Route("/[controller]")]
    public class PreviewController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var image = System.IO.File.OpenRead("vaultboy.jpg");
            return File(image, "image/jpeg");
        }
    }
}
