using Microsoft.AspNetCore.Mvc;
using Contingent.Models.Domain.Specialities;
using Contingent.Statistics.Tables;

namespace Contingent.Controllers;

public class StatisticController : Controller
{

    private readonly ILogger<HomeController> _logger;
    public StatisticController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    // добавить уровень квалификации (специалист или квалифицированный рабочий)


    [HttpGet]
    [Route("statistics/{query?}")]
    public IActionResult GetAgeStatistics(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return View("Views/Shared/Error.cshtml", "Неверно указан параметр запроса");
        }
        ITable? table = null;
        switch (query)
        {
            case "age_qualified":
                table = new AgeTable(TrainingProgramTypes.QualifiedWorker);
                break;
            case "age_specialist":
                table = new AgeTable(TrainingProgramTypes.GenericSpecialist);
                break;
            case "speciality":
                table = new GenericSpeciality();
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
