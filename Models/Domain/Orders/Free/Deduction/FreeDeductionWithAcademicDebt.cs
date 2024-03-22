using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeDeductionWithAcademicDebtOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _debtHolders;

    protected FreeDeductionWithAcademicDebtOrder() : base() {
    
    }

    protected FreeDeductionWithAcademicDebtOrder(int id) : base (id){
    
    }

    public static async Task<Result<FreeDeductionWithAcademicDebtOrder?>> Create(OrderDTO dto){
        var created = new FreeDeductionWithAcademicDebtOrder();
        var result =  await MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithAcademicDebtOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithAcademicDebtOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static async Task<Result<FreeDeductionWithAcademicDebtOrder?>> Create(int id, StudentGroupNullifyMoveDTO? dto){
        var fromDb = new FreeDeductionWithAcademicDebtOrder(id);
        var result = MapFromDbBaseForConduction(fromDb);
        if (result.IsFailure){
            return result;
        }
        var moves = await StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure){
            return result.RetraceFailure<FreeDeductionWithAcademicDebtOrder>();
        }
        var order = result.ResultObject;
        order._debtHolders = moves.ResultObject;
        var conductionPossible = await order.CheckConductionPossibility();
        return conductionPossible.Retrace(order);    
    }

    public override async Task ConductByOrder()
    {
        await ConductBase(_debtHolders.ToRecords(this));
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithAcademicDebt;
    }
    // приказ об отчислении по собственному желанию
    // не имеет ограничений вообще, главное, чтобы студент был зачислен и имел бесплатную группу
    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        var res = await base.CheckBaseConductionPossibility(_debtHolders.Moves.Select(x => x.Student));
        if (res.IsFailure){
            return res;
        }
        foreach (var debtHolder in _debtHolders){
            var aggregate = StudentHistory.Create(debtHolder.Student).GetLastRecord();
            var paidGroup = aggregate?.GroupTo?.SponsorshipType?.IsFree();
            if (paidGroup is null || (bool)paidGroup){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, указаных в приказе, не были зачислены"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}