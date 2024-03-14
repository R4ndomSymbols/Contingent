using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentInDTO {
    public StudentInDTO()
    {
        RusCitizenship = null;
        Id = null;
    }
    public int? Id {get; set; }

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
    public RussianCitizenshipInDTO? RusCitizenship {get; set;}
    [JsonRequired]
    public AddressDTO PhysicalAddress {get; set;}
    [JsonRequired]
    public List<StudentEducationRecordDTO> Education {get; set;}
   
    
}
