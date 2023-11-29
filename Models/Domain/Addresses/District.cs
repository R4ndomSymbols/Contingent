using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class District : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"округ"),
        new Regex(@"район")
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.AFTER)},
        {Types.CityTerritory, new NameFormatting("г.о.", "Городской округ", NameFormatting.AFTER)},
        {Types.MunicipalDistrict, new NameFormatting("м.р-н", "Муниципальный район", NameFormatting.AFTER)},
        {Types.MunicipalTerritory, new NameFormatting("м.о.", "Муниципальный округ", NameFormatting.AFTER)},
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
    private string _untypedName;
    private Types _districtType;
    public int Id
    {
       get => _id;
    }

    public async Task SetSubjectParent(int id, ObservableTransaction? scope){
        bool exists = await FederalSubject.IsCodeExists(id, scope);
        if (PerformValidation(() => exists,
            new DbIntegrityValidationError(nameof(SubjectParentId), "Id субъекта указан неверно"))){
                _federalSubjectCode = id;
            }
    }
    public int SubjectParentId
    {
        get => _federalSubjectCode;
    }
    public int DistrictType
    {
        get => (int)_districtType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(DistrictType), "Неверно указан тип района")
            ))
            {
                _districtType = (Types)value;
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
                new ValidationError(nameof(UntypedName), "Название субъекта содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 2, 200),
                    new ValidationError(nameof(UntypedName), "Название превышает допустимый лимит символов")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(nameof(UntypedName), "Название содержит недопустимые символы")))
                    {
                        _untypedName = value.ToLower();
                    }
                }
            }
        }
    }

    public string LongTypedName
    {
        get => Names[_districtType].FormatLong(UntypedName);
    }


    private District(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    protected District() : base()
    {
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(DistrictType));
        RegisterProperty(nameof(SubjectParentId));
        _untypedName = "";
        _districtType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
    }
    public static District MakeUnsafe(int id, string untypedName, int type)
    {
        var dist = new District
        {
            _id = id,
            _untypedName = untypedName,
            _districtType = (Types)type
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
        "INSERT INTO districts( " +
        " federal_subject_code, district_type, full_name) " +
        " VALUES (@p1, @p2, @p3) RETURNING id";
        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, args) => this._id = Utils.INVALID_ID));
        }
        else
        {
            command = new NpgsqlCommand(cmdText, connWithin);
        }
        command.Parameters.Add(new NpgsqlParameter<int>("p1", _federalSubjectCode));
        command.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_districtType));
        command.Parameters.Add(new NpgsqlParameter<string>("p3", _untypedName));
        await using (connWithin)
        await using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();      
        }
    }
    //public static List<District>? GetAllDistrictsWithin(int federalCode)
    //{
    //    return GetAllDistrictsWithin(federalCode, null, null);
    //}

    // protected static List<District>? GetAllDistrictsWithin(int federalCode, string? untypedName = null, Types? districtType = null)
    // {

    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {
    //         NpgsqlCommand cmd;
    //         if (untypedName != null && districtType != null)
    //         {
    //             cmd = new NpgsqlCommand("SELECT * FROM districts WHERE federal_subject_code = @p1 AND full_name = @p2 AND district_type = @p3", conn)
    //             {
    //                 Parameters = {
    //                     new ("p1", federalCode),
    //                     new ("p2", untypedName),
    //                     new ("p1", (int)districtType),
    //                 }
    //             };
    //         }
    //         else
    //         {
    //             cmd = new NpgsqlCommand("SELECT * FROM districts WHERE federal_subject_code = @p1", conn)
    //             {
    //                 Parameters = {
    //                 new ("p1", federalCode)
    //                 }
    //             };
    //         }
    //         using (cmd)
    //         {
    //             var reader = cmd.ExecuteReader();
    //             if (!reader.HasRows)
    //             {
    //                 return null;
    //             }
    //             else
    //             {
    //                 var result = new List<District>();
    //                 while (reader.Read())
    //                 {
    //                     //result.Add(new District((int)reader["id"], federalCode, (string)reader["full_name"], (Types)(int)reader["district_type"]));
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<District?> GetById(int dId, ObservableTransaction? scope)
    {
        NpgsqlConnection conn =  await Utils.GetAndOpenConnectionFactory();
        string query = "SELECT * FROM districts WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope == null)
        {
            cmd = new NpgsqlCommand(query, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", dId));
        await using (conn)
        await using (cmd)
        {   
            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            await reader.ReadAsync();
            var d = new District((int)reader["id"])
            {
                _federalSubjectCode = (int)reader["federal_subject_code"],
                _untypedName = (string)reader["full_name"],
                _districtType = (Types)(int)reader["district_type"]
            };
            return d;
        }
        
    }
    public static District? BuildByName(string? fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        District toBuild = new District();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.DistrictType = (int)pair.Key;
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
            var cmdText = "SELECT EXISTS(SELECT id FROM districts WHERE id = @p1)";
            NpgsqlCommand cmd;
            if (scope == null){
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            else{
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }

            await using (cmd)
            {
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? within = null)
    {
        District? got = await GetById(_id, within); 
        if (got == null)
        {
            await using (var conn = await Utils.GetAndOpenConnectionFactory())
            {

                string cmdText = "SELECT id FROM districts WHERE federal_subject_code = @p1 AND district_type = @p2 AND full_name = @p3";
                NpgsqlCommand? cmd = null;
                if (within != null){
                    cmd = new NpgsqlCommand(cmdText, within.Connection, within.Transaction);
                }
                else{
                    cmd = new NpgsqlCommand(cmdText, conn);
                }
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _federalSubjectCode));
                cmd.Parameters.Add(new NpgsqlParameter<int>("p2", (int)_districtType));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p3", _untypedName));
                await using (cmd)
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!reader.HasRows)
                    {
                        return null;
                    }
                    await reader.ReadAsync();
                    this._id = (int)reader["id"];
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
        if (obj is null)
        {
            return false;
        }
        else
        {
            if (obj.GetType() != typeof(District))
            {
                return false;
            }
            else
            {
                var right = (District)obj;
                return _districtType == right._districtType &&
                _untypedName == right._untypedName &&
                _federalSubjectCode == right._federalSubjectCode &&
                _id == right._id;
            }
        }
    }
}
