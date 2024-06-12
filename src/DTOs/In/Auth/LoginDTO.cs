using System.Text.Json.Serialization;

namespace Contingent.DTOs.In;

public class LoginDTO
{
    [JsonRequired]
    public string Login { get; set; }
    [JsonRequired]
    public string Password { get; set; }

    public LoginDTO()
    {
        Login = string.Empty;
        Password = string.Empty;
    }
}
