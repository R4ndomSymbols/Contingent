using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class SpecialitySearchQueryDTO {

    public SpecialitySearchQueryDTO()
    {
        SearchString = null;
    }
    [JsonInclude]
    public string? SearchString {get; set;}

}
