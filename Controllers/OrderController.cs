using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using StudentTracking.Models.JSON.Responses;
using StudentTracking.Models.Services;
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
    public IActionResult ProcessSpeciality(string query){
        if (query == "new"){
            return View(@"Views/Modify/OrderModify.cshtml", new FreeEnrollmentOrder()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = OrderConsistencyMaintain.GetByIdTypeIndependent(id);
            if (got!=null){
                return View(@"Views/Modify/OrderModify.cshtml", got);
            }
            else{
                return View(@"Views/Shared/Error.cshtml", "Приказа с таким id не существует");
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
            var deserialized = JsonSerializer.Deserialize<OrderModelJSON>(body);
            var built = await OrderBuilder.Build(deserialized);
            if (built!=null){
                await built.Save(null);
                if (await built.GetCurrentState(null) != RelationTypes.Bound){
                    return Json(built.GetErrors());
                }
                return Json(new {OrderId = built.Id});
            }
            return Json(new object());  
        }
    }
    [HttpPost]
    [Route("/orders/generateIdentity")]
    public async Task<JsonResult> GenerateIndentity(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<OrderModelJSON>(body);
            var built = await OrderBuilder.Build(deserialized);
            if (built!=null){
                return Json(new {built.OrderStringId});
            }
            return Json(new object());  
        }
    }
    [HttpGet]
    [Route("/orders/filter/{query?}")]
    public async Task<JsonResult> Filter(string? query){
        if (query == null || query.Length < 3){
            return Json(new object());
        }
        var mapper = new Mapper<OrderSuggestionJSONResponse>(
            (o, m) => {
                o.OrderId = (int)m["id"];
                o.GroupBehaviour = OrderTypeInfo.GetByType((OrderTypes)(int)m["type"]).GroupBehaviour.ToString();
                var name = (string)m["name"];
                var stringId = (string)m["org_id"];
                o.DisplayedName = OrderSuggestionJSONResponse.FormatOrderName(name, stringId);
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
        var select = new SelectQuery<OrderSuggestionJSONResponse>(
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