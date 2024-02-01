namespace StudentTracking.Models.Domain.Orders.OrderData;

public class OrderTypeInfo
{
    public readonly GroupDisplayBehaviour FrontendGroupBehaviour;
    public readonly string OrderTypeName;
    public readonly OrderTypes Type;
    public OrderTypeInfo(OrderTypes orderType, string orderTypeName, GroupDisplayBehaviour behaviour)
    {
        OrderTypeName = orderTypeName;
        Type = orderType;
        FrontendGroupBehaviour = behaviour;
    }

    public enum GroupDisplayBehaviour
    {
        NoChange = 1,
        MustChange = 2,
        Vipe = 3
    }

    public static OrderTypeInfo GetByType(OrderTypes type)
    {

        switch (type)
        {
            case OrderTypes.FreeEnrollment:
                return new OrderTypeInfo(
                    OrderTypes.FreeEnrollment,
                    "Зачисление (бюджет)",
                    GroupDisplayBehaviour.MustChange);
            case OrderTypes.FreeDeductionWithGraduation:
                return new OrderTypeInfo(
                    OrderTypes.FreeDeductionWithGraduation,
                    "Отчисление (бюджет)",
                    GroupDisplayBehaviour.Vipe);
            case OrderTypes.FreeTransferGroupToGroup:
                return new OrderTypeInfo(
                    OrderTypes.FreeTransferGroupToGroup,
                    "Перевод внутри колледжа (бюджет)",
                    GroupDisplayBehaviour.MustChange);
            default:
                throw new Exception("Такой тип приказа не зарегистрирован");
        }
    }

    public static IEnumerable<OrderTypeInfo> GetAllTypes()
    {
        var result = new List<OrderTypeInfo>();
        // метод получает не все типы, а только те, для которых
        // на данный момент написаны обработчики
        foreach (int t in Enum.GetValues(typeof(OrderTypes)))
        {   
            try {
                var got = GetByType((OrderTypes)t);
                result.Add(got);
            }
            catch {
                continue;
            }

        }
        return result;
    }

    public static bool IsAnyEnrollment(OrderTypes type)
    {
        return type == OrderTypes.FreeEnrollment;
    }
    public bool IsAnyDeduction(){
        return Type == OrderTypes.FreeDeductionWithGraduation;
    } 

}
