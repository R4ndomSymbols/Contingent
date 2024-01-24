using StudentTracking.Models.Domain.Misc;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class EducationalLevelRecordDTO{

    public int? StudentId {get; init;}
    public int Type {get; init; }
    public string TypeName {get; init;}
 
    public EducationalLevelRecordDTO(StudentEducationalLevelRecord model){
        StudentId = model.OwnerId;
        Type = (int)model.Recorded;
        TypeName = model.GetLevelName();
    }

    public EducationalLevelRecordDTO(EducationLevel level){
        StudentId = null;
        Type = (int)level.Level;
        TypeName = level.GetLevelName();
    }
}