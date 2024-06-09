using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Flow.History;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Contingent.Utilities;

namespace Contingent.Controllers;


public class OrderController : Controller
{

    private readonly ILogger<OrderController> _logger;

    public OrderController(ILogger<OrderController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("/orders/modify/{query}")]
    public IActionResult ProcessOrder(string query)
    {
        if (query == "new")
        {
            return View(@"Views/Modify/OrderModify.cshtml", EmptyOrder.Empty);
        }
        else if (int.TryParse(query, out int id))
        {
            var order = Order.GetOrderById(id);
            if (order is not null)
            {
                return View(@"Views/Modify/OrderModify.cshtml", order);
            }
            else
            {
                return View(@"Views/Shared/Error.cshtml", "Приказа не существует");
            }

        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id приказа");
        }
    }
    [HttpGet]
    [Route("/orders/view/{id:int?}")]
    public IActionResult ViewOrder(int id)
    {
        var order = Order.GetOrderById(id);
        if (order is not null)
        {
            return View(@"Views/Observe/Order.cshtml", new OrderSearchDTO(order));
        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Такого приказа не существует");
        }
    }

    [HttpPost]
    [Route("/orders/save")]
    public async Task<IActionResult> Save()
    {
        using (var reader = new StreamReader(Request.Body))
        {
            var body = await reader.ReadToEndAsync();
            var built = Order.Build(body);
            if (built.IsSuccess && built.ResultObject is not null)
            {
                using var transaction = ObservableTransaction.New;
                var order = built.ResultObject;
                order.Save(transaction);
                await transaction.CommitAsync();
                return Json(new OrderSearchDTO(order));
            }
            return BadRequest(new ErrorCollectionDTO(built.Errors));
        }
    }
    [HttpPost]
    [Route("/orders/generateIdentity")]
    public async Task<IActionResult> GenerateIndentity()
    {
        using (var reader = new StreamReader(Request.Body))
        {
            var body = await reader.ReadToEndAsync();
            var result = Order.Build(body);
            if (result.IsSuccess)
            {
                return Json(new { result.ResultObject.OrderOrgId });
            }
            return BadRequest(new ErrorCollectionDTO(result.Errors));
        }
    }

    [HttpGet]
    [Route("/orders/close/{id:int?}")]
    public IActionResult CloseOrder(int? id)
    {

        var order = Order.GetOrderById(id);
        if (order is null)
        {
            return BadRequest("Неверно указан id приказа");
        }
        var transaction = ObservableTransaction.New;
        order.Close(transaction);
        transaction.Commit();
        return Ok();
    }
    [HttpGet]
    [Route("/orders/history/{id?}")]
    public IActionResult GetStudentsInOrder(int? id)
    {
        var order = Order.GetOrderById(id);
        if (order is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Несуществующий приказ"));
        };
        var found = new OrderHistory(order);
        using var transaction = ObservableTransaction.New;
        var studentMovesHistoryRecords = new List<StudentHistoryMoveDTO>();
        foreach (var record in found.History)
        {
            var history = record.StudentNullRestrict.GetHistory(transaction);
            var byAnchor = history.GetByOrder(order);
            var previous = history.GetClosestBefore(order);
            studentMovesHistoryRecords.Add(new StudentHistoryMoveDTO(record.StudentNullRestrict, byAnchor?.GroupTo, previous?.GroupTo, order));
        }
        return Json(studentMovesHistoryRecords);
    }



}