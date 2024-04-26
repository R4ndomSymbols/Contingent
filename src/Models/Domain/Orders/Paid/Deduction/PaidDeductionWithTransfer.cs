using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidDeductionWithTransferOrder : AdditionalContingentOrder {

    private StudentGroupNullifyMoveList _studentLeaving;

    private PaidDeductionWithTransferOrder() : base(){
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithTransferOrder(int id) : base(id){
        _studentLeaving = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithTransferOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithTransferOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidDeductionWithTransferOrder>> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithTransferOrder>();
        }
        order._studentLeaving = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithTransferOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_studentLeaving.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_studentLeaving.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithTransfer;
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