using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Infrastructure;
using Contingent.SQL;
using Contingent.Utilities;

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
        OrderSearchParametersDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<OrderSearchParametersDTO>(await stream.ReadToEndAsync());
        }
        catch
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос"));
        }
        if (dto is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос"));
        }
        var orderBy = new OrderByCondition(
            new Column("id", "orders"), OrderByCondition.OrderByTypes.ASC
        );
        var where = ComplexWhereCondition.Empty;
        var parameters = new SQLParameterCollection();
        if (!string.IsNullOrEmpty(dto.SearchText) && !string.IsNullOrWhiteSpace(dto.SearchText))
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.OR,
                new ComplexWhereCondition(
                new WhereCondition(
                    new Column("lower", "name", "orders", null),
                    parameters.Add("%" + dto.SearchText.ToLower() + "%"),
                    WhereCondition.Relations.Like
                )));
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.OR,
                new ComplexWhereCondition(
                new WhereCondition(
                    new Column("lower", "org_id", "orders", null),
                    parameters.Add(dto.SearchText.ToLower()),
                    WhereCondition.Relations.Like
                )));
        }

        var found = Order.FindOrders(
            new QueryLimits(0, dto.PageSize),
            where,
            null, parameters,
            orderBy
        ).Result;

        return Json(found.Select(x => new OrderSearchDTO(x)));
    }

}
