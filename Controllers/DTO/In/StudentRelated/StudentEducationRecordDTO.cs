using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentEducationRecordDTO{

    public StudentEducationRecordDTO()
    {
        
    }
    
    [JsonRequired]
    public int Level { get; set; }
    public int StudentId {get; set;}

}
