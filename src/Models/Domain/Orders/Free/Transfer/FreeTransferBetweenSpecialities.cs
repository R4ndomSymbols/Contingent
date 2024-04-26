using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeTransferBetweenSpecialitiesOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    private FreeTransferBetweenSpecialitiesOrder() : base(){
        _moves = StudentToGroupMoveList.Empty;
    }

    private FreeTransferBetweenSpecialitiesOrder(int id) : base(id){
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeTransferBetweenSpecialitiesOrder> Create(OrderDTO? orderDTO){
        var created = new FreeTransferBetweenSpecialitiesOrder();
        var result = MapBase(orderDTO,created);
        return result;
    }
    public static QueryResult<FreeTransferBetweenSpecialitiesOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeTransferBetweenSpecialitiesOrder(id);
        return MapParticialFromDbBase(reader, order);
    }


    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder>> Create(int id, StudentGroupChangeMovesDTO? moves){
        var result = MapFromDbBaseForConduction<FreeTransferBetweenSpecialitiesOrder>(id);
        if (result.IsFailure){
            return result;
        }
        var got = result.ResultObject;
        var data = await StudentToGroupMoveList.Create(moves);
        if (data.IsFailure){
            return data.RetraceFailure<FreeTransferBetweenSpecialitiesOrder>();
        }
        got._moves = data.ResultObject;
        return result;

    }

    public override ResultWithoutValue ConductByOrder()
    {
        var checkResult = CheckConductionPossibility(_moves.Select(x => x.Student));
        if (checkResult.IsFailure){
            return checkResult;
        }
        ConductBase(_moves.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
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

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {   
        // проверка на конечный момент времени, без учета альтернативной истории
        foreach (var move in _moves){
            var currentStudentGroup = move.Student.History.GetCurrentGroup();
            var conditionsSatisfied = currentStudentGroup is not null &&
                currentStudentGroup.CourseOn == move.GroupTo.CourseOn   
                && currentStudentGroup.CreationYear == move.GroupTo.CreationYear; 
            if (!conditionsSatisfied){
                return ResultWithoutValue.Failure(new OrderValidationError(
                    string.Format("Студент {0} не может быть переведен в группу {1}", move.Student.GetName(), move.GroupTo.GroupName))
                );
            }
        }
        return ResultWithoutValue.Success();
    }
}