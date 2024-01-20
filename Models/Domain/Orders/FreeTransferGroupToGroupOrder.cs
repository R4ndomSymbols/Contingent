using System.Runtime.CompilerServices;
using Npgsql;
using StudentTracking.Controllers.DTO;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Orders;


public class FreeTransferGroupToGroupOrder : FreeEducationOrder
{
    private TransferGroupToGroupOrderFlowDTO _moves;
    protected FreeTransferGroupToGroupOrder() : base()
    {

    }
    public static async Task<Result<FreeTransferGroupToGroupOrder?>> Create(OrderDTO? order)
    {

        var created = new FreeTransferGroupToGroupOrder();
        var valResult = created.MapBase(order);
        await created.RequestAndSetNumber();
        created._alreadyConducted = false;

        if (valResult.IsSuccess)
        {
            return Result<FreeTransferGroupToGroupOrder>.Success(created);
        }
        else
        {
            return Result<FreeTransferGroupToGroupOrder>.Failure(valResult.Errors);
        }
    }
    public static async Task<Result<FreeTransferGroupToGroupOrder?>> Create(int id, TransferGroupToGroupOrderFlowDTO? dto)
    {
        var result = await Create(id);
        if (!result.IsSuccess)
        {
            return result;
        }
        var found = result.ResultObject;
        var errors = new List<ValidationError?>();

        if (!errors.IsValidRule(
            dto != null && dto.Moves != null && dto.Moves.Count > 0,
            message: "Агрументы проведения приказа не указаны",
            propName: nameof(_moves)
        ))
        {
            return Result<FreeTransferGroupToGroupOrder>.Failure(errors);
        }

        foreach (StudentMoveDTO sm in dto.Moves)
        {
            if (!errors.IsValidRule(
                await StudentModel.IsIdExists(sm.StudentId, null) && await GroupModel.IsIdExists(sm.GroupToId, null),
                message: "Неверно указаны студенты или группы при проведении приказа",
                propName: nameof(_moves)
            ))
            {
                return Result<FreeTransferGroupToGroupOrder>.Failure(errors);
            }
        }
        if (errors.IsValidRule(
            await found.CheckConductionPossibility(),
            message: "Проведение приказа невозможно",
            propName: nameof(_moves)
        ))
        {
            return Result<FreeTransferGroupToGroupOrder>.Success(found);
        }

        return Result<FreeTransferGroupToGroupOrder>.Failure(errors);
    }

    public static async Task<Result<FreeTransferGroupToGroupOrder?>> Create(int id)
    {
        var order = new FreeTransferGroupToGroupOrder();
        var result = await order.GetBase(id);
        if (!result.IsSuccess)
        {
            return Result<FreeTransferGroupToGroupOrder?>.Failure(result.Errors);
        }
        return Result<FreeTransferGroupToGroupOrder?>.Success(order);
    }

    public override Task<bool> ConductByOrder()
    {
        throw new NotImplementedException();
    }

    protected override OrderTypes GetOrderType()
    {
        return  OrderTypes.FreeTransferGroupToGroup;
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7) RETURNING id";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", OrderOrgId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderTypeDetails().Type));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        if (_orderDescription is null){
            cmd.Parameters.Add(new NpgsqlParameter<DBNull>("p7", DBNull.Value));
        }
        else {
            cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _orderDescription));
        }

        await using (conn)
        await using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            return;
        }
    }
    // приказ о переводе с одной группы в другую 

    internal override Task<bool> CheckConductionPossibility()
    {
        throw new NotImplementedException();
    }
}