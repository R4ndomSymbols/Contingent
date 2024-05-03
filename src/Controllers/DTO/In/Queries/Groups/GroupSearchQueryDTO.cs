using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;
[Serializable]
public class GroupSearchQueryDTO
{
    public GroupSearchQueryDTO()
    {
        GroupName = null;
    }

    public string? GroupName { get; set; }
    [JsonRequired]
    public bool IsActive { get; set; }
}