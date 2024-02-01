using System.Text.Json.Serialization;
using StudentTracking.Models.Domain;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentDTO {
    public StudentDTO()
    {
        RussianCitizenshipId = null;
        Id = null;
    }

    [JsonRequired]
    public string GradeBookNumber {get; set;}
    [JsonRequired]
    public string DateOfBirth { get; set; }
    [JsonRequired]
    public int Gender { get; set; }
    [JsonRequired]
    public string Snils { get; set; }
    [JsonRequired]
    public string Inn { get; set; }
    [JsonRequired]
    public int TargetAgreementType { get; set; }
    [JsonRequired]
    public int PaidAgreementType { get; set; }
    [JsonRequired]
    public string AdmissionScore { get; set; }
    [JsonRequired]
    public string GiaMark { get; set; }
    [JsonRequired]
    public string GiaDemoExamMark { get; set; }
    public int? RussianCitizenshipId { get; set; }
    public int? Id {get; set; }
    
}
