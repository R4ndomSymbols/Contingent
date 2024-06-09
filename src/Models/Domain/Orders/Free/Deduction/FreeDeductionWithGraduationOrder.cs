using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Orders;

public class FreeDeductionWithGraduationOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _graduates;
    protected FreeDeductionWithGraduationOrder() : base()
    {
        _graduates = StudentGroupNullifyMoveList.Empty;
    }
    protected FreeDeductionWithGraduationOrder(int id) : base(id)
    {
        _graduates = StudentGroupNullifyMoveList.Empty;
    }
    public static Result<FreeDeductionWithGraduationOrder> Create(OrderDTO? order)
    {
        var created = new FreeDeductionWithGraduationOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<FreeDeductionWithGraduationOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var model = new FreeDeductionWithGraduationOrder(id);
        var result = MapFromDbBaseForConduction<FreeDeductionWithGraduationOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure)
        {
            return dtoAsModelResult.RetraceFailure<FreeDeductionWithGraduationOrder>();
        }
        order._graduates = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<FreeDeductionWithGraduationOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new FreeDeductionWithGraduationOrder(id);
        return MapPartialFromDbBase(reader, order);
    }


    protected override ResultWithoutValue ConductByOrderInternal(ObservableTransaction? scope)
    {
        ConductBase(_graduates.ToRecords(this), scope);
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
    }
    // выпускная группа
    protected override ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope)
    {
        foreach (var graduate in _graduates)
        {
            var group = graduate.Student.GetHistory(scope).GetCurrentGroup();
            if (group is null || !group.IsGraduationGroup())
            {
                return ResultWithoutValue.Failure(new OrderValidationError("студент не учится в выпуской группе", graduate.Student));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var graduateDto = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var graduate = StudentGroupNullifyMove.Create(graduateDto);
        if (graduate.IsFailure)
        {
            return Result<Order>.Failure(graduate.Errors);
        }
        _graduates.Add(graduate.ResultObject);
        return Result<Order>.Success(this);
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        return _graduates.Select(x => x.Student);
    }
}
