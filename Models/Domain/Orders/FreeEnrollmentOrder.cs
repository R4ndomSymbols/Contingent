using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Net.Http.Headers;
using StudentTracking.Models.Services;
using StudentTracking.Models.Domain.Orders.OrderData;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.ExceptionServices;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.SQL;


namespace StudentTracking.Models.Domain.Orders;

public class FreeEnrollmentOrder : FreeEducationOrder
{    
    private List<StudentMove> _moves;
    private bool _populated; 
    public FreeEnrollmentOrder() : base()
    {
        _populated = false;
    }
    public FreeEnrollmentOrder(int id) : base(id){
        _populated = false;
    }
    
    public override async Task FromJSON(OrderModelJSON json){
        await base.FromJSON(json);
    }

    public override async Task Save(ObservableTransaction? scope)
    {
        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        PrintValidationLog();
        NpgsqlConnection? conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO public.orders( " +
        " specified_date, effective_date, serial_number, org_id, type, name, description) " +
        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7) RETURNING id";
        var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p1", _specifiedDate));
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p2", _effectiveDate));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p3", _orderNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", OrderStringId));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p5", (int)GetOrderType()));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p6", _orderDisplayedName));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p7", _orderDescription));

        await using (conn)
        await using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
            return;
        }  
    }

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        return await GetOrderById(_id);
    }

    // минимальное сравнение, модификация полей не предусмотрена
    public override bool Equals(IDbObjectValidated? other)
    {
        if (other is null){
            return false;
        }
        if (other.GetType() != this.GetType()){
            return false;
        }
        var unboxed = (FreeEnrollmentOrder)other;
        return 
            _id == unboxed._id;
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
     
    protected override async Task<bool> CheckInsertionPossibility()
    {
        if (_moves.Count == 0){
            return false;
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
        
        foreach (var stm in _moves){
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

    public override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeEnrollment;
    }

    public override async Task<bool> ConductOrder(object orderConductData)
    {
        

        if (await CheckInsertionPossibility()){
            return false; 
        }
        var toInsert = new List<StudentFlowRecord>();
        foreach(var stm in _moves){
            toInsert.Add(new StudentFlowRecord(
                _id,
                stm.StudentId,
                stm.GroupToId
            ));
        }
        await OrderConsistencyMaintain.InsertMany(toInsert);
        return true;

    }
    public override Task<bool> AddPendingMoves(OrderInStudentFlowJSON json)
    {   

        _moves ??= new List<StudentMove>();
        _moves.Clear();
        _populated = false;
        return new Task<bool>(
            () => {
                if (json == null){
                    return false;
                }
                if (Order.IsOrderExists(json.OrderId)){
                    return false;
                }
                if (json.Records == null || json.Records.Count == 0){
                    return false;
                }
                _populated = true;
                json.Records?.ForEach((x) => 
                    {
                        var pending = new StudentMove();
                        pending.GroupToId = x.GroupToId;
                        pending.StudentId = x.StudentId;
                        if (pending.CheckErrorsExist()){
                            _moves.Clear();
                            _populated = false;
                            return;
                        }
                        _moves.Add(pending);
                    }  
                );
                return _populated;
            });
    }
}


