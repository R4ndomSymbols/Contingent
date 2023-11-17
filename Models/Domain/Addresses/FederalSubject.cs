using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Address;


public class FederalSubject : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"республик(а|и)"),
        new Regex(@"(федеральн|город)"),
        new Regex(@"кра(й|я)"),
        new Regex(@"округ(и|а)"),
        new Regex(@"област(ь|и)")
    };

    private int _code;
    private string _subjectUntypedName;
    private Types _regionType;
    public string Code
    {
        get => _code.ToString();
        set
        {
            if (PerformValidation(
                () => int.TryParse(value, out int res),
                new ValidationError(nameof(Code), "Код региона не может содержать буквы")
            ))
            {
                var r = int.Parse(value);
                if (PerformValidation(
                    () => ValidatorCollection.CheckRange(r, 0, 300),
                    new ValidationError(nameof(Code), "Код региона не может быть таким")
                ))
                {  
                     _code = r;
                }
            }

        }
    }
    public int FederalSubjectType
    {
        get => (int)_regionType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(FederalSubjectType), "Неверно указан тип субъекта")
            ))
            {
                _regionType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => _subjectUntypedName;
        set
        {
            if (PerformValidation(
                () => !ValidatorCollection.CheckStringPatterns(value, Restrictions),
                new ValidationError(nameof(UntypedName), "Название субъекта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 100),
                    new ValidationError(nameof(UntypedName), "Название превышает допустимый лимит символов")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(nameof(UntypedName), "Название содержит недопустимые символы")))
                    {
                        _subjectUntypedName = value;
                    }
                }
            }
        }
    }
    public string LongTypedName {
        get {
            return Names[_regionType].FormatLong(_subjectUntypedName);
        }
    }

    protected FederalSubject(int code, string name, Types type) : this()
    {   
        _code = code;
        _subjectUntypedName = name;
        _regionType = type;
    }
    protected FederalSubject() : base(){
        _subjectUntypedName = "";
        _code = Utils.INVALID_ID;
    }

    public enum Types
    {
        NotMentioned = -1,
        Republic = 1,
        FederalCity = 2,
        Edge = 3, // край
        Autonomy = 4, // автономная область
        AutomomyDistrict = 5, // автономный округ
        Region = 6, // область
    }

    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Republic, new NameFormatting("респ.", "Республика", NameFormatting.BEFORE)},
        {Types.FederalCity, new NameFormatting("г.ф.з.", "Город федерального значения", NameFormatting.BEFORE)},
        {Types.Edge, new NameFormatting("край", "Край", NameFormatting.BEFORE)},
        {Types.Autonomy, new NameFormatting("а.обл.", "Автономная область", NameFormatting.BEFORE)},
        {Types.AutomomyDistrict, new NameFormatting("а.окр", "Автономный округ", NameFormatting.BEFORE)},
        {Types.Region, new NameFormatting("обл.", "Область", NameFormatting.BEFORE)},
    };

    public bool Save()
    {
        if (CurrentState != RelationTypes.Pending){
            return false;
        }
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand($"INSERT INTO federal_subjects (code,subject_type, full_name) VALUES (@p1,@p2,@p3)", conn)
            {
                Parameters = {
                        new("p1", _code),
                        new("p2", _regionType),
                        new("p3", _subjectUntypedName),
                    }
            })
            {
                var reader = cmd.ExecuteNonQuery();
                SetBound();
                return true;
            }
        }
    }
    public static List<FederalSubject> GetAll()
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM federal_subject", conn))
            {
                var found = new List<FederalSubject>();
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return found;
                }
                while (reader.Read())
                {
                    var f = new FederalSubject(
                        code: (int)reader["code"],
                        name: (string)reader["full_name"],
                        type: (Types)reader["subject_type"]
                    );
                    f.SetBound();
                    found.Add(f);
                }
                return found;
            }
        }
    }
    public static FederalSubject? GetByCode(int code){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT code, subject_type, full_name FROM federal_subjects WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", code)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                var f = new FederalSubject(
                        code: (int)reader["code"],
                        name: (string)reader["full_name"],
                        type: (Types)reader["subject_type"]);
                f.SetBound();
                return f;
            }
        }
    }
    public static FederalSubject? BuildByName(string? fullname){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        FederalSubject toBuild = new FederalSubject();
        string[] parts = fullname.Split(" ");
        if (parts.Length < 2){
            return toBuild;
        }
        else{
            toBuild.Code = parts[0];
        }
        int codeDelimiter = fullname.IndexOf(' ');
        if (fullname.Length-1 == codeDelimiter){
            return toBuild;
        }
        string fullnameWithoutCode = fullname.Substring(codeDelimiter+1); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.FederalSubjectType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            } 
        }
        return null;
    }
    public static bool IsCodeExists(int id){
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
        return GetByCode(_code);
    }
    public override bool Equals(IDbObjectValidated? obj)
    {
        if (obj == null){
            return false;
        }
        if (obj.GetType() != typeof(FederalSubject)){
            return false;
        }
        var unboxed = (FederalSubject)obj;
        return _code == unboxed._code &&
        _subjectUntypedName == unboxed._subjectUntypedName &&
        _regionType == unboxed._regionType;
    }


}