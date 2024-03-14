using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentMoveDTO 
{
    [JsonRequired]
    public int StudentId { get; set; }
    [JsonRequired]
    public int GroupToId { get; set; }
    public StudentMoveDTO(){
        
    }
}
