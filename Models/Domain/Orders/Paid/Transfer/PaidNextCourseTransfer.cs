using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidTransferNextCourseOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _moves;

    protected PaidTransferNextCourseOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferNextCourseOrder(int id) : base(id){
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferNextCourseOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferNextCourseOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidTransferNextCourseOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferNextCourseOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferNextCourseOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferNextCourseOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferNextCourseOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_moves.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_moves.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferNextCourse;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves){
            var history = move.Student.History;
            if (!history.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус", move.Student.GetName())
                    )
                );
            }
            var lastRecord = history.GetLastRecord();
            if (lastRecord is not null && lastRecord.GroupToNullRestrict.CourseOn == lastRecord.GroupToNullRestrict.EducationProgram.CourseCount){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("{0} имеет выпусную группу {1}", move.Student.GetName(), lastRecord.GroupToNullRestrict.GroupName)
                    )
                );
            }
            var group = move.GroupTo;
            if (!(group.HistoricalSequenceId == lastRecord.GroupToNullRestrict.HistoricalSequenceId
            && group.CourseOn - lastRecord.GroupToNullRestrict.CourseOn == 1)){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Группа {0}, куда зачисляется студент {1}, не сооствествует критериям", group.GroupName, move.Student.GetName())
                    )
                );
            }
        }
        return ResultWithoutValue.Success();
    }
}
