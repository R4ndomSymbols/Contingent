using Npgsql;
using Contingent.Models.Domain.Flow.History;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.SQL;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Flow;


public class StudentHistory
{
    private ObservableTransaction? _scope;
    private HistoryByOrderEffectiveDateAsc _history;
    private readonly StudentModel _byStudent;
    public HistoryByOrderEffectiveDateAsc History => _history;

    public ObservableTransaction? CurrentTransaction
    {
        get => _scope;
        set
        {
            _scope = value;
            _history = new HistoryByOrderEffectiveDateAsc(GetHistory());
        }
    }

    public StudentHistory(StudentModel student, ObservableTransaction? scope)
    {
        if (student is null || !Utils.IsValidId(student.Id))
        {
            throw new Exception("Студент должен быть сохранен прежде получения истории на него");
        }
        _byStudent = student;
        _scope = scope;
        _history = new HistoryByOrderEffectiveDateAsc(GetHistory());
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
                        // поиск измнения группы
                        if (rec.GroupTo?.Id != groupFrom.Id)
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
    private IEnumerable<StudentFlowRecord> GetHistory()
    {
        var found = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0, 50),
            new HistoryExtractSettings
            {
                ExtractByStudent = _byStudent,
                ExtractGroups = true,
                ExtractOrders = true,
                IncludeNotRegisteredStudents = false,
                Transaction = _scope,
                SuppressLogs = true
            }
        ).ToList();
        return found;
    }

    public GroupModel? GetCurrentGroup()
    {
        return GetLastRecord()?.GroupTo;
    }
    public GroupModel? GetGroupFromStudentWasDeducted()
    {
        for (int i = _history.Count() - 1; i >= 0; i--)
        {
            if (_history[i].OrderNullRestrict.GetOrderTypeDetails().IsAnyDeduction())
            {
                return _history[i - 1].GroupToNullRestrict;
            }
        }
        return null;
    }

    public StudentStates GetStudentState(out int cycleCount)
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

    public bool IsStudentSentInAcademicVacation()
    {
        var record = GetLastRecord();
        return record is not null && record.OrderNullRestrict.GetOrderTypeDetails().IsAcademicVacationSend();
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
        foreach (var order in _history.Select(rec => rec.OrderNullRestrict))
        {
            if (order.EffectiveDate >= previous && order.EffectiveDate <= next && order.GetOrderTypeDetails().IsAnyEnrollment())
            {
                return true;
            }
        }
        return false;
    }
    // параметер оставлен в случае, если потребуется
    // добавить опциональные приказы

    public static StudentFlowRecord? GetLastRecordOnStudent(StudentModel? student, ObservableTransaction? scope = null)
    {
        if (student is null)
        {
            return null;
        }
        var found = FlowHistory.GetRecordsByFilter(new QueryLimits(0, 1), new HistoryExtractSettings()
        {
            ExtractAbsoluteLastState = true,
            ExtractByStudent = student,
            IncludeNotRegisteredStudents = true,
            Transaction = scope
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

    public static IEnumerable<StudentFlowRecord> GetLastRecordsForManyStudents(QueryLimits limits, (bool actual, bool legal) addressSettings, ObservableTransaction? scope = null)
    {
        return FlowHistory.GetRecordsByFilter(limits, new HistoryExtractSettings()
        {
            ExtractAddress = addressSettings,
            IncludeNotRegisteredStudents = true,
            ExtractAbsoluteLastState = true,
            ExtractGroups = true,
            ExtractOrders = true,
            ExtractStudents = true,
            Transaction = scope
        });
    }
}
