using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;
public class GroupHistoryQueryDTO
{

    public GroupHistoryQueryDTO()
    {
        OnDate = "";
    }
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string OnDate { get; set; }

}