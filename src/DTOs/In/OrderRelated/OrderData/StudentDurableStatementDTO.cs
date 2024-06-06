using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;
// DTO для приказа об академическом отпуске
[Serializable]
public class StudentDurableStateDTO
{
    [JsonRequired]
    public StudentStatementDTO Student { get; set; }
    [JsonRequired]
    public string StartDate { get; set; }
    [JsonRequired]
    public string EndDate { get; set; }
    public StudentDurableStateDTO()
    {
        Student = new();
        StartDate = string.Empty;
        EndDate = string.Empty;
    }
}
// коллекция DTO для приказа об академическом отпуске
[Serializable]
public class StudentDurableStatesDTO
{

    [JsonRequired]
    public List<StudentDurableStateDTO> Statements { get; set; }

    public StudentDurableStatesDTO()
    {
        Statements = new();
    }

}

