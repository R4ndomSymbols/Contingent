using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupNullifyMoveDTO {

    [JsonRequired]
    public List<int> Students {get; set; }

    public StudentGroupNullifyMoveDTO(){
        Students = new List<int>();
    }
}
