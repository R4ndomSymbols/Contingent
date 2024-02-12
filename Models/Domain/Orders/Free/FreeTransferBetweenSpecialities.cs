using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

public class FreeTransferBetweenSpecialitiesOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    private FreeTransferBetweenSpecialitiesOrder() : base(){
    
    }

    private FreeTransferBetweenSpecialitiesOrder(int id) : base(id){
    
    }

    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(OrderDTO orderDTO){
        var created = new FreeTransferBetweenSpecialitiesOrder();
        var result = await created.MapBase(orderDTO);
        if (result.IsFailure){
            return Result<FreeTransferBetweenSpecialitiesOrder>.Failure(result.Errors);
        }
        return Result<FreeTransferBetweenSpecialitiesOrder>.Success(created);
    }
    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(int id){
        var created = new FreeTransferBetweenSpecialitiesOrder(id);
        var result = await created.MapFromDbBase(id);
        if (result.IsFailure){
            return Result<FreeTransferBetweenSpecialitiesOrder>.Failure(result.Errors);
        }
        return Result<FreeTransferBetweenSpecialitiesOrder>.Success(created);
    }


    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(int id, MoveOrderDataDTO moves){
        var created = await Create(id);
        if (created.IsFailure){
            return created;
        }
        var got = created.ResultObject;
        var result = await StudentToGroupMoveList.Create(moves.Moves);
        if (result.IsFailure){
            return Result<FreeTransferBetweenSpecialitiesOrder>.Failure(result.Errors);
        }
        got._moves = result.ResultObject;
        return Result<FreeTransferBetweenSpecialitiesOrder>.Success(got);

    }

    public override Task ConductByOrder()
    {
        return ConductBase(_moves.Select(x => new StudentFlowRecord(this, x.Student, x.GroupTo)));
    }

    public override Task Save(ObservableTransaction? scope)
    {
        return SaveBase();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }
    // у групп должен быть одинаковый курс, одинаковый год создания, разные id потоков
    // приказ о переводе внутри колледжа 
    // тот же курс тот же год поступления, различие в специальности не обязательно (???)

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {   
        var baseCheck = await CheckBaseConductionPossibility(_moves.Select(x => x.Student));
        if (baseCheck.IsFailure){
            return baseCheck;
        }
        // проверка на конечный момент времени, без учета альтернативной истории
        foreach (var move in _moves){
            var currentStudentGroup = await StudentHistory.GetCurrentStudentGroup(move.Student);
            var conditionsSatisfied = 
                currentStudentGroup.CourseOn == move.GroupTo.CourseOn   
                && currentStudentGroup.CreationYear == move.GroupTo.CreationYear; 
            if (!conditionsSatisfied){
                return ResultWithoutValue.Failure(new ValidationError("Один или несколько студентов состоят или переводятся в группы, недопустимые по условиям приказа"));
            }
        }
        return ResultWithoutValue.Success();
    }
}