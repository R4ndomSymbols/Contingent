using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class OrderResponseDTO {

    public string DisplayedName {get; set; } 
    public int OrderId {get; set; }
    public string GroupBehaviour {get; set; }

    public static string FormatOrderName(string name, string stringId){
        return name + $" ({stringId})"; 
    }

    public OrderResponseDTO(){
        DisplayedName = "";
        GroupBehaviour = "";
    }

}