using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class FreeDeductionWithAcademicVacationNoReturnOrder : FreeContingentOrder
{
    private const int _daysBeforeDeduction = 10;
    private StudentGroupNullifyMoveList _leavers;
    protected FreeDeductionWithAcademicVacationNoReturnOrder() : base()
    {
        _leavers = StudentGroupNullifyMoveList.Empty;
    }
    protected FreeDeductionWithAcademicVacationNoReturnOrder(int id) : base(id)
    {
        _leavers = StudentGroupNullifyMoveList.Empty;
    }
    public static Result<FreeDeductionWithAcademicVacationNoReturnOrder> Create(OrderDTO? order)
    {
        var created = new FreeDeductionWithAcademicVacationNoReturnOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<FreeDeductionWithAcademicVacationNoReturnOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var model = new FreeDeductionWithAcademicVacationNoReturnOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithAcademicVacationNoReturnOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeDeductionWithAcademicVacationNoReturnOrder>();
        }
        order._leavers = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<FreeDeductionWithAcademicVacationNoReturnOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithAcademicVacationNoReturnOrder(id);
        return MapPartialFromDbBase(reader, order);
    }


    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_leavers.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithAcademicVacationNoReturn;
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        // студент находился в академическом отпуске
        // но в течении x дней не вышел из него
        foreach (var leaver in _leavers)
        {
            var history = leaver.Student.GetHistory(scope);
            if (history.IsStudentSentInAcademicVacation())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент на находится в академическом отпуске", leaver.Student));
            }
            var lastRecord = history.GetLastRecord()!;
            if (!lastRecord.StatePeriod.IsEndedNow())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент все еще находится в академическом отпуске", leaver.Student));
            }
            if (lastRecord.StatePeriod.GetEndedDaysAgoCount() < _daysBeforeDeduction)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("еще не прошло достаточное количество времени для отчисления", leaver.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var graduateDto = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var graduate = StudentGroupNullifyMove.Create(graduateDto);
        if (graduate.IsFailure)
        {
            return Result<Order>.Failure(graduate.Errors);
        }
        _leavers.Add(graduate.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _leavers.Select(x => x.Student);
    }
}
