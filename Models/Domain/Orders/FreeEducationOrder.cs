using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Net.Http.Headers;
using StudentTracking.Models.Services;
using StudentTracking.Models.Domain.Orders.OrderData;


namespace StudentTracking.Models.Domain.Orders;

public abstract class FreeEducationOrder : Order
{
    public override string OrderStringId {
        get => _orderNumber + "-" + "к";
    }
    
    protected FreeEducationOrder() : base()
    {

    }
    protected FreeEducationOrder(int id) : base(id){

    }
    
    public override async Task FromJSON(OrderModelJSON json){
        await base.FromJSON(json);
    }
}
