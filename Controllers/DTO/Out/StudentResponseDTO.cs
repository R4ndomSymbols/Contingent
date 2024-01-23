using StudentTracking.Models;
using StudentTracking.Models.Domain;

namespace StudentTracking.Controllers.DTO.Out;

public class StudentResponseDTO {

    public int StudentId {get; init;}
    public string StudentFullName {get; init; }
    public int? GroupId {get; init; }
    public string GroupName {get; init; }
    public bool? IsNameGenerated {get; init; }

    public StudentResponseDTO(StudentModel student, GroupModel? group){
        StudentId = student.Id;
        StudentFullName = student.GetName().Result;
        GroupId = group?.Id;
        GroupName = group?.GroupName ?? "Нет"; 
        IsNameGenerated = true;
    }

}