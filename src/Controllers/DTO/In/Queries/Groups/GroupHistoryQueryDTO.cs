using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;
public class GroupHistoryQueryDTO {

    public GroupHistoryQueryDTO(){
        OnDate = "";
    }
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string OnDate {get; set;}

}