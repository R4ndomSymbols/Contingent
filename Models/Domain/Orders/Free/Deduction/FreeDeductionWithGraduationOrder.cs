using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class FreeDeductionWithGraduationOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _graduates;
    protected FreeDeductionWithGraduationOrder() : base()
    {

    }
    protected FreeDeductionWithGraduationOrder(int id) : base(id){

    }
    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(OrderDTO? order)
    {
        var created = new FreeDeductionWithGraduationOrder();
        var valResult = await MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id, StudentGroupNullifyMoveDTO? dto)
    {   var model = new FreeDeductionWithGraduationOrder(id);
        var result = await MapFromDbBaseForConduction(id, model);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentGroupNullifyMoveList.Create(dto?.Students);
        if (dtoAsModelResult.IsFailure){
            return dtoAsModelResult.RetraceFailure<FreeDeductionWithGraduationOrder>();
        }
        order._graduates = dtoAsModelResult.ResultObject;
        var conductionCheck = await order.CheckConductionPossibility();
        return conductionCheck.Retrace(order);
    }

    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id)
    {
        var order = new FreeDeductionWithGraduationOrder(id);
        var result = await MapFromDbBase(id, order);
        return result;
    }


    public override async Task ConductByOrder()
    {
        await ConductBase(_graduates.ToRecords(this));
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase(scope);
    }

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {   
        var check = await base.CheckBaseConductionPossibility(_graduates.ToStudentCollection()); 
        if (check.IsFailure){
            return check;
        }
        foreach(var i in _graduates){
            var aggregate = StudentHistory.GetLastRecordOnStudent(i.Student.Id);
            var group = aggregate?.GroupTo; 
            if (group is null || group.CourseOn != group.EducationProgram.CourseCount || group.SponsorshipType.IsPaid()){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_graduates), "Один или несколько студентов в приказе не соответствуют критериям"));
            }
        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();
    }
}
