using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class PaidEnrollmentOrder : AdditionalContingentOrder
{
    private StudentToGroupMoveList _enrollers;
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


    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_enrollers.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
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
    // либо студент не зачислялся, либо отчислен
    // группа внебюджет и специальность доступна
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var move in _enrollers)
        {
            var history = move.Student.GetHistory(scope);
            if (!(history.IsStudentDeducted() || history.IsStudentNotRecorded()))
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент имеет недопустимый статус (зачислен)", move.Student));
            }
            var group = move.GroupTo;
            var groupCheck = group.EducationProgram.IsStudentAllowedByEducationLevel(move.Student) && group.SponsorshipType.IsPaid();

            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    "студент не соответствует критериям специальности или группа бесплатная", move.Student)
                );
            }
        }
        return ResultWithoutValue.Success();

    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var enroller = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentToGroupMove.Create(enroller);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _enrollers.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _enrollers.Select(s => s.Student);
    }
}
