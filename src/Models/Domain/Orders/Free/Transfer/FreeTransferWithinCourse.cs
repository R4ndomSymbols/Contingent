using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class FreeTransferWithinCourseOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    private FreeTransferWithinCourseOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    private FreeTransferWithinCourseOrder(int id) : base(id)
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeTransferWithinCourseOrder> Create(OrderDTO? orderDTO)
    {
        var created = new FreeTransferWithinCourseOrder();
        var result = MapBase(orderDTO, created);
        return result;
    }
    public static QueryResult<FreeTransferWithinCourseOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeTransferWithinCourseOrder(id);
        return MapPartialFromDbBase(reader, order);
    }


    public static Result<FreeTransferWithinCourseOrder> Create(int id, StudentToGroupMovesDTO? moves)
    {
        var result = MapFromDbBaseForConduction<FreeTransferWithinCourseOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var got = result.ResultObject;
        var data = StudentToGroupMoveList.Create(moves);
        if (data.IsFailure)
        {
            return data.RetraceFailure<FreeTransferWithinCourseOrder>();
        }
        got._moves = data.ResultObject;
        return result;

    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_moves.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }
    // у групп должен быть одинаковый курс, одинаковый год создания, разные id потоков
    // приказ о переводе внутри колледжа 
    // тот же курс тот же год поступления, различие в специальности не обязательно (???)

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var move in _moves)
        {
            var currentStudentGroup = move.Student.GetHistory(scope).GetCurrentGroup();
            var conditionsSatisfied = currentStudentGroup is not null &&
                currentStudentGroup.CourseOn == move.GroupTo.CourseOn
                && currentStudentGroup.CreationYear == move.GroupTo.CreationYear && move.GroupTo.SponsorshipType.IsFree();
            if (!conditionsSatisfied)
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    string.Format("не может быть переведен в группу {0}", move.GroupTo.GroupName), move.Student)
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