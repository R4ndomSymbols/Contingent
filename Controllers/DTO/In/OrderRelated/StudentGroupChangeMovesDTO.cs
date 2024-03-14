using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupChangeMoveDTO {

    [JsonRequired]
    public List<StudentMoveDTO> Moves {get; set; }

    public StudentGroupChangeMoveDTO(){
        Moves = new List<StudentMoveDTO>();
    }
}
