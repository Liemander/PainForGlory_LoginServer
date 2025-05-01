using Microsoft.AspNetCore.Mvc;

namespace PainForGlory_LoginServer.Areas.Public.Controllers
{
    [Area("Public")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
