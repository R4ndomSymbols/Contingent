using System.Collections;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using Utilities;

namespace StudentTracking.Models.Domain.Orders.OrderData;

public class StudentToGroupMove {

    public StudentModel Student {get; private init;}
    public GroupModel GroupTo {get; private init; }

    private StudentToGroupMove(StudentModel student, GroupModel group){
        Student = student;
        GroupTo = group;
    }

    public static async Task<Result<StudentToGroupMove?>> Create(StudentMoveDTO? dto){

        if (dto is null){
            return Result<StudentToGroupMove?>.Failure(new ValidationError("Источник данных (dto) должен быть указан"));
        }

        var student = await StudentModel.GetStudentById(dto.StudentId);
        if (student is null) {
            return Result<StudentToGroupMove?>.Failure(new ValidationError("Студента, указанного в движении, не существует"));
        }
        var group = await GroupModel.GetGroupById(dto.GroupToId);
        if (group is null){
            return Result<StudentToGroupMove?>.Failure(new ValidationError("Группы, указанной в движении, не существует"));
        }
        return Result<StudentToGroupMove>.Success(new StudentToGroupMove(student, group)); 
    }

}

public class StudentToGroupMoveList : IEnumerable<StudentToGroupMove>{
    public IReadOnlyList<StudentToGroupMove> Moves {get; private init;}

    private StudentToGroupMoveList(List<StudentToGroupMove> moves){
        Moves = moves;
    }

    public static async Task<Result<StudentToGroupMoveList?>> Create(IEnumerable<StudentMoveDTO>? dtos){
        var list = new List<StudentToGroupMove>();
        if (dtos is null || dtos.Count() < 1){
            return Result<StudentToGroupMoveList?>.Failure(new ValidationError("Источник данных (dto) должен быть указан и иметь ненулевую длину"));
        }
        foreach (var dto in dtos){
            var result = await StudentToGroupMove.Create(dto);
            if (result.IsFailure){
                return Result<StudentToGroupMoveList?>.Failure(result.Errors);
            }
            else {
                list.Add(result.ResultObject);
            }
        }
        return Result<StudentToGroupMoveList?>.Success(new StudentToGroupMoveList(list));

    }
    public static async Task<Result<StudentToGroupMoveList?>> Create(StudentGroupChangeMoveDTO? dto)
    {
        return await Create(dto?.Moves);
    }

    public IEnumerable<StudentFlowRecord> ToRecords(Order orderBy){
        var parsed = new List<StudentFlowRecord>();
        foreach (var m in this){
            parsed.Add(new StudentFlowRecord(orderBy, m.Student, m.GroupTo));
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
}



