using System.Collections;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using Utilities;

namespace StudentTracking.Models.Domain.Orders.OrderData;

public class StudentGroupNullifyMove {
    public StudentModel Student {get; private init;} 

    private StudentGroupNullifyMove(StudentModel studentModel){
        Student = studentModel;
    }

    public static async Task<Result<StudentGroupNullifyMove>> Create(int studentId){
        var student = await StudentModel.GetStudentById(studentId);
        if (student is null){
            return Result<StudentGroupNullifyMove>.Failure(new ValidationError("Указанного студента не существует"));
        }
        return Result<StudentGroupNullifyMove>.Success(new StudentGroupNullifyMove(student));
    }
}

public class StudentGroupNullifyMoveList : IEnumerable<StudentGroupNullifyMove>
{
    public static StudentGroupNullifyMoveList Empty => new StudentGroupNullifyMoveList();

    public IReadOnlyList<StudentGroupNullifyMove> Moves {get; private init;}

    private StudentGroupNullifyMoveList(){
        Moves = new List<StudentGroupNullifyMove>();
    }

    public static async Task<Result<StudentGroupNullifyMoveList>> Create(IEnumerable<int>? ids){
        var list = new List<StudentGroupNullifyMove>();
        if (ids is null || !ids.Any()){
            return Result<StudentGroupNullifyMoveList>.Failure(new ValidationError("Список студентов пустой или не указан")); 
        }
        foreach (int id in ids){
            var result = await StudentGroupNullifyMove.Create(id);
            if (result.IsFailure){
                return Result<StudentGroupNullifyMoveList>.Failure(result.Errors); 
            }
            else{
                list.Add(result.ResultObject);
            }
        }
        return Result<StudentGroupNullifyMoveList>.Success(new StudentGroupNullifyMoveList(){Moves = list}); 
    }

    public static async Task<Result<StudentGroupNullifyMoveList>> Create(StudentGroupNullifyMovesDTO? moves)
    {
        return await Create(moves?.Students);
    }
    public IEnumerable<StudentFlowRecord> ToRecords(Order orderBy){
        var list = new List<StudentFlowRecord>();
        foreach(var i in this){
            list.Add(new StudentFlowRecord(orderBy, i.Student, null));
        }
        return list;  
    }
    public IEnumerable<StudentModel> ToStudentCollection(){
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



}
