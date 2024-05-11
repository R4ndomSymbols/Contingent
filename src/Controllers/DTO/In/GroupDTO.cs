using System.Text.Json.Serialization;
using Contingent.Import;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public sealed class GroupInDTO : IFromCSV<GroupInDTO>
{
    public const string EduProgramIdFieldName = "образовательная программа";
    public const string EduFormatCodeFieldName = "форма обучения";
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
    public int CreationYear { get; set; }
    [JsonRequired]
    public bool AutogenerateName { get; set; }
    public string GroupName { get; set; }
    public int CourseOn { get; set; }
    public int PreviousGroupId { get; set; }

    public GroupInDTO()
    {

    }

    public Result<GroupInDTO> MapFromCSV(CSVRow row)
    {
        if (int.TryParse(row[EduProgramIdFieldName], out int id))
        {
            EduProgramId = id;
        }
        if (int.TryParse(row[EduFormatCodeFieldName], out id))
        {
            EduFormatCode = id;
        }
        if (int.TryParse(row[SponsorshipTypeCodeFieldName], out id))
        {
            SponsorshipTypeCode = id;
        }
        if (int.TryParse(row[CreationYearFieldName], out int year))
        {
            CreationYear = year;
        }
        AutogenerateName = row[AutogenerateNameFieldName]!.ToLower() == "да";
        return Result<GroupInDTO>.Success(this);

    }
}