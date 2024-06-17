using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;

namespace Contingent.Controllers.Search;

public class OrderSearchController : Controller
{

    public OrderSearchController()
    {

    }

    [HttpGet]
    [Route("/orders/search")]
    public IActionResult GetMainPage()
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/orders/search",
            RequestType = "GET",
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/orders/search")]
    public IActionResult GetMainPageProtected()
    {
        return View(@"Views/Search/Orders.cshtml", new List<OrderSearchDTO>());
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
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
        if (dto.Type != -1)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(
                new WhereCondition(
                    new Column("type", "orders"),
                    parameters.Add(dto.Type),
                    WhereCondition.Relations.Equal
                )));
        }
        if (dto.Year != -1)
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                    new ComplexWhereCondition(
                        new WhereCondition(
                            new Column("specified_date", "orders"),
                            parameters.Add(new DateTime(dto.Year, 1, 1)),
                            WhereCondition.Relations.BiggerOrEqual
                        ),
                        new WhereCondition(
                            new Column("specified_date", "orders"),
                            parameters.Add(new DateTime(dto.Year, 12, 31)),
                            WhereCondition.Relations.LessOrEqual
                        ),
                        ComplexWhereCondition.ConditionRelation.AND,
                        true
                    )
                );
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
