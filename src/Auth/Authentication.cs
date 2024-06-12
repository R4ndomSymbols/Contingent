using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Unicode;

namespace Contingent.Auth;

public static class Authentication
{
    // 256 бит, массив 32 байта
    // ваш ключ тут
    private static readonly byte[] _storageKey =
    {
       0,0,0,0,0,0,0,0,
       0,0,0,0,0,0,0,0,
       0,0,0,0,0,0,0,0,
       0,0,0,0,0,0,0,0
    };
    public const string DatabaseConnectionStringFieldName = "ConnectionString";
    public const string JWTKeyFieldName = "JWTKey";
    public const string ActualSettingFileName = "appsettings.json";
    public const string ISSUER = "Contingent"; // издатель токена
    public const string AUDIENCE = "Client"; // потребитель токена
    public const int LIFETIME = 604800; // время жизни токена - одна неделя
    public static SymmetricSecurityKey JWTSecurityKey { get; private set; }
    public static string DatabaseConnectionString { get; private set; }


    static Authentication()
    {
        var builder = new ConfigurationBuilder().AddJsonFile(ActualSettingFileName).Build();
        using (var gen = Aes.Create())
        {
            gen.KeySize = 256;
            gen.Padding = PaddingMode.ISO10126;
            gen.Key = _storageKey;
            // в доках написано, что это значение должно быть в длину 1/8 размера блока
            gen.IV = Enumerable.Repeat((byte)0, gen.BlockSize / 8).ToArray();

            var connectionStringEncrypted = Convert.FromBase64String(builder[DatabaseConnectionStringFieldName]!);
            var JWTKeyEncrypted = Convert.FromBase64String(builder[JWTKeyFieldName]!);

            DatabaseConnectionString = Encoding.Unicode.GetString(gen.DecryptCbc(connectionStringEncrypted, gen.IV, gen.Padding));
            Console.WriteLine(DatabaseConnectionString);
            JWTSecurityKey = new SymmetricSecurityKey(gen.DecryptCbc(JWTKeyEncrypted, gen.IV, gen.Padding));
        }

    }



}