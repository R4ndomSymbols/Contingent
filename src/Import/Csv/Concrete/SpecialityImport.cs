using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Specialties;
using Utilities;

namespace Contingent.Import;

public class SpecialityImport : IFromCSV<SpecialityImport>
{
    public SpecialtyModel? Speciality { get; private set; }
    public SpecialityImport()
    {
        Speciality = null;
    }
    public Result<SpecialityImport> MapFromCSV(CSVRow row)
    {
        var specialityDTO = new SpecialityDTO().MapFromCSV(row).ResultObject;
        var speciality = SpecialtyModel.Build(specialityDTO);
        if (speciality.IsFailure)
        {
            return Result<SpecialityImport>.Failure(speciality.Errors);
        }
        Speciality = speciality.ResultObject;
        return Result<SpecialityImport>.Success(this);
    }
}