using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupNullifyMovesDTO {

    [JsonRequired]
    public List<int> Students {get; set; }

    public StudentGroupNullifyMovesDTO(){
        Students = new List<int>();
    }
}
