using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidDeductionWithAcademicDebtOrder : AdditionalContingentOrder
{

    private StudentGroupNullifyMoveList _studentLeaving;

    private PaidDeductionWithAcademicDebtOrder() : base()
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithAcademicDebtOrder(int id) : base(id)
    {
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithAcademicDebtOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithAcademicDebtOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithAcademicDebtOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithAcademicDebtOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithAcademicDebtOrder>();
        }
        order._studentLeaving = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithAcademicDebtOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithAcademicDebtOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_studentLeaving?.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithAcademicDebt;
    }
    // студент должен быть зачислен
    // и не быть в академическом отпуске
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var student in _studentLeaving)
        {
            if (!student.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "Студент должен быть зачислен прежде, чем быть отчисленным", student.Student)
                    );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var debtHolder = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(debtHolder);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _studentLeaving.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _studentLeaving.Select(x => x.Student);
    }
}