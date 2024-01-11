

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using StudentTracking.Models.Domain.Address;
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
    private string _name;
    private string _surname;
    private string? _patronymic;
    private string _passportNumber;
    private string _passportSeries;

    public int Id
    {
        get => _id;
    }
    public string Name
    {
        get => _name;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.RussianNamePart),
                new ValidationError(nameof(Name), "Имя содержит недопустимые символы")
            ))
            {
                _name = value;
            }
        }
    }
    public string Surname
    {
        get => _surname;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.RussianNamePart),
                new ValidationError(nameof(Surname), "Фамилия содержит недопустимые символы")
            ))
            {
                _surname = value;
            }
        }
    }
    public string Patronymic
    {
        get => _patronymic == null ? "" : _patronymic;
        set
        {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.RussianNamePart),
                new ValidationError(nameof(Patronymic), "Отчество содержит недопустимые символы либо имеет неверный формат")
            ))
            {
                _patronymic = value;
                return;
            }
            if (PerformValidation(() => value == string.Empty, new ValidationError(nameof(Patronymic), "Отчество должно быть пустым, либо соответствовать формату")))
            {
                _patronymic = null;
            }
        }

    }
    public string PassportNumber
    {
        get => _passportNumber;
        set
        {
            if (PerformValidation(
               () => ValidatorCollection.CheckStringLength(value, 6, 6),
               new ValidationError(nameof(PassportNumber), "Длина номера паспорта должна быть 6 символов")
           ))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits),
                    new ValidationError(nameof(PassportNumber), "Номер паспорта должен содержать только цифры")
                ))
                {
                    _passportNumber = value;
                }
            }
        }
    }
    public string PassportSeries
    {
        get => _passportSeries;
        set
        {
            if (PerformValidation(
               () => ValidatorCollection.CheckStringLength(value, 4, 4),
               new ValidationError(nameof(PassportSeries), "Длина серии паспорта должна быть 4 символа")
           ))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits),
                    new ValidationError(nameof(PassportSeries), "Серия паспорта должна содержать только цифры")
                ))
                {
                    _passportSeries = value;
                }
            }
        }
    }
    public int LegalAddressId
    {
        get => _legalAddress;
    }

    public async Task SetLegalAddressId(int id, ObservableTransaction? scope){
        bool exists = await AddressModel.IsIdExists(id, scope);
        if (PerformValidation(
            () => exists,
            new DbIntegrityValidationError(nameof(LegalAddressId), "Такой адрес не зарегистрирован"))){
                _legalAddress = id;
            }
    }


    public RussianCitizenship() : base()
    {
        RegisterProperty(nameof(Name));
        RegisterProperty(nameof(Surname));
        RegisterProperty(nameof(Patronymic));
        RegisterProperty(nameof(PassportNumber));
        RegisterProperty(nameof(PassportSeries));
        RegisterProperty(nameof(LegalAddressId));

        _name = "";
        _surname = "";
        _patronymic = "";
        _passportNumber = "";
        _passportSeries = "";
        _id = Utils.INVALID_ID;

    }
    public RussianCitizenship(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _name = "";
        _surname = "";
        _patronymic = "";
        _passportNumber = "";
        _passportSeries = "";
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
                _surname = (string)cursor["surname"],
                _name = (string)cursor["name"],
                _patronymic = cursor["patronymic"].GetType() == typeof(DBNull) ? null : (string)cursor["patronymic"],
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
        command.Parameters.Add(new NpgsqlParameter<string>("p3", _surname));
        command.Parameters.Add(new NpgsqlParameter<string>("p4", _name));
        command.Parameters.Add(new NpgsqlParameter("p5", _patronymic == null ? DBNull.Value : (string)_patronymic));
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

