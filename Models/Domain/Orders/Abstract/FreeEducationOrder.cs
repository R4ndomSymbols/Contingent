using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Net.Http.Headers;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Controllers.DTO.In;


namespace StudentTracking.Models.Domain.Orders;

public abstract class FreeEducationOrder : Order
{
    public override string OrderOrgId {
        get => _orderNumber + "-" + "к";
    }
    
    protected FreeEducationOrder() : base()
    {

    }
    protected FreeEducationOrder(int id) : base(id)
    {

    }
}
