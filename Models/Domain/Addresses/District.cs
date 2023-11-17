using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class District : InDbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"округ"),
        new Regex(@"район")
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.AFTER)},
        {Types.CityTerritory, new NameFormatting("г.о", "Городской округ", NameFormatting.AFTER)},
        {Types.MunicipalDistrict, new NameFormatting("м. р-н", "Муниципальный район", NameFormatting.AFTER)},
        {Types.MunicipalTerritory, new NameFormatting("м.о", "Муниципальный округ", NameFormatting.AFTER)},
    };
    public enum Types
    {
        NotMentioned = -1,
        CityTerritory = 1, // городской округ
        MunicipalDistrict = 2, // муниципальный район
        MunicipalTerritory = 3, // муниципальный округ 

    }

    private int _id;
    private int _federalSubjectCode;
    private FederalSubject? _parentFederalSubject;
    private string _fullName;
    private Types _districtType;
    public int Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public int SubjectParentId
    {
        get => _federalSubjectCode;
        set
        {
            _federalSubjectCode = value;
        }
    }
    public int DistrictType
    {
        get => (int)_districtType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(this, nameof(DistrictType), "Неверно указан тип района")
            ))
            {
                _districtType = (Types)value;
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
                new ValidationError(this,nameof(UntypedName), "Название субъекта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError(this,nameof(UntypedName), "Название превышает допустимый лимит символов")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(this,nameof(UntypedName), "Название содержит недопустимые символы")))
                    {
                        _fullName = value;
                    }
                }
            }
        }
    }
    public FederalSubject? Parent {
        get {
            return _parentFederalSubject;
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
                }, new DbIntegrityValidationError(this, nameof(Parent), "Неверно указан субъект федерации"))){
                _parentFederalSubject = value;
            }
        }
    } 
    public string LongTypedName {
        get => Names[_districtType].FormatLong(_fullName);
    }


    protected District(int id, int parentCode, string name, Types type) : this(parentCode)
    {
        _id = id;
        _fullName = name;
        _districtType = type;
    }
    protected District(FederalSubject? parent){

        Parent = parent;
        _fullName = "";
        _districtType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        _validationErrors = new List<ValidationError>(); 
    }
    protected District(int parentCode){

        _federalSubjectCode = parentCode;
        _fullName = "";
        _districtType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
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
            using (var cmd = new NpgsqlCommand("INSERT INTO districts( " + 
	            " federal_subject_code, district_type, full_name) " +
	            " VALUES (@p1, @p2, @p3) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _federalSubjectCode),
                        new("p2", _districtType),
                        new("p3", _fullName),
                    }
            })
            {
                var reader = cmd.ExecuteReader();
                _id = (int)reader["id"];
            }
        }
    }
    public static List<District>? GetAllDistrictsWithin(int federalCode){
        return GetAllDistrictsWithin(federalCode, null, null);
    }
    
    protected static List<District>? GetAllDistrictsWithin(int federalCode, string? untypedName = null, Types? districtType = null)
    {

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            NpgsqlCommand cmd;  
            if (untypedName!=null && districtType != null){
                cmd = new NpgsqlCommand("SELECT * FROM districts WHERE federal_subject_code = @p1 AND full_name = @p2 AND district_type = @p3", conn){
                    Parameters = {
                        new ("p1", federalCode),
                        new ("p2", untypedName),
                        new ("p1", (int)districtType),
                    }
                };
            }
            else {
                cmd = new NpgsqlCommand("SELECT * FROM districts WHERE federal_subject_code = @p1", conn)
                {
                    Parameters = {
                    new ("p1", federalCode)
                    }
                };
            }
            using (cmd)
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var result = new List<District>();
                    while (reader.Read())
                    {
                        result.Add(new District((int)reader["id"], federalCode, (string)reader["full_name"], (Types)(int)reader["district_type"]));
                    }
                    return result;
                }
            }
        }
    }
    public static District? GetById(int dId){
        if (dId == Utils.INVALID_ID){
            return null;
        }
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM districts WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", dId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                return new District((int)reader["id"], (int)reader["federal_subject_code"], (string)reader["full_name"], (Types)(int)reader["district_type"]);
            }
        }
    }
    public static District? BuildByName(string? fullname, FederalSubject parent){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        District toBuild = new District(parent); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.DistrictType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            } 
        }
        return null;
    }

    public override bool Equals(object? obj){
        if (obj is null){
            return false;
        }
        else{
            if (obj.GetType() != typeof(District)){
                return false;
            }
            else{
                var right = (District)obj;
                return _districtType == right._districtType &&
                _fullName == right._fullName &&
                _federalSubjectCode == right._federalSubjectCode &&
                _id == right._id;
            }        
        }
    }
}
