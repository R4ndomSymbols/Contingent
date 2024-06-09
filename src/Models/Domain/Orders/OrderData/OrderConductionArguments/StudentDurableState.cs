using System.Collections;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;
using Contingent.Models.Infrastructure;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders.OrderData;

// состояние приказа для проведения приказа об отправке в академический отпуск
public class StudentDurableState
{
    // для приказа о направлении в академический отпуск дата вступления в силу не играет роли
    public StudentModel Student { get; private set; }
    public Period StatePeriod { get; private set; }

    private StudentDurableState(StudentModel student, Period statePeriod)
    {
        Student = student;
        StatePeriod = statePeriod;
    }

    public static Result<StudentDurableState> Create(StudentDurableStateDTO? studentDTO)
    {
        if (studentDTO is null)
        {
            return Result<StudentDurableState>.Failure(
                ValidationError.GetNullReceivedError("Источник данных (dto) должен быть указан"));
        }
        var studentResult = OrderDataExtractions.GetStudent(studentDTO.Student);
        if (studentResult.IsFailure)
        {
            return Result<StudentDurableState>.Failure(studentResult.Errors);
        }
        if (!Utils.TryParseDate(studentDTO.StartDate, out DateTime startDate))
        {
            return Result<StudentDurableState>.Failure(new ValidationError(
                "StartDate", "Неверно указана дата направления в академический отпуск"));
        }
        if (!Utils.TryParseDate(studentDTO.EndDate, out DateTime endDate))
        {
            return Result<StudentDurableState>.Failure(new ValidationError(
                "EndDate", "Неверно указана дата окончания академического отпуска"));
        }
        if (startDate >= endDate)
        {
            return Result<StudentDurableState>.Failure(new ValidationError(
                "Time", "Дата начала не может быть позже даты окончания или равна ей"));
        }
        return Result<StudentDurableState>.Success(new StudentDurableState(
            studentResult.ResultObject,
            new Period(startDate, endDate)
        ));
    }

    public StudentFlowRecord ToRecord(Order orderBy, GroupModel groupFrom)
    {
        return StudentFlowRecord.FromModel(orderBy, Student, groupFrom, StatePeriod);
    }
}

public class StudentDurableStatesCollection : IEnumerable<StudentDurableState>
{

    private List<StudentDurableState> _states;

    public IReadOnlyCollection<StudentDurableState> States => _states;
    public static StudentDurableStatesCollection Empty => new(new List<StudentDurableState>());

    private StudentDurableStatesCollection(List<StudentDurableState> states)
    {
        _states = states;
    }

    public static Result<StudentDurableStatesCollection> Create(StudentDurableStatesDTO? states)
    {
        if (states is null || states.Statements is null || states.Statements.Count == 0)
        {
            return Result<StudentDurableStatesCollection>.Failure(
                ValidationError.GetNullReceivedError("Источник данных (dto) должен быть указан"));
        }
        var result = new List<StudentDurableState>();
        foreach (var state in states.Statements)
        {
            var stateResult = StudentDurableState.Create(state);
            if (stateResult.IsFailure)
            {
                return Result<StudentDurableStatesCollection>.Failure(stateResult.Errors);
            }
            if (result.Any(x => x.Student.Equals(stateResult.ResultObject.Student)))
            {
                return Result<StudentDurableStatesCollection>.Failure(new ValidationError(
                    nameof(States), "Набор временных состояний студента не может включать одного и того же студента дважды"
                ));
            }
            result.Add(stateResult.ResultObject);
        }
        return Result<StudentDurableStatesCollection>.Success(new StudentDurableStatesCollection(result));
    }

    public IEnumerator<StudentDurableState> GetEnumerator()
    {
        return _states.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
