using System.Text.Json.Serialization;
using Contingent.Import;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class OrderDTO : IFromCSV<OrderDTO>
{
    public const string OrderNameFieldName = "название приказа";
    public const string SpecifiedDateFieldName = "дата приказа";
    public const string EffectiveDateFieldName = "дата вступления в силу";
    public const string OrderDescriptionFieldName = "описание приказа";
    public const string OrderTypeFieldName = "тип приказа";


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
        OrderDisplayedName = row[OrderNameFieldName];
        SpecifiedDate = row[SpecifiedDateFieldName]!;
        EffectiveDate = row[EffectiveDateFieldName]!;
        OrderDescription = row[OrderDescriptionFieldName];
        OrderType = OrderTypeInfo.ImportOrderType(row[OrderTypeFieldName]);
        return Result<OrderDTO>.Success(this);
    }
}

