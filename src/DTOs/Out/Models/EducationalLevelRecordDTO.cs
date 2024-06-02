using Contingent.Models.Domain.Specialties;
using Contingent.Models.Domain.Students;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class EducationalLevelRecordDTO
{

    public int? StudentId { get; init; }
    public int Type { get; init; }
    public string TypeName { get; init; }

    public EducationalLevelRecordDTO(StudentEducationalLevelRecord model)
    {
        StudentId = model.Owner.Id;
        Type = (int)model.Level.LevelCode;
        TypeName = model.RussianName;
    }

    public EducationalLevelRecordDTO(LevelOfEducation level)
    {
        StudentId = null;
        Type = (int)level.LevelCode;
        TypeName = level.RussianName;
    }
}