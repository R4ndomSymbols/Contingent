using Npgsql;
using Contingent.Models.Domain.Flow.History;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.SQL;
using Contingent.Utilities;
using Contingent.Models.Domain.Orders.OrderData;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using Contingent.Models.Infrastructure;

namespace Contingent.Models.Domain.Flow;


public class StudentHistory
{
    private DateTime? _historyBeforeDate;
    private StudentStates _currentState;
    private ObservableTransaction? _scope;
    private HistoryByOrderEffectiveDateAsc _history;
    private readonly StudentModel _byStudent;
    public HistoryByOrderEffectiveDateAsc History => _history;
    public DateTime? HistoryOnDate => _historyBeforeDate;

    public ObservableTransaction? CurrentTransaction
    {
        get => _scope;
        set
        {
            _scope = value;
            _history = new HistoryByOrderEffectiveDateAsc(GetHistory());
        }
    }

    public StudentHistory(StudentModel student, ObservableTransaction? scope, DateTime? beforeDate = null)
    {
        if (student is null || !Utils.IsValidId(student.Id))
        {
            throw new Exception("Студент должен быть сохранен прежде получения истории на него");
        }
        _byStudent = student;
        _scope = scope;
        _historyBeforeDate = beforeDate;
        _history = new HistoryByOrderEffectiveDateAsc(GetHistory());
        _currentState = GetStudentState(out int cc);
    }


