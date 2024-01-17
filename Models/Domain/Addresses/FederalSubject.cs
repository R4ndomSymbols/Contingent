using System.Text.Json.Serialization;
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
    private Types _federalSubjectType;
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
    public int SubjectType
    {
        get => (int)_federalSubjectType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(SubjectType), "Неверно указан тип региона")
            ))
            {
                _federalSubjectType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_subjectUntypedName);
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
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyText),
                        new ValidationError(nameof(UntypedName), "Название содержит недопустимые символы")))
                    {
                        _subjectUntypedName = value.ToLower();
                    }
                }
            }
        }
    }
    public string LongTypedName
    {
        get
        {
            return Names[_federalSubjectType].FormatLong(UntypedName);
        }
    }
    public string NameWithCode
    {
        get
        {
            return Code + " " + LongTypedName;
        }
    }

    protected FederalSubject(int code) : base(RelationTypes.Bound)
    {
        _code = code;
        _subjectUntypedName = "";
    }
    public FederalSubject() : base()
    {

        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(Code));
        RegisterProperty(nameof(SubjectType));

        _subjectUntypedName = "";
        _federalSubjectType = Types.NotMentioned;
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

    public static FederalSubject MakeUnsafe(int code, string untypedName, int type)
    {
        var fed = new FederalSubject
        {
            _code = code,
            _subjectUntypedName = untypedName,
            _federalSubjectType = (Types)type
        };
        return fed;
    }

    public async Task Save(ObservableTransaction? scope)
    {   
        var connWithin = await Utils.GetAndOpenConnectionFactory();
        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        NpgsqlCommand? command = null;
        string cmdText = "INSERT INTO federal_subjects (code,subject_type, full_name) VALUES (@p1,@p2,@p3)";
        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, args) => this._code = Utils.INVALID_ID));
        }
        else
        {
            command = new NpgsqlCommand(cmdText, connWithin);
        }

        // разобраться с прописными и строчными буквами

        command.Parameters.Add(new NpgsqlParameter<int>("p1", _code));
        command.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_federalSubjectType));
        command.Parameters.Add(new NpgsqlParameter<string>("p3", _subjectUntypedName.ToLower()));

        await using (connWithin)
        await using (command)
        {
            var reader = await command.ExecuteNonQueryAsync();
            NotifyStateChanged();
        }
    }



    // public static List<FederalSubject> GetAll()
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {

    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM federal_subject", conn))
    //         {
    //             var found = new List<FederalSubject>();
    //             var reader = cmd.ExecuteReader();
    //             if (!reader.HasRows)
    //             {
    //                 return found;
    //             }
    //             while (reader.Read())
    //             {
    //                 var f = new FederalSubject((int)reader["code"])
    //                 {
    //                     _subjectUntypedName = (string)reader["full_name"],
    //                     _federalSubjectType = (Types)reader["subject_type"]
    //                 };
    //                 found.Add(f);
    //             }
    //             return found;
    //         }
    //     }
    // }
    public static async Task<FederalSubject?> GetByCode(int code, ObservableTransaction? scope = null)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string query = "SELECT subject_type, full_name FROM federal_subjects WHERE code = @p1";
        NpgsqlCommand cmd;
        if (scope == null)
        {
            cmd = new NpgsqlCommand(query, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", code));

        await using (conn)
        await using (cmd)
        {   

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            var f = new FederalSubject(code);
            f._subjectUntypedName = (string)reader["full_name"];
            f._federalSubjectType = (Types)reader["subject_type"];
            return f;
        }
    }

    public static FederalSubject? BuildByName(string? fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        FederalSubject toBuild = new FederalSubject();
        string[] parts = fullname.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return null;
        }
        else
        {
            toBuild.Code = parts[0];
        }
        int codeDelimiter = fullname.IndexOf(' ');
        if (fullname.Length - 1 == codeDelimiter)
        {
            return null;
        }
        string fullnameWithoutCode = fullname.Substring(codeDelimiter + 1);
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullnameWithoutCode);
            if (extracted != null)
            {
                toBuild.SubjectType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            }
        }
        return null;
    }
    public static async Task<bool> IsCodeExists(int id, ObservableTransaction? scope)
    {
        var conn = await Utils.GetAndOpenConnectionFactory();
        NpgsqlCommand? cmd = null;
        var cmdText = "SELECT EXISTS(SELECT code FROM federal_subjects WHERE code = @p1)";
        if (scope!=null){
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);    
        }
        else {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        using (conn)
        using (cmd)
        {
            
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));

            using var reader = await cmd.ExecuteReaderAsync();
            var t = await reader.ReadAsync();
            return (bool)reader["exists"];
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? within = null)
    {
        return await GetByCode(_code, within);
    }
    public override bool Equals(IDbObjectValidated? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(FederalSubject))
        {
            return false;
        }
        var unboxed = (FederalSubject)obj;
        return 
        _code == unboxed._code &&
        _subjectUntypedName == unboxed._subjectUntypedName &&
        _federalSubjectType == unboxed._federalSubjectType;
    }


}