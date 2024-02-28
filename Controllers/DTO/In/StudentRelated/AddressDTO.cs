using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class AddressDTO {
    public AddressDTO()
    {
        Address = null;
    }

    [JsonRequired]
    public string? Address {get; set; }

}
