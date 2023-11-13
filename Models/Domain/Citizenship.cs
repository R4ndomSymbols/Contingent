

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using StudentTracking.Models.Domain.Address;
using Utilities;

namespace StudentTracking.Models.Domain;

public interface ICitizenship
{
    public string GetName();

}

public class RussianCitizenship : ValidatedObject<RussianCitizenship>, ICitizenship
{
    // добавить ограничения уникальности некоторых параметров
    private AddressModel? _legalAddress;
    private int _id;
    private string _name;
    private string _surname;
    private string? _patronymic;
    private string _passportNumber;
    private string _passportSeries;

    public int Id
    {
        get => _id;
        set => _id = value;
    }
    public string Name
    {
        get => _name;
        set {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringLength(value, 1, 50),
                new ValidationError<RussianCitizenship>(nameof(Name), "Имя такой длины не является допустимым")   
            )){
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianLetters),
                    new ValidationError<RussianCitizenship>(nameof(Name), "Имя содержит недопустимые символы")
                )){
                    _name = value;
                }
            }
        }
    }
    public string Surname
    {
        get => _surname;
        set {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringLength(value, 1, 50),
                new ValidationError<RussianCitizenship>(nameof(Surname), "Фамилия такой длины не является допустимой")   
            )){
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.RussianNamePart),
                    new ValidationError<RussianCitizenship>(nameof(Surname), "Имя содержит недопустимые символы")
                )){
                    _surname = value;
                }
            }
        }
    }
    public string Patronymic
    {
        get => _patronymic == null ? "" : _patronymic;
        set {
            if (PerformValidation(
                () => ValidatorCollection.CheckStringLength(value, 1, 50),
                new ValidationError<RussianCitizenship>(nameof(Patronymic), "Отчество такой длины не является допустимым")   
            )){
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.RussianNamePart),
                    new ValidationError<RussianCitizenship>(nameof(Patronymic), "Отчество содержит недопустимые символы либо имеет неверный формат")
                )){
                    _patronymic = value;
                }
            }
        }
    }
    public string PassportNumber {
        get => _passportNumber;
        set {
             if (PerformValidation(
                () => ValidatorCollection.CheckStringLength(value, 6, 6),
                new ValidationError<RussianCitizenship>(nameof(PassportNumber), "Длина номера паспорта должна быть 6 символов")   
            )){
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits),
                    new ValidationError<RussianCitizenship>(nameof(PassportNumber), "Номер паспорта должен содержать только цифры")
                )){
                    _passportNumber = value;
                }
            }
        }
    }
     public string PassportSeries {
        get => _passportSeries;
        set {
             if (PerformValidation(
                () => ValidatorCollection.CheckStringLength(value, 4, 4),
                new ValidationError<RussianCitizenship>(nameof(PassportSeries), "Длина серии паспорта должна быть 4 символа")   
            )){
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyDigits),
                    new ValidationError<RussianCitizenship>(nameof(PassportSeries), "Серия паспорта должна содержать только цифры")
                )){
                    _passportSeries = value;
                }
            }
        }
    }
    public int LegalAddressId {
        get => _legalAddress == null ? Utils.INVALID_ID : _legalAddress.Id;
        set {
            PerformValidation(
                () => {
                    _legalAddress = AddressModel.GetAddressById(value);
                    return _legalAddress != null;
                }, new ValidationError<RussianCitizenship> (nameof(LegalAddressId), "Такой адрес не зарегистрирован"));
        }
    }

    [JsonIgnore]
    public AddressModel? LegalAddress { 
        get => _legalAddress; 
    }

    public RussianCitizenship()
    {
        _name = "";
        _surname = "";
        _patronymic = "";
        _passportNumber = "";
        _passportSeries = "";
        _legalAddress = null;
    }

    public static RussianCitizenship? GetById(int id)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var command = new NpgsqlCommand("SELECT * FROM rus_citizenship WHERE id = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", id),
                }
            })
            {
                var cursor = command.ExecuteReader();
                if (!cursor.HasRows)
                {
                    return null;
                }
                cursor.Read();
                return new RussianCitizenship
                {
                    _passportNumber = (string)cursor["passport_number"],
                    _passportSeries = (string)cursor["passport_series"],
                    _surname = (string)cursor["surname"],
                    _name = (string)cursor["name"],
                    _patronymic = cursor["patronymic"].GetType() == typeof(DBNull) ? null : (string)cursor["patronymic"],
                    _legalAddress = AddressModel.GetAddressById((int)cursor["legal_address"])
                };
            }
        }
    }

    public void Save()
    {
        if (CheckErrorsExist()){
            return;
        }
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            if (Id == Utils.INVALID_ID)
            {
                using (var command = new NpgsqlCommand("INSERT INTO rus_citizenship( " +
                        " passport_number, passport_series, surname, name, patronymic, legal_address " +
                        " VALUES (@p1, @p2, @p3, @p4, @p5, @p6) RETURNING id", conn)
                {
                    Parameters = {
                        new ("p1", _passportNumber),
                        new ("p2", _passportSeries),
                        new ("p3", _surname),
                        new ("p4", _name),
                        new ("p5", _patronymic == null ? DBNull.Value : _patronymic),
                        new ("p6", _legalAddress == null ? DBNull.Value : _legalAddress.Id),
                    }
                })
                {
                    var cursor = command.ExecuteReader();
                    cursor.Read();
                    _id = (int)cursor["id"];
                }
            }
            else
            {
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
            }
        }
    }


    public string GetName()
    {
        throw new NotImplementedException();
    }
}

