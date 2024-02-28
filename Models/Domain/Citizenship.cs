

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.ValueObjects.Students;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain;

public interface ICitizenship
{
    public string GetName();

}

public class RussianCitizenship : ICitizenship
{
    // добавить ограничения уникальности некоторых параметров
    private int _legalAddress;
    private int _id;
    private NamePart _name;
    private NamePart _surname;
    private NamePart? _patronymic;
    private string _passportNumber;
    private string _passportSeries;

    public int Id
    {
        get => _id;
    }
    public string Name
    {
        get => _name.NameToken;
    }
    public string Surname
    {
        get => _surname.NameToken;
    }
    public string Patronymic
    {
        get => _patronymic == null ? "" : _patronymic.NameToken;
    }
    public string PassportNumber
    {
        get => _passportNumber;
    }
    public string PassportSeries
    {
        get => _passportSeries;
    }
    public int LegalAddressId
    {
        get => _legalAddress;
        set => _legalAddress = value;
    }

    private RussianCitizenship() : base()
    {

    }

    public static async Task<RussianCitizenship?> GetById(int id, ObservableTransaction? scope)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT * FROM rus_citizenship WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using (cmd)
        {
            using var cursor = await cmd.ExecuteReaderAsync();
            if (!cursor.HasRows)
            {
                return null;
            }
            await cursor.ReadAsync();
            return new RussianCitizenship()
            {
                _id = id,
                _passportNumber = (string)cursor["passport_number"],
                _passportSeries = (string)cursor["passport_series"],
                _surname = NamePart.Create((string)cursor["surname"]).ResultObject,
                _name = NamePart.Create((string)cursor["name"]).ResultObject,
                _patronymic = cursor["patronymic"].GetType() == typeof(DBNull) ? null : NamePart.Create((string)cursor["patronymic"]).ResultObject,
                _legalAddress = (int)cursor["legal_address"],
            };
        }
    }
    public async Task Save(ObservableTransaction? scope)
    {
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO rus_citizenship( " +
                        " passport_number, passport_series, surname, name, patronymic, legal_address) " +
                        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6) RETURNING id";
        NpgsqlCommand? command;

        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            command = new NpgsqlCommand(cmdText, conn);
        }
        command.Parameters.Add(new NpgsqlParameter<string>("p1", _passportNumber));
        command.Parameters.Add(new NpgsqlParameter<string>("p2", _passportSeries));
        command.Parameters.Add(new NpgsqlParameter<string>("p3", Surname));
        command.Parameters.Add(new NpgsqlParameter<string>("p4", Name));
        command.Parameters.Add(new NpgsqlParameter("p5", _patronymic == null ? DBNull.Value : Patronymic));
        command.Parameters.Add(new NpgsqlParameter<int>("p6", _legalAddress));

        await using (conn)
        {
            await using (command)
            {
                using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                _id = (int)reader["id"];
            }
        }
    }

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope = null)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM rus_citizenship WHERE id = @p1)";
        NpgsqlCommand cmd;
        if (scope != null)
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        using (cmd)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (bool)reader["exists"];
        }
    }

    public string GetName()
    {
        return (Surname + " " + Name + " " + Patronymic).TrimEnd();
    }
    // переместить валидацию сюда
    public static Result<RussianCitizenship?> Build(RussianCitizenshipDTO? dto)
    {  
        if (dto is null)
        {
            return Result<RussianCitizenship>.Failure(new ValidationError(nameof(dto), "Непределенная модель гражданства"));
        }
        var errors = new List<ValidationError?>();
        var citizenship = new RussianCitizenship();

        if (dto.Id != null){
            citizenship._id = (int)dto.Id;
        }

        citizenship._legalAddress = dto.LegalAddressId;
        var name = NamePart.Create(dto.Name, 40);
        if (errors.IsValidRule(
            name.IsSuccess,
            message: "Имя указано неверно",
            propName: nameof(Name)
        ))
        {
            citizenship._name = name.ResultObject;
        }
        var surname = NamePart.Create(dto.Surname, 100);
        if (errors.IsValidRule(
            surname.IsSuccess,
            message: "Фамилия указана неверно",
            propName: nameof(Surname)
        ))
        {
            citizenship._surname = surname.ResultObject;
        }
        if (dto.Patronymic is null)
        {
            citizenship._patronymic = null;
        }
        else
        {
            var patronymic = NamePart.Create(dto.Patronymic);
            if (errors.IsValidRule(
                patronymic.IsSuccess,
                message: "Отчество указано неверно",
                propName: nameof(Patronymic)
            ))
            {
                citizenship._patronymic = patronymic.ResultObject;
            }
        }

        if (errors.IsValidRule(
            dto.PassportNumber != null &&
            dto.PassportNumber.Length == 6 &&
            dto.PassportNumber.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message: "Неверно указан номер паспорта",
            propName: nameof(PassportNumber)
        ))
        {
            citizenship._passportNumber = dto.PassportNumber;
        }
        if (errors.IsValidRule(
            dto.PassportSeries != null &&
            dto.PassportSeries.Length == 4 &&
            dto.PassportSeries.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message: "Неверно указана серия паспорта",
            propName: nameof(PassportSeries)
        ))
        {
            citizenship._passportSeries = dto.PassportSeries;
        }
        if (errors.Any())
        {
            return Result<RussianCitizenship>.Failure(errors);
        }
        return Result<RussianCitizenship>.Success(citizenship);

    }
    public async Task Update(ObservableTransaction? scope = null){
        using var conn = await Utils.GetAndOpenConnectionFactory(); 
        var cmdText = "UPDATE public.rus_citizenship " +
	    " SET passport_number=@p1, passport_series=@p2, " + 
        " surname=@p3, name=@p4, patronymic=@p5, legal_address=@p6" + 
	    " WHERE id = @p7";
        NpgsqlCommand cmd;
        if (scope is null){
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _passportNumber));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _passportSeries));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _surname.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", _name.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter("p5", _patronymic?.NameToken is null ? DBNull.Value : _patronymic.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p6", _legalAddress));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", _id));

        using (cmd){
            await cmd.ExecuteNonQueryAsync();
        }
    }
}