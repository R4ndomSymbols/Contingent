

using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;
public class FreeReEnrollmentOrder : FreeContingentOrder
{
    private static readonly TimeSpan REENROLL_TIME_LIMIT = TimeSpan.FromDays(365 * 5 + 1);
    private StudentToGroupMoveList _enrollers;

    private FreeReEnrollmentOrder() : base()
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }
    private FreeReEnrollmentOrder(int id) : base(id)
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeReEnrollmentOrder> Create(OrderDTO? dto)
    {
        var created = new FreeReEnrollmentOrder();
        return MapBase(dto, created);
    }
    public static QueryResult<FreeReEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeReEnrollmentOrder(id);
        return MapPartialFromDbBase(reader, order);
    }
    public static Result<FreeReEnrollmentOrder> Create(int orderId, StudentToGroupMovesDTO? dto)
    {
        var orderResult = MapFromDbBaseForConduction<FreeReEnrollmentOrder>(orderId);
        if (orderResult.IsFailure)
        {
            return orderResult;
        }
        var dtoResult = StudentToGroupMoveList.Create(dto);
        if (dtoResult.IsFailure)
        {
            return dtoResult.RetraceFailure<FreeReEnrollmentOrder>();
        }
        var order = orderResult.ResultObject;
        order._enrollers = dtoResult.ResultObject;
        return orderResult;
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_enrollers.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeReEnrollment;
    }
    // восстановление требует, чтобы студент был отчислен по собственному желанию
    // зачисление в бесплатную группу, с тем же курсом
    // не более 5 лет после отчисления
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var move in _enrollers)
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
            if (!group.SponsorshipType.IsFree())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Группа студента должна быть бесплатной", move.Student));
            }
            var timeSinceDeduction = history.GetTimeSinceLastAction(_effectiveDate);
            if (timeSinceDeduction is null || timeSinceDeduction.Value.TotalDays > REENROLL_TIME_LIMIT.TotalDays)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Студент не может быть восстановлен, если отчислен более 5 лет назад", move.Student));
            }
        }
        return ResultWithoutValue.Success();

    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var enroller = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(enroller);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _enrollers.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _enrollers.Select(x => x.Student);
    }
}