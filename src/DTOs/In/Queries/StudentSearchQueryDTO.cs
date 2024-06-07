using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;
[Serializable]
public class StudentSearchQueryDTO
{

    public string? Name { get; set; }
    public string? GroupName { get; set; }
    [JsonRequired]
    public int PageSize { get; set; }
    [JsonRequired]
    public int PageSkipCount { get; set; }
    public StudentSearchQuerySourceDTO? Source { get; set; }

    public StudentSearchQueryDTO()
    {
        Name = "";
        GroupName = "";
    }

}