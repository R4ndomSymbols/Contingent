using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidDeductionWithTransferOrder : AdditionalContingentOrder
{

    private StudentGroupNullifyMoveList _studentLeaving;

    private PaidDeductionWithTransferOrder() : base()
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithTransferOrder(int id) : base(id)
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithTransferOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithTransferOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithTransferOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithTransferOrder>();
        }
        order._studentLeaving = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithTransferOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_studentLeaving.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithTransfer;
    }
    // студент должен быть зачислен и не быть в академе
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var student in _studentLeaving)
        {
            var history = student.Student.GetHistory(scope);
            if (!history.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "студент должен быть зачислен прежде, чем быть отчисленным", student.Student
                    )
                );
            }
            if (history.IsStudentSentInAcademicVacation())
            {
                return ResultWithoutValue.Failure(new OrderAcademicVacationValidationError(student.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var graduate = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(graduate);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _studentLeaving.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _studentLeaving.Select(x => x.Student).ToList();
    }
}