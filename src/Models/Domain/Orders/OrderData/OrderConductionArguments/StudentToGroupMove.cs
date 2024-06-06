using System.Collections;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Utilities;

namespace Contingent.Models.Domain.Orders.OrderData;

public class StudentToGroupMove
{

    public StudentModel Student { get; private set; }
    public GroupModel GroupTo { get; private set; }

    private StudentToGroupMove()
    {
        Student = null!;
        GroupTo = null!;
    }

    public static Result<StudentToGroupMove> Create(StudentToGroupMoveDTO? dto)
    {

        if (dto is null)
        {
            return Result<StudentToGroupMove>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        var model = new StudentToGroupMove();
        var student = OrderDataExtractions.GetStudent(dto.Student);
        if (student.IsFailure)
        {
            return Result<StudentToGroupMove>.Failure(student.Errors);
        }
        var group = OrderDataExtractions.GetGroup(dto.Group);
        if (group.IsFailure)
        {
            return Result<StudentToGroupMove>.Failure(group.Errors);
        }
        model.GroupTo = group.ResultObject;
        model.Student = student.ResultObject;
        return Result<StudentToGroupMove>.Success(model);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not StudentToGroupMove)
        {
            return false;
        }
        return ((StudentToGroupMove)obj).Student.Equals(Student);
    }

}

public class StudentToGroupMoveList : IEnumerable<StudentToGroupMove>
{

    private List<StudentToGroupMove> _moves;
    public IReadOnlyList<StudentToGroupMove> Moves => _moves;

    public static StudentToGroupMoveList Empty => new StudentToGroupMoveList();

    private StudentToGroupMoveList()
    {
        _moves = new List<StudentToGroupMove>();
    }

    public static Result<StudentToGroupMoveList> Create(StudentToGroupMovesDTO? dto)
    {
        var model = new StudentToGroupMoveList();
        if (dto is null || dto.Moves.Count < 1)
        {
            return Result<StudentToGroupMoveList>.Failure(new ValidationError("Не указано ни одной записи для проведения"));
        }
        foreach (var moveDto in dto.Moves)
        {
            var result = StudentToGroupMove.Create(moveDto);
            if (result.IsFailure)
            {
                return Result<StudentToGroupMoveList>.Failure(result.Errors);
            }
            else
            {
                model.Add(result.ResultObject);
            }
        }
        return Result<StudentToGroupMoveList>.Success(model);
    }

    public IEnumerable<StudentFlowRecord> ToRecords(Order orderBy)
    {
        var parsed = new List<StudentFlowRecord>();
        foreach (var m in this)
        {
            parsed.Add(StudentFlowRecord.FromModel(orderBy, m.Student, m.GroupTo));
        }
        return parsed;
    }


    public IEnumerator<StudentToGroupMove> GetEnumerator()
    {
        return Moves.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Moves.GetEnumerator();
    }
    public void Add(StudentToGroupMove move)
    {
        if (_moves.Any(x => x.Equals(move)))
        {
            return;
        }
        _moves.Add(move);
    }
}



