using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;
using Utilities;

namespace StudentTracking.Models.Domain.Flow;


public class StudentHistory
{
    private List<StudentFlowRecord> _history;
    private StudentModel _byStudent;
    public IReadOnlyList<StudentFlowRecord> History => _history;
    private StudentHistory()
    {

    }

    public static StudentHistory Create(StudentModel student)
    {
        var result = new StudentHistory();
        result._byStudent = student;
        result._history = GetHistory(student);
        return result;
    }

    public Order? GetNextGroupChangingOrder(GroupModel groupFrom){
        // история отсортирована
        // сначала старые записи, потом новые
        // состояния 
        // группа не найдена - 0
        // группа найдена - 1
        int state = 0;
        foreach (var rec in _history){
            switch (state) 
            {
                case 0:
                if (rec.Record.GroupToId == groupFrom.Id){
                    state = 1;
                }
                break;
                case 1:
                {   
                    // поиск измнения группы
                    if (rec.Record.GroupToId != groupFrom.Id){
                        return rec.ByOrder;
                    }
                }
                break;
            }
        }
        return null;
    } 

    public StudentFlowRecord? GetByOrder(Order orderBy)
    {
        foreach (var rec in _history)
        {
            if (rec.ByOrder.Equals(orderBy))
            {
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
    public StudentFlowRecord? GetClosestAfter(Order order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }
        return GetClosestAfter(order.EffectiveDate);
    }
    public StudentFlowRecord? GetClosestBefore(Order order)
    {
        if (order is null)
        {
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
    private static List<StudentFlowRecord> GetHistory(StudentModel byStudent)
    {
        var comparison = new Comparison<StudentFlowRecord>(
            (left, right) =>{
                if (left.ByOrder.EffectiveDate < right.ByOrder.EffectiveDate){
                    return -1;
                }
                else if (left.ByOrder.EffectiveDate == right.ByOrder.EffectiveDate){
                    return 0;
                }
                else{
                    return 1;
                }
            }
        ); 

        var found = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0,50),
            new HistoryExtractSettings{
                ExtractByStudent = byStudent,
                ExtractGroups = true,
                ExtractOrders = true,
            }
        ).ToList(); 
        found.Sort(comparison);
        
        return found;
    }

    public static async Task<bool> IsAnyStudentInNotClosedOrder(IEnumerable<StudentModel> students)
    {
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
        if ((long)reader["count_ids"] == 0)
        {
            return false;
        }
        return !(bool)reader["all_students_in_closed"];
    }

    public GroupModel? GetCurrentGroup()
    {
        return GetLastRecord()?.GroupTo;
    }

    private StudentStates GetStudentState(out int cycleCount)
    {
        // если баланс = 0, студент не зачислен и не отчислен
        // если баланс %10 == 1 , то студент зачислен, но не отчислен
        // если баланс /10 != 0, то студент прошел круг зачисления, отчисления (отчислен) 
        int balance = 0;
        foreach (var record in _history)
        {
            var details = record.ByOrder.GetOrderTypeDetails();
            if (details.IsAnyEnrollment())
            {
                balance += 1;
            }
            else if (details.IsAnyDeduction())
            {
                balance += 9;
            }
        }
        cycleCount = balance / 10;
        if (balance == 0)
        {
            return StudentStates.NotRecorded;
        }
        if (balance % 10 == 1)
        {
            return StudentStates.Enlisted;
        }
        if (balance / 10 == 0)
        {
            return StudentStates.Deducted;
        }
        throw new Exception("Ошибка целостности истории приказов");
    }

    public bool IsStudentEnlisted()
    {
        return GetStudentState(out int cc) == StudentStates.Enlisted;
    }
    public bool IsStudentNotRecorded()
    {
        return GetStudentState(out int cc) == StudentStates.NotRecorded;
    }
    public bool IsStudentDeducted()
    {
        return GetStudentState(out int cc) == StudentStates.Deducted;
    }
    //количество полных кругов отчисления, зачисления
    public int GetCycleCount()
    {
        GetStudentState(out int cc);
        return cc;
    }

    public bool IsEnlistedInPeriod(DateTime previous, DateTime next)
    {
        foreach (var order in _history.Select(rec => rec.ByOrder))
        {
            if (order.EffectiveDate >= previous && order.EffectiveDate <= next && order.GetOrderTypeDetails().IsAnyEnrollment())
            {
                return true;
            }
        }
        return false;
    }
    public bool IsEnlistedInStandardPeriod()
    {
        return IsEnlistedInPeriod(FlowHistory.CurrentPeriodStartDate, FlowHistory.CurrentPeriodStartDate);
    }
    // параметер оставлен в случае, если потребуется
    // добавить опциональные приказы

    public static StudentFlowRecord? GetLastRecordOnStudent(StudentModel? student)
    {
        if (student is null)
        {
            return null;
        }
        var found = FlowHistory.GetRecordsByFilter(new QueryLimits(0, 1), new HistoryExtractSettings()
        {
            ExtractLastState = true,
            ExtractByStudent = student,
            IncludeNotRegisteredStudents = true
        });
        if (found.Any())
        {
            return found.First();
        }
        else
        {
            return null;
        }
    }

    public static IEnumerable<StudentFlowRecord> GetLastRecordsForManyStudents(QueryLimits limits, (bool actual, bool legal) addressSettings)
    {
        return FlowHistory.GetRecordsByFilter(limits, new HistoryExtractSettings()
        {
            ExtractAddress = addressSettings,
            ExtractStudentUnique = true,
            IncludeNotRegisteredStudents = true,
            ExtractLastState = true,
            ExtractGroups = true,
            ExtractOrders = true,
            ExtractStudents = true,
        });
    }
}
