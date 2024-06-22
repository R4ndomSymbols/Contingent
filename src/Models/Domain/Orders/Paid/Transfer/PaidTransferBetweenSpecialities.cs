using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;


public class PaidTransferBetweenSpecialtiesOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _transfer;

    protected PaidTransferBetweenSpecialtiesOrder() : base()
    {
        _transfer = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferBetweenSpecialtiesOrder(int id) : base(id)
    {
        _transfer = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferBetweenSpecialtiesOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferBetweenSpecialtiesOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidTransferBetweenSpecialtiesOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferBetweenSpecialtiesOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferBetweenSpecialtiesOrder>();
        }
        order._transfer = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferBetweenSpecialtiesOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferBetweenSpecialtiesOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_transfer?.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferBetweenSpecialties;
    }
    // группы одного курса
    // разные потоки
    // студент зачислен
    // группа платная
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var move in _transfer)
        {
            var history = move.Student.GetHistory(scope);
            if (!history.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не зачислен", move.Student));
            }
            var group = move.GroupTo;
            var currentGroup = history.GetLastGroup();
            var groupCheck = currentGroup is not null && !currentGroup.IsOnTheSameThread(group) && group.SponsorshipType.IsPaid() && group.CourseOn == currentGroup.CourseOn;
            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "студент не может быть переведен в эту группу (группа не удовлетворяет условиям)", move.Student)
                );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
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