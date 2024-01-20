
using System.Data.SqlTypes;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models;
using StudentTracking.Models.JSON;
using StudentTracking.Models.SQL;
using Utilities;
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

    [HttpGet]
    [Route("/groups/find/{query?}")]
    public async Task<JsonResult> FindGroups(string? query){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        if (query == null || query.Length <= 2){
            return Json(new object());
        }
        var mapper = new Mapper<GroupResponseDTO>(
            (m) => {
                var g = new GroupResponseDTO();
                g.GroupId = (int)m["gid"];
                g.GroupName = (string)m["gn"];
                g.IsNameGenerated = (bool)m["gen"];
                return new Task<GroupResponseDTO>(() => g);
            },
            new List<Column>(){
                new Column("id", "gid", "educational_group"),
                new Column("group_name", "gn", "educational_group"),
                new Column("name_generated", "gen", "educational_group"),
            } 
        );
        var par = new SQLParameterCollection();
        var p1 = par.Add("%" + query + "%");  
        var whereClause = new WhereCondition(
            new Column("group_name",  "educational_group"),
            p1,
            WhereCondition.Relations.Like 
        );
        var result = SelectQuery<GroupResponseDTO>.Init("educational_group")
        .AddMapper(mapper)
        .AddWhereStatement(new ComplexWhereCondition(whereClause))
        .AddParameters(par)
        .Finish();
        if (result.IsFailure){
            throw new Exception("Не удалось создать запрос на поиск групп");
        }
        var got = await result.ResultObject.Execute(conn, new QueryLimits(0,20));
        if (got!=null){
            return Json(got);
        }
        return Json(new object());

    }

}