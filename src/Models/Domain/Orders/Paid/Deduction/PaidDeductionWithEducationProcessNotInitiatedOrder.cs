using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using System.Text.RegularExpressions;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;
// приказ об отчислении в связи с неприступлением к обучению
public class PaidDeductionWithEducationProcessNotInitiatedOrder : AdditionalContingentOrder
{
    private StudentGroupNullifyMoveList _whoDisregard;

    private PaidDeductionWithEducationProcessNotInitiatedOrder() : base()
    {
        _whoDisregard = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithEducationProcessNotInitiatedOrder(int id) : base(id)
    {
        _whoDisregard = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithEducationProcessNotInitiatedOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithEducationProcessNotInitiatedOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithEducationProcessNotInitiatedOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithEducationProcessNotInitiatedOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithEducationProcessNotInitiatedOrder>();
        }
        order._whoDisregard = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithEducationProcessNotInitiatedOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithEducationProcessNotInitiatedOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_whoDisregard?.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithEducationProcessNotInitiated;
    }

    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var record in _whoDisregard)
        {
            if (!record.Student.GetHistory(scope).IsStudentEnlisted())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент должен быть зачислен прежде, чем быть отчисленным", record.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var graduate = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(graduate);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _whoDisregard.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _whoDisregard.Select(x => x.Student);
    }
}