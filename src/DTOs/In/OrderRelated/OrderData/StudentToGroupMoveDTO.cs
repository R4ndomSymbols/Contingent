using System.Text.Json.Serialization;
using Contingent.Import;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class StudentToGroupMovesDTO
{
    [JsonRequired]
    public List<StudentToGroupMoveDTO> Moves { get; set; }

    public StudentToGroupMovesDTO()
    {
        Moves = new List<StudentToGroupMoveDTO>();
    }
}

[Serializable]
public class StudentToGroupMoveDTO : IFromCSV<StudentToGroupMoveDTO>
{
    [JsonRequired]
    public StudentStatementDTO Student { get; set; }
    [JsonRequired]
    public GroupStatementDTO Group { get; set; }
    public StudentToGroupMoveDTO()
    {
        Student = new StudentStatementDTO();
        Group = new GroupStatementDTO();
    }

    public Result<StudentToGroupMoveDTO> MapFromCSV(CSVRow row)
    {
        Student = Student.MapFromCSV(row).ResultObject;
        Group = Group.MapFromCSV(row).ResultObject;
        return Result<StudentToGroupMoveDTO>.Success(this);
    }
}