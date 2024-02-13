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
        Vipe = 3,
        Undefined = 4, 
    }

    private static IReadOnlyCollection<OrderTypeInfo> _types = new OrderTypeInfo[]{

        new(
            OrderTypes.FreeEnrollment,
            "Зачисление на первый курс (бюджет)",
            GroupDisplayBehaviour.MustChange
        ),
        new (
            OrderTypes.FreeDeductionWithGraduation,
            "Отчисление в связи с выпуском (бюджет)",
            GroupDisplayBehaviour.Vipe
        ),
        new (
            OrderTypes.FreeNextCourseTransfer,
            "Перевод на следующий курс (бюджет)",
            GroupDisplayBehaviour.MustChange
        ),
        new (
            OrderTypes.EmptyOrder,
            "Не указано",
            GroupDisplayBehaviour.Undefined
        ),
    };



    public static OrderTypeInfo GetByType(OrderTypes type)
    {   
        var found = _types.Where(x => x.Type == type); 
        if (found.Any()){
            return found.First();
        }
        else {
            throw new ArgumentException("приказ типа " + type.ToString() + " не зарегистрирован");
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

    public bool IsAnyEnrollment()
    {
        return Type == OrderTypes.FreeEnrollment;
    }
    public bool IsAnyDeduction(){
        return Type == OrderTypes.FreeDeductionWithGraduation 
            || Type == OrderTypes.FreeDeductionWithOwnDesire;
    } 

}
