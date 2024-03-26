namespace StudentTracking.Controllers.DTO.In;

public class OrderSearchParamentersDTO {
    public string SearchText {get; set;}
    public int? Year {get; set;}
    public int? Type {get; set;}

    public OrderSearchParamentersDTO(){
        SearchText = "";
    }
}
