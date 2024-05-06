using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Import;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;
using System.Text.RegularExpressions;
using Contingent.Models.Domain.Groups;

namespace Contingent.Models.Domain.Orders;

public class PaidDeductionWithGraduationOrder : AdditionalContingentOrder
{
    private StudentGroupNullifyMoveList _graduates;

    private PaidDeductionWithGraduationOrder() : base()
    {
        _graduates = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithGraduationOrder(int id) : base(id)
    {
        _graduates = StudentGroupNullifyMoveList.Empty;
    }

    public static Result<PaidDeductionWithGraduationOrder> Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithGraduationOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static Result<PaidDeductionWithGraduationOrder> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithGraduationOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = StudentGroupNullifyMoveList.Create(dto);
        if (dtoAsModelResult.IsFailure || order is null)
        {
            return dtoAsModelResult.RetraceFailure<PaidDeductionWithGraduationOrder>();
        }
        order._graduates = dtoAsModelResult.ResultObject;
        return result;
    }

    public static QueryResult<PaidDeductionWithGraduationOrder?> Create(int id, NpgsqlDataReader reader)
    {
        var order = new PaidDeductionWithGraduationOrder(id);
        return MapPartialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var checkResult = base.CheckConductionPossibility(_graduates.Select(x => x.Student));
        if (checkResult.IsFailure)
        {
            return checkResult;
        }
        ConductBase(_graduates?.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithGraduation;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var student in _graduates)
        {
            var group = student.Student.History.GetCurrentGroup();
            var check = group is not null && group.IsGraduationGroup();
            if (!check)
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("cтудент не может быть отчислен из группы {0}", group is null ? GroupModel.InvalidNamePlaceholder : group.GroupName), student.Student)
                    );
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        Save(null);
        var graduate = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var result = StudentGroupNullifyMove.Create(graduate);
        if (result.IsFailure)
        {
            return Result<Order>.Failure(result.Errors);
        }
        _graduates.Add(result.ResultObject);
        return Result<Order>.Success(this);
    }
}