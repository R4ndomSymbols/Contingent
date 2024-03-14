using StudentTracking.Models.Domain;

namespace StudentTracking.Controllers.DTO.Out;

public class RussianCitizenshipDTO {

    public int? Id {get; private set;}
    public string Name { get; private set; }
    public string Surname { get; private set; }
    public string Patronymic { get; private set; }
    public string PassportNumber {get; private set; }
    public string PassportSeries {get; private set; }
    public AddressOutDTO LegalAddress {get; private set; }
    public RussianCitizenshipDTO(){
        Name = string.Empty;
        Surname = string.Empty;
        Patronymic = string.Empty;
        PassportNumber = string.Empty;
        PassportSeries = string.Empty;
        LegalAddress = new AddressOutDTO();
    }
    public RussianCitizenshipDTO(int? id) : this()
    {
        Id = id;
    }

    public RussianCitizenshipDTO(RussianCitizenship? model){
        if (model is null){
            throw new Exception();   
        }
        Id = model.Id;
        Name = model.Name;
        Surname = model.Surname;
        Patronymic = model.Patronymic;
        PassportNumber = model.PassportNumber;
        PassportSeries = model.PassportSeries;
        LegalAddress = model.LegalAddress is null ? new AddressOutDTO(model.LegalAddressId) : new AddressOutDTO(model.LegalAddress); 
    }
}
