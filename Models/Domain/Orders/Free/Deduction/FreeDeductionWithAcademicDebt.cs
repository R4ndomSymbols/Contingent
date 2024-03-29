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
        _debtHolders = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithAcademicDebtOrder(int id) : base (id){
        _debtHolders = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithAcademicDebtOrder> Create(OrderDTO? dto){
        var created = new FreeDeductionWithAcademicDebtOrder();
        var result =  MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithAcademicDebtOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithAcademicDebtOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static async Task<Result<FreeDeductionWithAcademicDebtOrder>> Create(int id, StudentGroupNullifyMovesDTO? dto){
        var fromDb = new FreeDeductionWithAcademicDebtOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithAcademicDebtOrder>(id);
        if (result.IsFailure){
            return result;
        }
        var moves = await StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure){
            return result.RetraceFailure<FreeDeductionWithAcademicDebtOrder>();
        }
        var order = result.ResultObject;
        order._debtHolders = moves.ResultObject;
        return result;    
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = CheckConductionPossibility(_debtHolders.Select(x => x.Student));
        if (check.IsFailure){
            return check;
        }
        ConductBase(_debtHolders.ToRecords(this)).RunSynchronously();
        return ResultWithoutValue.Success();
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithAcademicDebt;
    }
    // приказ об отчислении по собственному желанию
    // не имеет ограничений вообще, главное, чтобы студент был зачислен и имел бесплатную группу
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var debtHolder in _debtHolders){
            var aggregate = StudentHistory.Create(debtHolder.Student).GetLastRecord();
            var paidGroup = aggregate?.GroupTo?.SponsorshipType?.IsFree();
            if (paidGroup is null || (bool)paidGroup){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, указаных в приказе, не были зачислены"));
            }
        }
        return ResultWithoutValue.Success();
    }
}