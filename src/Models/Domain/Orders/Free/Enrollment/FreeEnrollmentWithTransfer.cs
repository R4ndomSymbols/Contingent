
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Models.Domain.Orders;
public class FreeEnrollmentWithTransferOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _toEnroll;

    private FreeEnrollmentWithTransferOrder() : base()
    {
        _toEnroll = StudentToGroupMoveList.Empty;
    }
    private FreeEnrollmentWithTransferOrder(int id) : base(id)
    {
        _toEnroll = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeEnrollmentWithTransferOrder> Create(OrderDTO? dto)
    {
        var created = new FreeEnrollmentWithTransferOrder();
        return MapBase(dto, created);
    }

    public static QueryResult<FreeEnrollmentWithTransferOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeEnrollmentWithTransferOrder(id);
        return MapParticialFromDbBase(reader, order);
    }

    public static Result<FreeEnrollmentWithTransferOrder> Create(int id, StudentToGroupMovesDTO? data)
    {
        var result = MapFromDbBaseForConduction<FreeEnrollmentWithTransferOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var dtoParseResult = StudentToGroupMoveList.Create(data);
        if (dtoParseResult.IsFailure)
        {
            return dtoParseResult.RetraceFailure<FreeEnrollmentWithTransferOrder>();
        }
        var order = result.ResultObject;
        order._toEnroll = dtoParseResult.ResultObject;
        return result;
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var check = CheckConductionPossibility(_toEnroll?.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_toEnroll?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollmentWithTransfer;
    }
    // добавить проверки на группу и уровень образования
    // на тип группы и т.д
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var rec in _toEnroll)
        {
            var history = StudentHistory.Create(rec.Student);
            var groupCheck = rec.GroupTo.EducationProgram.IsStudentAllowedByEducationLevel(rec.Student);
            if (history.IsStudentEnlisted() || !groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов в приказе на зачисление не соответствуют требованиям"));
            }

        }
        return ResultWithoutValue.Success();

    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var enroller = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(enroller);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _toEnroll.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }
}