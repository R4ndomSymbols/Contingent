using Microsoft.AspNetCore.Mvc;
using StudentTracking.Statistics;
using StudentTracking.SQL;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Statistics.Tables;

namespace StudentTracking.Controllers;

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
        if (string.IsNullOrEmpty(query)){
            return View("Views/Shared/Error.cshtml", "Неверно указан параметр запроса");
        }
        ITable? table = null;
        switch (query){
            case "age":
            table = new AgeTable();
            break;
            case "speciality":
            table = new GenericSpeciality();
            break;
            case "legalAddress":
            table = new AddressTable();
            break;
        }
        if (table is null){
            return View("Views/Shared/Error.cshtml", "Неверно указан параметр запроса");
        }
        else{
            return View("Views/Statistics/BaseTableView.cshtml", table);
        }
    }
}
