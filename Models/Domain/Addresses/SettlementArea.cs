using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class SettlementArea : ValidatedObject<SettlementArea>
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
    private int _districtId;
    private District? _parentDistrict;
    private string _fullName;
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
        get => _districtId;
        set
        {
            _districtId = value;
        }
    }
    public int SettlementAreaType
    {
        get => (int)_settlementAreaType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError<SettlementArea>(nameof(SettlementAreaType), "Неверно указан тип поселения")
            ))
            {
                _settlementAreaType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => _fullName;
        set
        {
            if (PerformValidation(
                () => !ValidatorCollection.CheckStringPatterns(value, Restrictions),
                new ValidationError<SettlementArea>(nameof(UntypedName), "Название поселения содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError<SettlementArea>(nameof(UntypedName), "Название поселения вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError<SettlementArea>(nameof(UntypedName), "Название поселения содержит недопустимые символы")))
                    {
                        _fullName = value;
                    }
                }
            }
        }
    }
    public string LongTypedName {
        get => Names[_settlementAreaType].FormatLong(_fullName);
    }

    protected SettlementArea(int id, int parentId, string name, Types type)
    {
        _id = id;
        _districtId = parentId;
        _fullName = name;
        _settlementAreaType = type;
        _validationErrors = new List<ValidationError<SettlementArea>>();
    }
    protected SettlementArea(){
        _fullName = "";
        _settlementAreaType = Types.NotMentioned;
        AddError(new ValidationError<SettlementArea>(nameof(UntypedName), "Имя поселения не может быть пустым"));
        AddError(new ValidationError<SettlementArea>(nameof(SettlementAreaType), "Тип поселения не может быть не указан"));
    }
    
    public void Save()
    {
        if (CheckErrorsExist())
        {
            return;
        }

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO settlement_areas( " +
                " district, settlement_area_type, full_name) " +
	            " VALUES (@p1, @p2, @p3) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _districtId),
                        new("p2", _settlementAreaType),
                        new("p3", _fullName),
                    }
            })
            {
                try
                {
                    var reader = cmd.ExecuteReader();
                    _id = (int)reader["id"];
                }
                catch (NpgsqlException)
                {
                    AddError(new ValidationError<SettlementArea>(nameof(DistrictParentId), "Неверно указан родитель"));
                }
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
                        result.Add(new SettlementArea((int)reader["id"], districtId, (string)reader["full_name"], (Types)reader["settlement_area_type"]));
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
                return new SettlementArea((int)reader["id"], (int)reader["district"], (string)reader["full_name"], (Types)(int)reader["settlement_area_type"]);
            }
        }
    }
    public static SettlementArea? BuildByName(string? fullname, District parent){
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
                toBuild._parentDistrict = parent;
                return toBuild;
            } 
        }
        return null;
    }
}
