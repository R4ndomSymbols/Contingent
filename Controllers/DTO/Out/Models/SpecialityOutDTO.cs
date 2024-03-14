using StudentTracking.Models;

namespace StudentTracking.Controllers.DTO.Out;
public class SpecialityOutDTO {
    
    public int Id {get; set;}
    public string FgosCode {get; set;}
    public string FgosName {get; set; }
    public string Qualification {get; set; }
    public string FgosPrefix {get; set; }
    public string QualificationPostfix {get; set; }
    public int CourseCount {get; set; }
    public int EducationalLevelIn {get; set; }
    public int EducationalLevelOut {get; set; }
    public int TeachingDepth {get; set;}
    public string FullName {get; set;}
    public int ProgramType { get; set; }

    public SpecialityOutDTO(SpecialityModel model)
    {   
        Id = model.Id;
        FgosCode = model.FgosCode;
        FgosName = model.FgosName;
        Qualification = model.Qualification;
        FgosPrefix = model.FgosPrefix;
        QualificationPostfix = model.QualificationPostfix ?? "";
        CourseCount = model.CourseCount;
        EducationalLevelIn = (int)model.EducationalLevelIn.LevelCode;
        EducationalLevelOut = (int)model.EducationalLevelOut.LevelCode;
        FullName = FgosCode + " " + FgosName + " / " + Qualification;
        ProgramType = (int)model.ProgramType.Type;
    }

    public SpecialityOutDTO(){
        
    }

}