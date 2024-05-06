using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Flow;

namespace Contingent.Models.Domain.Orders;

public class PaidEnrollmentOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _enrollers;
    private OrderTypes[] ForbiddenPreviousOrderTypes =>
        new OrderTypes[] {
            OrderTypes.FreeDeductionWithOwnDesire,
            OrderTypes.FreeDeductionWithAcademicDebt,
            OrderTypes.PaidDeductionWithOwnDesire,
            OrderTypes.PaidDeductionWithAcademicDebt
        };

    protected PaidEnrollmentOrder() : base()
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }
    protected PaidEnrollmentOrder(int id) : base(id)
    {
        _enrollers = StudentToGroupMoveList.Empty;
    }

    public static Result<PaidEnrollmentOrder> Create(OrderDTO? order)
    {
        var created = new PaidEnrollmentOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidEnrollmentOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidEnrollmentOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidEnrollmentOrder>();
        }
        order._enrollers = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidEnrollmentOrder(id);
        return MapPartialFromDbBase(reader, order);
    }


    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_enrollers.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_enrollers.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidEnrollment;
    }
    // первый курс не обязателен
    // TODO: спросить, можно ли зачислять студента, отчислившегося по собственному желанию на другую специальность
    // без приказа о восстановлении
    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _enrollers)
        {
            var history = new StudentHistory(move.Student);
            var lastRecord = history.GetLastRecord();
            var orderCheck = lastRecord is not null && ForbiddenPreviousOrderTypes.All(x => lastRecord.OrderNullRestrict.GetOrderTypeDetails().Type != x);
            // разница более чем в 5 лет между приказами является основанием для игнорирования статуса
            var timeCheck = history.GetTimeSinceLastAction(_effectiveDate) >= TimeSpan.FromDays(365 * 5);
            var group = move.GroupTo;
            var groupCheck = group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student) && group.SponsorshipType.IsPaid();

            if ((orderCheck || timeCheck) && groupCheck)
            {
                continue;
            }
            else
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    "студент имеет недопустимый статус или не соответствует критериям группы или группа бесплатная", move.Student)
                );
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
        _enrollers.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }
}
