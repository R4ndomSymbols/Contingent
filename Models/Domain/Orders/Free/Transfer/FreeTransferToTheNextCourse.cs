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

    }
    protected FreeTransferToTheNextCourseOrder(int id) : base(id)
    {

    }

    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(OrderDTO? order)
    {
        var created = new FreeTransferToTheNextCourseOrder();
        var valResult = await MapBase(order,created);
        return valResult;
    }
    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(int id, StudentGroupChangeMoveDTO? dto)
    {
        var model = new FreeTransferToTheNextCourseOrder(id);   
        var result = await MapFromDbBaseForConduction(id,model);
        if (result.IsFailure)
        {
            return result;
        }
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure){
            return Result<FreeTransferToTheNextCourseOrder?>.Failure(dtoAsModelResult.Errors);
        }
        var order = result.ResultObject; 
        order._moves = dtoAsModelResult.ResultObject;
        var conductionStatus = await order.CheckConductionPossibility();
        return conductionStatus.Retrace(order);
    }

    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(int id)
    {
        var order = new FreeTransferToTheNextCourseOrder(id);
        var result = await MapFromDbBase(id,order);
        return result;
    }

    public override async Task ConductByOrder()
    {   
        await ConductBase(_moves.ToRecords(this));
    }

    protected override OrderTypes GetOrderType()
    {
        return  OrderTypes.FreeNextCourseTransfer;
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase(scope);
    }
    // приказ о переводе с одного курса на другой
    // группы должны быть бесплатными 
    // одинаковая последовательность
    // отличия в курсе строго 1

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        var baseCheck = await base.CheckBaseConductionPossibility(_moves.Select(x => x.Student));
        if (baseCheck.IsFailure){
            return baseCheck;
        }

        foreach(var move in _moves.Moves){
    
            var history = await StudentHistory.Create(move.Student);
            var currentGroup = history.GetCurrentGroup();
            if (currentGroup is null || currentGroup.SponsorshipType.IsPaid() || currentGroup.CourseOn == currentGroup.EducationProgram.CourseCount){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Студент числится в группе, для которой невозможно проведение данного приказа"));
            }
            var targetGroup = move.GroupTo;
            if (targetGroup.SponsorshipType.IsPaid() || targetGroup.CourseOn - currentGroup.CourseOn != 1 || currentGroup.HistoricalSequenceId != targetGroup.HistoricalSequenceId){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Текущая группа и целевая группа несовместны в рамках данного приказа"));
            }    
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}