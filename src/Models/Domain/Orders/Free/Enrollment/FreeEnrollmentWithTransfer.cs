
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

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
        return MapPartialFromDbBase(reader, order);
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

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_toEnroll.ToRecords(this), scope);
        // делает активной группы, куда зачислены студенты и группу после
        foreach (var group in _toEnroll.Select(x => x.GroupTo).Distinct())
        {
            var suc = group.GetSuccessor();
            group.IsActive = true;
            if (suc is not null)
            {
                suc.IsActive = true;
                suc.Save();
            }
            group.Save();
        }
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollmentFromAnotherOrg;
    }
    // 
    // приказ о переводе с другой организации
    // бесплатная группа, студент незачислен
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var rec in _toEnroll)
        {
            var history = rec.Student.GetHistory(scope);
            var groupCheck = rec.GroupTo.EducationProgram.IsStudentAllowedByEducationLevel(rec.Student)
                && rec.GroupTo.SponsorshipType.IsFree();
            if (history.IsStudentEnlisted() || !groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Переводящийся студент либо переводится не в ту группу, либо не соотвествует критериям зачисления на специальность", rec.Student));
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

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _toEnroll.Select(x => x.Student);
    }
}
