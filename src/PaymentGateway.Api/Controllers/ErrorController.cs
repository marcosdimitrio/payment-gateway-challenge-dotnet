using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        [Route("/error")]
        public IActionResult HandleError() => Problem();
    }
}
