using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupChangeMovesDTO {

    [JsonRequired]
    public List<StudentMoveDTO> Moves {get; set; }

    public StudentGroupChangeMovesDTO(){
        Moves = new List<StudentMoveDTO>();
    }
}
