using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class SettlementArea : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"поселение"),
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.CitySettlement, new NameFormatting("г.п.", "Городское поселение", NameFormatting.BEFORE)},
        {Types.CountysideDistrict, new NameFormatting("с.п.", "Сельское поселение", NameFormatting.BEFORE)},
    };
    public enum Types
    {
        NotMentioned = -1,
        CitySettlement = 1, // городской округ
        CountysideDistrict = 2, // муниципальный район
    }

    private int _id;
    private int _parentDistrictId;
    private string _untypedName;
    private Types _settlementAreaType;
    public int Id
    {
        get => _id;
    }
    public async Task SetDistrictParent(int id, ObservableTransaction? scope){
        bool exists = await District.IsIdExists(id, scope);
        if (PerformValidation(() => exists, 
            new DbIntegrityValidationError(nameof(DistrictParentId), "ID муниципалитета ВУ должен быть указан")))
        {
            _parentDistrictId = id;
        }

    }

    public int DistrictParentId
    {
        get => _parentDistrictId;
    }
    public int SettlementAreaType
    {
        get => (int)_settlementAreaType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(SettlementAreaType), "Неверно указан тип поселения")
            ))
            {
                _settlementAreaType = (Types)value;
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
                new ValidationError(nameof(UntypedName), "Название поселения содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError(nameof(UntypedName), "Название поселения вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(nameof(UntypedName), "Название поселения содержит недопустимые символы")))
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
            return Names[_settlementAreaType].FormatLong(UntypedName);
        }
    }

    protected SettlementArea(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    protected SettlementArea() : base()
    {
        RegisterProperty(nameof(DistrictParentId));
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(SettlementAreaType));
        _untypedName = "";
        _settlementAreaType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        _parentDistrictId = Utils.INVALID_ID;
    }
    public static SettlementArea MakeUnsafe(int id, string untypedName, int type)
    {
        var dist = new SettlementArea
        {
            _id = id,
            _untypedName = untypedName,
            _settlementAreaType = (Types)type
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
        "INSERT INTO settlement_areas( " +
                " district, settlement_area_type, full_name) " +
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
        command.Parameters.Add(new NpgsqlParameter("p1", (int)_parentDistrictId) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter("p2", (int)_settlementAreaType) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter("p3", _untypedName) { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });

        await using (connWithin)
        await using (command)
        {   
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
    }

    // public static List<SettlementArea>? GetAllDistrictsWithin(int districtId)
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {

    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM settlement_areas WHERE district = @p1", conn)
    //         {
    //             Parameters = {
    //                 new NpgsqlParameter("p1", districtId)
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
    //                 var result = new List<SettlementArea>();
    //                 while (reader.Read())
    //                 {
    //                     var sa = new SettlementArea((int)reader["id"])
    //                     {
    //                         _parentDistrictId = districtId,
    //                         _untypedName = (string)reader["full_name"],
    //                         _settlementAreaType = (Types)reader["settlement_area_type"]
    //                     };
    //                     result.Add(sa);
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<SettlementArea?> GetById(int saId, ObservableTransaction? scope)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string query = "SELECT * FROM settlements WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope == null)
        {
            cmd = new NpgsqlCommand(query, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", saId));
        await using (conn)
        await using (cmd)
        {   
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            var sa = new SettlementArea((int)reader["id"])
            {
                _parentDistrictId = (int)reader["district"],
                _untypedName = (string)reader["full_name"],
                _settlementAreaType = (Types)(int)reader["settlement_area_type"]

            };
            return sa;
        }
    }
    public static SettlementArea? BuildByName(string? fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        SettlementArea toBuild = new SettlementArea();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.SettlementAreaType = (int)pair.Key;
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
            string cmdText = "SELECT EXISTS(SELECT id FROM settlement_areas WHERE id = @p1)";
            NpgsqlCommand? cmd = null; 
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
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        SettlementArea? got = await GetById(_id, scope);
        if (got == null)
        {
            await using (var conn = await Utils.GetAndOpenConnectionFactory())
            {
                string cmdText = "SELECT id FROM settlement_areas WHERE district = @p1 AND settlement_area_type = @p2 AND full_name = @p3";
                NpgsqlCommand? cmd  = null;
                if (scope != null){
                    cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
                }
                else{
                    cmd = new NpgsqlCommand(cmdText, conn);
                }
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _parentDistrictId));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_settlementAreaType));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _untypedName));
                await using (cmd)
                {
                    using var reader = await cmd.ExecuteReaderAsync();
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
        }
        else
        {
            return got;
        }
    }
    public override bool Equals(IDbObjectValidated? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(SettlementArea))
        {
            return false;
        }
        var unboxed = (SettlementArea)obj;
        return _id == unboxed._id && _untypedName == unboxed._untypedName &&
        _parentDistrictId == unboxed._parentDistrictId && _settlementAreaType == unboxed._settlementAreaType;
    }

}
