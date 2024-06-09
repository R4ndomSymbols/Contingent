using System.Data.Common;
using Contingent.Controllers.DTO;
using Contingent.Controllers.DTO.In;
using Contingent.Models;
using Contingent.Models.Domain.Students;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public class StudentImport : ImportCSV
{
    private List<StudentModel> _students;
    public StudentImport(Stream source, ObservableTransaction scope) : base(source, scope)
    {
        _students = new List<StudentModel>();
    }
    public override ResultWithoutValue Import()
    {
        var dtos = Read(() => new StudentInDTO(), out List<CSVRow> rows);
        if (dtos.IsFailure)
        {
            return ResultWithoutValue.Failure(dtos.Errors);
        }
        foreach (var studentDTO in dtos.ResultObject)
        {
            var student = StudentModel.Build(studentDTO, _scope);
            if (student.IsFailure)
            {
                return ResultWithoutValue.Failure(student.Errors);
            }
            _students.Add(student.ResultObject);
        }
        return ResultWithoutValue.Success();
    }

    public override ResultWithoutValue Save(bool commit)
    {
        foreach (var student in _students)
        {
            var result = student.Save(_scope);
            if (result.IsFailure)
            {
                FinishImport(false);
                return result;
            }
        }
        FinishImport(commit);
        return ResultWithoutValue.Success();
    }
}
