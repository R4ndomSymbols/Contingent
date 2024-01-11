using Npgsql;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Orders;

public class DeductionWithGraduationOrder : Order
{
    public DeductionWithGraduationOrder() :  base(){

        
    }
    protected DeductionWithGraduationOrder(int id) : base(id) {

    }
    public DeductionWithGraduationOrder(IEnumerable<StudentStatement> toGraduate){

    }
    
    public override Task<bool> ConductOrder()
    {
        throw new NotImplementedException();
    }

    public override OrderTypes GetOrderType()
    {
        throw new NotImplementedException();
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7) RETURNING id";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", _orderStringId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderType()));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _orderDescription));

        await using (conn)
        await using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
            return;
        }
    }
    public override Task FromJSON(OrderModelJSON json)
    {
        return base.FromJSON(json);
    }
   public static async Task<Order?> GetById(int id, ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = scope == null ? await Utils.GetAndOpenConnectionFactory() : null; 
        string cmdText = "SELECT * FROM orders WHERE id = @p1";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));

        await using(conn)
        await using(cmd)
        {
            await using var reader =await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            return new DeductionWithGraduationOrder(id)
            {
                _effectiveDate = (DateTime)reader["effective_date"],
                _specifiedDate = (DateTime)reader["specified_date"],
                _orderNumber = (int)reader["serial_number"],
                _orderDescription = (string)reader["description"],
                _orderDisplayedName = (string)reader["name"],
                _orderStringId = (string)reader["org_id"]
            };
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        return await GetById(_id, scope);
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other is null){
            return false;
        }
        if (other.GetType() != this.GetType()){
            return false;
        }
        var unboxed = (DeductionWithGraduationOrder)other;
        return 
            _id == unboxed._id;
    }

    public override Task SetData(OrderDataJSON json)
    {
        throw new NotImplementedException();
    }
}
