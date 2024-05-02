using System.Data.Common;
using StudentTracking.Controllers.DTO;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Students;
using Utilities;

namespace StudentTracking.Import.Concrete;

public class StudentImport : IFromCSV<StudentImport>
{
    public StudentModel? Student { get; set; }
    public StudentImport()
    {
        Student = null;
    }
    public Result<StudentImport> MapFromCSV(CSVRow row)
    {
        var studentResult = StudentModel.Build(new StudentInDTO().MapFromCSV(row).ResultObject);
        if (studentResult.IsFailure)
        {
            return Result<StudentImport>.Failure(studentResult.Errors);
        }
        Student = studentResult.ResultObject;
        return Result<StudentImport>.Success(this);
    }
}
