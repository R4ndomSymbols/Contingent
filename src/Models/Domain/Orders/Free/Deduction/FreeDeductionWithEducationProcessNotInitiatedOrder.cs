using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;
// приказ о об отчислении в связи с неприступлением к обучению
public class FreeDeductionWithEducationProcessNotInitiatedOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _eduProcessDisregardful;

    protected FreeDeductionWithEducationProcessNotInitiatedOrder() : base()
    {
        _eduProcessDisregardful = StudentGroupNullifyMoveList.Empty;
    }

    protected FreeDeductionWithEducationProcessNotInitiatedOrder(int id) : base(id)
    {
        _eduProcessDisregardful = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<FreeDeductionWithEducationProcessNotInitiatedOrder> Create(OrderDTO? dto)
    {
        var created = new FreeDeductionWithEducationProcessNotInitiatedOrder();
        var result = MapBase(dto, created);
        return result;
    }

    public static QueryResult<FreeDeductionWithEducationProcessNotInitiatedOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithEducationProcessNotInitiatedOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    public static Result<FreeDeductionWithEducationProcessNotInitiatedOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var model = new FreeDeductionWithEducationProcessNotInitiatedOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithEducationProcessNotInitiatedOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var moves = StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure)
        {
            return result.RetraceFailure<FreeDeductionWithEducationProcessNotInitiatedOrder>();
        }
        var order = result.ResultObject;
        order._eduProcessDisregardful = moves.ResultObject;
        return result;

    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        ConductBase(_eduProcessDisregardful.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithEducationProcessNotInitiated;
    }
    // приказ об отчислении в связи с неприступлением к обучению
    // студент должен быть зачислен, это достаточное условие
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility()
    {
        foreach (var graduate in _eduProcessDisregardful)
        {
            if (!graduate.Student.History.IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не может быть отчислен прежде собственного зачисления", graduate.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var desiredDto = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var desired = StudentGroupNullifyMove.Create(desiredDto);
        if (desired.IsFailure)
        {
            return Result<Order>.Failure(desired.Errors);
        }
        _eduProcessDisregardful.Add(desired.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _eduProcessDisregardful.Select(s => s.Student);
    }
}