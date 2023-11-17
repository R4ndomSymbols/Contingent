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
        set
        {
            _id = value;
        }
    }
    public int? SettlementAreaParentId
    {
        get => _parentSettlementAreaId;
        set
        {
            _parentSettlementAreaId = value;
        }
    }
    public int? DistrictParentId
    {
        get => _parentDistrictId;
        set
        {
            _parentDistrictId = value;
        }
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
        get => _untypedName;
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
                        _untypedName = value;
                    }
                }
            }
        }
    }
    public string LongTypedName {
        get {
            return Names[_settlementType].FormatLong(_untypedName);
        }
    }
    // валидация не проверяет типы, внутри которых возможно размещение городов, добавить

    private Settlement() : base(){
        _untypedName = "";
        _settlementType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        _parentDistrictId = null;
        _parentSettlementAreaId = null;
    }

    protected Settlement(int id, int? parentDistrict, int? parentSettlementArea, string name, Types type) : this()
    {
        _id = id;
        if (
            (parentDistrict == null && parentSettlementArea == null) ||
            (parentDistrict != null && parentSettlementArea != null)
        ){
            throw new ArgumentException("У потомка не может быть двух родителей");
        }

        _parentSettlementAreaId = parentSettlementArea;
        _parentDistrictId = parentDistrict;
        _untypedName = name;
        _settlementType = type;
    }

    // ошибка для привлечения внимания, переделать сохранение (неверные NULL значения)
    --
    public bool Save()
    {
        if (CurrentState != RelationTypes.Pending || 
            ((_parentSettlementAreaId == null ? false : !SettlementArea.IsIdExists((int)_parentSettlementAreaId))
            ^
            (_parentDistrictId == null ? false : !District.IsIdExists((int)_parentDistrictId))
            )){
            return false; 
        }
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO settlements( " +
	            " settlement_area, settlement_type, full_name) " +
	            " VALUES (@p1, @p2, @p3) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _parentSettlementAreaId),
                        new("p2", _settlementType),
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
    public static List<Settlement>? GetAllSettlementsWithin(int settlementAreaId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM settlements WHERE settlement_area = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", settlementAreaId)
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
                    var result = new List<Settlement>();
                    while (reader.Read())
                    {
                        int? parentSettlementArea = null;
                        int? parentDistrict = null;
                        if (reader["settlement_area"].GetType() != typeof(DBNull)){
                            parentSettlementArea = (int)reader["settlement_area"];
                        }
                        if (reader["district"].GetType() != typeof(DBNull)){
                            parentDistrict = (int)reader["district"];
                        }
                        var s = new Settlement((int)reader["id"], parentSettlementArea, parentDistrict, (string)reader["full_name"], (Types)(int)reader["settlement_type"]);
                        s.SetBound();
                        result.Add(s);
                    }
                    return result;
                }
            }
        }
    }
    public static Settlement? GetById(int sId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM settlements WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", sId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                int? parentSettlementArea = null;
                int? parentDistrict = null;
                if (reader["settlement_area"].GetType() != typeof(DBNull)){
                    parentSettlementArea = (int)reader["settlement_area"];
                }
                if (reader["district"].GetType() != typeof(DBNull)){
                    parentDistrict = (int)reader["district"];
                }
                var s = new Settlement((int)reader["id"], parentDistrict, parentSettlementArea, (string)reader["full_name"], (Types)(int)reader["settlement_area_type"]); 
                return s; 
            }
        }
    }
    public static Settlement? BuildByName(string fullname){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        Settlement toBuild = new Settlement();
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.SettlementType = (int)pair.Key;
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
            using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM settlements WHERE id = @p1)", conn) 
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
        if (obj.GetType() != typeof(Settlement)){
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
