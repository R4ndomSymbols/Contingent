using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidEnrollmentWithTransferOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _moves;
    protected PaidEnrollmentWithTransferOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected PaidEnrollmentWithTransferOrder(int id) : base(id)
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidEnrollmentWithTransferOrder> Create(OrderDTO? order)
    {
        var created = new PaidEnrollmentWithTransferOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidEnrollmentWithTransferOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidEnrollmentWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidEnrollmentWithTransferOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidEnrollmentWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidEnrollmentWithTransferOrder(id);
        return MapParticialFromDbBase(reader, order);

    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_moves.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_moves?.ToRecords(this)).RunSynchronously();
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidEnrollmentWithTransfer;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves)
        {
            var history = move.Student.History;
            if (!(history.IsStudentNotRecorded() && history.IsStudentDeducted()))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                         string.Format("Студент {0} не имеет недопустимый статус", move.Student.GetName())
                    )
                );
            }
            var group = move.GroupTo;
            if (!(group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student) && group.SponsorshipType.IsPaid())){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                         string.Format("Студент {0} не соответствует критериям перевода в группу {1}", move.Student.GetName(), group.GroupName)
                    )
                );
            }
        }
        return ResultWithoutValue.Success();
    }
}
