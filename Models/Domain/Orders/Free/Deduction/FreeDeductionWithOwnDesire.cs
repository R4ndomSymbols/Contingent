using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeDeductionWithOwnDesireOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _toDeduct;

    protected FreeDeductionWithOwnDesireOrder() : base() {
    
    }

    protected FreeDeductionWithOwnDesireOrder(int id) : base (id){
    
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder?>> Create(OrderDTO dto){
        var created = new FreeDeductionWithOwnDesireOrder();
        var result =  await MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithOwnDesireOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithOwnDesireOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder?>> Create(int id, StudentGroupNullifyMoveDTO? dto){
        var model = new FreeDeductionWithOwnDesireOrder(id);
        var result = MapFromDbBaseForConduction(model);
        if (result.IsFailure){
            return result;
        }
        var moves = await StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure){
            return result.RetraceFailure<FreeDeductionWithOwnDesireOrder>();
        }
        var order = result.ResultObject;
        order._toDeduct = moves.ResultObject;
        var conductionPossible = await order.CheckConductionPossibility();
        return conductionPossible.Retrace(order);
    
    }

    public override async Task ConductByOrder()
    {
        await ConductBase(_toDeduct.ToRecords(this));
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithOwnDesire;
    }
    // приказ об отчислении по собственному желанию
    // не имеет ограничений вообще, главное, чтобы студент был зачислен
    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        var res = await base.CheckBaseConductionPossibility(_toDeduct.Moves.Select(x => x.Student));
        if (res.IsFailure){
            return res;
        }
        foreach (var graduate in _toDeduct){
            if (!graduate.Student.History.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, указаных в приказе, не были зачислены"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}