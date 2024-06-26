using Contingent.Models.Domain.Specialties;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.Out;
public class SpecialityOutDTO
{

    public int Id { get; set; }
    public string FgosCode { get; set; }
    public string FgosName { get; set; }
    public string Qualification { get; set; }
    public string FgosPrefix { get; set; }
    public string QualificationPostfix { get; set; }
    public int CourseCount { get; set; }
    public int EducationalLevelIn { get; set; }
    public int EducationalLevelOut { get; set; }
    public string EducationalLevelInName { get; set; }
    public string EducationalLevelOutName { get; set; }
    public int TeachingDepth { get; set; }
    public string TeachingDepthName { get; set; }
    public string FullName { get; set; }
    public int ProgramType { get; set; }
    public string ProgramTypeName { get; set; }

    public SpecialityOutDTO(SpecialtyModel model)
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
        EducationalLevelInName = model.EducationalLevelIn.RussianName;
        EducationalLevelOutName = model.EducationalLevelOut.RussianName;
        FullName = FgosCode + " " + FgosName + " / " + Qualification;
        ProgramType = (int)model.ProgramType.Type;
        ProgramTypeName = model.ProgramType.Name;
        TeachingDepthName = model.TeachingLevel.RussianName;
    }

    public SpecialityOutDTO()
    {
        Id = Utils.INVALID_ID;
        FgosCode = "";
        FgosName = "";
        Qualification = "";
        FgosPrefix = "";
        QualificationPostfix = "";
        CourseCount = 0;
        EducationalLevelIn = 0;
        EducationalLevelOut = 0;
        EducationalLevelInName = "";
        EducationalLevelOutName = "";
        FullName = "";
        ProgramType = 0;
        ProgramTypeName = "";
        TeachingDepthName = "";
    }

}