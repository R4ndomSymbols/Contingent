using Contingent.Auth;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using System.Text;
using Contingent.Utilities;
using Contingent.Import.CSV;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Security.Cryptography;
using Contingent.DTOs.In;
namespace Tests;


public class Tests
{
    [Fact]
    public void MakeImportFile()
    {
        var csv = new CSVGenerator().GenerateOrders(100);
        File.WriteAllText(@"/home/RandomSymbols/fhdd/projects/Contingent/docs/test.csv", csv);
    }
    public void GenerateStudents()
    {
        var csv = new CSVGenerator().GenerateStudents(10000);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new StudentImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }
    }
    public void GenerateSpecialities()
    {
        var csv = new CSVGenerator().GetSpecialties();
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new SpecialtyImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }

    }
    private void GenerateGroups()
    {
        var csv = new CSVGenerator().GenerateGroups(100);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new GroupImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }

    }
    public void GenerateOrdersTest()
    {
        var csv = new CSVGenerator().GenerateOrders(100);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new OrderImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }
    }
    public void FlowImportTest()
    {
        var csv = new CSVGenerator().GenerateFlow(100, OrderTypes.FreeEnrollment);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new FlowImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        import.CloseOrders = true;
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }

    }
    public void EncTest()
    {
        // ваш ключ здесь
        byte[] _storageKey = new byte[32];

        string rawDb = "****";
        string rawJWT = "****";
        string encDBBase64 = "";
        string encJWTBase64 = "";
        string decDb = "";
        string decJWT = "";

        using (var gen = Aes.Create())
        {
            gen.KeySize = 256;
            gen.Padding = PaddingMode.ISO10126;
            gen.Key = _storageKey;
            // в доках написано, что это значение должно быть в длину 1/8 размера блока
            gen.IV = Enumerable.Repeat((byte)0, gen.BlockSize / 8).ToArray();

            // тестовый код для генерации шифротекста
            encDBBase64 = Convert.ToBase64String(
                gen.EncryptCbc(
                    Encoding.Unicode.GetBytes(rawDb),
                    gen.IV, gen.Padding)
            );
            encJWTBase64 = Convert.ToBase64String(
                gen.EncryptCbc(
                    Encoding.Unicode.GetBytes(rawJWT),
                gen.IV, gen.Padding)
            );
            decDb = Encoding.Unicode.GetString(
                gen.DecryptCbc(Convert.FromBase64String(encDBBase64), gen.IV, gen.Padding)
            );
            decJWT = Encoding.Unicode.GetString(
                gen.DecryptCbc(Convert.FromBase64String(encJWTBase64), gen.IV, gen.Padding)
            );
            Console.WriteLine(rawDb);
            Console.WriteLine(rawJWT);
            Console.WriteLine(encDBBase64);
            Console.WriteLine(encJWTBase64);
            Console.WriteLine(decDb);
            Console.WriteLine(decJWT);
        }
    }
    public void RegisterTest()
    {
        ContingentUser.RegisterUser(
            new RegisterDTO
            {
                Login = "****",
                Username = "*****",
                Password = "*****",
                Role = (int)Roles.Admin
            }
        );
    }

}

// moving to branch dev