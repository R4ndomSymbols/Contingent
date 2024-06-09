using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Students;
using Npgsql;
using Contingent.Utilities;
// приказ об возвращении из академического отпуска
namespace Contingent.Models.Domain.Orders;

public class FreeAcademicVacationReturnOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _returners;

    private FreeAcademicVacationReturnOrder(int id) : base(id)
    {
        _returners = StudentToGroupMoveList.Empty;
    }
    private FreeAcademicVacationReturnOrder() : base()
    {
        _returners = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeAcademicVacationReturnOrder> Create(OrderDTO? dto)
    {
        var created = new FreeAcademicVacationReturnOrder();
        var valResult = MapBase(dto, created);
        return valResult;
    }
    public static QueryResult<FreeAcademicVacationReturnOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeAcademicVacationReturnOrder(id);
        return MapPartialFromDbBase(reader, order);
    }
    public static Result<FreeAcademicVacationReturnOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<FreeAcademicVacationReturnOrder>(id);
        if (result.IsFailure)
        {
            return result.RetraceFailure<FreeAcademicVacationReturnOrder>();
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeAcademicVacationReturnOrder>();
        }
        order._returners = dtoAsModelResult.ResultObject;
        return result;
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        throw new NotImplementedException();
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        // предыдущим приказом должен быть приказ о направлении в академический отпуск
        // студент должен быть в академическом отпуске
        // группа должна быть бюджетной и того же курса
        foreach (var move in _returners)
        {
            var studentState = move.Student.GetHistory(scope);
            var lastState = studentState.GetLastRecord();
            if (lastState is null)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не зачислен", move.Student));
            }
            var lastOrderType = lastState.OrderNullRestrict.GetOrderTypeDetails();
            if (!(studentState.IsStudentSentInAcademicVacation() && lastOrderType.Type == OrderTypes.FreeAcademicVacationSend))
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не находится в академическом отпуске", move.Student));
            }
            if (!lastState.StatePeriod.IsEndedNow())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("академический отпуск у студента еще не закончился", move.Student));
            }
            if (move.GroupTo.SponsorshipType.IsPaid())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть восстановлен во внебюджетную группу по К приказу", move.Student));
            }
            if (move.GroupTo.CourseOn != lastState.GroupToNullRestrict.CourseOn)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть восстановлен в группу другого курса", move.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        // группа у студента будет в любом случае, т.к. он должен быть зачислен прежде, чем уйдет в академ
        ConductBase(_returners.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeAcademicVacationReturn;
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _returners.Select(x => x.Student);
    }
}