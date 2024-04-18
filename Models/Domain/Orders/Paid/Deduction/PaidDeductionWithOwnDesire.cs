using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidDeductionWithOwnDesireOrder : AdditionalContingentOrder {

    private StudentGroupNullifyMoveList _studentLeaving;

    private PaidDeductionWithOwnDesireOrder() : base(){
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithOwnDesireOrder(int id) : base(id){
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithOwnDesireOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithOwnDesireOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidDeductionWithOwnDesireOrder>> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithOwnDesireOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithOwnDesireOrder>();
        }
        order._studentLeaving = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithOwnDesireOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithOwnDesireOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = CheckConductionPossibility(_studentLeaving?.Select(x => x.Student));
        if (check.IsFailure){
            return check;
        }
        ConductBase(_studentLeaving?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithOwnDesire;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var student in _studentLeaving){
            if (!student.Student.History.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} должен быть зачислен прежде, чем быть отчисленным", student.Student.GetName())
                    )
                );
            }
        }
        return ResultWithoutValue.Success();
    }
} 
