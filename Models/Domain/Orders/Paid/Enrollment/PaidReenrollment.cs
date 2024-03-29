using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidReenrollmentOrder : AdditionalContingentOrder {
    private StudentToGroupMoveList _moves;
    protected PaidReenrollmentOrder() : base(){
        _moves = StudentToGroupMoveList.Empty;
    }
    protected PaidReenrollmentOrder(int id) : base(id){
        _moves = StudentToGroupMoveList.Empty;
    } 

    public static Result<PaidReenrollmentOrder> Create(OrderDTO? order)
    {
        var created = new PaidReenrollmentOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidReenrollmentOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidReenrollmentOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidReenrollmentOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidReenrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidReenrollmentOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_moves?.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_moves?.ToRecords(this)).RunSynchronously();
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidReenrollment;
    }
   
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves){
            var history = move.Student.History;
            if (!(history.IsStudentNotRecorded() || history.IsStudentDeducted())){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус", move.Student.GetName())
                    )
                );
            }
            var group = move.GroupTo;
            if (group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student)){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не соответствует критериям зачисления в группу {1}", move.Student.GetName(), group.GroupName)
                    )
                ); 
            }
        }
        return ResultWithoutValue.Success();
    }
}