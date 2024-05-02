using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Import;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidEnrollmentWithTransferOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _enrollers;
    protected PaidEnrollmentWithTransferOrder() : base()
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }
    protected PaidEnrollmentWithTransferOrder(int id) : base(id)
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidEnrollmentWithTransferOrder> Create(OrderDTO? order)
    {
        var created = new PaidEnrollmentWithTransferOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidEnrollmentWithTransferOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidEnrollmentWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidEnrollmentWithTransferOrder>();
        }
        order._enrollers = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidEnrollmentWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidEnrollmentWithTransferOrder(id);
        return MapParticialFromDbBase(reader, order);

    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_enrollers.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_enrollers.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidEnrollmentWithTransfer;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _enrollers)
        {
            var history = move.Student.History;
            if (!(history.IsStudentNotRecorded() || history.IsStudentDeducted()))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                         string.Format("Студент {0} имеет недопустимый статус", move.Student.GetName())
                    )
                );
            }
            var group = move.GroupTo;
            if (!(group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student) && group.SponsorshipType.IsPaid()))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                         string.Format("Студент {0} не соответствует критериям перевода в группу {1}", move.Student.GetName(), group.GroupName)
                    )
                );
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
}
