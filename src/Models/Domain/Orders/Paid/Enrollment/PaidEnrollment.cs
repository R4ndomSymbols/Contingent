using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Import;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidEnrollmentOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _emrollers;
    private OrderTypes[] ForbiddenPreviousOrderTypes =>
        new OrderTypes[] {
            OrderTypes.FreeDeductionWithOwnDesire,
            OrderTypes.FreeDeductionWithAcademicDebt,
            OrderTypes.PaidDeductionWithOwnDesire,
            OrderTypes.PaidDeductionWithAcademicDebt
        };

    protected PaidEnrollmentOrder() : base()
    {
        _emrollers = StudentToGroupMoveList.Empty;
    }
    protected PaidEnrollmentOrder(int id) : base(id)
    {
        _emrollers = StudentToGroupMoveList.Empty;
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
        order._emrollers = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidEnrollmentOrder(id);
        return MapParticialFromDbBase(reader, order);
    }


    public override ResultWithoutValue ConductByOrder()
    {
        var check = base.CheckConductionPossibility(_emrollers.Select(x => x.Student));
        if (check.IsFailure)
        {
            return check;
        }
        ConductBase(_emrollers.ToRecords(this));
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

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var move in _emrollers)
        {
            var lastRecord = move.Student.History.GetLastRecord();
            if (lastRecord is not null &&
                ForbiddenPreviousOrderTypes.Any(x => lastRecord.OrderNullRestict.GetOrderTypeDetails().Type == x) &&
                // разница более чем в 5 лет между приказами является основанием для игнорирования статуса
                (this.EffectiveDate - lastRecord.OrderNullRestict.EffectiveDate) > new TimeSpan(365 * 5, 0, 0, 0)
            )
            {
                return ResultWithoutValue.Failure(new OrderValidationError(string.Format("{0} ранее числился в базе (имеет недопустимый статус)", move.Student.GetName())));
            }
            var group = move.GroupTo;
            // группа первого курса, специальность доступна студенту  
            if (!(group.CourseOn == 1
                && group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student)
                && group.SponsorshipType.IsPaid()
                ))
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Группа {0}, куда зачисляется студент {1}, не сооствествует критериям", group.GroupName, move.Student.GetName()
                        )
                    ));
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
        _emrollers.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }
}
