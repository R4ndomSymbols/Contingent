using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class SpecialtySearchQueryDTO
{

    public SpecialtySearchQueryDTO()
    {
        SearchString = null;
    }
    [JsonInclude]
    public string? SearchString { get; set; }

}
