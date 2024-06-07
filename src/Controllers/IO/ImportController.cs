using Microsoft.AspNetCore.Mvc;

namespace Contingent.Controllers;

public class ImportController : Controller
{
    private readonly ILogger<ImportController> _logger;

    public ImportController(ILogger<ImportController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("/import")]
    public IActionResult GetMainPage()
    {
        return View(@"Views/Processing/Import.cshtml");
    }

}