using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.Out;
using Contingent.Models;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Flow.History;
using Contingent.SQL;
using Contingent.DTOs.Out;
using Microsoft.AspNetCore.Authorization;

namespace Contingent.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/")]
    public IActionResult Index()
    {
        return View("~/Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/home",
            RequestType = "GET",
        });
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/home")]
    public IActionResult IndexProtected()
    {
        return View("~/Views/Home/Index.cshtml");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/home/last")]
    public IActionResult GetLastHistoryRecords()
    {
        var records = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0, 50),
            new HistoryExtractSettings()
            {
                OverallHistorical = true,
                ExtractAddress = (false, false),
                ExtractOrders = true,
                ExtractStudents = true,
                ExtractGroups = false,
            }
        );
        records = records.OrderByDescending(x => x.Id);
        var studentMovesHistoryRecords = new List<InGroupRelation>();
        foreach (var record in records)
        {
            studentMovesHistoryRecords.Add(new InGroupRelation(record.Student, record.ByOrder));
        }
        return Json(studentMovesHistoryRecords);
    }
}
