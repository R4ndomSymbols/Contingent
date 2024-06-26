using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Address;
using Contingent.SQL;
using Contingent.Utilities;
using Contingent.Utilities.Validation;

namespace Contingent.Models.Domain.Citizenship;

public class RussianCitizenship : Citizenship
{
    public static Mapper<RussianCitizenship> GetMapper(Column? source, bool includeAddress, JoinSection.JoinType joinType = JoinSection.JoinType.InnerJoin)
    {
        var mapper = new Mapper<RussianCitizenship>(
            (reader) =>
            {
                if (reader["id_r_cit"].GetType() == typeof(DBNull))
                {
                    return QueryResult<RussianCitizenship>.NotFound();
                }
                var found = new RussianCitizenship()
                {
                    _id = (int)reader["id_r_cit"],
                    _surname = NamePart.CreateFromDatabase((string)reader["surname"]),
                    _name = NamePart.CreateFromDatabase((string)reader["name"]),
                    _patronymic = reader["patronymic"].GetType() == typeof(DBNull) ? null : NamePart.CreateFromDatabase((string)reader["patronymic"]),
                };
                found._legalAddressId = reader["legal_address"].GetType() == typeof(DBNull) ? null : (int)reader["legal_address"];
                found._legalAddress = includeAddress && found._legalAddressId is not null ? AddressModel.GetAddressById((int)reader["legal_address"], null).Result : null;
                return QueryResult<RussianCitizenship>.Found(found);

            },
            new Column[] {
            new Column("surname", "rus_citizenship"),
            new Column("name", "rus_citizenship"),
            new Column("patronymic", "rus_citizenship"),
            new Column("legal_address", "rus_citizenship"),
            new Column("id_r_cit", "rus_citizenship"),
            }
        );
        if (source is not null)
        {
            mapper.PathTo.AddHead(joinType, source, new Column("id_r_cit", "rus_citizenship"));
        }
        return mapper;
    }
    public static ComplexWhereCondition GetFilterClause(RussianCitizenshipInDTO parameters, ref SQLParameterCollection paramCollection)
    {
        var where = ComplexWhereCondition.Empty;
        if (!string.IsNullOrEmpty(parameters.Name) && !string.IsNullOrWhiteSpace(parameters.Name))
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("lower", "name", "rus_citizenship", null),
                    paramCollection.Add(parameters.Name.ToLower() + "%"),
                    WhereCondition.Relations.Like
                ))
            );
        }
        if (!string.IsNullOrEmpty(parameters.Surname) && !string.IsNullOrWhiteSpace(parameters.Surname))
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("lower", "surname", "rus_citizenship", null),
                    paramCollection.Add(parameters.Surname.ToLower() + "%"),
                    WhereCondition.Relations.Like
                ))
            );
        }
        if (!string.IsNullOrEmpty(parameters.Patronymic) && !string.IsNullOrWhiteSpace(parameters.Patronymic))
        {
            where = where.Unite(
                ComplexWhereCondition.ConditionRelation.AND,
                new ComplexWhereCondition(new WhereCondition(
                    new Column("lower", "patronymic", "rus_citizenship", null),
                    paramCollection.Add(parameters.Patronymic.ToLower() + "%"),
                    WhereCondition.Relations.Like
                ))
            );
        }
        return where;
    }

    // добавить ограничения уникальности некоторых параметров
    private int? _id;
    private NamePart _name;
    private NamePart _surname;
    private NamePart? _patronymic;

    public int? Id
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
    // зависимости
    private int? _legalAddressId;
    private AddressModel? _legalAddress;

    public int? LegalAddressId
    {
        get => _legalAddress is null ? _legalAddressId : _legalAddress.Id;
    }
    public AddressModel? LegalAddress
    {
        get
        {
            if (_legalAddress is null)
            {
                _legalAddress = AddressModel.GetAddressById(_legalAddressId, null).Result;
            }
            return _legalAddress;
        }
        set
        {
            _legalAddressId = value?.Id;
            _legalAddress = value;
        }
    }

    private RussianCitizenship() : base()
    {
        _id = null;
        _legalAddressId = null;
    }

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope = null)
    {
        await using var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "SELECT EXISTS(SELECT id FROM rus_citizenship WHERE id_r_cit = @p1)";
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

    public override string GetName()
    {
        return (Surname + " " + Name + " " + Patronymic).TrimEnd();
    }
    // переместить валидацию сюда
    public static Result<RussianCitizenship> Create(RussianCitizenshipInDTO? dto, ObservableTransaction? scope = null)
    {
        if (dto is null)
        {
            return Result<RussianCitizenship>.Failure(new ValidationError(nameof(dto), "Непределенная модель гражданства"));
        }
        var errors = new List<ValidationError>();
        RussianCitizenship citizenship = null!;

        if (dto.Id != null && dto.Id != Utils.INVALID_ID)
        {
            var fromDB = GetById(dto.Id);
            if (errors.IsValidRule(
                fromDB is not null,
                "Неверно указан id гражданства",
                nameof(RussianCitizenship)
            ))
            {
                citizenship = fromDB!;
            }
        }
        citizenship ??= new RussianCitizenship();
        if (dto.LegalAddress.IsAddressStated())
        {
            var addressResult = AddressModel.Create(dto.LegalAddress, scope);
            if (addressResult.IsSuccess)
            {
                citizenship.LegalAddress = addressResult.ResultObject;
            }
            else
            {
                errors.AddRange(addressResult.Errors);
            }
        }
        else
        {
            citizenship.LegalAddress = null;
        }

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
        if (string.IsNullOrEmpty(dto.Patronymic))
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
        if (errors.Any())
        {
            return Result<RussianCitizenship>.Failure(errors);
        }
        return Result<RussianCitizenship>.Success(citizenship);

    }
    public async Task<ResultWithoutValue> Save(ObservableTransaction? scope)
    {
        if (_legalAddress is not null)
        {
            var result = await _legalAddress.Save(scope);
            if (result.IsFailure)
            {
                return result;
            }
        }

        if (_id is not null)
        {
            await Update(scope);
            return ResultWithoutValue.Success();
        }

        var conn = await Utils.GetAndOpenConnectionFactory();
        string cmdText = "INSERT INTO rus_citizenship( " +
                        " surname, name, patronymic, legal_address) " +
                        " VALUES (@p1, @p2, @p3, @p4) RETURNING id_r_cit";
        NpgsqlCommand? command;

        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        else
        {
            command = new NpgsqlCommand(cmdText, conn);
        }
        command.Parameters.Add(new NpgsqlParameter<string>("p1", Surname));
        command.Parameters.Add(new NpgsqlParameter<string>("p2", Name));
        command.Parameters.Add(new NpgsqlParameter("p3", _patronymic == null ? DBNull.Value : Patronymic));
        command.Parameters.Add(new NpgsqlParameter("p4", LegalAddressId is null ? DBNull.Value : LegalAddressId.Value));

        await using (conn)
        {
            await using (command)
            {
                using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                _id = (int)reader["id_r_cit"];
            }
        }
        return ResultWithoutValue.Success();
    }
    private async Task Update(ObservableTransaction? scope = null)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var cmdText = "UPDATE public.rus_citizenship " +
        " SET " +
        " surname=@p3, name=@p4, patronymic=@p5, legal_address=@p6" +
        " WHERE id_r_cit = @p7";
        NpgsqlCommand cmd;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _surname.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter<string>("p4", _name.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter("p5", _patronymic?.NameToken is null ? DBNull.Value : _patronymic.NameToken));
        cmd.Parameters.Add(new NpgsqlParameter("p6", LegalAddressId.HasValue ? LegalAddressId.Value : DBNull.Value));
        cmd.Parameters.Add(new NpgsqlParameter<int>("p7", (int)_id!));

        using (cmd)
        {
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public static RussianCitizenship? GetById(int? id)
    {
        if (id is null || id.Value == Utils.INVALID_ID)
        {
            return null;
        }
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        var parameters = new SQLParameterCollection();
        var sqlQuery = SelectQuery<RussianCitizenship>.Init("rus_citizenship")
        .AddMapper(GetMapper(null, true))
        .AddWhereStatement(new ComplexWhereCondition(new WhereCondition(
            new Column("id_r_cit", "rus_citizenship"), parameters.Add((int)id), WhereCondition.Relations.Equal
        )))
        .AddParameters(parameters)
        .Finish().ResultObject;
        return sqlQuery.Execute(conn, new QueryLimits(0, 1)).Result.FirstOrDefault();
    }
}