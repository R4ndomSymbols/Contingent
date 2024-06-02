using Contingent.Models.Domain.Orders;
using Utilities;

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
    public string OrderGroupChangePolicy { get; init; }
    public bool IsClosed { get; set; }
    public string OrderDescription { get; set; }

    public OrderSearchDTO(Order order)
    {
        OrderId = order.Id;
        OrderGroupChangePolicy = order.GetOrderTypeDetails().FrontendGroupBehaviour.ToString();
        IsClosed = order.IsClosed;
        if (!IsClosed)
        {
            OrderFlowLink = "/studentflow/" + OrderId.ToString();
            OrderModifyLink = "/orders/modify/" + OrderId.ToString();
            OrderCloseLink = "/orders/close/" + OrderId.ToString();
        }
        else
        {
            OrderFlowLink = null;
            OrderModifyLink = null;
            OrderCloseLink = null;
        }
        OrderDescription = order.OrderDescription;
        OrderViewLink = "/orders/view/" + OrderId.ToString();
        OrderFullName = order.OrderDisplayedName;
        OrderOrgId = order.OrderOrgId;
        var details = order.GetOrderTypeDetails();
        OrderRussianTypeName = details.OrderTypeName;
        OrderEnumTypeName = details.Type.ToString();
        OrderEffectiveDate = Utils.FormatDateTime(order.EffectiveDate);
        OrderSpecifiedDate = Utils.FormatDateTime(order.SpecifiedDate);

    }


}
