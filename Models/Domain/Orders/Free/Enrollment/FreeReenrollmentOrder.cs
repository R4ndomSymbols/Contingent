

using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;
public class FreeReenrollmentOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _enrollers;

    private FreeReenrollmentOrder() : base(){
    
    }
    private FreeReenrollmentOrder(int id) : base(id){
    
    }

    public static async Task<Result<FreeReenrollmentOrder?>> Create(OrderDTO dto){
        var created = new FreeReenrollmentOrder();
        return await MapBase(dto, created);
    }
    public static QueryResult<FreeReenrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeReenrollmentOrder(id);
        return MapParticialFromDbBase(reader, order);
    }
    public static async Task<Result<FreeReenrollmentOrder?>> Create(int orderId, StudentGroupChangeMoveDTO? dto){
        var orderResult = MapFromDbBaseForConduction<FreeReenrollmentOrder>(orderId);
        if (orderResult.IsFailure)
        {
            return orderResult;
        } 
        var dtoResult = await StudentToGroupMoveList.Create(dto);
        if (dtoResult.IsFailure)
        {
            return dtoResult.RetraceFailure<FreeReenrollmentOrder>();
        }
        var order = orderResult.ResultObject;
        order._enrollers = dtoResult.ResultObject;
        return (await order.CheckConductionPossibility()).Retrace(order); 
    }

    public override async Task ConductByOrder()
    {
        await ConductBase(_enrollers.ToRecords(this));
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeReenrollment;
    }
    // восстановление требует, чтобы студент был отчислен по собственному желанию
    // зачисление в бесплатную группу
    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        var baseCheck = await base.CheckBaseConductionPossibility(_enrollers.Select(x => x.Student)); 
        if (baseCheck.IsFailure){
            return baseCheck;
        }
        foreach (var move in _enrollers){
            var history = StudentHistory.Create(move.Student);
            if (history.GetLastRecord()?.ByOrder?.GetOrderTypeDetails().Type != OrderTypes.FreeDeductionWithOwnDesire
            || move.GroupTo.SponsorshipType.IsPaid()
            ){
                return ResultWithoutValue.Failure(new OrderValidationError("Студент в приказе на восстановление не имеет условий для восстановелния"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}