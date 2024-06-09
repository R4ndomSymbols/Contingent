using System.Text.Json.Serialization;
using Contingent.Import;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Specialties;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public sealed class GroupInDTO : IFromCSV<GroupInDTO>
{
    public const string EduProgramNameFieldName = "образовательная программа";
    public const string QualificationFieldName = "квалификация";
    public const string EduFormatFieldName = "форма обучения";
    public const string SponsorshipTypeCodeFieldName = "тип финансирования";
    public const string CreationYearFieldName = "год создания";
    public const string AutogenerateNameFieldName = "генерация потока";


    [JsonRequired]
    public int EduProgramId { get; set; }
    [JsonRequired]
    public int EduFormatCode { get; set; }
    [JsonRequired]
    public int SponsorshipTypeCode { get; set; }
    [JsonRequired]
    public string CreationYear { get; set; }
    [JsonRequired]
    public bool AutogenerateName { get; set; }
    public string GroupName { get; set; }
    public int CourseOn { get; set; }
    public int PreviousGroupId { get; set; }

    public GroupInDTO()
    {
        EduProgramId = Utils.INVALID_ID;
        CreationYear = "";
        GroupName = "";
    }
    // импорт групп с негенерируемым названием
    // пока не предусмотрен
    public Result<GroupInDTO> MapFromCSV(CSVRow row)
    {
        var foundSpecialty = SpecialtyModel.FindByNameAndQualification(
            row[EduProgramNameFieldName],
            row[QualificationFieldName]
            );
        if (foundSpecialty.FirstOrDefault() is not null)
        {
            EduProgramId = foundSpecialty.First().Id;
        }
        else
        {
            return Result<GroupInDTO>.Failure(new ImportValidationError("Не найдена специальность"));
        }
        var foundEducationFormat = GroupEducationFormat.GetByTypeName(row[EduFormatFieldName]);
        if (foundEducationFormat is not null)
        {
            EduFormatCode = (int)foundEducationFormat.FormatType;
        }
        else
        {
            return Result<GroupInDTO>.Failure(new ImportValidationError("Не найден такой формат обучения"));
        }
        var foundSponsorshipType = GroupSponsorship.GetByTypeName(row[SponsorshipTypeCodeFieldName]);
        if (foundSponsorshipType is not null)
        {
            SponsorshipTypeCode = (int)foundSponsorshipType.TypeOfSponsorship;
        }
        else
        {
            return Result<GroupInDTO>.Failure(new ImportValidationError("Не найден такой тип финансирования "));
        }
        if (int.TryParse(row[CreationYearFieldName], out int year))
        {
            CreationYear = year.ToString();
        }
        AutogenerateName = row[AutogenerateNameFieldName]!.ToLower() == "да";
        return Result<GroupInDTO>.Success(this);

    }
}