using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Net.Http.Headers;
using StudentTracking.Models.Domain.Orders.OrderData;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ExceptionServices;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.SQL;
using StudentTracking.Controllers.DTO.In;
using Npgsql.Replication;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization.Infrastructure;


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

        foreach (StudentMoveDTO sm in dto.Moves){
            if (!errors.IsValidRule(
                await StudentModel.IsIdExists(sm.StudentId, null) && await GroupModel.IsIdExists(sm.GroupToId, null),
                message: "Неверно указаны студенты или группы при проведении приказа",
                propName: nameof(_moves)
            )){
                return Result<FreeEnrollmentOrder>.Failure(errors);
            }
        }
        found._moves = dto;
        if (errors.IsValidRule(
            await found.CheckConductionPossibility(),
            message: "Проведение приказа невозможно",
            propName: nameof(_moves)
        )){
            return Result<FreeEnrollmentOrder>.Success(found);
        }
        
        return Result<FreeEnrollmentOrder>.Failure(errors); 
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
     
    internal override async Task<bool> CheckConductionPossibility()
    {
        if (_moves is null){
            throw new Exception("Приказ не может быть пустым при вызове:" + nameof(CheckConductionPossibility));
        }

        var conn = await Utils.GetAndOpenConnectionFactory();
        string groupQuery = 
        "SELECT " +
        "educational_group.type_of_financing AS tof, " +
        "educational_program.speciality_in_education_level AS ln " +
        "FROM educational_group " +
        "LEFT JOIN educational_program ON educational_group.program_id = educational_program.id " +
        "WHERE educational_group.id = @p1";

        string studentQuery = 
        "SELECT MAX(level_code) AS sm " +
        "FROM education_tag_history " +
        "JOIN students ON education_tag_history.student_id = student.id " +
        "WHERE education_tag_history.student_id = @p1";
        
        foreach (var stm in _moves.Moves){
            var history = new StudentHistory(stm.StudentId); 
            await history.PopulateHistory();
            bool orderBeforeHasCorrectType = false;
            bool orderAfterHasCorrectType = false;
            if (history != null){
                var orderBefore = history.GetClosestBefore(_effectiveDate);
                orderBeforeHasCorrectType = orderBefore == null || 
                ((StudentFlowRecord)orderBefore).OrderType == OrderTypes.FreeDeductionWithGraduation;
                var orderAfter = history.GetClosestAfter(_effectiveDate);
                orderAfterHasCorrectType = orderAfter == null || !OrderTypeInfo.IsAnyEnrollment(((StudentFlowRecord)orderAfter).OrderType);
            }
            else {
                orderBeforeHasCorrectType = true;
                orderAfterHasCorrectType = true;
            }

            if (!orderBeforeHasCorrectType || !orderAfterHasCorrectType){
                return false;
            }
            var specialityLevel = StudentEducationalLevelRecord.EducationalLevels.NotMentioned;
            var cmd = new NpgsqlCommand(groupQuery, conn);
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", stm.GroupToId));
            using (cmd){
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows){
                    return false;
                }
                reader.Read();
                var fin = (GroupSponsorshipType.Types)(int)reader["tof"];
                // проверка бесплатности обучения в указанной группе
                if (!GroupSponsorshipType.IsFree(fin)){
                    return false;
                }
                specialityLevel = 
                    (StudentEducationalLevelRecord.EducationalLevels)(int)reader["ln"];
            }
            cmd = new NpgsqlCommand(studentQuery, conn);
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", stm.StudentId));
            using (cmd){
                using var reader = cmd.ExecuteReader();
                if (!reader.HasRows){
                    return false;
                }
                reader.Read();
                // проверка соответствия уровня образования студента и группы
                var studentLevel = 
                    (StudentEducationalLevelRecord.EducationalLevels)(int)reader["sm"];
                if (studentLevel<specialityLevel){
                    return false;
                }
            }
        }
        return true;
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollment;
    }

    public override async Task ConductByOrder()
    {
        if (!_alreadyConducted){
            throw new Exception("Невозможно провести приказ повторно");
        }
        var toInsert = new List<StudentFlowRecord>();
        foreach(var stm in _moves.Moves){
            toInsert.Add(new StudentFlowRecord(
                _id,
                stm.StudentId,
                stm.GroupToId
            ));
        }
        await InsertMany(toInsert);
    }
}


