using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

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
        return MapPartialFromDbBase(reader, order);

    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
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
            var group = move.GroupTo;
            var stateCheck = history.IsStudentDeducted() || history.IsStudentNotRecorded();
            var groupCheck = group.SponsorshipType.IsPaid() && group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student);
            if (!stateCheck || !groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    "студент имеет недопустимый статус или не соответствует критериям группы или группа бесплатная", move.Student)
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

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _enrollers.Select(x => x.Student).ToList();
    }
}
