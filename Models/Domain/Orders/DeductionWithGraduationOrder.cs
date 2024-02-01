using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Orders;

public class FreeDeductionWithGraduationOrder : FreeEducationOrder
{
    private DeductionWithGraduationOrderFlowDTO _graduates;
    protected FreeDeductionWithGraduationOrder() : base()
    {

    }
    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(OrderDTO? order)
    {
        var created = new FreeDeductionWithGraduationOrder();
        var valResult = created.MapBase(order);
        await created.RequestAndSetNumber();
        created._alreadyConducted = false;

        if (valResult.IsSuccess)
        {
            return Result<FreeDeductionWithGraduationOrder>.Success(created);
        }
        else
        {
            return Result<FreeDeductionWithGraduationOrder>.Failure(valResult.Errors);
        }
    }
    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id, DeductionWithGraduationOrderFlowDTO? dto)
    {
        var result = await Create(id);
        if (!result.IsSuccess)
        {
            return result;
        }
        var found = result.ResultObject;
        var errors = new List<ValidationError?>();

        if (!errors.IsValidRule(
            dto != null && dto.Students != null && dto.Students.Count > 0,
            message: "Агрументы проведения приказа не указаны",
            propName: nameof(_graduates)
        ))
        {
            return Result<FreeDeductionWithGraduationOrder>.Failure(errors);
        }

        var conductionStatus = await found.CheckConductionPossibility(); 
        if (conductionStatus.IsFailure){
            errors.AddRange(conductionStatus.Errors);
        }
        if (errors.Any()){
            return Result<FreeDeductionWithGraduationOrder>.Failure(errors);
        }        
        return Result<FreeDeductionWithGraduationOrder>.Success(found);
        

        return Result<FreeDeductionWithGraduationOrder>.Failure(errors);
    }

    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id)
    {
        var order = new FreeDeductionWithGraduationOrder();
        var result = await order.GetBase(id);
        if (!result.IsSuccess)
        {
            return Result<FreeDeductionWithGraduationOrder?>.Failure(result.Errors);
        }
        return Result<FreeDeductionWithGraduationOrder?>.Success(order);
    }


    public override Task<bool> ConductByOrder()
    {
        throw new NotImplementedException();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description, is_closed) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8) RETURNING id";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", OrderOrgId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderTypeDetails().Type));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        if (_orderDescription == null){
            cmd.Parameters.Add(new NpgsqlParameter<DBNull>("p7", DBNull.Value));
        }
        else{   
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

    internal override Task<Result<bool>> CheckConductionPossibility()
    {
        throw new NotImplementedException();
    }
}
