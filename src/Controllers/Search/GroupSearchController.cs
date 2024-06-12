using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Groups;
using Contingent.SQL;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;

namespace Contingent.Controllers.Search;

public class GroupSearchController : Controller
{

    public GroupSearchController()
    {

    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/groups/search")]
    public IActionResult GetSearchPage()
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/groups/search",
            RequestType = "GET",
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/groups/search")]
    public IActionResult GetMainPage()
    {
        return View("Views/Search/Groups.cshtml", new List<GroupSearchResultDTO>());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/groups/search/find")]
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
        var searchResult = GroupModel.FindGroupsByName(new QueryLimits(0, 30), dto.GroupName, dto.OnlyActive);
        return Json(searchResult.Select(x => new GroupSearchResultDTO(x)));
    }
}
