using Contingent.Controllers.DTO.Out;
using Contingent.DTOs.Out;
using Contingent.Import;
using Contingent.Utilities;
using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    [Route("/import")]
    public IActionResult GetLoginPage()
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/import",
            RequestType = "GET",
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/import")]
    public IActionResult GetMainPage()
    {
        return View(@"Views/Processing/Import.cshtml");
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/import/upload/{importType:int}")]
    public IActionResult ProcessFile(int importType)
    {
        using var transaction = ObservableTransaction.New;
        var import = ImportTypeInfo.GetImporterByType(importType, Request.Body, transaction);
        if (import is null)
        {
            return BadRequest(new ErrorCollectionDTO(new ImportValidationError("Не верно указан тип импорта")));
        }
        var result = import.Import();
        if (result.IsFailure)
        {
            transaction.Rollback();
            return BadRequest(new ErrorCollectionDTO(result.Errors));
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(saveResult.Errors));
        }
        return Ok();
    }

}