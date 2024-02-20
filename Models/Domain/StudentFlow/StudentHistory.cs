using System.Collections;
using System.Data.SqlTypes;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Npgsql;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;
using Utilities;

namespace StudentTracking.Models.Domain.Flow;


public class StudentHistory
{
    public static DateTime CurrentPeriodStartDate {
        get {
            var now = DateTime.Now;         
            return new DateTime(now.Year-1, 10, 1);
        }
    }
    public static DateTime CurrentPeriodEndDate {
        get {
            var now = DateTime.Now;         
            return new DateTime(now.Year, 9, 30);
        }
    }
    private List<StudentFlowRecord> _history;
    public IReadOnlyList<StudentFlowRecord> History => _history;
    private int _studentId;
    private StudentHistory()
    {

    }

    public static async Task<StudentHistory> Create(int studentId){
        var result = new StudentHistory();
        result._history = await GetHistory(studentId);
        return result;
    }

    public static async Task<StudentHistory> Create(StudentModel student){
        var result = new StudentHistory();
        result._history = await GetHistory(student.Id);
        return result;
    }

    public StudentFlowRecord? GetByOrder(Order orderBy){
        foreach (var rec in _history){
            if (rec.ByOrder.Equals(orderBy)){
                return rec;
            }
        }
        return null;
    }

    public StudentFlowRecord? GetClosestBefore(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].ByOrder.EffectiveDate;
            if (effDate < anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }
    public StudentFlowRecord? GetClosestAfter(Order order){
        if (order is null){
            throw new ArgumentNullException(nameof(order)); 
        }
        return GetClosestAfter(order.EffectiveDate);
    }
    public StudentFlowRecord? GetClosestBefore(Order order){
        if (order is null){
            throw new ArgumentNullException(nameof(order)); 
        }
        return GetClosestBefore(order.EffectiveDate);
    }

    public StudentFlowRecord? GetClosestAfter(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].ByOrder.EffectiveDate;
            if (effDate > anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }

    public StudentFlowRecord? GetLastRecord()
    {
        StudentFlowRecord? last = null;
        DateTime max = DateTime.MinValue;
        for (int i = 0; i < _history.Count; i++)
        {
            if (_history[i].ByOrder.EffectiveDate > max)
            {
                last = _history[i];
                max = last.ByOrder.EffectiveDate;
            }
        }
        return last;
        
    }

