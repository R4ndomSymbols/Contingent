using Microsoft.AspNetCore.Mvc;
using Contingent.Models.Domain.Specialties;
using Contingent.Statistics.Tables;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;

namespace Contingent.Controllers;

public class StatisticController : Controller
{

    private readonly ILogger<HomeController> _logger;
    public StatisticController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/statistics/{table}")]
    public IActionResult GetMainPage(string table)
    {
        return View("Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/statistics/" + table,
            RequestType = "GET",
        });
    }

    // добавить уровень квалификации (специалист или квалифицированный рабочий)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/statistics/{tableName?}")]
    public IActionResult GetAgeStatistics(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            return View("Views/Shared/Error.cshtml", "Неверно указан параметр запроса");
        }
        ITable? table = null;
        switch (tableName)
        {
            case "age_qualified":
                table = new AgeTable(TrainingProgramTypes.QualifiedWorker);
                break;
            case "age_specialist":
                table = new AgeTable(TrainingProgramTypes.GenericSpecialist);
                break;
            case "specialty":
                table = new GenericSpecialty();
                break;
            case "legalAddress":
                table = new AddressTable();
                break;
        }
        if (table is null)
        {
            return View("Views/Shared/Error.cshtml", "Неверно указан параметр запроса");
        }
        else
        {
            return View("Views/Statistics/BaseTableView.cshtml", table);
        }
    }
}
