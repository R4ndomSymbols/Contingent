using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

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
        var valResult = await created.MapBase(order);
        if (valResult.IsFailure)
        {
            return Result<FreeDeductionWithGraduationOrder>.Failure(valResult.Errors);
        }
        return Result<FreeDeductionWithGraduationOrder>.Success(created);
        
    }
    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id, StudentGroupNullifyFlowDTO? dto)
    {   var order = new FreeDeductionWithGraduationOrder(id);
        var result = await order.MapFromDbBaseForConduction(id);
        if (result.IsFailure)
        {
            return Result<FreeDeductionWithGraduationOrder?>.Failure(result.Errors);
        }
        var dtoAsModelResult = await StudentGroupNullifyMoveList.Create(dto?.Students);
        if (dtoAsModelResult.IsFailure){
            return Result<FreeDeductionWithGraduationOrder>.Failure(dtoAsModelResult.Errors);
        }
        order._graduates = dtoAsModelResult.ResultObject;
        var conductionCheck = await order.CheckConductionPossibility();

        if (conductionCheck.IsFailure){
            return Result<FreeDeductionWithGraduationOrder>.Failure(conductionCheck.Errors);
        }
        order._conductionStatus = OrderConductionStatus.NotConducted;     
        return Result<FreeDeductionWithGraduationOrder>.Success(order);
    }

    public static async Task<Result<FreeDeductionWithGraduationOrder?>> Create(int id)
    {
        var order = new FreeDeductionWithGraduationOrder(id);
        var result = await order.MapFromDbBase(id);
        if (!result.IsSuccess)
        {
            return Result<FreeDeductionWithGraduationOrder?>.Failure(result.Errors);
        }
        return Result<FreeDeductionWithGraduationOrder?>.Success(order);
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
        if (await StudentHistory.IsAnyStudentInNotClosedOrder(_graduates.Select(x => x.Student))){
            return ResultWithoutValue.Failure(new ValidationError(nameof(_graduates), "Один или несколько студентов числятся в незакрытых приказах"));
        }
        foreach(var i in _graduates){
            var group = await i.Student.GetCurrentGroup(); 
            if (group is null || group.CourseOn != group.EducationProgram.CourseCount || !group.SponsorshipType.IsFree()){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_graduates), "Один или несколько студентов в приказе не соответствуют критериям"));
            }
        }
        return ResultWithoutValue.Success();
    }
}
