using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeTransferBetweenSpecialitiesOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    private FreeTransferBetweenSpecialitiesOrder() : base(){
    
    }

    private FreeTransferBetweenSpecialitiesOrder(int id) : base(id){
    
    }

    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(OrderDTO orderDTO){
        var created = new FreeTransferBetweenSpecialitiesOrder();
        var result = await MapBase(orderDTO,created);
        return result;
    }
    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(int id){
        var created = new FreeTransferBetweenSpecialitiesOrder(id);
        var result = await MapFromDbBase(id,created);
        return result;
    }


    public static async Task<Result<FreeTransferBetweenSpecialitiesOrder?>> Create(int id, StudentGroupChangeMoveDTO moves){
        var created = new FreeTransferBetweenSpecialitiesOrder(id); 
        var result = await MapFromDbBaseForConduction(id, created);
        if (result.IsFailure){
            return result;
        }
        var got = result.ResultObject;
        var data = await StudentToGroupMoveList.Create(moves.Moves);
        if (data.IsFailure){
            return data.RetraceFailure<FreeTransferBetweenSpecialitiesOrder>();
        }
        got._moves = data.ResultObject;
        var conductionResult = await got.CheckConductionPossibility();
        return conductionResult.Retrace(got);

    }

    public override Task ConductByOrder()
    {
        return ConductBase(_moves.ToRecords(this));
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
        var baseCheck = await base.CheckBaseConductionPossibility(_moves.Select(x => x.Student));
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
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов состоят или переводятся в группы, недопустимые по условиям приказа"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}