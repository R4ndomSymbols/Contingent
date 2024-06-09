using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Students;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders;

public class FreeDeductionWithAcademicDebtOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _debtHolders;

    protected FreeDeductionWithAcademicDebtOrder() : base()
    {
        _debtHolders = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithAcademicDebtOrder(int id) : base(id)
    {
        _debtHolders = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithAcademicDebtOrder> Create(OrderDTO? dto)
    {
        var created = new FreeDeductionWithAcademicDebtOrder();
        var result = MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithAcademicDebtOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithAcademicDebtOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    public static Result<FreeDeductionWithAcademicDebtOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var fromDb = new FreeDeductionWithAcademicDebtOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithAcademicDebtOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var moves = StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure)
        {
            return result.RetraceFailure<FreeDeductionWithAcademicDebtOrder>();
        }
        var order = result.ResultObject;
        order._debtHolders = moves.ResultObject;
        return result;
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_debtHolders.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithAcademicDebt;
    }
    // приказ об отчислении в связи с неуспеваемостью
    // не имеет ограничений вообще, главное, чтобы студент был зачислен
    // не нужна проверка группы
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var debtHolder in _debtHolders)
        {
            if (debtHolder.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть отчислен раньше своего зачисления", debtHolder.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var result = new StudentGroupNullifyMoveDTO().MapFromCSV(row);
        var holder = StudentGroupNullifyMove.Create(result.ResultObject);
        if (holder.IsFailure)
        {
            return Result<Order>.Failure(holder.Errors);
        }
        _debtHolders.Add(holder.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _debtHolders.Select(x => x.Student);
    }
}