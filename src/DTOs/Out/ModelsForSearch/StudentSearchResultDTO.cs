using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;

namespace Contingent.Controllers.DTO.Out;

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
    public StudentSearchResultDTO(StudentModel? student, GroupModel? group)
    {
        if (student is null)
        {
            throw new Exception("Студент как результат поиска должен быть найден");
        }
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