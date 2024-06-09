using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidDeductionWithOwnDesireOrder : AdditionalContingentOrder
{
    private StudentGroupNullifyMoveList _studentLeaving;

    private PaidDeductionWithOwnDesireOrder() : base()
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithOwnDesireOrder(int id) : base(id)
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithOwnDesireOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithOwnDesireOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithOwnDesireOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithOwnDesireOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithOwnDesireOrder>();
        }
        order._studentLeaving = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithOwnDesireOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithOwnDesireOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_studentLeaving?.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithOwnDesire;
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var student in _studentLeaving)
        {
            if (!student.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "студент должен быть зачислен прежде, чем быть отчисленным", student.Student
                    )
                );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var graduate = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(graduate);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _studentLeaving.Add(result.ResultObject);
        return Result<Order>.Success(this); throw new NotImplementedException();
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _studentLeaving.Select(x => x.Student).ToList();
    }
}
