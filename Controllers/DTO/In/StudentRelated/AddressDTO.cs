using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class AddressDTO {
    public AddressDTO()
    {
        Address = null;
        AddressId = null;
    }

    public int? AddressId {get; set;}

    [JsonRequired]
    public string? Address {get; set; }

}
