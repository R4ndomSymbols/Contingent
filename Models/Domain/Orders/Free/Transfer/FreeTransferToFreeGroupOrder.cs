using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeTransferToFreeGroupOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    private FreeTransferToFreeGroupOrder() : base(){
    
    }

    private FreeTransferToFreeGroupOrder(int id) : base(id){
    
    }

    public static async Task<Result<FreeTransferToFreeGroupOrder?>> Create(OrderDTO orderDTO){
        var created = new FreeTransferToFreeGroupOrder();
        var result = await MapBase(orderDTO,created);
        return result;
    }
    public static QueryResult<FreeTransferToFreeGroupOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeTransferToFreeGroupOrder(id);
        return MapParticialFromDbBase(reader, order);
    }


    public static async Task<Result<FreeTransferToFreeGroupOrder?>> Create(int id, StudentGroupChangeMoveDTO moves){
        var created = new FreeTransferToFreeGroupOrder(id); 
        var result = MapFromDbBaseForConduction(created);
        if (result.IsFailure){
            return result;
        }
        var got = result.ResultObject;
        var data = await StudentToGroupMoveList.Create(moves);
        if (data.IsFailure){
            return data.RetraceFailure<FreeTransferToFreeGroupOrder>();
        }
        got._moves = data.ResultObject;
        var conductionResult = await got.CheckConductionPossibility();
        return conductionResult.Retrace(got);

    }

    public override Task ConductByOrder()
    {
        return ConductBase(_moves.ToRecords(this));
    }
    // проведение приказа должно аннулировать договор о платном обучении студента
    public async override Task Save(ObservableTransaction? scope)
    {
        await base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }
    // перевод с платного на бесплатное - одинаковая специальность одинаковый курс и год зачисления
    // из платной группы в бесплатную

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {   
        var baseCheck = _moves.All(x => x.Student.PaidAgreement.IsConcluded()) && await StudentHistory.IsAnyStudentInNotClosedOrder(_moves.Select(x => x.Student));
        if (!baseCheck){
            return ResultWithoutValue.Failure(new OrderValidationError("Студент или студенты, находящиеся в приказе на перевод с платного на беспланое не удовлетворяют условиям (договор о платном образовани или незакрытый приказ)"));
        }
        foreach (var move in _moves){
            var currentStudentGroup = move.Student.History.GetCurrentGroup();
            var conditionsSatisfied = 
                currentStudentGroup.CourseOn == move.GroupTo.CourseOn   
                && currentStudentGroup.CreationYear == move.GroupTo.CreationYear
                && currentStudentGroup.SponsorshipType.IsPaid() 
                && move.GroupTo.SponsorshipType.IsFree(); 
            if (!conditionsSatisfied){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов состоят или переводятся в группы, недопустимые по условиям приказа"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}