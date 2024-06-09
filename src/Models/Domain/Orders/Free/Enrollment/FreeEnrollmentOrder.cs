using Contingent.Utilities;
using Contingent.Models.Domain.Flow;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Orders.OrderData;
using Npgsql;
using System.Net.Http.Headers;
using Contingent.Import;
using Contingent.Models.Domain.Students;


namespace Contingent.Models.Domain.Orders;

public class FreeEnrollmentOrder : FreeContingentOrder
{
    private StudentToGroupMoveList _moves;

    protected FreeEnrollmentOrder() : base()
    {
        _moves = StudentToGroupMoveList.Empty;
    }
    protected FreeEnrollmentOrder(int id) : base(id)
    {
        _moves = StudentToGroupMoveList.Empty;
    }

    public static Result<FreeEnrollmentOrder> Create(OrderDTO? order)
    {
        var created = new FreeEnrollmentOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<FreeEnrollmentOrder> Create(int id, StudentToGroupMovesDTO? dto)
    {
        var model = new FreeEnrollmentOrder(id);
        var result = MapFromDbBaseForConduction<FreeEnrollmentOrder>(id);
        if (result.IsFailure)
        {
            return result.RetraceFailure<FreeEnrollmentOrder>();
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentToGroupMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeEnrollmentOrder>();
        }
        order._moves = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<FreeEnrollmentOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeEnrollmentOrder(id);
        return MapPartialFromDbBase(reader, order);

    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    // условия приказа о зачислении
    // Студент должен не иметь статуса вообще 
    // либо он должен быть отчислен в связи с выпуском
    // приказ после должен быть любым приказом, кроме приказа о зачислении (любой)
    // зачисление возможно только на ту специальность, которой соответствует уровень 
    // образования студента
    // группа должна быть бесплатной
    // первый курс для группы необязателен

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var stm in _moves.Moves)
        {
            var history = stm.Student.GetHistory(scope);
            var studentCheck = !history.IsStudentEnlisted();
            if (!studentCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError(
                    string.Format("Студент уже зачислен по приказу: {0}", history.GetLastRecord()!.OrderNullRestrict.OrderDisplayedName), stm.Student));
            }
            var targetGroup = stm.GroupTo;
            var groupCheck = targetGroup.EducationProgram.IsStudentAllowedByEducationLevel(stm.Student) && targetGroup.SponsorshipType.IsFree();
            if (!groupCheck)
            {
                return ResultWithoutValue.Failure(new OrderValidationError("Не соблюдены критерии по одной из позиций зачисления", stm.Student));
            }

        }
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollment;
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_moves.ToRecords(this), scope);
        // делает активной группы, куда зачислены студенты и группу после
        foreach (var group in _moves.Select(x => x.GroupTo).Distinct())
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

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var enrolled = new StudentToGroupMoveDTO().MapFromCSV(row).ResultObject;
        var move = StudentToGroupMove.Create(enrolled);
        if (move.IsFailure)
        {
            return Result<Order>.Failure(move.Errors);
        }
        _moves.Add(move.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _moves.Select(x => x.Student);
    }
}


