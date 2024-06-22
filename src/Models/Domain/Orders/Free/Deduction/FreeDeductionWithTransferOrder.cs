using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class FreeDeductionWithTransferOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _toBeLeftForAnotherOrg;

    protected FreeDeductionWithTransferOrder() : base()
    {
        _toBeLeftForAnotherOrg = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithTransferOrder(int id) : base(id)
    {
        _toBeLeftForAnotherOrg = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithTransferOrder> Create(OrderDTO? dto)
    {
        var created = new FreeDeductionWithTransferOrder();
        var result = MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithTransferOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    public static Result<FreeDeductionWithTransferOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<FreeDeductionWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var moves = StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure)
        {
            return result.RetraceFailure<FreeDeductionWithTransferOrder>();
        }
        var order = result.ResultObject;
        order._toBeLeftForAnotherOrg = moves.ResultObject;
        return result;

    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_toBeLeftForAnotherOrg.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithTransfer;
    }
    // приказ об отчислении в связи с переводом
    // студент должен быть зачислен
    // TODO: спросить
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var left in _toBeLeftForAnotherOrg)
        {
            if (!left.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть отчислен прежде собственного зачисления", left.Student));
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
        _toBeLeftForAnotherOrg.Add(desired.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _toBeLeftForAnotherOrg.Select(s => s.Student);
    }
}