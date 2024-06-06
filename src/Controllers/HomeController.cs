using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.Out;
using Contingent.Models;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Flow.History;
using Contingent.SQL;

namespace Contingent.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    public IActionResult Index()
    {
        return View("~/Views/Home/Index.cshtml");
    }

    [HttpGet]
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
