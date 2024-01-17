using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.SQL;
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
    [Route("/orders/filter/{query?}")]
    public async Task<JsonResult> Filter(string? query){
        if (query == null || query.Length < 3){
            return Json(new object());
        }
        var mapper = new Mapper<OrderResponseDTO>(
            (o, m) => {
                o.OrderId = (int)m["id"];
                o.GroupBehaviour = OrderTypeInfo.GetByType((OrderTypes)(int)m["type"]).GroupBehaviour.ToString();
                var name = (string)m["name"];
                var stringId = (string)m["org_id"];
                o.DisplayedName = OrderResponseDTO.FormatOrderName(name, stringId);
            },
            new List<Column>(){
                new Column("id", null, "orders"),
                new Column("type", null, "orders"),
                new Column("name", null, "orders"),
                new Column("org_id", null, "orders")
            }   
        );
        var par = new SQLParameters();
        var where1 = new WhereCondition(
            par,
            new Column("name", null, "orders"),
            new SQLParameter<string>("%" + query + "%"),
            WhereCondition.Relations.Like
        );
        var where2 = new WhereCondition(
            par,
            new Column("org_id", null, "orders"),
            new SQLParameter<string>("%" + query + "%"),
            WhereCondition.Relations.Like
        );
        var select = new SelectQuery<OrderResponseDTO>(
            "orders",
            par,
            mapper,
            null,
            new ComplexWhereCondition(where1, where2, ComplexWhereCondition.ConditionRelation.OR, false)
        );

        
        var found = await Order.FindOrders(select);
        if (found == null){
            return Json(new object());
        }
        else {
            return Json(found);
        }
    }
}