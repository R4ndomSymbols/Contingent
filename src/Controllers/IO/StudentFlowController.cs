using Microsoft.AspNetCore.Mvc;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Controllers.DTO.Out;
using System.Text.Json;
using System.Transactions;
using Contingent.Utilities;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;
namespace Contingent.Controllers;
public class StudentFlowController : Controller
{

    private readonly ILogger<StudentFlowController> _logger;

    public StudentFlowController(ILogger<StudentFlowController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/studentflow/{id:int}")]
    public IActionResult GetMainPage(int id)
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/studentflow/" + id,
            RequestType = "GET",
        });
    }

    // страница проведения приказов
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/studentflow/{id:int}")]
    public IActionResult GetMainPageProtected(int id)
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
    [Authorize(Roles = "Admin")]
    [Route("/studentflow/save/{id?}")]
    public async Task<IActionResult> SaveFlowChanges(string id)
    {
        bool parsed = int.TryParse(id, out int orderId);
        if (!parsed)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("неверный номер приказа"));
        }
        using var transaction = ObservableTransaction.New;
        using var stream = new StreamReader(Request.Body);
        var jsonString = await stream.ReadToEndAsync();
        var result = await Order.GetOrderForConduction(orderId, jsonString);
        if (result.IsSuccess)
        {
            var conductionStatus = result.ResultObject.ConductByOrder(transaction);
            if (conductionStatus.IsSuccess)
            {
                transaction.Commit();
                return Ok();
            }
            else
            {
                transaction.Rollback();
                return BadRequest(conductionStatus.Errors.AsErrorCollection());
            }

        }
        else
        {
            transaction.Rollback();
            return BadRequest(result.Errors.AsErrorCollection());
        }
    }
    // получение истории студента
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/studentflow/history/{id:int?}")]
    public IActionResult GetHistory(int id)
    {
        var student = StudentModel.GetStudentById(id);
        if (student is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("такого студента не существует"));
        }
        using var transaction = ObservableTransaction.New;
        var history = student.GetHistory(transaction).History;
        List<StudentHistoryMoveDTO> moves = new();
        GroupModel? previous = null;
        foreach (var rec in history)
        {
            moves.Add(new StudentHistoryMoveDTO(
                student,
                rec.GroupTo,
                previous,
                rec.OrderNullRestrict,
                rec.StatePeriod
            ));
            previous = rec.GroupTo;

        }
        transaction.Commit();
        return Json(moves);
    }
    // удаление истории студента в приказа либо всей истории приказа
    [HttpDelete]
    [Authorize(Roles = "Admin")]
    [Route("/studentflow/revert/{orderId:int}/{studentId:int?}")]
    public IActionResult DeleteFlowRecords(int orderId, int? studentId)
    {
        var order = Order.GetOrderById(orderId);
        if (order is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Id приказа указан неверно"));
        }
        using var transaction = ObservableTransaction.New;
        if (studentId is not null)
        {
            // удаление одного студента
            // если приказ помечен как запрещенный на удаление - удаляется последняя запись
            // по факту, явления тождественные
            var student = StudentModel.GetStudentById(studentId);
            if (student is null)
            {
                transaction.Rollback();
                return BadRequest(ErrorCollectionDTO.GetGeneralError("Id студента указан неверно"));
            }
            var result = order.RevertConducted(new StudentModel[] { student }, transaction);
            if (result.IsFailure)
            {
                transaction.Rollback();
                return BadRequest(result.Errors.AsErrorCollection());
            }
            transaction.Commit();
            return Ok();
        }
        order.RevertAllConducted(transaction);
        transaction.Commit();
        return Ok();
    }
}