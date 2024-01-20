
using StudentTracking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
using StudentTracking.Models.SQL;
using System.Net.Http.Headers;
using static Utilities.Utils;
using StudentTracking.Models.Domain;
using Utilities;
using StudentTracking.Models.JSON;
using StudentTracking.Models.Domain.Orders;
using System.Runtime.Serialization.Json;
using System.ComponentModel.DataAnnotations;
using Utilities.Validation;
using StudentTracking.Controllers.DTO.Out;
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
        using var conn = await Utils.GetAndOpenConnectionFactory();
        if (int.TryParse(query, out int id)){
            var mapper = new Mapper<StudentResponseDTO>(
                (m) => {
                    var s = new StudentResponseDTO();
                    s.StudentId = (int)m["sid"];
                    s.StudentFullName =  (string)m["dn"];
                    s.Group = new GroupResponseDTO();
                    var groupId = m["gid"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    var groupName = m["gn"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    return new Task<StudentResponseDTO>(() => s); 
                }, new List<Column>(){
                    new Column("id", "sid", "students"),
                    new Column("displayed_name", "dn", "students"),
                    new Column("id", "gid", "educational_group"),
                    new Column("group_name", "gn", "educational_group"),
                }
            );
            var joinSection = new JoinSection()
            .AppendJoin(JoinSection.JoinType.InnerJoin,
                 new Column("id", "students"),
                 new Column("student_id", "student_flow"))
            .AppendJoin(JoinSection.JoinType.InnerJoin,
                 new Column("order_id", "student_flow"),
                 new Column("id", "orders"))
            .AppendJoin(JoinSection.JoinType.RightJoin,
                 new Column("group_id_to", "student_flow"),
                 new Column("id", "educational_group"));
            var param = new SQLParameterCollection(); 
            var p1 = param.Add(id);
            var filter = new WhereCondition(
                new Column("id", "orders"),
                p1,
                WhereCondition.Relations.Equal
            );
            var result = SelectQuery<StudentResponseDTO>.Init("students")
            .AddMapper(mapper)
            .AddJoins(joinSection)
            .AddWhereStatement(new ComplexWhereCondition(filter))
            .AddParameters(param)
            .Finish();
            if (result.IsFailure){
                throw new Exception("Неверно сформированный запрос"); 
            }
            var got = await result.ResultObject.Execute(conn, new QueryLimits(0,20));
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
            using var conn = await Utils.GetAndOpenConnectionFactory();
            var mapper = new Mapper<StudentResponseDTO>(
                (m) => {
                    var s = new StudentResponseDTO();
                    s.StudentId = (int)m["sid"];
                    s.StudentFullName =  (string)m["dn"];                        
                    s.Group = new GroupResponseDTO();
                    var groupId = m["gid"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    var groupName = m["gn"];
                    if (groupId.GetType() != typeof(DBNull)){
                        s.Group.GroupId = (int)groupId;
                    }
                    return new Task<StudentResponseDTO>(() => s); 
                }, new List<Column>(){
                    new Column("id", "sid", "students"),
                    new Column("displayed_name", "dn", "students"),
                    new Column("id", "gid", "educational_group"),
                    new Column("group_name", "gn", "educational_group"),
                }
            );
            var joinSection = new JoinSection()
            .AppendJoin(JoinSection.JoinType.LeftJoin,
                 new Column("id", "students"),
                 new Column("student_id", "student_flow"))
            .AppendJoin(JoinSection.JoinType.LeftJoin,
                 new Column("group_id_to", "student_flow"),
                 new Column("id", "educational_group"));
            var param = new SQLParameterCollection();
            var p1 = param.Add(id); 
            var left = new WhereCondition(
                new Column("order_id", "student_flow"),
                p1,
                WhereCondition.Relations.NotEqual
            );
            var right = new WhereCondition(
                new Column("order_id", "student_flow"),
                WhereCondition.Relations.Is
            );
            var result = SelectQuery<StudentResponseDTO>.Init("students")
            .AddMapper(mapper)
            .AddJoins(joinSection)
            .AddWhereStatement( new ComplexWhereCondition(left, right, ComplexWhereCondition.ConditionRelation.OR, true))
            .AddParameters(param)
            .Finish();
            if (result.IsFailure){
                throw new Exception("Запрос написан неверно");
            }
            var got = await result.ResultObject.Execute(conn, new QueryLimits(0, 20));
            if (got!=null){
                return Json(got);
            }
        }
        return Json(new object());
    }

    [HttpPost]
    [Route("studentflow/save/{id?}")]
    // контроллер проведения приказов, не зависит от типа приказа
    public async Task<ActionResult> SaveFlowChanges(string id){

        bool parsed = int.TryParse(id, out int orderId); 
        if (!parsed){
            return BadRequest("id приказа указан неверно");
        }
        using var stream = new StreamReader(Request.Body);
        var jsonString = await stream.ReadToEndAsync();
        var result = await Order.GetOrderForConduction(orderId, jsonString);
        if (result.IsSuccess){
            var order = result.ResultObject;
            await order.ConductByOrder();
            return Ok(); 
        }
        else {
            return BadRequest(result.Errors?.ErrorsToString() ?? "");
        }        
    } 
}