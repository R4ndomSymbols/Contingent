using StudentTracking.Models.Domain.Citizenship;

namespace StudentTracking.Controllers.DTO.Out;

public class RussianCitizenshipDTO
{

    public int? Id { get; private set; }
    public string Name { get; private set; }
    public string Surname { get; private set; }
    public string Patronymic { get; private set; }
    public AddressOutDTO LegalAddress { get; private set; }
    public RussianCitizenshipDTO()
    {
        Name = string.Empty;
        Surname = string.Empty;
        Patronymic = string.Empty;
        LegalAddress = new AddressOutDTO();
    }
    public RussianCitizenshipDTO(int? id) : this()
    {
        Id = id;
    }

    public RussianCitizenshipDTO(RussianCitizenship? model)
    {
        if (model is null)
        {
            throw new Exception();
        }
        Id = model.Id;
        Name = model.Name;
        Surname = model.Surname;
        Patronymic = model.Patronymic;
        LegalAddress = model.LegalAddress is null ? new AddressOutDTO(model.LegalAddressId) : new AddressOutDTO(model.LegalAddress);
    }
}
