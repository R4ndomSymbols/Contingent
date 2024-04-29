using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Import;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeDeductionWithOwnDesireOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _desiredToDeduct;

    protected FreeDeductionWithOwnDesireOrder() : base()
    {
        _desiredToDeduct = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithOwnDesireOrder(int id) : base(id)
    {
        _desiredToDeduct = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithOwnDesireOrder> Create(OrderDTO? dto)
    {
        var created = new FreeDeductionWithOwnDesireOrder();
        var result = MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithOwnDesireOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithOwnDesireOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static Result<FreeDeductionWithOwnDesireOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var model = new FreeDeductionWithOwnDesireOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithOwnDesireOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var moves = StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure)
        {
            return result.RetraceFailure<FreeDeductionWithOwnDesireOrder>();
        }
        var order = result.ResultObject;
        order._desiredToDeduct = moves.ResultObject;
        return result;

    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_desiredToDeduct.Select(s => s.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_desiredToDeduct.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithOwnDesire;
    }
    // приказ об отчислении по собственному желанию
    // не имеет ограничений вообще, главное, чтобы студент был зачислен
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var graduate in _desiredToDeduct)
        {
            if (!graduate.Student.History.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, указаных в приказе, не были зачислены"));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var desiredDto = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var desired = StudentGroupNullifyMove.Create(desiredDto);
        if (desired.IsFailure)
        {
            return Result<Order>.Failure(desired.Errors);
        }
        _desiredToDeduct.Add(desired.ResultObject);
        return Result<Order>.Success(this);
    }
}