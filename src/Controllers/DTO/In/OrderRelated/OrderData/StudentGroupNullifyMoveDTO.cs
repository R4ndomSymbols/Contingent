using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Metadata;
using StudentTracking.Import;
using Utilities;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupNullifyMovesDTO
{

    [JsonRequired]
    public List<StudentGroupNullifyMoveDTO> Students { get; set; }

    public StudentGroupNullifyMovesDTO()
    {
        Students = new List<StudentGroupNullifyMoveDTO>();
    }
}

public class StudentGroupNullifyMoveDTO : IFromCSV<StudentGroupNullifyMoveDTO>
{

    [JsonRequired]
    public StudentStatementDTO Student { get; set; }

    public StudentGroupNullifyMoveDTO()
    {
        Student = new StudentStatementDTO();
    }

    public Result<StudentGroupNullifyMoveDTO> MapFromCSV(CSVRow row)
    {
        Student = Student.MapFromCSV(row).ResultObject;
        return Result<StudentGroupNullifyMoveDTO>.Success(this);
    }
}