    public Order? GetNextGroupChangingOrder(GroupModel groupFrom)
    {
        // история отсортирована
        // сначала старые записи, потом новые
        // состояния 
        // группа не найдена - 0
        // группа найдена - 1
        int state = 0;
        foreach (var rec in _history)
        {
            switch (state)
            {
                case 0:
                    if (rec.GroupTo?.Id == groupFrom.Id)
                    {
                        state = 1;
                    }
                    break;
                case 1:
                    {
                        // поиск изменения группы
                        if (!groupFrom.Equals(rec.GroupTo))
                        {
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
            if (rec.OrderNullRestrict.Equals(orderBy))
            {
                return rec;
            }
        }
        return null;
    }

    public StudentFlowRecord? GetClosestBefore(DateTime anchor)
    {
        return _history.GetClosestBefore(anchor);
    }
    public StudentFlowRecord? GetClosestBefore(Order order)
    {
        return _history.GetClosestBefore(order);
    }

    public StudentFlowRecord? GetClosestAfter(DateTime anchor)
    {
        return _history.GetClosestAfter(anchor);
    }

    public StudentFlowRecord? GetLastRecord()
    {
        if (!_history.Any())
        {
            return null;
        }
        return _history.Last();
    }
    public TimeSpan? GetTimeSinceLastAction(DateTime countTo)
    {
        var last = GetLastRecord();
        if (last is null || last.OrderNullRestrict.EffectiveDate > countTo)
        {
            return null;
        }
        return countTo - last.OrderNullRestrict.EffectiveDate;
    }

    public void RevertHistory(Order startingPoint, ObservableTransaction scope)
    {
        List<int> _toRemove = new();
        _history.RemoveOlderOrEqualThan(startingPoint, (rec) => _toRemove.Add((int)rec.Id));
        FlowHistory.DeleteRecords(_toRemove, scope);
    }

    // получает всю историю студента в приказах
    private ReadOnlyCollection<StudentFlowRecord> GetHistory()
    {
        var found = FlowHistory.GetRecordsByFilter(
            QueryLimits.Unlimited,
            new HistoryExtractSettings
            {
                ExtractByStudent = _byStudent,
                ExtractGroups = true,
                ExtractOrders = true,
                IncludeNotRegisteredStudents = false,
                Transaction = _scope,
                SuppressLogs = false,
                ExtractOnDate = _historyBeforeDate,
            }
        ).ToList();
        return new ReadOnlyCollection<StudentFlowRecord>(found);
    }


    public GroupModel? GetLastGroup()
    {
        return GetLastRecord()?.GroupTo;
    }
    public GroupModel? GetGroupFromStudentWasDeducted()
    {
        if (_currentState != StudentStates.Deducted)
        {
            return null;
        }

        for (int i = _history.Count() - 1; i >= 0; i--)
        {
            if (_history[i].OrderNullRestrict.GetOrderTypeDetails().IsAnyDeduction())
            {
                return _history[i - 1].GroupToNullRestrict;
            }
        }
        return null;
    }

    private StudentStates GetStudentState(out int cycleCount)
    {
        // если баланс = 0, студент не зачислен и не отчислен
        // если баланс %10 == 1 , то студент зачислен, но не отчислен
        // если баланс /10 != 0, то студент прошел круг зачисления, отчисления (отчислен) 
        int balance = 0;
        foreach (var record in _history)
        {
            var details = record.OrderNullRestrict.GetOrderTypeDetails();
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
        // проверка на приказ об академическом отпуске
        var last = _history.Last();
        if (last is not null)
        {
            if (last.OrderNullRestrict.GetOrderTypeDetails().IsAcademicVacationSend())
            {
                return StudentStates.EnlistedInAcademicVacation;
            }
        }
        if (balance == 0)
        {
            return StudentStates.NotRecorded;
        }
        if (balance % 10 == 1)
        {
            return StudentStates.Enlisted;
        }
        // баланс больше нуля, проверка эта выше
        if (balance / 10 == 0)
        {
            return StudentStates.Deducted;
        }
        throw new Exception("Ошибка целостности истории приказов");
    }

    public bool IsStudentSentInAcademicVacation()
    {
        // академический отпуск следует определять как:
        // последний приказ на студента это приказ об отправке в академический отпуск
        // и период отпуска еще не закончился
        return _currentState == StudentStates.EnlistedInAcademicVacation;
    }
    // возвращает текущий период академического отпуска, иначе - null
    public Period? GetCurrentAcademicVacationPeriod()
    {
        if (IsStudentSentInAcademicVacation())
        {
            // конечно, ассоциировать StatePeriod и длительность отпуска не стоит, но, 
            // иначе будет type cast и приведение к типу приказа об академическом отпуске
            // или интерфейсу, что не сильно лучше
            return GetLastRecord()?.StatePeriod;
        }
        return null;
    }

    public bool IsStudentEnlisted()
    {
        return _currentState == StudentStates.Enlisted || _currentState == StudentStates.EnlistedInAcademicVacation;
    }
    public bool IsStudentNotRecorded()
    {
        return _currentState == StudentStates.NotRecorded;
    }
    public bool IsStudentDeducted()
    {
        return _currentState == StudentStates.Deducted;
    }

    public static IEnumerable<StudentFlowRecord> GetLastRecordsForManyStudents(QueryLimits limits, (bool actual, bool legal) addressSettings, ObservableTransaction? scope = null)
    {
        return FlowHistory.GetRecordsByFilter(limits, new HistoryExtractSettings()
        {
            ExtractAddress = addressSettings,
            IncludeNotRegisteredStudents = true,
            ExtractOnlyLastState = true,
            ExtractGroups = true,
            ExtractOrders = true,
            ExtractStudents = true,
            Transaction = scope
        });
    }
    // метод получает студентов по статусу, исходя из множества открывающих студентов
    public static IEnumerable<StudentModel> GetStudentByOrderState(
        DateTime beforeDate,
        IEnumerable<OrderTypeInfo> openOrders,
        IEnumerable<OrderTypeInfo> closingOrders,
        ObservableTransaction? scope)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText =
            " select * from ( " +
            " select student_flow.student_id " +
            " , sum( " +
            " case when orders.type = ANY(@p1) then 1 " +
            " when orders.type = ANY(@p2) " +
            " then -1 end) > 0 as is_enrolled " +
            " from student_flow" +
            " inner join orders on (orders.id = student_flow.order_id)" +
            " WHERE orders.effective_date <= @p3" +
            " group by student_flow.student_id" +
            " ) AS states where states.is_enrolled ";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter()
        {
            ParameterName = "p1",
            Value = openOrders.Select(x => (int)x.Type).ToArray(),
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer
        });
        cmd.Parameters.Add(new NpgsqlParameter()
        {
            ParameterName = "p2",
            Value = closingOrders.Select(x => (int)x.Type).ToArray(),
            NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer
        });
        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("p3", beforeDate));
        var ids = new List<int>();
        using (cmd)
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add((int)reader["student_id"]);
            }

        }
        return StudentModel.GetManyStudents(ids);
    }

    public GroupModel? GetGroupOnDate(DateTime onDate)
    {
        return _history.On(onDate)?.GroupToNullRestrict;
    }
}
