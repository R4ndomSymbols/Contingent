
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;

namespace StudentTracking.Controllers;

public class GroupController : Controller{

    private readonly ILogger<GroupController> _logger;

    public GroupController(ILogger<GroupController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("groups/group/{query}")]
    public IActionResult ProcessGroup(string query){
        if (query == "new"){
            return View(@"Views/Models/Group.cshtml", new GroupModel()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = GroupModel.GetGroupById(id);
            if(got == null){
                return View(@"Views/Shared/Error.cshtml", "Группы с таким Id не существует" );
            }
            return View(@"Views/Models/Group.cshtml", got); 
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id группы");
        }
    }
    [HttpGet]
    [Route("/groups/types")]
    public JsonResult GetGroupTypes(){
        var got = GroupModel.GetGroupEduTypes();
        if (got == null){
            return Json(null);
        }
        return Json(got.ToArray());
    }
    [HttpGet]
    [Route("/groups/forms")]
    public JsonResult GetGroupForms(){
        var got = GroupModel.GetGroupEduForms();
        if (got == null){
            return Json(null);
        }
        return Json(got.ToArray());
    }
    [HttpPost]
    [Route("/groups/add")]
    public async Task<JsonResult> SaveOrUpdateGroup(){
        using (var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<GroupModel>(body);
            if (deserialized == null){
                return Json(null);
            }
            return Json(GroupModel.CreateOrUpdateGroup(deserialized));
        }
    }
    [HttpGet]
    [Route("/groups/find/{query?}")]
    public JsonResult FindGroups(string? query){
        var found = GroupModel.FindGroup(query);
        return Json(found);
    }
}