using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidTransferFromPaidToFreeOrder : FreeContingentOrder
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
        return MapPartialFromDbBase(reader, order);
    }


    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_transfer.ToRecords(this));
        foreach (var move in _transfer)
        {
            move.Student.TerminatePaidEducationAgreement();
        }
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeTransferFromPaidToFree;
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility()
    {
        foreach (var move in _transfer)
        {
            var history = move.Student.History;
            var groupNow = history.GetCurrentGroup();
            var groupTo = move.GroupTo;
            var groupCheck =
                groupNow is not null && groupNow.GetRelationTo(groupTo) == Groups.GroupRelations.None
                && groupTo.CreationYear == groupNow.CreationYear
                && groupTo.CourseOn == groupNow.CourseOn
                && groupNow.EducationProgram.Equals(groupTo.EducationProgram)
                && groupTo.SponsorshipType.IsFree();

            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "студента нельзя перевести на бесплатное либо группа указана неверно", move.Student)
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

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _transfer.Select(x => x.Student);
    }
}