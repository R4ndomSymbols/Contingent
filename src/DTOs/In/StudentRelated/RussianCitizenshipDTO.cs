using System.Text.Json.Serialization;
using Contingent.Import;
using Utilities;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class RussianCitizenshipInDTO : IFromCSV<RussianCitizenshipInDTO>
{
    public const string NameFieldName = "имя";
    public const string SurnameFieldName = "фамилия";
    public const string PatronymicFieldName = "отчество";
    public const string LegalAddressFieldName = "прописка";
    public RussianCitizenshipInDTO()
    {
        Id = null;
        Name = "";
        Surname = "";
        Patronymic = null;
        LegalAddress = new AddressInDTO();

    }
    public int? Id { get; set; }
    [JsonRequired]
    public string? Name { get; set; }
    [JsonRequired]
    public string? Surname { get; set; }
    public string? Patronymic { get; set; }

    [JsonRequired]
    public AddressInDTO LegalAddress { get; set; }

    public Result<RussianCitizenshipInDTO> MapFromCSV(CSVRow row)
    {
        Name = row[NameFieldName]!;
        Surname = row[SurnameFieldName]!;
        Patronymic = row[PatronymicFieldName];
        LegalAddress = new AddressInDTO
        {
            Address = row[LegalAddressFieldName]
        };
        return Result<RussianCitizenshipInDTO>.Success(this);
    }
}
