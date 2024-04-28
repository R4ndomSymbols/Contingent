using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models;
using StudentTracking.SQL;

namespace StudentTracking.Controllers.Search;

public class GroupSearchController : Controller {
    
    public GroupSearchController(){

    }

    [HttpGet]
    [Route("groups/search/menu")]
    public IActionResult GetMainPage(){
        return View("Views/Search/Groups.cshtml", new List<GroupSearchResultDTO>());
    }

    [HttpPost]
    [Route("groups/search/query")]
    public JsonResult Search(){
        var request = new StreamReader(Request.Body);
        GroupSearchQueryDTO? dto;
        try {
            dto = JsonSerializer.Deserialize<GroupSearchQueryDTO>(request.ReadToEndAsync().Result);
        }
        catch {
            return Json(new ErrorsDTO(new ValidationError("Неверный поисковый запрос")));
        }
        if (dto is null){
            return Json(new ErrorsDTO(new ValidationError("Неверный поисковый запрос (не разобран)")));
        }
        var searchResult = GroupModel.FindGroupsByName(new QueryLimits(0,30), dto.GroupName, dto.IsActive).Result;
        return Json(searchResult.Select(x => new GroupSearchResultDTO(x)));
    }
}
