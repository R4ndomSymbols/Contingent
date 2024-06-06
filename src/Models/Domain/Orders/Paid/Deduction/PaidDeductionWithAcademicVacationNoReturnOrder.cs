using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;
// приказ об отчислении невышедших с отпуска платников 
public class PaidDeductionWithAcademicVacationNoReturnOrder : AdditionalContingentOrder
{
    private const int _daysBeforeDeduction = 10;
    private StudentGroupNullifyMoveList _leftForReason;

    private PaidDeductionWithAcademicVacationNoReturnOrder() : base()
    {
        _leftForReason = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithAcademicVacationNoReturnOrder(int id) : base(id)
    {
        _leftForReason = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithAcademicVacationNoReturnOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithAcademicVacationNoReturnOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithAcademicVacationNoReturnOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithAcademicVacationNoReturnOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithAcademicVacationNoReturnOrder>();
        }
        order._leftForReason = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithAcademicVacationNoReturnOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithAcademicVacationNoReturnOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_leftForReason.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithAcademicVacationNoReturn;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        // предыдущий приказ - это приказ об академическом отпуске
        // академ закончился, в добавок прошло еще x дней
        foreach (var record in _leftForReason)
        {
            var history = record.Student.History;
            if (!history.IsStudentSentInAcademicVacation())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не находится в академическом отпуске", record.Student));
            }
            var lastRecord = history.GetLastRecord()!;
            if (!lastRecord.StatePeriod.IsEndedNow())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент все еще находится в академическом отпуске", record.Student));
            }
            if (lastRecord.StatePeriod.GetEndedDaysAgoCount() < _daysBeforeDeduction)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("еще не прошло достаточное количество времени для отчисления", record.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var debtHolder = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(debtHolder);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _leftForReason.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _leftForReason.Select(x => x.Student);
    }
}