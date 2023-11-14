using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class District : InDbValidatedObject<District>
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
                new ValidationError<District>(nameof(DistrictType), "Неверно указан тип района")
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
                new ValidationError<District>(nameof(UntypedName), "Название субъекта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError<District>(nameof(UntypedName), "Название превышает допустимый лимит символов")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError<District>(nameof(UntypedName), "Название содержит недопустимые символы")))
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
                }, new DbIntegrityValidationError<District>(nameof(Parent), "Неверно указан субъект федерации"))){
                _parentFederalSubject = value;
            }
        }
    } 
    public string LongTypedName {
        get => Names[_districtType].FormatLong(_fullName);
    }


    protected District(int id, int parentCode, string name, Types type)
    {
        _id = id;
        _federalSubjectCode = parentCode;
        _fullName = name;
        _districtType = type;
        _validationErrors = new List<ValidationError<District>>(); 
    }
    protected District(FederalSubject? parent){

        Parent = parent;
        _fullName = "";
        _districtType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
        
        AddError(new ValidationError<District>(nameof(UntypedName), "Имя района не может быть пустым"));
        AddError(new ValidationError<District>(nameof(DistrictType), "Тип района должен быть указан"));
    }
    public void Save()
    {
        if (CheckErrorsExist())
        {
            return;
        }
        if (_)
        District? alter = GetAllDistrictsWithin();
        if (alter != null){
            if (alter._districtType != this._districtType){
                AddError(new ValidationError<District>(nameof(DistrictType), "Несовпадение типа муниципального образования с зарегистрированным"));
            }
            if (alter._fullName != this._fullName){
                AddError(new ValidationError<District>(nameof(UntypedName), "Несовпадение названия муниципального образования с зарегистрированным"));
            }
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
                try
                {
                    var reader = cmd.ExecuteReader();
                    _id = (int)reader["id"];
                }
                catch (NpgsqlException)
                {
                    AddError(new ValidationError<District>(nameof(SubjectParentId), "Неверно указан родитель"));
                }
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

    protected override void ValidateDbIntegrity()
    {
        if (_id == Utils.INVALID_ID){
            if (CheckErrorsExist()){
                _dbRelation = RelationTypes.UnboundInvalid;
            }
        }
        else{
            var alter = GetById(_id);
            if (alter == null){
                if (CheckErrorsExist()){
                    _dbRelation = RelationTypes.UnboundInvalid;
                }
                else{
                    _dbRelation = RelationTypes.Pending;
                }
            }
            else if (alter.Parent != ){
                
            }
        }
        if (CheckErrorsExist()){
            _dbRelation = RelationTypes.UnboundInvalid;
            return;
        }
        else {

        }
        PerformValidation()

    }

    public static bool operator == (District? left, District? right){
        if (left is null || right is null ){
            return false;
        }
        else{
            return left._districtType == right._districtType &&
            left._fullName == right._fullName &&
            left._federalSubjectCode == 
        }
    }
}
