using System.Text.Json.Serialization;
using Contingent.Import;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class OrderDTO : IFromCSV<OrderDTO>
{
    [JsonRequired]
    public string SpecifiedDate { get; set; }
    [JsonRequired]
    public string EffectiveDate { get; set; }
    [JsonRequired]
    public string? OrderDescription { get; set; }
    [JsonRequired]
    public string? OrderDisplayedName { get; set; }
    [JsonRequired]
    public int OrderType { get; set; }

    public OrderDTO()
    {
        SpecifiedDate = "";
        EffectiveDate = "";
        OrderDescription = "";
        OrderType = (int)OrderTypes.EmptyOrder;
    }

    public Result<OrderDTO> MapFromCSV(CSVRow row)
    {
        OrderDisplayedName = row["название приказа"];
        SpecifiedDate = row["дата приказа"]!;
        EffectiveDate = row["дата вступления в силу"]!;
        OrderDescription = row["описание приказа"];
        OrderType = OrderTypeInfo.ImportOrderType(row["тип приказа"]);
        return Result<OrderDTO>.Success(this);
    }
}

