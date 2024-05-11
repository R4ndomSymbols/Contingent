

using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;
public class FreeReEnrollmentOrder : FreeContingentOrder
{
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

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_enrollers.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
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
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility()
    {
        foreach (var move in _enrollers)
        {
            var history = move.Student.History;
            var lastOrder = history.GetLastRecord()?.ByOrder;
            var orderCheck = lastOrder is not null && lastOrder.GetOrderTypeDetails().CanBePreviousToReEnrollment();
            var deductionGroup = history.GetGroupFromStudentWasDeducted();
            var groupCheck = move.GroupTo.SponsorshipType.IsFree()
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