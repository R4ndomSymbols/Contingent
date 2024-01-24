

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

public class RussianCitizenship : DbValidatedObject, ICitizenship
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
    }

    public RussianCitizenship() : base()
    {

    }
    public RussianCitizenship(int id) : base(RelationTypes.Bound)
    {

    }

    public static async Task<RussianCitizenship?> GetById(int id, ObservableTransaction? scope)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT * FROM rus_citizenship WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope!= null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
        await using (cmd){
            using var cursor = await cmd.ExecuteReaderAsync();
            if (!cursor.HasRows)
            {
                return null;
            }
            await cursor.ReadAsync();
            return new RussianCitizenship(id)
            {
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

        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO rus_citizenship( " +
                        " passport_number, passport_series, surname, name, patronymic, legal_address) " +
                        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6) RETURNING id";
        NpgsqlCommand? command;
        
        if (scope!=null){
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }        
        else {
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
                NotifyStateChanged();
            }
        }
    }

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM rus_citizenship WHERE id = @p1)";
        NpgsqlCommand cmd;
        if (scope != null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else{
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
    

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        var rawCheck = await GetById(_id, scope);
        if (rawCheck == null)
        {
            await using var conn = await Utils.GetAndOpenConnectionFactory();
            string cmdText = "SELECT id FROM rus_citizenship WHERE passport_number = @p1 AND passport_series = @p2";
            NpgsqlCommand cmd;
            if (scope != null){
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else{
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            cmd.Parameters.Add(new NpgsqlParameter<string>("p1", _passportNumber));
            cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _passportSeries));

            await using (cmd)
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                await reader.ReadAsync();
                _id = (int)reader["id"];
                NotifyStateChanged();
                return this;
            }
        }
        return rawCheck;
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.GetType() != typeof(RussianCitizenship))
        {
            return false;
        }
        var rc = (RussianCitizenship)other;
        return _passportNumber == rc._passportNumber &&
        _passportSeries == rc._passportSeries &&
        _id == rc._id;

    }


    public string GetName()
    {
        return (Surname + " " + Name + " " + Patronymic).TrimEnd();
    }
    // переместить валидацию сюда
    public static Result<RussianCitizenship?> Build(RussianCitizenshipDTO dto){
        var errors = new List<ValidationError?>();
        var citizenship = new RussianCitizenship();
        citizenship._legalAddress = dto.LegalAddressId;
        var name = NamePart.Create(dto.Name, 40);
        if (errors.IsValidRule(
            name.IsSuccess,
            message: "Имя указано неверно"
        )){
            citizenship._name = name.ResultObject;
        }
        var surname = NamePart.Create(dto.Surname, 100);
        if (errors.IsValidRule(
            surname.IsSuccess,
            message: "Фамилия указана неверно"
        )){
            citizenship._surname = surname.ResultObject;
        }
        var patronymic = NamePart.Create(dto.Patronimyc);
        if (patronymic.IsFailure){
            patronymic = null;
        }  
        if (errors.IsValidRule(
            patronymic is null ||
            patronymic.IsSuccess,
            message: "Имя указано неверно"
        )){
            citizenship._patronymic = patronymic.ResultObject;
        }
        if (errors.IsValidRule(
            dto.PassportNumber != null &&
            dto.PassportNumber.Length == 6 &&
            dto.PassportNumber.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message: "Неверно указан номер паспорта"
        )){
            citizenship._passportNumber = dto.PassportNumber;
        }
        if (errors.IsValidRule(
            dto.PassportSeries != null &&
            dto.PassportSeries.Length == 4 &&
            dto.PassportSeries.CheckStringPatternD(ValidatorCollection.OnlyDigits),
            message:"Неверно указана серия паспорта"
        )){
            citizenship._passportSeries = dto.PassportSeries;
        }
        if (errors.Any()){
            return Result<RussianCitizenship>.Failure(errors);
        }
        return Result<RussianCitizenship>.Success(citizenship);
         
    }

    /*

                using (var command = new NpgsqlCommand("UPDATE rus_citizenship " +
                    " SET passport_number=@p1, passport_series=@p2, surname=@p3, name=@p4, patronymic=@p5, legal_address= @p6" +
                    " WHERE id = @p7", conn)
                {
                    Parameters = {
                        new ("p1", _passportNumber),
                        new ("p2", _passportSeries),
                        new ("p3", _surname),
                        new ("p4", _name),
                        new ("p5", _patronymic == null ? DBNull.Value : _patronymic),
                        new ("p6", _legalAddress == null ? DBNull.Value : _legalAddress.Id),
                        new ("p7", _id),
                    }
                })
                {
                    var cursor = command.ExecuteNonQuery();
                }
    
    */
}

