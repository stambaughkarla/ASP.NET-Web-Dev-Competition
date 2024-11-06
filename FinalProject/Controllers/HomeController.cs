using Microsoft.AspNetCore.Mvc;

##stacey
namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
