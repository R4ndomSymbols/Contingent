using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class Settlement : ValidatedObject<Settlement>
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
    private int? _settlementAreaId;
    private int? _districtId;
    private District? _districtParent;
    private SettlementArea? _settlementAreaParent;
    private string _fullName;
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
        get => _settlementAreaId;
        set
        {
            _settlementAreaId = value;
        }
    }
    public int? DistrictParentId
    {
        get => _districtId;
        set
        {
            _districtId = value;
        }
    }
    public int SettlementType
    {
        get => (int)_settlementType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError<Settlement>(nameof(SettlementType), "Неверно указан тип населенного пункта")
            ))
            {
                _settlementType = (Types)value;
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
                new ValidationError<Settlement>(nameof(UntypedName), "Название населенного пункта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError<Settlement>(nameof(UntypedName), "Название населенного пункта вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError<Settlement>(nameof(UntypedName), "Название населенного пункта содержит недопустимые символы")))
                    {
                        _fullName = value;
                    }
                }
            }
        }
    }
    public string LongTypedName {
        get => Names[_settlementType].FormatLong(_fullName);
    }

    protected Settlement(int id, int? parentDistrict, int? parentSettlementArea, string name, Types type)
    {
        _id = id;
        if (
            (parentDistrict == null && parentSettlementArea == null) ||
            (parentDistrict != null && parentSettlementArea != null)
        ){
            throw new ArgumentException("У потомка не может быть двух родителей");
        }

        _settlementAreaId = parentSettlementArea;
        _districtId = parentDistrict;
        _fullName = name;
        _settlementType = type;
        _validationErrors = new List<ValidationError<Settlement>>();
    }

    protected Settlement(SettlementArea? sap, District? dp){
        
        if ((sap == null && dp == null) || (sap != null && dp != null)){
            throw new ArgumentException("Должен быть указан только один родитель");
        }
        _districtParent = dp;
        _settlementAreaParent = sap;

        _fullName = "";
        _settlementType = Types.NotMentioned;
        AddError(new ValidationError<Settlement>(nameof(UntypedName), "Имя населенного пункта не может быть пустым"));
        AddError(new ValidationError<Settlement>(nameof(SettlementType), "Тип населенного пункта должен быть указан"));
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
            using (var cmd = new NpgsqlCommand("INSERT INTO settlements( " +
	            " settlement_area, settlement_type, full_name) " +
	            " VALUES (@p1, @p2, @p3) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _settlementAreaId),
                        new("p2", _settlementType),
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
                    AddError(new ValidationError<Settlement>(nameof(SettlementAreaParentId), "Неверно указан родитель"));
                }
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
                        result.Add(new Settlement((int)reader["id"], parentSettlementArea, parentDistrict, (string)reader["full_name"], (Types)(int)reader["settlement_type"]));
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
                return new Settlement((int)reader["id"], parentDistrict, parentSettlementArea, (string)reader["full_name"], (Types)(int)reader["settlement_area_type"]);
            }
        }
    }
    public static Settlement? BuildByName(string fullname, SettlementArea? sparent, District? dparent){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        Settlement toBuild = new Settlement(sparent, dparent); 
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
    

}
