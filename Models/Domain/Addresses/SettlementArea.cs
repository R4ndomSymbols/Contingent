using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class SettlementArea : InDbValidatedObject
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
    private District? _parentDistrict;
    private string _nameWithoutType;
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
                new ValidationError(this, nameof(SettlementAreaType), "Неверно указан тип поселения")
            ))
            {
                _settlementAreaType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => _nameWithoutType;
        set
        {
            if (PerformValidation(
                () => !ValidatorCollection.CheckStringPatterns(value, Restrictions),
                new ValidationError(this,nameof(UntypedName), "Название поселения содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError(this,nameof(UntypedName), "Название поселения вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(this,nameof(UntypedName), "Название поселения содержит недопустимые символы")))
                    {
                        _nameWithoutType = value;
                    }
                }
            }
        }
    }
    
    public District? Parent {
        get {
            return _parentDistrict;
        }
        set {
            if (PerformValidation(
                () => {
                    if (value == null){
                        return false;
                    }
                    if (value.CurrentState == RelationTypes.Invalid || value.CurrentState == RelationTypes.UnboundInvalid){
                        return false;
                    }
                    return true;
                }, new DbIntegrityValidationError(this, nameof(Parent), "Неверно указан муниципалитет верхнего уровня"))){
                if (value != null){
                    _parentDistrict = value;
                    _parentDistrictId = value.Id;
                }   
        }
        }
    }

    protected SettlementArea(int id, int parentId, string name, Types type) : this()
    {
        _id = id;
        _parentDistrictId = parentId;
        _nameWithoutType = name;
        _settlementAreaType = type;
    }
    protected SettlementArea(){
        _nameWithoutType = "";
        _settlementAreaType = Types.NotMentioned;
        _validationErrors = new List<ValidationError>();
    }
    
    public void Save()
    {
        if (CheckErrorsExist())
        {
            return;
        }
        UpdateObjectIntegrityState(GetById(_id));
        if (CurrentState != RelationTypes.Pending){
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
                        new("p1", _parentDistrictId),
                        new("p2", _settlementAreaType),
                        new("p3", _nameWithoutType),
                    }
            })
            {
                var reader = cmd.ExecuteReader();
                _id = (int)reader["id"];
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
    public override bool Equals(object? obj)
    {
        if (obj == null){
            return false;
        }
        if (obj.GetType() != typeof(SettlementArea)){
            return false;
        }
        var unboxed = (SettlementArea)obj;
        return _id == unboxed._id && _nameWithoutType == unboxed._nameWithoutType &&
        _parentDistrictId == unboxed._parentDistrictId && _settlementAreaType == unboxed._settlementAreaType;
    }

}
