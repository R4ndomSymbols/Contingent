using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidReEnrollmentOrder : AdditionalContingentOrder
{
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

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_reEnrollers?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidReEnrollment;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _reEnrollers)
        {
            var history = move.Student.History;
            var lastOrder = history.GetLastRecord()?.ByOrder;
            var orderCheck = lastOrder is not null && lastOrder.GetOrderTypeDetails().CanBePreviousToReEnrollment();
            var deductionGroup = history.GetGroupFromStudentWasDeducted();
            var groupCheck = move.GroupTo.SponsorshipType.IsPaid()
            && deductionGroup is not null && deductionGroup.CourseOn == move.GroupTo.CourseOn;
            var timeSinceDeduction = history.GetTimeSinceLastAction(_effectiveDate);
            var timeCheck = timeSinceDeduction is not null && timeSinceDeduction.Value.TotalDays / 365 <= 5;
            if (orderCheck && groupCheck && timeCheck)
            {
                continue;
            }
            else
            {
                return ResultWithoutValue.Failure(
                new OrderValidationError(
                    "не может быть восстановлен из-за несоблюдения условий восстановления", move.Student
                ));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
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