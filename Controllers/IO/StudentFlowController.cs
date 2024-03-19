using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Orders;
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
        var card = new OrderSearchDTO(order.ResultObject);
        return View(@"Views/Processing/StudentFlow.cshtml", card);
    }

    [HttpGet]
    [Route("/studentflow/inorder/{query?}")]
    // нужно выбирать уникальных студентов
    public IActionResult GetStudentsByOrder(string query)
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
        var found = StudentHistory.GetRecordsForOrder(id, StudentHistory.OrderRelationMode.OnlyIncluded);
        var result = new List<StudentSearchResultDTO>();
        foreach (var s in found)
        {
            result.Add(new StudentSearchResultDTO(s.Student, s.GroupTo));
        }
        return Json(result);

    }

    [HttpGet]
    [Route("/studentflow/studentsByOrder/{query?}")]
    public async Task<IActionResult> GetStudentsInOrder(string query)
    {
        if (!int.TryParse(query, out int id))
        {
            return BadRequest("Неверный id");
        }
        int convertedId = int.Parse(query);
        var orderResult = await Order.GetOrderById(convertedId);
        if (orderResult.IsFailure)
        {
            return BadRequest("Несуществующий id");
        }
        var order = orderResult.ResultObject;
        var found = await order.GetStudentsByOrder();
        var studentMovesHitoryRecords = new List<StudentHistoryMoveDTO>();
        foreach (var student in found){
            var history = await StudentHistory.Create(student);
            var byAnchor = history.GetByOrder(order);
            var previous = history.GetClosestBefore(order);
            studentMovesHitoryRecords.Add(new StudentHistoryMoveDTO(student, byAnchor?.GroupTo, previous?.GroupTo, order));
        }
        return Json(studentMovesHitoryRecords);

    }


    [HttpGet]
    [Route("/studentflow/notinorder")]
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
        var found = StudentHistory.GetRecordsForOrder(id, StudentHistory.OrderRelationMode.OnlyExcluded);
        found = found.Take(30);
        var result = new List<StudentSearchResultDTO>();
        foreach (var s in found)
        {
            result.Add(new StudentSearchResultDTO(s.Student, s.GroupTo));
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
    [HttpGet]
    [Route("/studentflow/history/{id?}")]
    public async Task<IActionResult> GetHistory(string id){
        if (int.TryParse(id, out int parsed)){
            var student = await StudentModel.GetStudentById(parsed);
            if(student is null){
                return BadRequest("такого студента не существует");
            }
            var history = student.History.History;
            List<StudentHistoryMoveDTO> moves = new();

            for(int i = 0; i < history.Count; i++){
                moves.Add(new StudentHistoryMoveDTO(
                    student,
                    history[i].GroupTo,
                    (i == 0) ? null : history[i-1].GroupTo,
                    history[i].ByOrder
                ));
            }
            return Json(moves);
        }
        else {
            return BadRequest("id студента указан неверно");
        }
    }
}