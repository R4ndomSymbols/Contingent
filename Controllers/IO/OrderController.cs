using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;

namespace StudentTracking.Controllers;


public class OrderController : Controller{

    private readonly ILogger<OrderController> _logger;

    public OrderController(ILogger<OrderController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    [Route("/orders/modify/{query}")]
    public async Task<IActionResult> ProcessOrder(string query){
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
    [HttpGet]
    [Route("/orders/view/{query}")]
    public async Task<IActionResult> ViewOrder(string query){
        if(int.TryParse(query, out int id)){
            var result = await Order.GetOrderById(id);
            if (result.IsSuccess){
                return View(@"Views/Observe/Order.cshtml", new OrderSearchDTO(result.ResultObject));
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
    public async Task<IActionResult> Save(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var built = await Order.Build(body);
            if (built.IsSuccess){
                var order = built.ResultObject; 
                await order.Save(null);
                return Ok(Json(new {OrderId = order.Id}).Value);
            }
            return BadRequest(Json(new ErrorsDTO(built.Errors)).Value);  
        }
    }
    [HttpPost]
    [Route("/orders/generateIdentity")]
    public async Task<IActionResult> GenerateIndentity(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var result = await Order.Build(body);
            if (result.IsSuccess){
                return Json(new {result.ResultObject.OrderOrgId});
            }
            return BadRequest(Json(new ErrorsDTO(result.Errors)).Value);  
        }
    }

    [HttpGet]
    [Route("/orders/search")]
    public IActionResult GetSearchPage(){
        return View(@"Views/Search/Orders.cshtml", new List<OrderSearchDTO>());
    }
    [HttpPost]
    [Route("/orders/search/find")]
    public async Task<JsonResult> GetOrdersByFilters(){
        using var stream = new StreamReader(Request.Body);
        var parameters = JsonSerializer.Deserialize<OrderSearchParamentersDTO>(await stream.ReadToEndAsync());
        var result = await Order.FindOrders(new QueryLimits(0,20));
        List<OrderSearchDTO> cards = new List<OrderSearchDTO>();
        foreach (Order o in result){
            cards.Add(new OrderSearchDTO(o));
        }
        return Json(cards);
    }
    [HttpGet]
    [Route("/orders/close/{id?}")]
    public async Task<IActionResult> CloseOrder(string id){
        
        if (int.TryParse(id, out int parsed)){
            var result = await Order.GetOrderById(parsed);
            if (result.IsFailure){
                return BadRequest(new ErrorsDTO(result.Errors));
            }
            await result.ResultObject.Close();
            return Ok();
        }
        
        return BadRequest(new ErrorsDTO(new ValidationError("Такого приказа не существует"))); 
    }
}