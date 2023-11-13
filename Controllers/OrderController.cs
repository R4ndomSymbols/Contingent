using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;

namespace StudentTracking.Controllers;


public class OrderController : Controller{

    private readonly ILogger<OrderController> _logger;

    public OrderController(ILogger<OrderController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("/orders/order/{query}")]
    public IActionResult ProcessSpeciality(string query){
        if (query == "new"){
            return View(@"Views/Models/Order.cshtml", new OrderModel()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = OrderModel.GetById(id);
            if (got!=null){
                return View(@"Views/Models/Order.cshtml", got);
            }
            else{
                return View(@"Views/Shared/Error.cshtml", "Приказа с таким id не существует");
            }
            
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id приказа");
        }
    }


    [HttpGet]
    [Route("/orders/types")]
    public JsonResult GetTypes(){
        
        var got = OrderModel.GetAllTypes();
        if (got!=null){
            return Json(got.ToArray());
        }
        return Json(null);
    }
    
    [HttpPost]
    [Route("/orders/save")]
    public async Task<JsonResult> Save(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<OrderModel>(body);
            int result = -1;
            if (deserialized!=null){
                result = OrderModel.CreateOrUpdateOrder(deserialized);
            }
            return Json(result);
        }
    }
    [HttpGet]
    [Route("/orders/filter/{query?}")]
    public JsonResult FilterOrders(string? query){
        if (query == null){
            return Json(null);
        
        }
        else{
            var found = OrderModel.FindOrdersByOrgId(query);
            if (found == null){
                return Json(null);
            }
            return Json(found.ToArray());
        }
    }
}