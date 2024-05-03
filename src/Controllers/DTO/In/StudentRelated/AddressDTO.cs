using System.Text.Json.Serialization;
using Contingent.Import;

namespace Contingent.Controllers.DTO.In;

[Serializable]
public class AddressInDTO
{
    public AddressInDTO()
    {
        Address = null;
        AddressId = null;
    }

    public int? AddressId { get; set; }

    [JsonRequired]
    public string? Address { get; set; }
}
