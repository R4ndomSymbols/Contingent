using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models;
using StudentTracking.SQL;
using Utilities;

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
            return View(@"Views/Modify/GroupModify.cshtml", new GroupOutDTO()); 
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
    [Route("/groups/addsequence")]
    public async Task<IActionResult> SaveEntireSequence(){
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        GroupInDTO? deserialized; 
        try {
            deserialized = JsonSerializer.Deserialize<GroupInDTO>(body);
        }
        catch (Exception e){
            Console.WriteLine(e.Message);
            return BadRequest("Неверный формат JSON");
        }
        var groupResult = await GroupModel.Build(deserialized);
        if (groupResult.IsFailure){
            return BadRequest(JsonSerializer.Serialize(new ErrorsDTO(groupResult.Errors)));
        }
        var saved = await GroupModel.SaveAllNextGroups(groupResult.ResultObject);
        return Json(saved.Select(x => new GroupOutDTO(x)));
        
    }
    [HttpPost]
    [Route("/groups/getname")]
    public async Task<IActionResult> GenerateName(){
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        GroupInDTO? deserialized; 
        try {
            deserialized = JsonSerializer.Deserialize<GroupInDTO>(body);
        }
        catch (Exception e){
            Console.WriteLine(e.Message);
            return BadRequest("Неверный формат JSON");
        }
        var groupResult = await GroupModel.Build(deserialized);
        
        if (groupResult.IsFailure){
            return BadRequest(JsonSerializer.Serialize(new ErrorsDTO(groupResult.Errors)));
        }
        return Json(new {GroupName = groupResult.ResultObject.GroupName});
    }

    [HttpGet]
    [Route("/groups/find/{query?}")]
    public async Task<IActionResult> FindGroups(string? query){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        if (query == null || query.Length <= 2){
            return BadRequest("Запрос не может быть пустым");
        }
        var par = new SQLParameterCollection();
        var p1 = par.Add("%" + query + "%");
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("group_name", "educational_group"),
                p1,
                WhereCondition.Relations.Like
            )
        );
        var result = await GroupModel.FindGroups(new QueryLimits(0, 30),
        additionalConditions: where,
        addtitionalParameters: par);
        var dtos = new List<GroupMinimalDTO>();
        foreach(var model in result){
            dtos.Add(new GroupMinimalDTO(model));
        }
        return Json(dtos);
    }
}