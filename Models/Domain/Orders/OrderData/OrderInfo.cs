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

    public static OrderTypeInfo? GetByType(OrderTypes type){

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
                    _orderTypeDisplayedName = "Отчисление (бюджет)",
                    _type = OrderTypes.FreeDeductionWithGraduation
                };
            case OrderTypes.FreeTransferGroupToGroup:
                return new OrderTypeInfo(){
                    _frontendGroupBehaviour = GroupDisplayBehaviour.MustChange,
                    _orderTypeDisplayedName = "Перевод (бюджет)",
                    _type = OrderTypes.FreeTransferGroupToGroup
                };
            default:
                return null;
        }  
    }

    public static IEnumerable<OrderTypeInfo> GetAllTypes(){
        var result = new List<OrderTypeInfo>();
        // метод получает не все типы, а только те, для которых
        // на данный момент написаны обработчики
        // исключений не выбрасывает
        foreach (int t in Enum.GetValues(typeof(OrderTypes))){
            var got = GetByType((OrderTypes)t);
            if (got!=null){
                result.Add(got);
            }
        }
        return result;
    }

    public static bool IsAnyEnrollment(OrderTypes type){
        return type == OrderTypes.FreeEnrollment;

    }

}
