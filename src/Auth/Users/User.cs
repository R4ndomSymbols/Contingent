using System.Diagnostics.Contracts;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Contingent.DTOs.In;
using Contingent.Utilities;
using Npgsql;

namespace Contingent.Auth;

public class ContingentUser
{
    private static string salt = "9k0sgGgLEJBzhS8yEZdf5gTJfLJiMxfm";
    public int Id { get; private set; }
    public string Login { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }

    private ContingentUser()
    {
        Login = string.Empty;
        Username = string.Empty;
        PasswordHash = string.Empty;
        Role = string.Empty;
    }

    public static Result<ContingentUser> LogIn(LoginDTO loginData)
    {
        if (string.IsNullOrEmpty(loginData.Login) || string.IsNullOrEmpty(loginData.Login)
        || string.IsNullOrWhiteSpace(loginData.Password) || string.IsNullOrWhiteSpace(loginData.Password))
        {
            return Result<ContingentUser>.Failure(new ValidationError("Логин и пароль не могут быть пустыми"));
        }
        string passwordHash = ComputeHash(loginData.Password);
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "SELECT * FROM users WHERE login = @p1 AND password_hash = @p2";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", loginData.Login));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", passwordHash));
        using var reader = cmd.ExecuteReader();
        if (!reader.HasRows)
        {
            return Result<ContingentUser>.Failure(new ValidationError("Неверный логин или пароль"));
        }
        reader.Read();
        return Result<ContingentUser>.Success(new ContingentUser
        {
            Id = (int)reader["id"],
            Login = (string)reader["login"],
            Username = (string)reader["full_name"],
            PasswordHash = passwordHash,
            Role = ((Roles)(int)reader["role"]).ToString()
        });
    }

    public static void RegisterUser(RegisterDTO dto)
    {
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "INSERT INTO users (login, full_name, password_hash, role) VALUES (@p1, @p2, @p3, @p4)";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", dto.Login));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", dto.Username));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", ComputeHash(dto.Password)));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p4", (int)Roles.Admin));
        cmd.ExecuteNonQuery();
    }

    private static string ComputeHash(string password)
    {
        string passwordHash = string.Empty;
        using (SHA256 hash = SHA256.Create())
        {
            // пароль в UTF8, 
            var byteHash = hash.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
            foreach (byte b in byteHash)
            {
                passwordHash += b.ToString("x2");
            }
        }
        return passwordHash;
    }

    public ClaimsIdentity GetIdentity()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimsIdentity.DefaultNameClaimType, this.Login),
            new Claim(ClaimsIdentity.DefaultRoleClaimType, this.Role)
        };
        var identity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
        return identity;

    }

}