using StudentTracking.Import;
using Utilities;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentStatementDTO : IFromCSV<StudentStatementDTO>
{
    public int StudentId { get; set; }
    public string StudentGradeBookNumber { get; set; }
    public string NamePart { get; set; }

    public StudentStatementDTO()
    {
        StudentId = Utils.INVALID_ID;
        StudentGradeBookNumber = "";
        NamePart = "";
    }

    public Result<StudentStatementDTO> MapFromCSV(CSVRow row)
    {
        StudentGradeBookNumber = row["номер зачетной книжки"]!;
        StudentGradeBookNumber ??= "";
        NamePart = row["фамилия"]!;
        NamePart = NamePart is null ? "" : NamePart.Split(' ').First();
        return Result<StudentStatementDTO>.Success(this);
    }
}
