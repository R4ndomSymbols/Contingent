using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Orders;
using Utilities.Validation;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
namespace StudentTracking.Controllers;
public class StudentFlowController : Controller
{

    private readonly ILogger<StudentFlowController> _logger;

    public StudentFlowController(ILogger<StudentFlowController> logger)
    {
        _logger = logger;
    }
    // страница проведения приказов
    [HttpGet]
    [Route("/studentflow/{id:int?}")]
    public IActionResult GetMainPage(int? id)
    {
        var order = Order.GetOrderById(id);
        if (order is null)
        {
            return BadRequest("Id приказа указан неверно");
        }
        var card = new OrderSearchDTO(order);
        return View(@"Views/Processing/StudentFlow.cshtml", card);
    }
    // сохранение результатов
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
            var jsonString = await stream.ReadToEndAsync();
            try
            {
                var result = await Order.GetOrderForConduction(orderId, jsonString);
                if (result.IsSuccess)
                {
                    var conductionStatus = result.ResultObject.ConductByOrder();
                    if (conductionStatus.IsSuccess){
                        return Ok();
                    }
                    else{
                        return BadRequest(JsonSerializer.Serialize(new ErrorsDTO(conductionStatus.Errors)));
                    } 
                    
                }
                else
                {
                    return BadRequest(JsonSerializer.Serialize(new ErrorsDTO(result.Errors)));
                }
            }
            catch (Exception e){
                return BadRequest(JsonSerializer.Serialize(new ErrorsDTO(new ValidationError("GENERAL","Произошла непредвиденная ошибка"))));
            }
        }
    }
    // получение истории студента
    [HttpGet]
    [Route("/studentflow/history/{id:int?}")]
    public async Task<IActionResult> GetHistory(int id)
    {

        var student = await StudentModel.GetStudentById(id);
        if (student is null)
        {
            return BadRequest("такого студента не существует");
        }
        var history = student.History.History;
        List<StudentHistoryMoveDTO> moves = new();
        GroupModel? previous = null;
        foreach (var rec in history)
        {

            moves.Add(new StudentHistoryMoveDTO(
                student,
                rec.GroupTo,
                previous,
                rec.ByOrder
            ));
            previous = rec.GroupTo;

        }
        return Json(moves);
    }
    // удаление истории студента в приказа либо всей истории приказа
    [HttpDelete]
    [Route("/studentflow/revert/{orderId:int}/{studentId:int?}")]
    public IActionResult DeleteFlowRecords(int orderId, int? studentId)
    {
        var order = Order.GetOrderById(orderId);
        if (order is null)
        {
            return BadRequest("Id приказа указан неверно");
        }
        if (studentId is not null)
        {
            var student = StudentModel.GetStudentById(studentId).Result;
            if (student is null)
            {
                return BadRequest("Id студента указан неверно");
            }
            order.RevertConducted(new StudentModel[] { student });
            return Ok();
        }
        order.RevertAllConducted();
        return Ok();
    }
}