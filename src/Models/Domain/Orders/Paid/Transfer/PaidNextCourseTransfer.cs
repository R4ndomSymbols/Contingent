using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

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
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_transfer.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
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
            if (!history.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус", move.Student.GetName())
                    )
                );
            }
            var lastRecord = history.GetLastRecord();
            if (lastRecord is not null && lastRecord.GroupToNullRestrict.CourseOn == lastRecord.GroupToNullRestrict.EducationProgram.CourseCount)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("{0} имеет выпусную группу {1}", move.Student.GetName(), lastRecord.GroupToNullRestrict.GroupName)
                    )
                );
            }
            var group = move.GroupTo;
            if (!(group.HistoricalSequenceId == lastRecord.GroupToNullRestrict.HistoricalSequenceId
            && group.CourseOn - lastRecord.GroupToNullRestrict.CourseOn == 1))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Группа {0}, куда зачисляется студент {1}, не сооствествует критериям", group.GroupName, move.Student.GetName())
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
