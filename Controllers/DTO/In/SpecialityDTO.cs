using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class SpecialityDTO {
    [JsonInclude]
    public string FgosCode {get; set;}
    [JsonInclude]
    public string FgosName {get; set; }
    [JsonInclude]
    public string Qualification {get; set; }
    [JsonInclude]
    public string FgosPrefix {get; set; }
    [JsonInclude]
    public string? QualificationPostfix {get; set; }
    [JsonInclude]
    public int CourseCount {get; set; }
    [JsonInclude]
    public int EducationalLevelIn {get; set; }
    [JsonInclude]
    public int EducationalLevelOut {get; set; }
    [JsonInclude]
    public int TeachingDepth {get; set;}

    public SpecialityDTO()
    {
        
    }

}