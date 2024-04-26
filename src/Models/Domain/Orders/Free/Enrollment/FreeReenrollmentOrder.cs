

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
        _enrollers = StudentToGroupMoveList.Empty;
    }
    private FreeReenrollmentOrder(int id) : base(id){
        _enrollers = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeReenrollmentOrder> Create(OrderDTO? dto){
        var created = new FreeReenrollmentOrder();
        return MapBase(dto, created);
    }
    public static QueryResult<FreeReenrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeReenrollmentOrder(id);
        return MapParticialFromDbBase(reader, order);
    }
    public static async Task<Result<FreeReenrollmentOrder>> Create(int orderId, StudentGroupChangeMovesDTO? dto){
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
        return orderResult; 
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_enrollers.Select(x => x.Student));
        if (check.IsFailure){
            return check;
        }
        ConductBase(_enrollers.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeReenrollment;
    }
    // восстановление требует, чтобы студент был отчислен по собственному желанию
    // зачисление в бесплатную группу
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _enrollers){
            var history = StudentHistory.Create(move.Student);
            if (history.GetLastRecord()?.ByOrder?.GetOrderTypeDetails().Type != OrderTypes.FreeDeductionWithOwnDesire
            || move.GroupTo.SponsorshipType.IsPaid()
            ){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} в приказе на восстановление не имеет условий для восстановелния", move.Student.GetName())
                    ));
            }
        }
        return ResultWithoutValue.Success();
    }
}