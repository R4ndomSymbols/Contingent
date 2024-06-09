using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Students;
using Npgsql;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders;

public class FreeAcademicVacationSendOrder : FreeContingentOrder
{
    private StudentDurableStatesCollection _toSendToVacation;

    private FreeAcademicVacationSendOrder(int id) : base(id)
    {
        _toSendToVacation = StudentDurableStatesCollection.Empty;
    }
    private FreeAcademicVacationSendOrder() : base()
    {
        _toSendToVacation = StudentDurableStatesCollection.Empty;
    }

    public static Result<FreeAcademicVacationSendOrder> Create(OrderDTO? dto)
    {
        var created = new FreeAcademicVacationSendOrder();
        var valResult = MapBase(dto, created);
        return valResult;
    }
    public static QueryResult<FreeAcademicVacationSendOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeAcademicVacationSendOrder(id);
        return MapPartialFromDbBase(reader, order);
    }
    public static Result<FreeAcademicVacationSendOrder> Create(int id, StudentDurableStatesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<FreeAcademicVacationSendOrder>(id);
        if (result.IsFailure)
        {
            return result.RetraceFailure<FreeAcademicVacationSendOrder>();
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentDurableStatesCollection.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeAcademicVacationSendOrder>();
        }
        order._toSendToVacation = dtoAsModelResult.ResultObject;
        return result;
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        throw new NotImplementedException();
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var student in _toSendToVacation)
        {
            // для данного приказа студент должен быть зачислен
            var studentState = student.Student.GetHistory(scope);
            if (!studentState.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не зачислен", student.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction scope)
    {
        // группа у студента будет в любом случае, т.к. он должен быть зачислен прежде, чем уйдет в академ
        ConductBase(
             _toSendToVacation.States.Select(x => x.ToRecord(
                this,
                x.Student.GetHistory(scope).GetLastRecord()!.GroupToNullRestrict
             )), scope);

        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeAcademicVacationSend;
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _toSendToVacation.Select(x => x.Student);
    }
}