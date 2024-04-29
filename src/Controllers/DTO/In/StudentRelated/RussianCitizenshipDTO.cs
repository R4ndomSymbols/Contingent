using System.Text.Json.Serialization;
using StudentTracking.Import;
using Utilities;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class RussianCitizenshipInDTO : IFromCSV<RussianCitizenshipInDTO>
{

    public RussianCitizenshipInDTO()
    {
        Id = null;
        Name = "";
        Surname = "";
        Patronymic = null;

    }
    public int? Id { get; set; }
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public string Surname { get; set; }
    public string? Patronymic { get; set; }

    [JsonRequired]
    public AddressInDTO LegalAddress { get; set; }

    public Result<RussianCitizenshipInDTO> MapFromCSV(CSVRow row)
    {
        Name = row["имя"]!;
        Surname = row["фамилия"]!;
        Patronymic = row["отчество"];
        LegalAddress = new AddressInDTO
        {
            Address = row["прописка"]
        };
        return Result<RussianCitizenshipInDTO>.Success(this);
    }
}
