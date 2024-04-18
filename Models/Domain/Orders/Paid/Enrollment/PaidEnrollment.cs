using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.Domain.Orders.Infrastructure;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidEnrollmentOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _moves;
    private OrderTypes[] ForbiddenPreviousOrderTypes =>
        new OrderTypes[] {
            OrderTypes.FreeDeductionWithOwnDesire,
            OrderTypes.FreeDeductionWithAcademicDebt,
            OrderTypes.PaidDeductionWithOwnDesire,
            OrderTypes.PaidDeductionWithAcademicDebt
        };

    protected PaidEnrollmentOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected PaidEnrollmentOrder(int id) : base(id)
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidEnrollmentOrder> Create(OrderDTO? order)
    {
        var created = new PaidEnrollmentOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidEnrollmentOrder>> Create(int id, StudentGroupChangeMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidEnrollmentOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidEnrollmentOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidEnrollmentOrder(id);
        return MapParticialFromDbBase(reader, order);
    }


    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_moves.Select(x => x.Student));
        if (check.IsFailure){
            return check;
        }
        ConductBase(_moves.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidEnrollment;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _moves)
        {
            var lastRecord = move.Student.History.GetLastRecord();
            if (lastRecord is not null && 
                ForbiddenPreviousOrderTypes.Any(x => lastRecord.OrderNullRestict.GetOrderTypeDetails().Type == x) &&
                // разница более чем в 5 лет между приказами является основанием для игнорирования статуса
                (this.EffectiveDate - lastRecord.OrderNullRestict.EffectiveDate) > new TimeSpan(365*5,0,0,0) 
            ){
                return ResultWithoutValue.Failure(new OrderValidationError(string.Format("{0} ранее числился в базе (имеет недопустимый статус)", move.Student.GetName())));
            }
            var group = move.GroupTo;
            // группа первого курса, специальность доступна студенту  
            if (!(group.CourseOn == 1 
                && group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student)
                && group.SponsorshipType.IsPaid()
                )){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Группа {0}, куда зачисляется студент {1}, не сооствествует критериям", group.GroupName, move.Student.GetName()
                        )
                    ));
            } 
        }
        return ResultWithoutValue.Success();

    }
}
