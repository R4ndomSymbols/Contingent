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
        _toDeduct = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithOwnDesireOrder(int id) : base (id){
        _toDeduct = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithOwnDesireOrder> Create(OrderDTO? dto){
        var created = new FreeDeductionWithOwnDesireOrder();
        var result =  MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithOwnDesireOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithOwnDesireOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder>> Create(int id, StudentGroupNullifyMovesDTO? dto){
        var model = new FreeDeductionWithOwnDesireOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithOwnDesireOrder>(id);
        if (result.IsFailure){
            return result;
        }
        var moves = await StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure){
            return result.RetraceFailure<FreeDeductionWithOwnDesireOrder>();
        }
        var order = result.ResultObject;
        order._toDeduct = moves.ResultObject;
        return result;
    
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_toDeduct.Select(s => s.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_toDeduct.ToRecords(this)).RunSynchronously();
        return ResultWithoutValue.Success();
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
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var graduate in _toDeduct){
            if (!graduate.Student.History.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, указаных в приказе, не были зачислены"));
            }
        }
        return ResultWithoutValue.Success();
    }
}