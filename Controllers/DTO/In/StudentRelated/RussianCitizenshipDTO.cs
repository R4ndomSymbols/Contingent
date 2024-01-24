using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;
[Serializable]
public class RussianCitizenshipDTO {

    public RussianCitizenshipDTO()
    {
        
    }
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public string Surname { get; set; }
    public string? Patronimyc { get; set; }
    [JsonRequired]
    public string PassportNumber {get; set; }
    [JsonRequired]
    public string PassportSeries {get; set; }
    public int LegalAddressId { get; set; }

}
