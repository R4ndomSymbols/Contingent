using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class OrderResponseDTO {

    [JsonInclude]
    public string DisplayedName {
        get => _orderName + $" ({_orderOrgId})"; 
    }
    [JsonIgnore]
    private string _orderName;
    [JsonIgnore]
    private string _orderOrgId; 
    public int OrderId {get; set; }
    public string GroupBehaviour {get; set; }
    public OrderResponseDTO(string orderName, string orderOrgId){
        _orderName = orderName;
        _orderOrgId = orderOrgId;
    }

}