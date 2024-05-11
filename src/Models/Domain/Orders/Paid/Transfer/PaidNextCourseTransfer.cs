using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Microsoft.AspNetCore.Components.Forms;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidTransferNextCourseOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _transfer;

    protected PaidTransferNextCourseOrder() : base()
    {
        _transfer = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferNextCourseOrder(int id) : base(id)
    {
        _transfer = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferNextCourseOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferNextCourseOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidTransferNextCourseOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferNextCourseOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferNextCourseOrder>();
        }
        order._transfer = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferNextCourseOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferNextCourseOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_transfer.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferNextCourse;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _transfer)
        {
            var history = move.Student.History;
            var group = move.GroupTo;
            var groupCheck = group.GetRelationTo(history.GetCurrentGroup()) == Groups.GroupRelations.DirectChild;
            // группа студента элиминирует почти все проверки:
            // она null, если он не зачислен
            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "студент имеет недопустимый статус или группа указана неверно", move.Student)
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
