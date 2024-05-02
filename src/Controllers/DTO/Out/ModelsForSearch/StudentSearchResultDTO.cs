using StudentTracking.Models.Domain.Groups;
using StudentTracking.Models.Domain.Students;

namespace StudentTracking.Controllers.DTO.Out;

public class StudentSearchResultDTO
{

    public string GradeBookNumber { get; init; }
    public int StudentId { get; init; }
    public string StudentFullName { get; init; }
    public int? GroupId { get; init; }
    public string GroupName { get; init; }
    public bool? IsNameGenerated { get; init; }
    public string LinkToModify { get; init; }
    public string LinkToView { get; init; }
    public int RequiredOffset { get; set; }
    public StudentSearchResultDTO(StudentModel? student, GroupModel? group, int requiredOffset = 0)
    {
        if (student is null)
        {
            throw new Exception("Студент как результат поиска должен быть найден");
        }
        RequiredOffset = requiredOffset;
        StudentId = (int)student.Id!;
        StudentFullName = student.GetName();
        GroupId = group?.Id;
        LinkToModify = "/students/modify/" + StudentId.ToString();
        LinkToView = "/students/view/" + StudentId.ToString();
        GroupName = group?.GroupName ?? GroupModel.InvalidNamePlaceholder;
        IsNameGenerated = true;
        GradeBookNumber = student.GradeBookNumber;
    }

}