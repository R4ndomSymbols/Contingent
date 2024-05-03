using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Models.Domain.Orders;


public class PaidTransferBetweenSpecialitiesOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _transfer;

    protected PaidTransferBetweenSpecialitiesOrder() : base()
    {
        _transfer = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferBetweenSpecialitiesOrder(int id) : base(id)
    {
        _transfer = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferBetweenSpecialitiesOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferBetweenSpecialitiesOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidTransferBetweenSpecialitiesOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferBetweenSpecialitiesOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferBetweenSpecialitiesOrder>();
        }
        order._transfer = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferBetweenSpecialitiesOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferBetweenSpecialitiesOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = CheckConductionPossibility(_transfer?.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_transfer?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferBetweenSpecialities;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _transfer)
        {
            var history = move.Student.History;
            if (!history.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус (не зачислен)", move.Student.GetName())
                    )
                );
            }
            var group = move.GroupTo;
            var lastRecord = history.GetLastRecord();
            if (lastRecord is not null && (
                lastRecord.GroupToNullRestrict.CourseOn != group.CourseOn ||
                lastRecord.GroupToNullRestrict.EducationProgram.Equals(group.EducationProgram)
                ))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("{0} переводится в группу не того же курса или специальности ({1})", move.Student.GetName(), group.GroupName)
                    )
                );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var transferer = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(transferer);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _transfer.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }
}