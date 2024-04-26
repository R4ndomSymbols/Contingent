using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;


public class PaidTransferBetweenSpecialitiesOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _moves;

    protected PaidTransferBetweenSpecialitiesOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected PaidTransferBetweenSpecialitiesOrder(int id) : base(id){
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidTransferBetweenSpecialitiesOrder> Create(OrderDTO? order)
    {
        var created = new PaidTransferBetweenSpecialitiesOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidTransferBetweenSpecialitiesOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidTransferBetweenSpecialitiesOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidTransferBetweenSpecialitiesOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidTransferBetweenSpecialitiesOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidTransferBetweenSpecialitiesOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = CheckConductionPossibility(_moves?.Select(x => x.Student));
        if (check.IsFailure){
            return check;
        }
        ConductBase(_moves?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidTransferBetweenSpecialities;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves){
            var history = move.Student.History;
            if (!history.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус (не зачислен)", move.Student.GetName())
                    )
                );
            }
            var group = move.GroupTo;
            var lastRecord = history.GetLastRecord();
            if (lastRecord is not null && (
                lastRecord.GroupToNullRestrict.CourseOn!= group.CourseOn ||
                lastRecord.GroupToNullRestrict.EducationProgram.Equals(group.EducationProgram)
                )){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("{0} переводится в группу не того же курса или специальности ({1})", move.Student.GetName(), group.GroupName)
                    )
                );
            }   
        }
        return ResultWithoutValue.Success();
    }
}