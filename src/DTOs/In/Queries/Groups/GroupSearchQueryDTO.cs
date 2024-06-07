using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;
[Serializable]
public class GroupSearchQueryDTO
{
    public GroupSearchQueryDTO()
    {
        GroupName = null;
    }
    [JsonRequired]
    public string? GroupName { get; set; }
    public bool OnlyActive { get; set; }
}