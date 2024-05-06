using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

// приказ о переводе на следующий курс

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
    public static Result<FreeTransferToTheNextCourseOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<FreeTransferToTheNextCourseOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
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
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
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

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility()
    {
        foreach (var move in _moves.Moves)
        {

            var history = move.Student.History;
            var currentGroup = history.GetCurrentGroup();
            if (currentGroup is null)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "указанный в приказе студент еще не зачислен ни в какую группу", move.Student
                    )
                );
            }
            var targetGroup = move.GroupTo;
            var groupCheck = targetGroup.GetRelationTo(currentGroup) == Groups.GroupRelations.DirectChild
            && targetGroup.SponsorshipType.IsFree();
            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    string.Format(
                        "студента невозможно перевести на следующий курс({1} => {2})",
                            move.Student.GetName(), currentGroup.GroupName, move.GroupTo.GroupName
                        ), move.Student)
                    );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var transfer = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(transfer);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _moves.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _moves.Select(x => x.Student);
    }
}