using StudentTracking.Models.Domain.Orders;
using Utilities;

namespace StudentTracking.Controllers.DTO.Out;

public class OrderCardResponseDTO {

    public int OrderId {get; set;}
    public string OrderFullName {get; set; }
    public string OrderModifyLink {get; set; }
    public string OrderTypeName {get; set; }
    public string OrderEffectiveDate {get; set; }
    public string OrderSpecifiedDate {get; set; }
    public string OrderOrgId {get; set; }

    public OrderCardResponseDTO(Order order){
        OrderId = order.Id;
        OrderFullName = order.OrderDisplayedName;
        OrderOrgId = order.OrderOrgId;
        OrderModifyLink = "/orders/modify/" + OrderId.ToString();
        var details = order.GetOrderTypeDetails();
        OrderTypeName = details.OrderTypeName;
        OrderEffectiveDate = Utils.FormatDateTime(order.EffectiveDate);
        OrderSpecifiedDate = Utils.FormatDateTime(order.SpecifiedDate);

    }


}
