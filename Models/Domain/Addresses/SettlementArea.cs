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
        set
        {
            _id = value;
        }
    }
    public int DistrictParentId
    {
        get => _parentDistrictId;
        set
        {
            _parentDistrictId = value;
        }
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
        get => _untypedName;
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
                        _untypedName = value;
                    }
                }
            }
        }
    }
     public string LongTypedName {
        get {
            return Names[_settlementAreaType].FormatLong(_untypedName);
        }
    }

    protected SettlementArea(int id, int parentId, string name, Types type) : this()
    {
        _id = id;
        _parentDistrictId = parentId;
        _untypedName = name;
        _settlementAreaType = type;
    }
    protected SettlementArea() : base() {
        _untypedName = "";
        _settlementAreaType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        _parentDistrictId = Utils.INVALID_ID;
    }
    public static SettlementArea MakeUnsafe(int id, string untypedName, int type){
        var dist = new SettlementArea
        {
            _id = id,
            _untypedName = untypedName,
            _settlementAreaType = (Types)type 
        };
        return dist;
    }
    
    public bool Save()
    {
        if (CurrentState != RelationTypes.Pending || !District.IsIdExists(_parentDistrictId)){
            return false;
        }

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO settlement_areas( " +
                " district, settlement_area_type, full_name) " +
	            " VALUES (@p1, @p2, @p3) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _parentDistrictId),
                        new("p2", _settlementAreaType),
                        new("p3", _untypedName),
                    }
            })
            {
                var reader = cmd.ExecuteReader();
                _id = (int)reader["id"];
                SetBound();
                return true;
            }
        }
    }
    public static List<SettlementArea>? GetAllDistrictsWithin(int districtId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM settlement_areas WHERE district = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", districtId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var result = new List<SettlementArea>();
                    while (reader.Read())
                    {
                        var sa = new SettlementArea((int)reader["id"], districtId, (string)reader["full_name"], (Types)reader["settlement_area_type"]);
                        sa.SetBound();
                        result.Add(sa);
                    }
                    return result;
                }
            }
        }
    }
    public static SettlementArea? GetById(int saId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM settlement_areas WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", saId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                var sa = new SettlementArea((int)reader["id"], (int)reader["district"], (string)reader["full_name"], (Types)(int)reader["settlement_area_type"]); 
                sa.SetBound();
                return sa;
            }
        }
    }
    public static SettlementArea? BuildByName(string? fullname){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        SettlementArea toBuild = new SettlementArea(); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.SettlementAreaType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            } 
        }
        return null;
    }
     public static bool IsIdExists(int id){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM settlement_areas WHERE id = @p1)", conn) 
            {
                Parameters = {
                    new("p1", id),
                }
            })
            {
               return (bool)cmd.ExecuteReader()["exists"];
            }
        }
    } 
    public override IDbObjectValidated? GetDbRepresentation()
    {
        return GetById(_id);
    }
    public override bool Equals(IDbObjectValidated? obj)
    {
        if (obj == null){
            return false;
        }
        if (obj.GetType() != typeof(SettlementArea)){
            return false;
        }
        var unboxed = (SettlementArea)obj;
        return _id == unboxed._id && _untypedName == unboxed._untypedName &&
        _parentDistrictId == unboxed._parentDistrictId && _settlementAreaType == unboxed._settlementAreaType;
    }

}
