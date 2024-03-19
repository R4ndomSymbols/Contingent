using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;
[Serializable]
public class StudentSearchQueryDTO {

    public string? Name { get; set; }
    public string? GroupName { get; set; }
    public StudentSearchQuerySourceDTO Source {get; set; }

    [JsonRequired]
    public int PageSize { get; set; }
    [JsonRequired]
    public int GlobalOffset { get; set; }

    public StudentSearchQueryDTO(){
        Name = "";
        GroupName = "";
    }

}