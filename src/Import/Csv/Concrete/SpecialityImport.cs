using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Specialties;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public class SpecialtyImport : ImportCSV
{
    private readonly List<SpecialtyModel> _specialties;
    public SpecialtyImport(Stream dataSource, ObservableTransaction scope) : base(dataSource, scope)
    {
        _specialties = new List<SpecialtyModel>();
    }

    public override ResultWithoutValue Import()
    {
        var dtos = Read(() => new SpecialtyInDTO(), out List<CSVRow> rows);
        if (dtos.IsFailure)
        {
            return ResultWithoutValue.Failure(dtos.Errors);
        }
        foreach (var specialtyDTO in dtos.ResultObject)
        {
            var specialty = SpecialtyModel.Build(specialtyDTO);
            if (specialty.IsFailure)
            {
                return ResultWithoutValue.Failure(specialty.Errors);
            }
            _specialties.Add(specialty.ResultObject);
        }
        return ResultWithoutValue.Success();
    }
    public override ResultWithoutValue Save(bool commit)
    {
        foreach (var specialty in _specialties)
        {
            var result = specialty.Save(_scope);
            if (result.IsFailure)
            {
                return result;
            }
        }
        FinishImport(commit);
        return ResultWithoutValue.Success();
    }
}