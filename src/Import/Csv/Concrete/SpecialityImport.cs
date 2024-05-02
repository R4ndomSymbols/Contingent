using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Specialities;
using Utilities;

namespace StudentTracking.Import;

public class SpecialityImport : IFromCSV<SpecialityImport>
{
    public SpecialityModel? Speciality { get; private set; }
    public SpecialityImport()
    {
        Speciality = null;
    }
    public Result<SpecialityImport> MapFromCSV(CSVRow row)
    {
        var specialityDTO = new SpecialityDTO().MapFromCSV(row).ResultObject;
        var speciality = SpecialityModel.Build(specialityDTO);
        if (speciality.IsFailure)
        {
            return Result<SpecialityImport>.Failure(speciality.Errors);
        }
        Speciality = speciality.ResultObject;
        return Result<SpecialityImport>.Success(this);
    }
}