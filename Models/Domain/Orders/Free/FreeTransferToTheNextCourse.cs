using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Orders;

// приказ о переводе на следующий курс
// 

public class FreeTransferToTheNextCourseOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;
    protected FreeTransferToTheNextCourseOrder() : base()
    {

    }
    protected FreeTransferToTheNextCourseOrder(int id) : base(id)
    {

    }

    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(OrderDTO? order)
    {
        var created = new FreeTransferToTheNextCourseOrder();
        var valResult = await created.MapBase(order);
        if (valResult.IsFailure)
        {
            return Result<FreeTransferToTheNextCourseOrder>.Failure(valResult.Errors);
        }
        return Result<FreeTransferToTheNextCourseOrder>.Success(created);
    }
    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(int id, StudentGroupChangeMoveDTO? dto)
    {
        var order = new FreeTransferToTheNextCourseOrder(id);   
        var result = await order.MapFromDbBaseForConduction(id);
        if (result.IsFailure)
        {
            return Result<FreeTransferToTheNextCourseOrder>.Failure(result.Errors);
        }
        var dtoAsModelResult = await StudentToGroupMoveList.Create(dto?.Moves);
        if (dtoAsModelResult.IsFailure){
            return Result<FreeTransferToTheNextCourseOrder?>.Failure(dtoAsModelResult.Errors);
        }
        order._moves = dtoAsModelResult.ResultObject;
        var conductionStatus = await order.CheckConductionPossibility();

        if (conductionStatus.IsFailure){
            return Result<FreeTransferToTheNextCourseOrder>.Failure(conductionStatus.Errors);
        }

        order._conductionStatus = OrderConductionStatus.NotConducted;

        return Result<FreeTransferToTheNextCourseOrder>.Success(order);
    }

    public static async Task<Result<FreeTransferToTheNextCourseOrder?>> Create(int id)
    {
        var order = new FreeTransferToTheNextCourseOrder(id);
        var result = await order.MapFromDbBase(id);
        if (!result.IsSuccess)
        {
            return Result<FreeTransferToTheNextCourseOrder?>.Failure(result.Errors);
        }
        return Result<FreeTransferToTheNextCourseOrder?>.Success(order);
    }

    public override async Task ConductByOrder()
    {   
        await ConductBase(_moves.ToRecords(this));
    }

    protected override OrderTypes GetOrderType()
    {
        return  OrderTypes.FreeNextCourseTransfer;
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase(scope);
    }
    // приказ о переводе с одной группы в другую 

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        if (await StudentHistory.IsAnyStudentInNotClosedOrder(_moves.Select(x => x.Student))){
            return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Один или несколько студентов числятся в незакрытых приказах"));
        }
        foreach(var move in _moves.Moves){
    
            var history = await StudentHistory.Create(move.Student);
            var currentGroup = history.GetCurrentGroup();
            if (currentGroup is null || !currentGroup.SponsorshipType.IsFree() || currentGroup.CourseOn == currentGroup.EducationProgram.CourseCount){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Студент числится в группе, для которой невозможно проведение данного приказа"));
            }
            var targetGroup = move.GroupTo;
            if (!targetGroup.SponsorshipType.IsFree() || targetGroup.CourseOn - currentGroup.CourseOn != 1 || currentGroup.HistoricalSequenceId != targetGroup.HistoricalSequenceId){
                return ResultWithoutValue.Failure(new ValidationError(nameof(_moves), "Текущая группа и целевая группа несовместны в рамках данного приказа"));
            }    
        }
        return ResultWithoutValue.Success();
    }
}