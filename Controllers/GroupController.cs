
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;
using StudentTracking.Models.JSON;
using Utilities.Validation;

namespace StudentTracking.Controllers;

public class GroupController : Controller{

    private readonly ILogger<GroupController> _logger;

    public GroupController(ILogger<GroupController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("groups/modify/{query}")]
    public async Task<IActionResult> ProcessGroup(string query){
        if (query == "new"){
            return View(@"Views/Modify/GroupModify.cshtml", new GroupModel()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = await GroupModel.GetGroupById(id, null);
            if(got == null){
                return View(@"Views/Shared/Error.cshtml", "Группы с таким id не существует" );
            }
            return View(@"Views/Modify/GroupModify.cshtml", got); 
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id группы");
        }
    }

    [HttpPost]
    [Route("/groups/add")]
    public async Task<JsonResult> SaveOrUpdateGroup(){
        using (var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<GroupModelJSON>(body);
            if (deserialized == null){
                return Json(new object());
            }
            else{
                var processed = await GroupModel.FromJSON(deserialized, null);
                await processed.SaveAsync(null);
                if (await processed.GetCurrentState(null) != RelationTypes.Bound){
                    return Json(processed.GetErrors());
                }
                else{
                    return Json(new {GroupId = processed.Id}); 
                }
            }
        }
    }
    [HttpPost]
    [Route("/groups/getname")]
    public async Task<JsonResult> GenerateName(){
        using (var reader = new StreamReader(Request.Body)){
            string jsonString = await reader.ReadToEndAsync();
            GroupModelJSON? group = JsonSerializer.Deserialize<GroupModelJSON>(jsonString);
            string name = "";
            if (group!=null){
                var parsed = await GroupModel.FromJSON(group, null);
                name = parsed.GroupName;
            }
            return Json(new {GroupName = name});
        }
    }
}