using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

// приказ о переводе на следующий курс
// 

public class FreeTransferToTheNextCourseOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;
    protected FreeTransferToTheNextCourseOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected FreeTransferToTheNextCourseOrder(int id) : base(id)
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeTransferToTheNextCourseOrder> Create(OrderDTO? order)
    {
        var created = new FreeTransferToTheNextCourseOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<FreeTransferToTheNextCourseOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<FreeTransferToTheNextCourseOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeTransferToTheNextCourseOrder>();
        }
        var order = result.ResultObject;
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<FreeTransferToTheNextCourseOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeTransferToTheNextCourseOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var checkResult = base.CheckConductionPossibility(_moves.Select(x => x.Student));
        if (checkResult.IsFailure)
        {
            return checkResult;
        }
        ConductBase(_moves.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeTransferNextCourse;
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }
    // приказ о переводе с одного курса на другой
    // группы должны быть бесплатными 
    // одинаковая последовательность
    // отличия в курсе строго 1

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves.Moves)
        {

            var history = StudentHistory.Create(move.Student);
            var currentGroup = history.GetCurrentGroup();
            if (currentGroup is null)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус (не зачислен)", move.Student.GetName())
                    )
                );
            }
            var targetGroup = move.GroupTo;
            if (
                currentGroup.SponsorshipType.IsPaid() || currentGroup.CourseOn == currentGroup.EducationProgram.CourseCount
                || targetGroup.SponsorshipType.IsPaid() || targetGroup.CourseOn - currentGroup.CourseOn != 1 || currentGroup.HistoricalSequenceId != targetGroup.HistoricalSequenceId)
            {
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves),
                    string.Format(
                        "Студента {0} невозможно перевести на следующий курс({1} => {2})",
                            move.Student.GetName(), currentGroup.GroupName, move.GroupTo.GroupName
                        ))
                    );
            }
        }
        return ResultWithoutValue.Success();
    }
}