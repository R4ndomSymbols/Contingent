using Contingent.Models.Domain.Address;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class AddressOutDTO
{
    public int? Id { get; set; }
    public string AddressName { get; set; }

    public AddressOutDTO(AddressModel model)
    {
        Id = model.Id;
        AddressName = model.ToString();
    }

    public AddressOutDTO(int? id)
    {
        Id = id;
        AddressName = "Не указано";
    }
    public AddressOutDTO()
    {
        Id = null;
        AddressName = "Не указано";
    }

}