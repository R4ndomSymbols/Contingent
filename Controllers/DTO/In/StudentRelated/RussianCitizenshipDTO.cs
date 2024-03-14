using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class RussianCitizenshipInDTO {

    public RussianCitizenshipInDTO()
    {
        Id = null;
        Name = "";
        Surname = "";
        Patronymic = null;
        PassportNumber = "";
        PassportSeries = "";
           
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
    [JsonRequired]
    public AddressDTO LegalAddress{get; set;}

}
