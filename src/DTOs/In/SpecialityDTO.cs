using System.Text.Json.Serialization;
using Contingent.Import;
using Contingent.Models.Domain.Specialties;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class SpecialityDTO : IFromCSV<SpecialityDTO>
{
    public const string FgosCodeFieldName = "номер фгос";
    public const string FgosNameFieldName = "название фгос";
    public const string QualificationFieldName = "квалификация";
    public const string FgosPrefixFieldName = "префикс названия группы по фгос";
    public const string QualificationPostfixFieldName = "постфикс названия группы по квалификации";
    public const string CourseCountFieldName = "количество курсов на специальности";
    public const string EducationalLevelInFieldName = "поступление на основании";
    public const string EducationalLevelOutFieldName = "выпускной уровень образования";
    public const string TeachingDepthFieldName = "глубина образовательной программы";
    public const string ProgramTypeFieldName = "тип образовательной программы";

    public int? Id { get; set; }

    [JsonInclude]
    public string FgosCode { get; set; }
    [JsonInclude]
    public string FgosName { get; set; }
    [JsonInclude]
    public string Qualification { get; set; }
    [JsonInclude]
    public string FgosPrefix { get; set; }
    [JsonInclude]
    public string? QualificationPostfix { get; set; }
    [JsonInclude]
    public int CourseCount { get; set; }
    [JsonInclude]
    public int EducationalLevelIn { get; set; }
    [JsonInclude]
    public int EducationalLevelOut { get; set; }
    [JsonInclude]
    public int TeachingDepthCode { get; set; }
    [JsonInclude]
    public int ProgramType { get; set; }

    public SpecialityDTO()
    {
        FgosCode = "";
        FgosName = "";
        Qualification = "";
        FgosPrefix = "";
        QualificationPostfix = null;
    }

    public Result<SpecialityDTO> MapFromCSV(CSVRow row)
    {
        FgosCode = row[FgosCodeFieldName]!;
        FgosName = row[FgosNameFieldName]!;
        Qualification = row[QualificationFieldName]!;
        FgosPrefix = row[FgosPrefixFieldName]!;
        QualificationPostfix = row[QualificationPostfixFieldName];
        if (int.TryParse(row[CourseCountFieldName], out int course))
        {
            CourseCount = course;
        }
        EducationalLevelIn = LevelOfEducation.ImportLevelCode(row[EducationalLevelInFieldName]);
        EducationalLevelOut = LevelOfEducation.ImportLevelCode(row[EducationalLevelOutFieldName]);
        ProgramType = TrainingProgram.ImportProgramTypeCode(row[ProgramTypeFieldName]);
        TeachingDepthCode = TeachingDepth.ImportTeachingDepthCode(row[TeachingDepthFieldName]);
        return Result<SpecialityDTO>.Success(this);
    }
}
