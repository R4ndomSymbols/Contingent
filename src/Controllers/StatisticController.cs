using Microsoft.AspNetCore.Mvc;
using Contingent.Models.Domain.Specialties;
using Contingent.Statistics.Tables;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;
using Contingent.Controllers.DTO.Out;
using Contingent.Utilities;
using Contingent.DTOs.In;
using System.Text.Json;
using Contingent.Models.Infrastructure;

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
    [Route("/statistics")]
    public IActionResult GetMainPage(string table)
    {
        return View("Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/statistics",
            RequestType = "GET",
        });
    }

    // добавить уровень квалификации (специалист или квалифицированный рабочий)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/statistics")]
    public IActionResult GetMainView(string tableName)
    {
        return View("Views/Statistics/BaseTableView.cshtml");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/statistics/table/{tableName?}")]
    public IActionResult GetTable(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            return BadRequest(new ErrorCollectionDTO(new StatisticValidationError("Неверно указано название таблицы")));
        }
        PeriodDTO? onPeriod;
        try
        {
            using var reader = new StreamReader(Request.Body);
            string requestData = reader.ReadToEndAsync().Result;
            onPeriod = JsonSerializer.Deserialize<PeriodDTO>(requestData);
        }
        catch
        {
            return BadRequest(new ErrorCollectionDTO(new StatisticValidationError("Неверно указан период")));
        }
        var result = Period.CreateFromDTO(onPeriod);
        if (result.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(new StatisticValidationError(result.Errors.First().ErrorMessage)));
        }
        ITable? table = null;
        try
        {
            switch (tableName)
            {
                case "age_qualified":
                    table = new AgeTable(TrainingProgramTypes.QualifiedWorker, result.ResultObject);
                    break;
                case "age_specialist":
                    table = new AgeTable(TrainingProgramTypes.GenericSpecialist, result.ResultObject);
                    break;
                case "specialty":
                    table = new GenericSpecialty(result.ResultObject);
                    break;
                case "legalAddress":
                    table = new AddressTable(result.ResultObject);
                    break;
            }
        }
        catch //(Exception e)
        {
            return BadRequest(new ErrorCollectionDTO(new StatisticValidationError("Таблица использует другую периодизацию, вероятно, вы указали лишнюю дату")));
        }
        if (table is null)
        {
            return BadRequest(new ErrorCollectionDTO(new StatisticValidationError("Неверно указано имя таблицы")));
        }
        return Content(table.HtmlContent);

    }

}
