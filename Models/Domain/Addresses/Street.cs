using System.Text.RegularExpressions;
using Npgsql;
using Utilities;
using StudentTracking.Models.Domain.Misc;
using Utilities.Validation;
using StudentTracking.Models.JSON;
namespace StudentTracking.Models.Domain.Address;

public class Street : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"улица"),
        new Regex(@"набережная"),
        new Regex(@"проспект"),
        new Regex(@"тупик"),
        new Regex(@"аллея"),
        new Regex(@"площадь"),
        new Regex(@"проезд"),
        new Regex(@"шоссе"),
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Street, new NameFormatting("ул.", "Улица", NameFormatting.BEFORE)},
        {Types.Embankment, new NameFormatting("наб.", "Набережная", NameFormatting.BEFORE)},
        {Types.Avenue, new NameFormatting("пр-кт", "Проспект", NameFormatting.BEFORE)},
        {Types.DeadEnd, new NameFormatting("туп.", "Тупик", NameFormatting.BEFORE)},
        {Types.Alley, new NameFormatting("ал.", "Аллея", NameFormatting.BEFORE)},
        {Types.Square, new NameFormatting("пл.", "Площадь", NameFormatting.BEFORE)},
        {Types.Passage, new NameFormatting("пр-д", "Проезд", NameFormatting.BEFORE)},
        {Types.Highway, new NameFormatting("ш.", "Шоссе", NameFormatting.BEFORE)},
    };
    public enum Types
    {
        NotMentioned = -1,
        Street = 1, // улица
        Embankment = 2, // набережная
        Avenue = 3, // проспект 
        DeadEnd = 4, // тупик
        Alley = 5, // аллея
        Square = 6, // площадь
        Passage = 7, // проезд 
        Highway = 8, // шоссе 
    }

    private int _id;
    private int _parentSettlementId;
    private string _untypedName;
    private Types _streetType;
    public int Id
    {
        get => _id;
    }

    public async Task SetSettlementParent(int id, ObservableTransaction? scope)
    {

        bool exists = await Settlement.IsIdExists(id, scope);
        if (PerformValidation(
               () => exists,
               new DbIntegrityValidationError(nameof(SettlementParentId), "ID н.п. должно быть указано")
       ))
        {
            _parentSettlementId = id;
        }

    }

    public int SettlementParentId
    {
        get => _parentSettlementId;
    }
    public int StreetType
    {
        get => (int)_streetType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(StreetType), "Неверно указан тип улицы")
            ))
            {
                _streetType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_untypedName);
        set
        {
            if (PerformValidation(
                () => !ValidatorCollection.CheckStringPatterns(value, Restrictions),
                new ValidationError(nameof(UntypedName), "Название улицы содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 200),
                    new ValidationError(nameof(UntypedName), "Название улицы вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(nameof(UntypedName), "Название улицы содержит недопустимые символы")))
                    {
                        _untypedName = value.ToLower();
                    }
                }
            }
        }
    }
    public string LongTypedName
    {
        get
        {
            return Names[_streetType].FormatLong(UntypedName);
        }
    }


    protected Street(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    protected Street() : base()
    {

        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(StreetType));
        RegisterProperty(nameof(SettlementParentId));

        _id = Utils.INVALID_ID;
        _untypedName = "";
        _streetType = Types.NotMentioned;
        _parentSettlementId = Utils.INVALID_ID;
    }
    public static Street MakeUnsafe(int id, string untypedName, int type)
    {
        var dist = new Street
        {
            _id = id,
            _untypedName = untypedName,
            _streetType = (Types)type
        };
        return dist;
    }
    public async Task Save(ObservableTransaction? scope)
    {
        var connWithin = await Utils.GetAndOpenConnectionFactory();
        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        NpgsqlCommand? command = null;
        string cmdText =
        "INSERT INTO streets( " +
                " settlement, street_type, full_name) " +
                " VALUES (@p1, @p2, @p3) RETURNING id";
        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, args) => this._id = Utils.INVALID_ID));
        }
        else
        {
            command = new NpgsqlCommand(cmdText, connWithin);
        }
        command.Parameters.Add(new NpgsqlParameter<int>("p1", _parentSettlementId));
        command.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_streetType));
        command.Parameters.Add(new NpgsqlParameter<string>("p3", _untypedName));
        await using (connWithin)
        await using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
    }

    // public static List<Street>? GetAllStreetWithin(int settlementId)
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {
    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM streets WHERE settlement = @p1", conn)
    //         {
    //             Parameters = {
    //                 new NpgsqlParameter("p1", settlementId)
    //             }
    //         })
    //         {
    //             var reader = cmd.ExecuteReader();
    //             if (!reader.HasRows)
    //             {
    //                 return null;
    //             }
    //             else
    //             {
    //                 var result = new List<Street>();
    //                 while (reader.Read())
    //                 {
    //                     var st = new Street((int)reader["id"])
    //                     {
    //                         _parentSettlementId = settlementId,
    //                         _untypedName = (string)reader["full_name"],
    //                         _streetType = (Types)reader["street_type"]
    //                     };
    //                     result.Add(st);
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<Street?> GetById(int strId, ObservableTransaction? scope)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string query = "SELECT * FROM streets WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope == null)
        {
            cmd = new NpgsqlCommand(query, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", strId));

        await using (conn)
        await using (cmd)
        {
            scope?.Capture();
            using var reader = await cmd.ExecuteReaderAsync();
            scope?.Release();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            var st = new Street((int)reader["id"])
            {
                _parentSettlementId = (int)reader["settlement"],
                _untypedName = (string)reader["full_name"],
                _streetType = (Types)reader["street_type"]
            };
            return st;
        }
    }
    public static Street? BuildByName(string? fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        Street toBuild = new Street();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.StreetType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            }
        }
        return null;
    }

    public static async Task<bool> IsIdExists(int id, ObservableTransaction? scope)
    {
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            string cmdText = "SELECT EXISTS(SELECT id FROM streets WHERE id = @p1)";
            NpgsqlCommand? cmd = null;
            if (scope != null){
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else{
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            cmd.Parameters.Add(new NpgsqlParameter<int> ("p1", id)); 
            await using (cmd)
            {
                scope?.Capture();
                using var reader = await cmd.ExecuteReaderAsync();
                scope?.Release();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        Street? got = await GetById(_id, scope);
        if (got == null)
        {
            await using (var conn = await Utils.GetAndOpenConnectionFactory())
            {
                string cmdText = "SELECT id FROM streets WHERE settlement = @p1 AND full_name = @p2 AND street_type = @p3";
                NpgsqlCommand? cmd = null;
                if (scope != null){
                    cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
                }
                else{
                    cmd = new NpgsqlCommand(cmdText, conn);
                }
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _parentSettlementId));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _untypedName));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p3", (int)_streetType));
                using (cmd)
                {
                    scope?.Capture();
                    using var reader = await cmd.ExecuteReaderAsync();
                    scope?.Release();
                    if (!reader.HasRows)
                    {
                        return null;
                    }
                    await reader.ReadAsync();
                    _id = (int)reader["id"];
                    return this;
                }
            }
        }
        else
        {
            return got;
        }
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.GetType() != typeof(Street))
        {
            return false;
        }
        var parsed = (Street)other;
        return parsed._id == _id && parsed._parentSettlementId == _parentSettlementId
            && parsed._streetType == _streetType
            && parsed._untypedName == _untypedName;

    }
}
