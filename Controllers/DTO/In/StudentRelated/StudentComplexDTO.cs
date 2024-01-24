using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentComplexDTO
{
    public StudentComplexDTO()
    {
        
    }
    [JsonRequired]
    public AddressDTO ActualAddress { get; set; }
    [JsonRequired]
    public StudentDTO Student { get; set; }
    [JsonRequired]
    public AddressDTO FactAddress { get; set; }
    public RussianCitizenshipDTO? RusCitizenship { get; set; }
    [JsonRequired]
    public List<StudentEducationRecordDTO> Education {get; set;}




}

