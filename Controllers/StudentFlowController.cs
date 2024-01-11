
using StudentTracking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using StudentTracking.Models.Services;
using StudentTracking.Models.SQL;
using System.Net.Http.Headers;
using StudentTracking.Models.JSON.Responses;
using static Utilities.Utils;
using StudentTracking.Models.SQL;
using StudentTracking.Models.Domain;
using Utilities;
using StudentTracking.Models.JSON;
using StudentTracking.Models.Domain.Orders;
using System.Runtime.Serialization.Json;
namespace StudentTracking.Controllers;
public class StudentFlowController : Controller{

    private readonly ILogger<StudentFlowController> _logger;

    public StudentFlowController(ILogger<StudentFlowController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Route("/studentflow/")]
    public IActionResult GetMainPage(string query){
        return View(@"Views/Processing/StudentFlow.cshtml");
    }

    [HttpGet]
    [Route("/studentflow/inorder/{query?}")]
    public async Task<JsonResult> GetStudentsByOrder(string query){
        if (int.TryParse(query, out int id)){
            var mapper = new Mapper<StudentViewJSONResponse>(
                (s, m) => {
                    s.StudentId = (int)m["sid"];
                    s.StudentFullName =  (string)m["dn"];
                    if (s.Group == null){
                        s.Group = new GroupViewJSONResponse();
                    }
                    var groupId = m["gid"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    var groupName = m["gn"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    } 
                }, new List<Column>(){
                    new Column("id", "sid", "students"),
                    new Column("displayed_name", "dn", "students"),
                    new Column("id", "gid", "educational_group"),
                    new Column("group_name", "gn", "educational_group"),
                }
            );
            var joinSection = new JoinSection()
            .AppendJoin(JoinSection.JoinType.InnerJoin,
                 new Column("id", null, "students"),
                 new Column("student_id", null, "student_flow"))
            .AppendJoin(JoinSection.JoinType.InnerJoin,
                 new Column("order_id", null, "student_flow"),
                 new Column("id", null, "orders"))
            .AppendJoin(JoinSection.JoinType.RightJoin,
                 new Column("group_id_to", null, "student_flow"),
                 new Column("id", null, "educational_group"));
            var param = new SQLParameters(); 
            var filter = new WhereCondition(
                param,
                new Column("id", null, "orders"),
                new SQLParameter<int>(id),
                WhereCondition.Relations.Equal
            );
            SelectQuery<StudentViewJSONResponse> sqlQuery = new SelectQuery<StudentViewJSONResponse>(
                "students",
                param,
                mapper,
                joinSection,
                new ComplexWhereCondition(filter)
            );
            var got = await StudentModel.FilterStudents(sqlQuery);
            if (got!=null){
                return Json(got);
            }
        }
        return Json(new object());
    }
    [HttpGet]
    [Route("/studentflow/notinorder/{query?}")]
    public async Task<JsonResult> FilterStudents(string query){
        if (int.TryParse(query, out int id)){
            var mapper = new Mapper<StudentViewJSONResponse>(
                (s, m) => {
                    s.StudentId = (int)m["sid"];
                    s.StudentFullName =  (string)m["dn"];
                    if (s.Group == null){
                        s.Group = new GroupViewJSONResponse();
                    }
                    var groupId = m["gid"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    var groupName = m["gn"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    } 
                }, new List<Column>(){
                    new Column("id", "sid", "students"),
                    new Column("displayed_name", "dn", "students"),
                    new Column("id", "gid", "educational_group"),
                    new Column("group_name", "gn", "educational_group"),
                }
            );
            var joinSection = new JoinSection()
            .AppendJoin(JoinSection.JoinType.LeftJoin,
                 new Column("id", null, "students"),
                 new Column("student_id", null, "student_flow"))
            .AppendJoin(JoinSection.JoinType.LeftJoin,
                 new Column("group_id_to", null, "student_flow"),
                 new Column("id", null, "educational_group"));
            var param = new SQLParameters(); 
            var left = new WhereCondition(
                param,
                new Column("order_id", null, "student_flow"),
                new SQLParameter<int>(id),
                WhereCondition.Relations.NotEqual
            );
            var right = new WhereCondition(
                new Column("order_id", null, "student_flow"),
                WhereCondition.Relations.Is
            );
            SelectQuery<StudentViewJSONResponse> sqlQuery = new SelectQuery<StudentViewJSONResponse>(
                "students",
                param,
                mapper,
                joinSection,
                new ComplexWhereCondition(left, right, ComplexWhereCondition.ConditionRelation.OR, true)  
            );
            var got = await StudentModel.FilterStudents(sqlQuery);
            if (got!=null){
                return Json(got);
            }
        }
        return Json(new object());
    }

    [HttpPost]
    [Route("studentflow/save")]
    public async Task<ActionResult> SaveFlowChanges(){

        OrderInStudentFlowJSON? deserialized = null;      
        try {
            deserialized = JsonSerializer.Deserialize<OrderInStudentFlowJSON>(Request.Body);
        }
        catch {
            return BadRequest("Неверный формат тела запроса");
        }
        if (deserialized == null){
            return BadRequest("Неверный формат json");
        }
        if (deserialized.OrderId == null){
            return BadRequest("Приказ не указан");
        }
        if (!await Order.IsOrderExists((int)deserialized.OrderId)){
            return BadRequest("Приказа не существует");
        }
        if (deserialized.Records == null){
            return BadRequest("В приказе не указано ни одного студента");
        }
        var order = Order.

        if (!await StudentModel.IsAllExists(deserialized.Records.Select(x => x.StudentId))){
            return BadRequest("В приказе указаны несуществующие студенты");
        }
        if (!await GroupModel.IsAllExists(deserialized.Records.Select(x => x.GroupToId))){
            return BadRequest("В приказе указаны несуществующие группы");
        }



    } 


   
}