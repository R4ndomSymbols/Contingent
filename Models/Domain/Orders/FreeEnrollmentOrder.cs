using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.SQL;
using StudentTracking.Controllers.DTO.In;


namespace StudentTracking.Models.Domain.Orders;

public class FreeEnrollmentOrder : FreeEducationOrder
{
    private EnrollmentOrderFlowDTO _moves; 

    protected FreeEnrollmentOrder() : base()
    {
        
    }

    public static async Task<Result<FreeEnrollmentOrder?>> Create(OrderDTO? order){
        
        var created = new FreeEnrollmentOrder();
        var valResult = created.MapBase(order);
        await created.RequestAndSetNumber();
        created._alreadyConducted = false;
         
        if (valResult.IsSuccess){
            return Result<FreeEnrollmentOrder>.Success(created);
        }
        else {
            return Result<FreeEnrollmentOrder>.Failure(valResult.Errors);
        }  
    }
    public static async Task<Result<FreeEnrollmentOrder?>> Create(int id, EnrollmentOrderFlowDTO? dto){
        var result = await Create(id);
        if (!result.IsSuccess){
            return result;
        }
        var found = result.ResultObject;
        var errors = new List<ValidationError?>();

        if (!errors.IsValidRule(
            dto != null && dto.Moves != null && dto.Moves.Count > 0,
            message: "Агрументы проведения приказа не указаны",
            propName: nameof(_moves)
        )){
            return Result<FreeEnrollmentOrder>.Failure(errors);
        }
        found._moves = dto;
        var conductionStatus = await found.CheckConductionPossibility(); 
        if (conductionStatus.IsFailure){
            errors.AddRange(conductionStatus.Errors);
        }
        if (errors.Any()){
            return Result<FreeEnrollmentOrder>.Failure(errors);
        }        
        return Result<FreeEnrollmentOrder>.Success(found);
    }

    public static async Task<Result<FreeEnrollmentOrder?>> Create (int id){
        var order = new FreeEnrollmentOrder();
        var result = await order.GetBase(id);
        if (!result.IsSuccess){
            return Result<FreeEnrollmentOrder?>.Failure(result.Errors);
        }
        return Result<FreeEnrollmentOrder?>.Success(order);   
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description, is_closed) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8) RETURNING id";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", OrderOrgId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderTypeDetails().Type));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        if (_orderDescription is null){
            cmd.Parameters.Add(new NpgsqlParameter<DBNull>("p7", DBNull.Value));
        }
        else {
            cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _orderDescription));
        }
        cmd.Parameters.Add(new NpgsqlParameter<bool>("p8", _isClosed));
        

        await using (conn)
        await using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            return;
        }  
    }

    // условия приказа о зачислении
    // Студент должен не иметь статуса вообще 
    // либо он должен быть отчислен в связи с выпуском
    // приказ после должен быть любым приказом, кроме приказа о зачислении (любой)
    // зачисление возможно только на ту специальность, которой соответствует уровень 
    // образования студента
    // группа должна быть бесплатной

    // TODO:
    // сейчас возможна запись только одного приказа в день на каждого студента
    // нет проверки на совпадение даты, спросить у предст. предметной области
    
     
    internal override async Task<Result<bool>> CheckConductionPossibility()
    {
        if (_moves is null){
            throw new Exception("Данные для проведения не могут быть пустыми при вызове: " + nameof(CheckConductionPossibility));
        }
        
        foreach (var stm in _moves.Moves){
            var student = await StudentModel.GetStudentById(stm.StudentId);
            if (student == null){
                return Result<bool>.Failure(new ValidationError(nameof(_moves), "Одного из указанных студентов не существует"));
            }
            var history = await StudentHistory.Create(student.Id); 
            bool orderBeforeConditionSatisfied = false;
            var recordBefore = history.GetClosestBefore(_effectiveDate);
            orderBeforeConditionSatisfied =
                recordBefore is null || recordBefore.ByOrder.GetOrderTypeDetails().IsAnyDeduction(); 

            var group = await GroupModel.GetGroupById(stm.GroupToId);
            if (group is null){
                return Result<bool>.Failure(new ValidationError(nameof(_moves), "Одна из указанных групп не существует"));
            }
            var studentTags = await StudentEducationalLevelRecord.GetByOwnerId(stm.StudentId); 
            var validMove =  
                orderBeforeConditionSatisfied &&
                studentTags.Any(x => x.Level.Weight >= group.EducationProgram.EducationalLevelIn.Weight) &&
                group.SponsorshipType.IsFree();
            if (!validMove){
                return Result<bool>.Failure(new ValidationError(nameof(_moves), "Не соблюдены критерии по одной из позиций зачисления"));
            }
        }
        return Result<bool>.Success(true);
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollment;
    }

    public override async Task ConductByOrder()
    {
        if (_alreadyConducted){
            throw new Exception("Невозможно провести приказ повторно");
        }
        var toInsert = new List<RawStudentFlowRecord>();
        foreach(var stm in _moves.Moves){
            toInsert.Add(new RawStudentFlowRecord()
            {
                StudentId = stm.StudentId,
                OrderId = _id,
                GroupToId = stm.GroupToId
            });
            
        }
        await InsertMany(toInsert);
        _alreadyConducted = true;
    }
}


