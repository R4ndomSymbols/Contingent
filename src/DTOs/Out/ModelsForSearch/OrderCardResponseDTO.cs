using Contingent.Models.Domain.Orders;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class OrderSearchDTO
{

    public int OrderId { get; set; }
    public string OrderFullName { get; set; }
    public string? OrderModifyLink { get; set; }
    public string? OrderFlowLink { get; set; }
    public string OrderViewLink { get; set; }
    public string? OrderCloseLink { get; set; }
    public string OrderRussianTypeName { get; set; }
    public string OrderEnumTypeName { get; set; }
    public string OrderEffectiveDate { get; set; }
    public string OrderSpecifiedDate { get; set; }
    public string OrderOrgId { get; set; }
    public string OrderInputPolicy { get; init; }
    public bool IsClosed { get; set; }
    public string OrderDescription { get; set; }
    public int OrderType { get; set; }

    public OrderSearchDTO(Order order)
    {
        OrderId = order.Id;
        OrderInputPolicy = order.GetOrderTypeDetails().FrontendGroupBehavior.ToString();
        IsClosed = order.IsClosedForDeletion;
        if (!IsClosed)
        {
            OrderModifyLink = "/orders/modify/" + OrderId.ToString();
            OrderCloseLink = "/orders/close/" + OrderId.ToString();
        }
        else
        {
            OrderModifyLink = null;
            OrderCloseLink = null;
        }
        OrderFlowLink = "/studentflow/" + OrderId.ToString();
        OrderDescription = order.OrderDescription;
        OrderViewLink = "/orders/view/" + OrderId.ToString();
        OrderFullName = order.OrderDisplayedName;
        OrderOrgId = order.OrderOrgId;
        var details = order.GetOrderTypeDetails();
        OrderRussianTypeName = details.OrderTypeName;
        OrderEnumTypeName = details.Type.ToString();
        OrderEffectiveDate = Utils.FormatDateTime(order.EffectiveDate);
        OrderSpecifiedDate = Utils.FormatDateTime(order.SpecifiedDate);
        OrderType = (int)details.Type;
    }


}
