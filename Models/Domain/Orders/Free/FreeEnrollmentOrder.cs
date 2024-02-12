using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.SQL;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;


namespace StudentTracking.Models.Domain.Orders;

public class FreeEnrollmentOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves; 

    protected FreeEnrollmentOrder() : base()
    {
        
    }
    protected FreeEnrollmentOrder(int id) : base(id)
    {
        
    }

    public static async Task<Result<FreeEnrollmentOrder?>> Create(OrderDTO? order){
        
        var created = new FreeEnrollmentOrder();
        var valResult = await created.MapBase(order);
         
        if (valResult.IsFailure){
            return Result<FreeEnrollmentOrder>.Failure(valResult.Errors);
        }
        return Result<FreeEnrollmentOrder>.Success(created);  
    }
    public static async Task<Result<FreeEnrollmentOrder?>> Create(int id, StudentGroupChangeOrderFlowDTO? dto){
        var order = new FreeEnrollmentOrder(id);

        var result = await order.MapFromDbBaseForConduction(id);
        if (result.IsFailure){
            return Result<FreeEnrollmentOrder?>.Failure(result.Errors);
        }

        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        
        if (dtoAsModelResult.IsFailure){
            return Result<FreeEnrollmentOrder?>.Failure(dtoAsModelResult.Errors);
        }

        order._moves = dtoAsModelResult.ResultObject;

        var checkResult = await order.CheckConductionPossibility(); 
        
        if (checkResult.IsFailure){
            return Result<FreeEnrollmentOrder?>.Failure(checkResult.Errors);
        }
        order._conductionStatus = OrderConductionStatus.NotConducted;

        return Result<FreeEnrollmentOrder>.Success(order);
    }

    public static async Task<Result<FreeEnrollmentOrder?>> Create (int id){
        var order = new FreeEnrollmentOrder(id);
        var result = await order.MapFromDbBase(id);
        if (result.IsFailure){
            return Result<FreeEnrollmentOrder?>.Failure(result.Errors);
        }
        return Result<FreeEnrollmentOrder?>.Success(order);   
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase(scope);
    }

    // условия приказа о зачислении
    // Студент должен не иметь статуса вообще 
    // либо он должен быть отчислен в связи с выпуском
    // приказ после должен быть любым приказом, кроме приказа о зачислении (любой)
    // зачисление возможно только на ту специальность, которой соответствует уровень 
    // образования студента
    // группа должна быть бесплатной

    // TODO:
    // сейчас возможна запись только одного приказа в день на каждого студента
    // нет проверки на совпадение даты, спросить у предст. предметной области
    
     
    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        if (await StudentHistory.IsAnyStudentInNotClosedOrder(_moves.Select(x => x.Student))){
            return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Один или несколько студентов числятся в незакрытых приказах"));
        }
        
        foreach (var stm in _moves.Moves){
            var history = await StudentHistory.Create(stm.Student); 
            bool orderBeforeConditionSatisfied = false;
            var recordBefore = history.GetClosestBefore(this);
            orderBeforeConditionSatisfied =
                recordBefore is null || recordBefore.ByOrder.GetOrderTypeDetails().IsAnyDeduction(); 

            var targetGroup = stm.GroupTo;

            var studentTags = await StudentEducationalLevelRecord.GetByOwnerId(stm.Student.Id); 
            var validMove =  
                orderBeforeConditionSatisfied &&
                studentTags.Any(x => x.Level.Weight >= targetGroup.EducationProgram.EducationalLevelIn.Weight) &&
                targetGroup.SponsorshipType.IsFree(); 
            if (!validMove){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Не соблюдены критерии по одной из позиций зачисления"));
            }
        }
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollment;
    }

    public override async Task ConductByOrder()
    {
        await ConductBase(_moves.ToRecords(this));
    }
}


