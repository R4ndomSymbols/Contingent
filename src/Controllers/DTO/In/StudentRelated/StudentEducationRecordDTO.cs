using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class StudentEducationRecordInDTO
{

    public StudentEducationRecordInDTO()
    {

    }

    [JsonRequired]
    public int Level { get; set; }
    public int StudentId { get; set; }

}
