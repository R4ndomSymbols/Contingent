using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Settlement : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"город"),
        new Regex(@"поселок"),
        new Regex(@"село\s"),
        new Regex(@"деревня"),
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.City, new NameFormatting("г.", "Город", NameFormatting.BEFORE)},
        {Types.Town, new NameFormatting("пгт.", "Поселок городского типа", NameFormatting.BEFORE)},
        {Types.Village, new NameFormatting("с.", "Село", NameFormatting.BEFORE)},
        {Types.SmallVillage, new NameFormatting("д.", "Деревня", NameFormatting.BEFORE)},
        {Types.TinyVillage, new NameFormatting("п.", "Поселок", NameFormatting.BEFORE)},
    };
    public enum Types
    {
        NotMentioned = -1,
        City = 1, // город
        Town = 2, // поселок городского типа
        Village = 3, // село 
        SmallVillage = 4, // деревня
        TinyVillage = 5, // поселок

    }

    private int _id;
    private int? _parentSettlementAreaId;
    private int? _parentDistrictId;
    private string _untypedName;
    private Types _settlementType;
    public int Id
    {
        get => _id;
    }
    private int ParentId
    {
        get => _id;
    }

    public int? SettlementAreaParentId
    {
        get => _parentSettlementAreaId;
    }
    public async Task SetSettlementAreaParent(int? id, ObservableTransaction? scope)
    {
        if (PerformValidation(
            () => id != null, 
            new DbIntegrityValidationError(nameof(ParentId), "Id поселения не указан")
        ))
        {
            if (id != null){
                bool exists = await SettlementArea.IsIdExists((int)id, scope);
                if (PerformValidation(() => exists, 
                new DbIntegrityValidationError(nameof(ParentId), "Id поселения указан неверно"))){
                    _parentDistrictId = null;
                    _parentSettlementAreaId = id;
                }
            }
        }
    }
    public async Task SetDistrictParent(int? id, ObservableTransaction? scope)
    {
        if (PerformValidation(
            () => id != null, 
            new DbIntegrityValidationError(nameof(ParentId), "Id муниципального образования верхнего уровня не указан")
        ))
        {
            if (id != null){
                bool exists = await District.IsIdExists((int)id, scope);
                if (PerformValidation(() => exists, 
                new DbIntegrityValidationError(nameof(ParentId), "Id муниципального образования верхнего уровня указан неверно"))){
                    _parentDistrictId = id;
                    _parentSettlementAreaId = null;
                }
            }
        }
    }
    public int? DistrictParentId
    {
        get => _parentDistrictId;
    }
    public int SettlementType
    {
        get => (int)_settlementType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(SettlementType), "Неверно указан тип населенного пункта")
            ))
            {
                _settlementType = (Types)value;
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
                new ValidationError(nameof(UntypedName), "Название населенного пункта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError(nameof(UntypedName), "Название населенного пункта вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(nameof(UntypedName), "Название населенного пункта содержит недопустимые символы")))
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
            return Names[_settlementType].FormatLong(UntypedName);
        }
    }
    // валидация не проверяет типы, внутри которых возможно размещение городов, добавить

    private Settlement() : base()
    {
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(SettlementType));
        RegisterProperty(nameof(ParentId));

        _untypedName = "";
        _settlementType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        _parentDistrictId = null;
        _parentSettlementAreaId = null;
    }

    protected Settlement(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    public static Settlement MakeUnsafe(int id, string untypedName, int type)
    {
        var dist = new Settlement
        {
            _id = id,
            _untypedName = untypedName,
            _settlementType = (Types)type
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
        "INSERT INTO settlements( " +
                " settlement_area, settlement_type, full_name, district) " +
                " VALUES (@p1, @p2, @p3, @p4) RETURNING id";
        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, arg) => this._id = Utils.INVALID_ID));
        }
        else
        {
            command = new NpgsqlCommand(cmdText, connWithin);
        }
        command.Parameters.Add(new NpgsqlParameter("p1", _parentSettlementAreaId == null ? DBNull.Value : (int)_parentSettlementAreaId));
        command.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_settlementType));
        command.Parameters.Add(new NpgsqlParameter<string>("p3", _untypedName));
        command.Parameters.Add(new NpgsqlParameter("p4", _parentDistrictId == null ? DBNull.Value : (int)_parentDistrictId));

        await using (connWithin)
        await using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
    }

    // public static List<Settlement>? GetAllSettlementsWithin(int settlementAreaId)
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {

    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM settlements WHERE settlement_area = @p1", conn)
    //         {
    //             Parameters = {
    //                 new NpgsqlParameter("p1", settlementAreaId)
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
    //                 var result = new List<Settlement>();
    //                 while (reader.Read())
    //                 {
    //                     int? parentSettlementArea = null;
    //                     int? parentDistrict = null;
    //                     if (reader["settlement_area"].GetType() != typeof(DBNull))
    //                     {
    //                         parentSettlementArea = (int)reader["settlement_area"];
    //                     }
    //                     if (reader["district"].GetType() != typeof(DBNull))
    //                     {
    //                         parentDistrict = (int)reader["district"];
    //                     }
    //                     var s = new Settlement((int)reader["id"])
    //                     {
    //                         _parentSettlementAreaId = parentSettlementArea,
    //                         _parentDistrictId = parentDistrict,
    //                         _untypedName = (string)reader["full_name"],
    //                         _settlementType = (Types)(int)reader["settlement_type"]
    //                     };

    //                     result.Add(s);
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<Settlement?> GetById(int sId, ObservableTransaction? scope)
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
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", sId));

        await using (conn)
        await using (cmd)
        {   
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            int? parentSettlementArea = null;
            int? parentDistrict = null;
            if (reader["settlement_area"].GetType() != typeof(DBNull))
            {
                parentSettlementArea = (int)reader["settlement_area"];
            }
            if (reader["district"].GetType() != typeof(DBNull))
            {
                parentDistrict = (int)reader["district"];
            }
            var s = new Settlement((int)reader["id"])
            {
                _parentDistrictId = parentDistrict,
                _parentSettlementAreaId = parentSettlementArea,
                _untypedName = (string)reader["full_name"],
                _settlementType = (Types)(int)reader["settlement_type"]
            };
            return s;
        }
    }
    public static Settlement? BuildByName(string fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        Settlement toBuild = new Settlement();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.SettlementType = (int)pair.Key;
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
            string cmdText = "SELECT EXISTS(SELECT id FROM settlements WHERE id = @p1)";
            NpgsqlCommand command;
            if (scope != null){
                command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else{
                command = new NpgsqlCommand(cmdText, conn);
            }
            command.Parameters.Add(new NpgsqlParameter<int> ("p1", id));
            await using (command)
            {   
                scope?.Capture();
                using var reader = await command.ExecuteReaderAsync();
                scope?.Release();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        Settlement? got = await GetById(_id, scope);
        if (got == null)
        {
            await using (var conn = await Utils.GetAndOpenConnectionFactory())
            {
                string cmdText;
                if (_parentDistrictId == null){
                    cmdText = "SELECT id FROM settlements WHERE settlement_area = @p1 AND settlement_type = @p2 AND district IS NULL AND full_name = @p4";
                }
                else{
                    cmdText = "SELECT id FROM settlements WHERE settlement_area IS NULL AND settlement_type = @p2 AND district = @p3 AND full_name = @p4";
                }

                NpgsqlCommand? cmd = null;
                if (scope != null){
                    cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
                }
                else{
                    cmd =  new NpgsqlCommand(cmdText, conn);
                }
                if (_parentSettlementAreaId!=null){
                    cmd.Parameters.Add(new NpgsqlParameter("p1", (int)_parentSettlementAreaId));
                }
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_settlementType));
                if (_parentDistrictId!=null){
                    cmd.Parameters.Add(new NpgsqlParameter("p3", _parentDistrictId));
                }
                cmd.Parameters.Add(new NpgsqlParameter<string>("p4", _untypedName));
                using (cmd)
                {
                    using var reader = await cmd.ExecuteReaderAsync();
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
    public override bool Equals(IDbObjectValidated? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(Settlement))
        {
            return false;
        }
        var unboxed = (Settlement)obj;
        return _id == unboxed._id &&
        _untypedName == unboxed._untypedName &&
        _parentDistrictId == unboxed._parentDistrictId &&
        _parentSettlementAreaId == unboxed._parentSettlementAreaId &&
        _settlementType == unboxed._settlementType;
    }


}
