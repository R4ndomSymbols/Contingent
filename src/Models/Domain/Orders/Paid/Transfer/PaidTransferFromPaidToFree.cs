using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Models.Domain.Orders;

public class PaidTransferFromPaidToFreeOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _transfer;

    protected PaidTransferFromPaidToFreeOrder() : base()
    {
        _transfer = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferFromPaidToFreeOrder(int id) : base(id)
    {
        _transfer = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferFromPaidToFreeOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferFromPaidToFreeOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidTransferFromPaidToFreeOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferFromPaidToFreeOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferFromPaidToFreeOrder>();
        }
        order._transfer = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferFromPaidToFreeOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferFromPaidToFreeOrder(id);
        return MapParticialFromDbBase(reader, order);
    }


    public override ResultWithoutValue ConductByOrder()
    {
        var upperCheck = base.CheckConductionPossibility(_transfer.Select(x => x.Student));
        if (upperCheck.IsFailure)
        {
            return upperCheck;
        }
        ConductBase(_transfer?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferFromPaidToFree;
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
            var groupNow = lastRecord.GroupToNullRestrict;
            if (groupNow.CourseOn != group.CourseOn
                || !groupNow.EducationProgram.Equals(group.EducationProgram)
                || group.CreationYear != groupNow.CreationYear
                || groupNow.SponsorshipType.IsFree() || group.SponsorshipType.IsPaid()
                )
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("{0} переводится в группу {1}, которая не соответствует условиям", move.Student.GetName(), lastRecord.GroupToNullRestrict.GroupName)
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