    // получает всю историю студента в приказах
    // история отсортирована по дате регистрации приказа
    private static async Task<List<StudentFlowRecord>> GetHistory(int id)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT student_flow.id AS sid, student_id, order_id, group_id_to, orders.effective_date AS oed, orders.type AS ot " +
        " FROM student_flow" +
        " JOIN orders ON orders.id = student_flow.order_id " +
        " WHERE student_id = @p1 " +
        " ORDER BY oed ASC";
        NpgsqlCommand cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));

        var raws = new List<RawStudentFlowRecord>();
        List<StudentFlowRecord> found = new();

        await using (conn)
        await using (cmd)
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return found;
            }
            while (reader.Read())
            {   
                var record = new RawStudentFlowRecord();
                record.Id = (int)reader["sid"];
                record.OrderId = (int)reader["order_id"];
                record.StudentId = id;
                record.GroupToId = reader["group_id_to"].GetType() == typeof(DBNull) ? null : (int)reader["group_id_to"];
                raws.Add(record);
            }
        } 
        var groupParams = new SQLParameterCollection();
        var gp1 = groupParams.Add(raws.Where(x => x.GroupToId !=null).Select(x => x.GroupToId).ToArray(), NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer);
        gp1.UseBrackets = true;
        var groupWhere = new ComplexWhereCondition(
            new WhereCondition(
                new Column("group_id", "educational_group"),
                gp1,
                WhereCondition.Relations.InArray
            )
        );
        var groupsFound = await GroupModel.FindGroups(new QueryLimits(0, 100), additionalConditions: groupWhere, addtitionalParameters: groupParams);

        var ordersParams = new SQLParameterCollection();
        var sp1 = ordersParams.Add(raws.Select(x => x.OrderId).ToArray(), NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer);
        sp1.UseBrackets = true;
        var orderWhere = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "orders"),
                sp1,
                WhereCondition.Relations.InArray
            )
        );

        var student = await StudentModel.GetStudentById(id);
        var ordersFound = await Order.FindOrders(new QueryLimits(0, 100), additionalParams: ordersParams, filter: orderWhere);
        foreach(var raw in raws){
            found.Add(
                new StudentFlowRecord(
                    order: ordersFound.Where(x => x.Id == raw.OrderId).First(),
                    student: student,
                    group: groupsFound.Where(x => x.Id == raw.GroupToId).FirstOrDefault()
                )
            );
        }
        return found;
    }

    public static async Task<GroupModel?> GetCurrentStudentGroup(int studentId)
    {   
        var joins = new JoinSection()
        .AppendJoin(
            JoinSection.JoinType.RightJoin,
            new Column("group_id", "educational_group"),
            new Column("group_id_to", "student_flow")
            
        )
        .AppendJoin(JoinSection.JoinType.InnerJoin,
            new Column("order_id", "student_flow"),
            new Column("id" , "orders")
        );
        
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add(studentId);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("student_id", "student_flow"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var orderBy = new OrderByCondition(new Column("effective_date", "orders"), OrderByCondition.OrderByTypes.DESC);
        var found = await GroupModel.FindGroups(new QueryLimits(0,1), 
            additionalJoins: joins, 
            additionalConditions: where, 
            additionalOrderBy: orderBy, 
            addtitionalParameters: parameters);
        if (found.Count == 0){
            return null;
        }
        return found.First();
    }

    public static async Task<GroupModel?> GetCurrentStudentGroup(StudentModel student){
        return await GetCurrentStudentGroup(student.Id);
    }


    public static async Task<bool> IsAnyStudentInNotClosedOrder(IEnumerable<StudentModel> students){
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var cmdText = "SELECT bool_and(orders.is_closed) as all_students_in_closed, COUNT(student_flow.id) AS count_ids FROM student_flow " +
        " JOIN orders on student_flow.order_id = orders.id" +
        " WHERE student_flow.student_id = ANY(@p1)";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        var parameter = new NpgsqlParameter();
        parameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer; 
        parameter.Value = students.Select(x => x.Id).ToArray();
        parameter.ParameterName = "p1";
        cmd.Parameters.Add(parameter);
        using var reader = await cmd.ExecuteReaderAsync();
        reader.Read();
        if ((long)reader["count_ids"] == 0){
            return false;
        }
        return !(bool)reader["all_students_in_closed"];
    }

    public GroupModel? GetCurrentGroup(){
        var last = GetLastRecord();
        return GetLastRecord()?.GroupTo;
    }

    private StudentStates GetStudentState(out int cycleCount){
        // если баланс = 0, студент не зачислен и не отчислен
        // если баланс %10 == 1 , то студент зачислен, но не отчислен
        // если баланс /10 != 0, то студент прошел круг зачисления, отчисления (отчислен) 
        int balance = 0;
        foreach (var record in _history){
            var details = record.ByOrder.GetOrderTypeDetails();
            if (details.IsAnyEnrollment()){
                balance+=1;
            }
            else if (details.IsAnyDeduction()){
                balance+=9;
            }
        }
        cycleCount = balance / 10;
        if (balance == 0){
            return StudentStates.NotRecorded;
        }
        if (balance % 10 == 1){
            return StudentStates.Enlisted;
        } 
        if (balance / 10 == 0){
            return StudentStates.Deducted;
        }
        throw new Exception("Ошибка целостности истории приказов");
    }

    public bool IsStudentEnlisted(){
        return GetStudentState(out int cc) == StudentStates.Enlisted;
    }
    public bool IsStudentNotRecorded(){
        return GetStudentState(out int cc) == StudentStates.NotRecorded;
    }
    public bool IsStudentDeducted(){
        return GetStudentState(out int cc) == StudentStates.Deducted;
    }
    //количество полных кругов отчисления, зачисления
    public int GetCycleCount(){
        GetStudentState(out int cc);
        return cc;
    }

    public bool IsEnlistedInPeriod(DateTime previous, DateTime next){
        foreach(var order in _history.Select(rec => rec.ByOrder)){
            if (order.EffectiveDate >= previous && order.EffectiveDate <= next && order.GetOrderTypeDetails().IsAnyEnrollment()){
                return true;
            }
        }
        return false;
    }
    public bool IsEnlistedInStandardPeriod(){
        return IsEnlistedInPeriod(CurrentPeriodStartDate, CurrentPeriodStartDate);
    }

    public static async Task<bool> IsStudentEnlisted(StudentModel student){
        return (await StudentHistory.Create(student)).IsStudentEnlisted(); 
    }
}
