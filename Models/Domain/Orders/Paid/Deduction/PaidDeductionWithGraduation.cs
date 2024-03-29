using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;

public class PaidDeductionWithGraduationOrder : AdditionalContingentOrder
{
    private StudentGroupNullifyMoveList _graduates;

    private PaidDeductionWithGraduationOrder() : base(){
        _graduates = StudentGroupNullifyMoveList.Empty;
    }
    private PaidDeductionWithGraduationOrder(int id) : base(id){
        _graduates = StudentGroupNullifyMoveList.Empty;
    }   

    public static Result<PaidDeductionWithGraduationOrder>Create(OrderDTO? order)
    {
        var created = new PaidDeductionWithGraduationOrder();
        var valResult = MapBase(order, created);
        return valResult;
    }
    public static async Task<Result<PaidDeductionWithGraduationOrder>> Create(int id, StudentGroupNullifyMovesDTO? dto)
    {
        var result = MapFromDbBaseForConduction<PaidDeductionWithGraduationOrder>(id);
        if (result.IsFailure)
        {
            return result;
        }
        var order = result.ResultObject;
        var dtoAsModelResult = await StudentGroupNullifyMoveList.Create(dto);
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
        return MapParticialFromDbBase(reader, order);
    }

    public override ResultWithoutValue ConductByOrder()
    {
        var checkResult = base.CheckConductionPossibility(_graduates.Select(x => x.Student));
        if (checkResult.IsFailure)
        {
            return checkResult;
        }
        ConductBase(_graduates?.ToRecords(this)).RunSynchronously();
        return ResultWithoutValue.Success();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.PaidDeductionWithGraduation;
    }

    protected override ResultWithoutValue CheckSpecificConductionPossibility()
    {
        foreach (var student in _graduates){
            var history = student.Student.History;
            if (history.IsStudentEnlisted()){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не имеет недопустимый статус (не зачислен)", student.Student.GetName())
                    )
                );
            }
            var group = history.GetLastRecord().GroupToNullRestrict;
            if (group.EducationProgram.CourseCount != group.CourseOn){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Группа {0} не является выпускной для студента {1}", group.GroupName, student.Student.GetName())
                    )
                );
            }
        }
        return ResultWithoutValue.Success(); 
    }

}