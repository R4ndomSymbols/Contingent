using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Students;
using Npgsql;
using Utilities;

namespace Contingent.Models.Domain.Orders;
// приказ о восстановлении студента из академического отпуска
public class PaidAcademicVacationReturnOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _toReturnFromVacation;

    private PaidAcademicVacationReturnOrder(int id) : base(id)
    {
        _toReturnFromVacation = StudentToGroupMoveList.Empty;
    }
    private PaidAcademicVacationReturnOrder() : base()
    {
        _toReturnFromVacation = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidAcademicVacationReturnOrder> Create(OrderDTO? dto)
    {
        var created = new PaidAcademicVacationReturnOrder();
        var valResult = MapBase(dto, created);
        return valResult;
    }
    public static QueryResult<PaidAcademicVacationReturnOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidAcademicVacationReturnOrder(id);
        return MapPartialFromDbBase(reader, order);
    }
    public static Result<PaidAcademicVacationReturnOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidAcademicVacationReturnOrder>(id);
        if (result.IsFailure)
        {
            return result.RetraceFailure<PaidAcademicVacationReturnOrder>();
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<PaidAcademicVacationReturnOrder>();
        }
        order._toReturnFromVacation = dtoAsModelResult.ResultObject;
        return result;
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        throw new NotImplementedException();
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility()
    {
        // предыдущим приказом должен быть приказ о направлении в академический отпуск
        // студент должен быть в академическом отпуске
        // группа должна быть внебюджетной и того же курса
        foreach (var move in _toReturnFromVacation)
        {
            var studentState = move.Student.History;
            var lastState = studentState.GetLastRecord();
            if (lastState is null)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не зачислен", move.Student));
            }
            var lastOrderType = lastState.OrderNullRestrict.GetOrderTypeDetails();
            if (!(studentState.IsStudentSentInAcademicVacation() && lastOrderType.Type == OrderTypes.PaidAcademicVacationSend))
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не находится в академическом отпуске", move.Student));
            }
            if (!lastState.StatePeriod.IsEndedNow())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("академический отпуск у студента еще не закончился", move.Student));
            }
            if (move.GroupTo.SponsorshipType.IsFree())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть восстановлен в бюджетную группу по ДК приказу", move.Student));
            }
            if (move.GroupTo.CourseOn != lastState.GroupToNullRestrict.CourseOn)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть восстановлен в группу другого курса", move.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_toReturnFromVacation.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidAcademicVacationReturn;
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _toReturnFromVacation.Select(x => x.Student);
    }
}