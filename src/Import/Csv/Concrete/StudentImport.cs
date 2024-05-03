using System.Data.Common;
using Contingent.Controllers.DTO;
using Contingent.Controllers.DTO.In;
using Contingent.Models;
using Contingent.Models.Domain.Students;
using Utilities;

namespace Contingent.Import.Concrete;

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
