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
    [Route("/orders/filter/{query?}")]
    public async Task<JsonResult> Filter(string? query){
        if (query == null || query.Length < 3){
            return Json(new object());
        }
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var mapper = new Mapper<OrderResponseDTO>(
            (m) => {
                var o = new OrderResponseDTO((string)m["name"], (string)m["org_id"]);
                o.OrderId = (int)m["id"];
                o.GroupBehaviour = OrderTypeInfo.GetByType((OrderTypes)(int)m["type"]).FrontendGroupBehaviour.ToString();
                return new Task<OrderResponseDTO>(() => o);
            },
            new List<Column>(){
                new Column("id", null, "orders"),
                new Column("type", null, "orders"),
                new Column("name", null, "orders"),
                new Column("org_id", null, "orders")
            }   
        );
        var par = new SQLParameterCollection();
        var whereParam = par.Add("%" + query + "%");
        var where1 = new WhereCondition(
            new Column("name", "orders"),
            whereParam,
            WhereCondition.Relations.Like
        );
        var where2 = new WhereCondition(
            new Column("org_id", "orders"),
            whereParam,
            WhereCondition.Relations.Like
        );
        var result = SelectQuery<OrderResponseDTO>.Init("orders")
        .AddMapper(mapper)
        .AddWhereStatement(new ComplexWhereCondition(where1, where2, ComplexWhereCondition.ConditionRelation.OR, false))
        .AddParameters(par)
        .Finish();

        if (result.IsFailure){
            throw new Exception("Неверно сформирован запрос SQL");
        }
        var found = await result.ResultObject.Execute(conn, new QueryLimits(0, 20));
        if (found == null){
            return Json(new object());
        }
        else {
            return Json(found);
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
        var result = await Order.FindOrders(null, null, null, new QueryLimits(0,20));
        List<OrderCardResponseDTO> cards = new List<OrderCardResponseDTO>();
        if (result!=null){
            foreach (Order o in result){
                cards.Add(new OrderCardResponseDTO(o));
            }
        }
        return Json(cards);
    }
}