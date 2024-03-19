
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;
public class FreeEnrollmentWithTransferOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _toEnroll;

    private FreeEnrollmentWithTransferOrder() : base()
    {

    }
    private FreeEnrollmentWithTransferOrder(int id) : base(id)
    {

    }

    public static async Task<Result<FreeEnrollmentWithTransferOrder?>> Create(OrderDTO dto)
    {
        var created = new FreeEnrollmentWithTransferOrder();
        return await MapBase(dto, created);
    }

    public static QueryResult<FreeEnrollmentWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeEnrollmentWithTransferOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static async Task<Result<FreeEnrollmentWithTransferOrder?>> Create(int id, StudentGroupChangeMoveDTO? data)
    {
        var created = new FreeEnrollmentWithTransferOrder(id);
        var result = MapFromDbBaseForConduction(created);
        if (result.IsFailure)
        {
            return result;
        }
        var dtoParseResult = await StudentToGroupMoveList.Create(data);
        if (dtoParseResult.IsFailure)
        {
            return dtoParseResult.Retrace(created);
        }
        var order = result.ResultObject;
        order._toEnroll = dtoParseResult.ResultObject;
        var conductionCheck = await order.CheckConductionPossibility();
        return conductionCheck.Retrace(order);
    }

    public override async Task ConductByOrder()
    {
        await ConductBase(_toEnroll.ToRecords(this));
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        await SaveBase();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollmentWithTransfer;
    }
    // добавить проверки на группу и уровень образования
    // на тип группы и т.д
    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        var baseCheck = await CheckBaseConductionPossibility(_toEnroll.Select(x => x.Student));
        if (baseCheck.IsFailure)
        {
            return baseCheck;
        }
        foreach (var rec in _toEnroll)
        {
            var history = await StudentHistory.Create(rec.Student);
            var groupCheck = await rec.GroupTo.EducationProgram.IsStudentAllowedByEducationLevel(rec.Student);
            if (history.IsStudentEnlisted() || !groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов в приказе на зачисление не соответствуют требованиям"));
            }

        }
        _conductionStatus = OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();

    }
}