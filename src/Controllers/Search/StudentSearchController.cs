using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Infrastructure;
using Contingent.SQL;
using Contingent.Models.Domain.Citizenship;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;
namespace Contingent.Controllers.Search;



public class StudentSearchController : Controller
{

    private readonly ILogger<StudentSearchController> _logger;

    public StudentSearchController(ILogger<StudentSearchController> logger)
    {
        _logger = logger;
    }
    // головное меню поиска

    [HttpGet]
    [AllowAnonymous]
    [Route("/students/search")]
    public IActionResult GetMainPage()
    {

        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/students/search",
            RequestType = "GET",
        });
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/students/search")]
    public IActionResult GetMainPageProtected()
    {
        return View(@"Views/Search/Students.cshtml", new object());
    }
    // поиск студента по параметрам из тела запроса

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/students/search/find")]
    public IActionResult FindStudents()
    {
        var stream = new StreamReader(Request.Body);
        StudentSearchQueryDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<StudentSearchQueryDTO>(stream.ReadToEndAsync().Result);
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос: " + e.Message));
        }
        if (dto is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос"));
        }
        SQLParameterCollection parameters = new();
        var condition = ComplexWhereCondition.Empty;
        var byOrder = Order.GetOrderById(dto.Source?.OrderId);
        if (dto.Name is not null && dto.Name.Length > 2)
        {
            var parts = dto.Name.Split(' ').Where(x => x != string.Empty).Select(x => x.Trim());
            condition = condition.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                RussianCitizenship.GetFilterClause(
                    new RussianCitizenshipInDTO()
                    {
                        Surname = parts.First(),
                        Name = parts.Count() >= 2 ? parts.ElementAt(1) : null,
                        Patronymic = parts.Count() == 3 ? parts.ElementAt(2) : null,
                    },
                ref parameters)
            );
        }
        if (!(string.IsNullOrEmpty(dto.GroupName) || string.IsNullOrWhiteSpace(dto.GroupName)))
        {
            condition = condition.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                GroupModel.GetFilterForGroup("%" + dto.GroupName.Trim().ToLower() + "%", ref parameters)
            );
        }
        var limits = new QueryLimits(dto.PageSkipCount, dto.PageSize);
        var found = FlowHistory.GetRecordsByFilter(
            limits,
            new HistoryExtractSettings()
            {
                ExtractAddress = (false, false),
                ExtractGroups = true,
                ExtractOrders = false,
                ExtractStudents = true,
                ExtractOnlyLastState = true,
                IncludeNotRegisteredStudents = true,
                SortByStudentNames = true,
                // source никогда не будет null 
                ExtractByOrder = (byOrder is null) ? null : (byOrder, dto.Source!.OrderMode == OrderRelationMode.OnlyIncluded.ToString() ? OrderRelationMode.OnlyIncluded : OrderRelationMode.OnlyExcluded),

            },
            condition, parameters
        ).Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo));
        return Json(found);
    }
}