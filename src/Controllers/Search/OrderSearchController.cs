using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Infrastructure;
using Contingent.SQL;
using Utilities;

namespace Contingent.Controllers.Search;

public class OrderSearchController : Controller
{

    public OrderSearchController()
    {

    }

    [HttpGet]
    [Route("/orders/search")]
    public IActionResult GetSearchPage()
    {
        return View(@"Views/Search/Orders.cshtml", new List<OrderSearchDTO>());
    }
    [HttpPost]
    [Route("/orders/search/find")]
    public async Task<IActionResult> GetOrdersByFilters()
    {
        using var stream = new StreamReader(Request.Body);
        OrderSearchParamentersDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<OrderSearchParamentersDTO>(await stream.ReadToEndAsync());
        }
        catch
        {
            return BadRequest("Неверный поисковой запрос");
        }
        if (dto is null)
        {
            return BadRequest("Неверный поисковой запрос");
        }
        var orderBy = new OrderByCondition(
            new Column("id", "orders"), OrderByCondition.OrderByTypes.ASC
        );
        float tableSize = 0;
        using (var conn = Utils.GetAndOpenConnectionFactory().Result)
        {
            var cmd = new NpgsqlCommand("SELECT reltuples AS estimate FROM pg_class WHERE relname = \'orders\'", conn);
            using (cmd)
            {
                using var reader = cmd.ExecuteReader();
                reader.Read();
                tableSize = (float)reader["estimate"];
            }
        }
        List<OrderSearchDTO> found = new List<OrderSearchDTO>();
        var filter = new SearchHelper().GetFilterForOrder(dto);
        int page = 0;
        int offset = 0;
        while (found.Count < dto.PageSize && offset < tableSize)
        {
            offset = page * dto.PageSize;
            found.AddRange(
                filter.Execute(
                    Order.FindOrders(new QueryLimits(page, dto.PageSize),
                    orderBy: orderBy
            ).Result).Select(x => new OrderSearchDTO(x)));
            page++;

        }
        return Json(found);
    }

}
