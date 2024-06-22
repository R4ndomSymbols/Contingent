namespace Contingent.DTOs.In;

public class RegisterDTO
{

    public string Login { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    public int Role { get; set; }

    public RegisterDTO()
    {
        Login = string.Empty;
        Password = string.Empty;
        Username = string.Empty;
    }

}