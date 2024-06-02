using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Groups;
using Contingent.SQL;

namespace Contingent.Controllers.Search;

public class GroupSearchController : Controller
{

    public GroupSearchController()
    {

    }

    [HttpGet]
    [Route("groups/search/menu")]
    public IActionResult GetMainPage()
    {
        return View("Views/Search/Groups.cshtml", new List<GroupSearchResultDTO>());
    }

    [HttpPost]
    [Route("groups/search/query")]
    public IActionResult Search()
    {
        var request = new StreamReader(Request.Body);
        GroupSearchQueryDTO? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GroupSearchQueryDTO>(request.ReadToEndAsync().Result);
        }
        catch
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос"));
        }
        if (dto is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос, не удалось разобрать параметры"));
        }
        var searchResult = GroupModel.FindGroupsByName(new QueryLimits(0, 30), dto.GroupName, dto.IsActive);
        return Json(searchResult.Select(x => new GroupSearchResultDTO(x)));
    }
}
