using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.JSON;

namespace StudentTracking.Models.Services;

public class OrderBuilder {

    public static async Task<Order?> Build(OrderModelJSON? json){
        OrderTypes typeConverted;
        Order? toReturn = null;
        if (json == null){
            return null;
        }
        try {
            typeConverted = (OrderTypes)json.OrderType;
        }
        catch (InvalidCastException){
            return toReturn;
        }
        
        switch (typeConverted){
            case OrderTypes.FreeEnrollment:
                toReturn = new FreeEnrollmentOrder();
                break;
            case OrderTypes.DeductionWithGraduation:
                toReturn = new DeductionWithGraduationOrder();
                break;
            case OrderTypes.TransferGroupToGroup:
                toReturn = new TransferGroupToGroupOrder();
                break;
            default:
                return null;
        }

        await toReturn.FromJSON(json);
        return toReturn;
    }
}
