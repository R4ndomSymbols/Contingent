using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Import;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

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
        return MapParticialFromDbBase(reader, order);
    }


    public override ResultWithoutValue ConductByOrder()
    {
        var result = base.CheckConductionPossibility(_graduates.Select(x => x.Student));
        if (result.IsFailure)
        {
            return result;
        }
        ConductBase(_graduates.ToRecords(this));
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithGraduation;
    }

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var i in _graduates)
        {
            var aggregate = StudentHistory.GetLastRecordOnStudent(i.Student);
            var group = aggregate?.GroupTo;
            if (group is null || group.CourseOn != group.EducationProgram.CourseCount || group.SponsorshipType.IsPaid())
            {
                return ResultWithoutValue.Failure(new ValidationError(nameof(_graduates), "Один или несколько студентов в приказе не соответствуют критериям"));
            }
        }
        return ResultWithoutValue.Success();
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        var graduateDto = new StudentGroupNullifyMoveDTO().MapFromCSV(row).ResultObject;
        var gradute = StudentGroupNullifyMove.Create(graduateDto);
        if (gradute.IsFailure)
        {
            return Result<Order>.Failure(gradute.Errors);
        }
        _graduates.Add(gradute.ResultObject);
        return Result<Order>.Success(this);
    }
}
