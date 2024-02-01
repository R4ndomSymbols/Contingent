
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
using StudentTracking.Models.Domain.Flow;
namespace StudentTracking.Controllers;
public class StudentFlowController : Controller
{

    private readonly ILogger<StudentFlowController> _logger;

    public StudentFlowController(ILogger<StudentFlowController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Route("/studentflow/{id?}")]
    public async Task<IActionResult> GetMainPage(string id)
    {
        if (!int.TryParse(id, out int parsed))
        {
            return BadRequest("Id не распознан");
        }
        var order = await Order.GetOrderById(int.Parse(id));
        if (order.IsFailure)
        {
            return BadRequest();
        }
        var card = new OrderCardResponseDTO(order.ResultObject);
        return View(@"Views/Processing/StudentFlow.cshtml", card);
    }

    [HttpGet]
    [Route("/studentflow/inorder/{query?}")]
    // нужно выбирать уникальных студентов
    public async Task<IActionResult> GetStudentsByOrder(string query)
    {
        if (!int.TryParse(query, out int id))
        {
            return BadRequest("Неверный id");
        }
        int convertedId = int.Parse(query);
        var order = Order.GetOrderById(convertedId);
        if (order is null)
        {
            return BadRequest("Несуществующий id");
        }
        var joins = new JoinSection()
        .AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("id", "students"),
            new Column("student_id", "student_flow")
        );
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(convertedId);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("order_id", "student_flow"),
                p1,
                WhereCondition.Relations.Equal));

        var found = await StudentModel.FindStudents(new QueryLimits(0, 30), joins, where, parameters);
        var result = new List<StudentResponseDTO>();
        foreach (var s in found)
        {
            result.Add(new StudentResponseDTO(s, await StudentHistory.GetCurrentStudentGroup(s.Id)));
        }
        return Json(result);

    }
    [HttpGet]
    [Route("/studentflow/notinorder/{query?}")]
    public async Task<IActionResult> FilterStudents(string query)
    {

        if (!int.TryParse(query, out int id))
        {
            return BadRequest("Неверный id");
        }
        int convertedId = int.Parse(query);
        var order = Order.GetOrderById(convertedId);
        if (order is null)
        {
            return BadRequest("Несуществующий id");
        }
        var joins = new JoinSection()
        .AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("id", "students"),
            new Column("student_id", "student_flow")
        );
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(convertedId);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("order_id", "student_flow"),
                p1,
                WhereCondition.Relations.NotEqual),
                new WhereCondition(
                new Column("student_id", "student_flow"),
                WhereCondition.Relations.Is),
            ComplexWhereCondition.ConditionRelation.OR,
            false);
        var found = await StudentModel.FindStudents(new QueryLimits(0, 30), joins, where, parameters);
        var result = new List<StudentResponseDTO>();
        foreach (var s in found)
        {
            result.Add(new StudentResponseDTO(s, await StudentHistory.GetCurrentStudentGroup(s.Id)));
        }
        return Json(result);
    }

    [HttpPost]
    [Route("/studentflow/save/{id?}")]
    public async Task<IActionResult> SaveFlowChanges(string id)
    {
        bool parsed = int.TryParse(id, out int orderId);
        if (!parsed)
        {
            return BadRequest("id is undefined");
        }
        using (var stream = new StreamReader(Request.Body))
        {
            var jsonString =  await stream.ReadToEndAsync();
            var result = await Order.GetOrderForConduction(orderId, jsonString);
            if (result.IsSuccess)
            {
                var order = result.ResultObject;
                await order.ConductByOrder();
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors?.ErrorsToString() ?? "");
            }
        }
    }
}