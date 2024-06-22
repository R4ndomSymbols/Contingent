using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidReEnrollmentOrder : AdditionalContingentOrder
{
    private static readonly TimeSpan REENROLLMENT_TIME_LIMIT = TimeSpan.FromDays(365 * 5 + 1);
    private StudentToGroupMoveList _reEnrollers;
    protected PaidReEnrollmentOrder() : base()
    {
        _reEnrollers = StudentToGroupMoveList.Empty;
    }
    protected PaidReEnrollmentOrder(int id) : base(id)
    {
        _reEnrollers = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidReEnrollmentOrder> Create(OrderDTO? order)
    {
        var created = new PaidReEnrollmentOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidReEnrollmentOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidReEnrollmentOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidReEnrollmentOrder>();
        }
        order._reEnrollers = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidReEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidReEnrollmentOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_reEnrollers?.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidReEnrollment;
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var move in _reEnrollers)
        {
            var history = move.Student.GetHistory(scope);
            if (!history.IsStudentDeducted() || history.GetLastRecord() is null)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Студент не отчислялся", move.Student));
            }
            var lastRecord = history.GetLastRecord();
            if (!lastRecord!.ByOrder!.GetOrderTypeDetails().CanBePreviousToReEnrollment())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Студента нельзя восстановить", move.Student));
            }
            var deductionGroup = history.GetGroupFromStudentWasDeducted();
            var group = move.GroupTo;
            if (group.CourseOn != deductionGroup!.CourseOn)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Студента нельзя восстановить на другой курс", move.Student));
            }
            if (!group.SponsorshipType.IsPaid())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Группа студента должна быть бесплатной", move.Student));
            }
            var timeSinceDeduction = history.GetTimeSinceLastAction(_effectiveDate);
            if (timeSinceDeduction is null || timeSinceDeduction.Value.TotalDays > REENROLLMENT_TIME_LIMIT.TotalDays)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Студент не может быть восстановлен, если отчислен более 5 лет назад", move.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var reEnroller = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(reEnroller);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _reEnrollers.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _reEnrollers.Select(x => x.Student);
    }
}