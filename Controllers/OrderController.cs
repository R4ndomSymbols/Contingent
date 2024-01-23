using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.SQL;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Controllers;


public class OrderController : Controller{

    private readonly ILogger<OrderController> _logger;

    public OrderController(ILogger<OrderController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    [Route("/orders/modify/{query}")]
    public async Task<IActionResult> ProcessSpeciality(string query){
        if (query == "new"){
            return View(@"Views/Modify/OrderModify.cshtml", EmptyOrder.Empty); 
        }
        else if(int.TryParse(query, out int id)){
            var result = await Order.GetOrderById(id);
            if (result.IsSuccess){
                return View(@"Views/Modify/OrderModify.cshtml", result.ResultObject);
            }
            else{
                return View(@"Views/Shared/Error.cshtml", result.Errors?.First()?.ToString() ?? "");
            }
            
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id приказа");
        }
    }
    
    [HttpPost]
    [Route("/orders/save")]
    public async Task<JsonResult> Save(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var built = await Order.Build(body);
            if (built.IsSuccess){
                var order = built.ResultObject; 
                await order.Save(null);
                return Json(new {OrderId = order.Id});
            }
            return Json(new object());  
        }
    }
    [HttpPost]
    [Route("/orders/generateIdentity")]
    public async Task<JsonResult> GenerateIndentity(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var result = await Order.Build(body);
            if (result.IsSuccess){
                return Json(new {result.ResultObject.OrderOrgId});
            }
            Console.WriteLine(result.Errors?.ErrorsToString() ?? "");
            return Json(new object());  
        }
    }

    [HttpGet]
    [Route("/orders/search")]
    public IActionResult GetSearchPage(){
        return View(@"Views/Search/Orders.cshtml", new List<OrderCardResponseDTO>());
    }
    [HttpPost]
    [Route("/orders/search/find")]
    public async Task<JsonResult> GetOrdersByFilters(){
        using var stream = new StreamReader(Request.Body);
        var parameters = JsonSerializer.Deserialize<OrderSearchParamentersDTO>(await stream.ReadToEndAsync());
        var result = await Order.FindOrders(new QueryLimits(0,20));
        List<OrderCardResponseDTO> cards = new List<OrderCardResponseDTO>();
        foreach (Order o in result){
            cards.Add(new OrderCardResponseDTO(o));
        }
        return Json(cards);
    }
    [HttpGet]
    [Route("/orders/close/{id?}")]
    public async Task<IActionResult> CloseOrder(){
        return BadRequest();
    }
}