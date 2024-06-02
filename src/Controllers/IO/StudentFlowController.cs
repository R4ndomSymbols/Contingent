using Microsoft.AspNetCore.Mvc;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Controllers.DTO.Out;
using System.Text.Json;
namespace Contingent.Controllers;
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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("неверный номер приказа"));
        }
        using (var stream = new StreamReader(Request.Body))
        {
            var jsonString = await stream.ReadToEndAsync();
            var result = await Order.GetOrderForConduction(orderId, jsonString);
            if (result.IsSuccess)
            {
                var conductionStatus = result.ResultObject.ConductByOrder();
                if (conductionStatus.IsSuccess)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest(conductionStatus.Errors.AsErrorCollection());
                }

            }
            else
            {
                return BadRequest(result.Errors.AsErrorCollection());
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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("такого студента не существует"));
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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Id приказа указан неверно"));
        }
        if (studentId is not null)
        {
            var student = StudentModel.GetStudentById(studentId).Result;
            if (student is null)
            {
                return BadRequest(ErrorCollectionDTO.GetGeneralError("Id студента указан неверно"));
            }
            order.RevertConducted(new StudentModel[] { student });
            return Ok();
        }
        order.RevertAllConducted();
        return Ok();
    }
}