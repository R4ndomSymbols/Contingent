using Utilities;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using Npgsql;
using System.Net.Http.Headers;


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
        var valResult = await MapBase(order,created);
        return valResult;
    }
    public static async Task<Result<FreeEnrollmentOrder?>> Create(int id, StudentGroupChangeMoveDTO? dto){
        var model = new FreeEnrollmentOrder(id);
        var result = MapFromDbBaseForConduction<FreeEnrollmentOrder>(id);
        if (result.IsFailure){
            return result.RetraceFailure<FreeEnrollmentOrder>();
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure){
            return dtoAsModelResult.RetraceFailure<FreeEnrollmentOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        var checkResult = await order.CheckConductionPossibility();
        return checkResult.Retrace(order); 
    }

    public static QueryResult<FreeEnrollmentOrder?> Create (int id, NpgsqlDataReader reader){
        var order = new FreeEnrollmentOrder(id);
        return MapParticialFromDbBase(reader,order);
        
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
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
        var baseCheck = await base.CheckBaseConductionPossibility(_moves.Select(x => x.Student));
        if (baseCheck.IsFailure){
            return baseCheck;
        }
        
        foreach (var stm in _moves.Moves){

            var history = StudentHistory.Create(stm.Student); 
            var targetGroup = stm.GroupTo;
            var validMove =  
                history.IsStudentNotRecorded() &&
                await targetGroup.EducationProgram.IsStudentAllowedByEducationLevel(stm.Student)&&
                targetGroup.SponsorshipType.IsFree(); 
            if (!validMove){
                return ResultWithoutValue.Failure(new OrderValidationError("Не соблюдены критерии по одной из позиций зачисления"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
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


