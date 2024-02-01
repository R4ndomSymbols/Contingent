using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;
[Serializable]
public class RussianCitizenshipDTO {

    public RussianCitizenshipDTO()
    {
        Id = null;   
    }
    public int? Id {get; set; }
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public string Surname { get; set; }
    public string? Patronymic { get; set; }
    [JsonRequired]
    public string PassportNumber {get; set; }
    [JsonRequired]
    public string PassportSeries {get; set; }
    public int LegalAddressId { get; set; }

}
