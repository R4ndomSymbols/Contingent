using Contingent.Import;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class StudentStatementDTO : IFromCSV<StudentStatementDTO>
{
    public int StudentId { get; set; }
    public string StudentGradeBookNumber { get; set; }
    public string Name { get; set; }

    public StudentStatementDTO()
    {
        StudentId = Utils.INVALID_ID;
        StudentGradeBookNumber = "";
        Name = "";
    }

    public Result<StudentStatementDTO> MapFromCSV(CSVRow row)
    {
        StudentGradeBookNumber = row[FlowImport.GradeBookFieldName]!;
        StudentGradeBookNumber = StudentGradeBookNumber is null ? "" : StudentGradeBookNumber.Trim();
        Name = row[FlowImport.StudentFullNameFieldName]!;
        Name = Name is null ? "" : Name.Trim();
        return Result<StudentStatementDTO>.Success(this);
    }
}
