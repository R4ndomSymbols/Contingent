using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Models.JSON.Responses;

[Serializable]
public class OrderSuggestionJSONResponse {

    public string DisplayedName {get; set; } 
    public int OrderId {get; set; }
    public string GroupBehaviour {get; set; }

    public static string FormatOrderName(string name, string stringId){
        return name + $" ({stringId})"; 
    }

    public OrderSuggestionJSONResponse(){
        DisplayedName = "";
        GroupBehaviour = "";
    }

}