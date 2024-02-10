using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using Utilities;

namespace StudentTracking.Models.Domain.Orders.OrderData;

public class StudentGroupNullifyMove {
    public StudentModel Student {get; private init;} 

    private StudentGroupNullifyMove(StudentModel studentModel){
        Student = studentModel;
    }

    public static async Task<Result<StudentGroupNullifyMove?>> Create(int studentId){
        var student = await StudentModel.GetStudentById(studentId);
        if (student is null){
            return Result<StudentGroupNullifyMove>.Failure(new ValidationError("Студент, указанный в dto, не существует"));
        }
        return Result<StudentGroupNullifyMove>.Success(new StudentGroupNullifyMove(student));
    }
}

public class StudentGroupNullifyMoveList : IEnumerable<StudentGroupNullifyMove>
{

    public IReadOnlyList<StudentGroupNullifyMove> Moves {get; private init;}

    private StudentGroupNullifyMoveList(){

    }

    public static async Task<Result<StudentGroupNullifyMoveList?>> Create(IEnumerable<int>? ids){
        var list = new List<StudentGroupNullifyMove>();
        if (ids is null || !ids.Any()){
            return Result<StudentGroupNullifyMoveList>.Failure(new ValidationError("Список DTO студентов пустой или не указан")); 
        }
        foreach (int id in ids){
            var result = await StudentGroupNullifyMove.Create(id);
            if (result.IsFailure){
                return Result<StudentGroupNullifyMoveList?>.Failure(result.Errors); 
            }
            else{
                list.Add(result.ResultObject);
            }
        }
        return Result<StudentGroupNullifyMoveList?>.Success(new StudentGroupNullifyMoveList(){Moves = list}); 
    }

    public IEnumerable<StudentFlowRecord> ToRecords(Order orderBy){
        var list = new List<StudentFlowRecord>();
        foreach(var i in this){
            list.Add(new StudentFlowRecord(orderBy, i.Student, null));
        }
        return list;  
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
