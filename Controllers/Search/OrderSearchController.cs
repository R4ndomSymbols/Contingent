using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Infrastruture;
using StudentTracking.SQL;

namespace StudentTracking.Controllers.Search;

public class OrderSearchController : Controller {

    public OrderSearchController(){

    } 

    [HttpGet]
    [Route("/orders/search")]
    public IActionResult GetSearchPage(){
        return View(@"Views/Search/Orders.cshtml", new List<OrderSearchDTO>());
    }
    [HttpPost]
    [Route("/orders/search/find")]
    public async Task<IActionResult> GetOrdersByFilters(){
        using var stream = new StreamReader(Request.Body);
        OrderSearchParamentersDTO? dto = null;
        try{
            dto = JsonSerializer.Deserialize<OrderSearchParamentersDTO>(await stream.ReadToEndAsync());
        }
        catch {
            return BadRequest("Неверный поисковой запрос");
        }
        var orderBy = new OrderByCondition(
            new Column("id","orders"), OrderByCondition.OrderByTypes.ASC
        );
        List<OrderSearchDTO> found = new List<OrderSearchDTO>();
        var filter = new SearchHelper().GetFilterForOrder(dto);
        int totalOffset = 0;
        int pageSize = 20;
        while (found.Count < pageSize){
            var chunk = await Order.FindOrders(
                new QueryLimits(0, pageSize, totalOffset),
                orderBy: orderBy
            );
            totalOffset+=chunk.Count;
            found.AddRange(filter.Execute(chunk).Select(x => new OrderSearchDTO(x)));
            if (chunk.Count < pageSize){
                break;
            }
        }
        return Json(found);
    }

}
