using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

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
        return MapPartialFromDbBase(reader, order);
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

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_desiredToDeduct.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithOwnDesire;
    }
    // приказ об отчислении по собственному желанию
    // не имеет ограничений вообще, главное, чтобы студент был зачислен
    // проверка на бесплатную группу не нужна, т.к. студент не может быть зачислен в такую группу
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var graduate in _desiredToDeduct)
        {
            if (!graduate.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть отчислен прежде собственного зачисления", graduate.Student));
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

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _desiredToDeduct.Select(s => s.Student);
    }
}