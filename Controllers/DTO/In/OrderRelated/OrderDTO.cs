using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class OrderDTO {
    [JsonRequired]
    public string SpecifiedDate {get; set; }
    [JsonRequired]
    public string EffectiveDate {get; set; }
    [JsonRequired]
    public string? OrderDescription {get; set; }
    [JsonRequired]
    public string? OrderDisplayedName {get; set; }
    [JsonRequired]
    public int OrderType {get; set; }

    public OrderDTO(){
        SpecifiedDate = "";
        EffectiveDate = "";
        OrderDescription = "";
        OrderType = (int)OrderTypes.EmptyOrder;
    }

}

