namespace StudentTracking.Models.Domain.Orders.OrderData;

public class OrderTypeInfo
{
    private GroupDisplayBehaviour _frontendGroupBehaviour;
    private string _orderTypeDisplayedName;
    private OrderTypes _type;
    public GroupDisplayBehaviour GroupBehaviour {
        get => _frontendGroupBehaviour;
    }
    public string OrderTypeDisplayedName {
        get => _orderTypeDisplayedName;
    }
    public OrderTypes Type {
        get => _type;
    }

    private OrderTypeInfo(){
        _orderTypeDisplayedName = "";
    }

    public enum GroupDisplayBehaviour
    {
        NoChange = 1,
        MustChange = 2,
        Vipe = 3
    }

    public static OrderTypeInfo GetByType(OrderTypes type){
        switch (type) {
            case OrderTypes.FreeEnrollment:
                return new OrderTypeInfo(){
                    _frontendGroupBehaviour = GroupDisplayBehaviour.MustChange,
                    _orderTypeDisplayedName = "Зачисление (бюджет)",
                    _type = OrderTypes.FreeEnrollment
                };
            case OrderTypes.FreeDeductionWithGraduation:
                return new OrderTypeInfo(){
                    _frontendGroupBehaviour = GroupDisplayBehaviour.Vipe,
                    _orderTypeDisplayedName = "Отчисление (внебюджет)",
                    _type = OrderTypes.FreeDeductionWithGraduation
                };
            default:
                throw new InvalidOperationException("Указаный тип приказа не зарегистрирован");
        }  
    }

    public static IEnumerable<OrderTypeInfo> GetAllTypes(){
        var result = new List<OrderTypeInfo>();
        foreach (int t in Enum.GetValues(typeof(OrderTypes))){
            result.Add(GetByType((OrderTypes)t));
        }
        return result;
    }

    public static bool IsAnyEnrollment(OrderTypes type){
        return type == OrderTypes.FreeEnrollment;

    }

}
