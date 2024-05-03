using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class SpecialitySearchQueryDTO
{

    public SpecialitySearchQueryDTO()
    {
        SearchString = null;
    }
    [JsonInclude]
    public string? SearchString { get; set; }

}
