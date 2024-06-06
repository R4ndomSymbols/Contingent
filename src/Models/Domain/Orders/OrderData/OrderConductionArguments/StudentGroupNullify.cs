using System.Collections;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Students;
using Contingent.SQL;
using Utilities;

namespace Contingent.Models.Domain.Orders.OrderData;

public class StudentGroupNullifyMove
{
    public StudentModel Student { get; private set; }

    private StudentGroupNullifyMove(StudentModel studentModel)
    {
        Student = studentModel;
    }

    public static Result<StudentGroupNullifyMove> Create(StudentGroupNullifyMoveDTO? dto)
    {
        if (dto is null)
        {
            return Result<StudentGroupNullifyMove>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }
        var student = OrderDataExtractions.GetStudent(dto.Student);
        if (student.IsFailure)
        {
            return Result<StudentGroupNullifyMove>.Failure(student.Errors);
        }
        return Result<StudentGroupNullifyMove>.Success(new StudentGroupNullifyMove(student.ResultObject));
    }
}

public class StudentGroupNullifyMoveList : IEnumerable<StudentGroupNullifyMove>
{
    public static StudentGroupNullifyMoveList Empty => new StudentGroupNullifyMoveList();
    private List<StudentGroupNullifyMove> _moves;
    public IReadOnlyList<StudentGroupNullifyMove> Moves => _moves;

    private StudentGroupNullifyMoveList()
    {
        _moves = new List<StudentGroupNullifyMove>();
    }

    public static Result<StudentGroupNullifyMoveList> Create(StudentGroupNullifyMovesDTO? dto)
    {
        var built = new StudentGroupNullifyMoveList();
        if (dto is null || !dto.Students.Any())
        {
            return Result<StudentGroupNullifyMoveList>.Failure(new ValidationError("Список студентов пустой или не указан"));
        }
        foreach (var studentMove in dto.Students)
        {
            var result = StudentGroupNullifyMove.Create(studentMove);
            if (result.IsFailure)
            {
                return Result<StudentGroupNullifyMoveList>.Failure(result.Errors);
            }
            else
            {
                built.Add(result.ResultObject);
            }
        }
        return Result<StudentGroupNullifyMoveList>.Success(built);
    }
    public IEnumerable<StudentFlowRecord> ToRecords(Order orderBy)
    {
        var list = new List<StudentFlowRecord>();
        foreach (var i in this)
        {
            list.Add(StudentFlowRecord.FromModel(orderBy, i.Student, null));
        }
        return list;
    }
    public IEnumerable<StudentModel> ToStudentCollection()
    {
        return Moves.Select(x => x.Student);
    }

    public IEnumerator<StudentGroupNullifyMove> GetEnumerator()
    {
        return Moves.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return Moves.GetEnumerator();
    }
    public void Add(StudentGroupNullifyMove move)
    {
        _moves.Add(move);
    }



}
