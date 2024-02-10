using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Apartment : DbValidatedObject
{

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"квартира"),
        new Regex(@"кв\u002E"),
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Apartment, new NameFormatting("кв.", "Квартира", NameFormatting.BEFORE)},
    };
    public enum Types
    {
        NotMentioned = -1,
        Apartment = 1
    }

    private int _id;
    private int _parentBuildingId;
    private Types _apartmentType;
    private string _untypedName;
    public int Id
    {
        get => _id;
    }
    public async Task SetParentBuildingId(int id, ObservableTransaction? scope){
        bool exists = await Building.IsIdExists(id, scope);
        if (PerformValidation(
            () => exists,
            new DbIntegrityValidationError(nameof(ParentBuildingId), "Квартира должна быть размещена в пределах дома")
        )){
            _parentBuildingId = id;
        }
    }
    public int ParentBuildingId
    {
        get => _parentBuildingId;
    }
    public int ApartmentType
    {
        get => (int)_apartmentType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(ApartmentType), "Неверно указан тип квартиры")
            ))
            {
                _apartmentType = (Types)value;
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
                new ValidationError(nameof(UntypedName), "Номер квартиры содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 10),
                    new ValidationError(nameof(UntypedName), "Название квартиры слишком длинное или короткое")))
                {
                    _untypedName = value.ToLower();
                }
            }
        }
    }
    public string LongTypedName
    {
        get => Names[_apartmentType].FormatLong(_untypedName);
    }

    private Apartment(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    protected Apartment() : base()
    {
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(ApartmentType));
        RegisterProperty(nameof(ParentBuildingId));
        _untypedName = "";
        _apartmentType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
    }

    public async Task Save(ObservableTransaction? scope)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        if (await GetCurrentState(scope) != RelationTypes.Pending)
        {
            return;
        }
        NpgsqlCommand? command = null;
        string cmdText =
       "INSERT INTO apartments( " +
                    " building, apartment_number) " +
                    " VALUES (@p1, @p2) RETURNING id";
        if (scope != null)
        {
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, args) => this._id = Utils.INVALID_ID));
        }
        else
        {
            command = new NpgsqlCommand(cmdText, conn);
        }
        command.Parameters.Add(new NpgsqlParameter<int>("p1", _parentBuildingId));
        command.Parameters.Add(new NpgsqlParameter<string>("p2", _untypedName));
        await using (command)
        {   
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
    }
    // public static List<Apartment>? GetAllApartmentsIn(int buildingId)
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {
    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM apartments WHERE building = @p1", conn)
    //         {
    //             Parameters = {
    //                 new NpgsqlParameter("p1", buildingId)
    //             }
    //         })
    //         {
    //             var reader = cmd.ExecuteReader();
    //             if (!reader.HasRows)
    //             {
    //                 return null;
    //             }
    //             else
    //             {
    //                 var result = new List<Apartment>();
    //                 while (reader.Read())
    //                 {
    //                     var apt = new Apartment((int)reader["id"]);
    //                     apt._parentBuildingId = buildingId; 
    //                     apt._untypedName = (string)reader["full_name"];
    //                     result.Add(apt);
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<Apartment?> GetById(int aId, ObservableTransaction? scope)
    {
            NpgsqlConnection conn =  await Utils.GetAndOpenConnectionFactory();
            string query = "SELECT * FROM apartments WHERE id = @p1";
            NpgsqlCommand cmd;
            if (scope == null){
                cmd = new NpgsqlCommand(query, conn);
            }
            else{
                cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
            }
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", aId));

            using(conn)
            using (cmd)
            {   
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return null;
                }
                await reader.ReadAsync();
                var apt = new Apartment((int)reader["id"]);
                apt._untypedName = (string)reader["apartment_number"];
                apt._parentBuildingId =  (int)reader["building"];
                apt._apartmentType = Types.Apartment; 
                return apt;
            }
    }
    public static Apartment? BuildByName(string? fullname)
    {
        if (string.IsNullOrEmpty(fullname))
        {
            return null;
        }
        NameToken? extracted;
        Apartment toBuild = new Apartment();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.ApartmentType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                return toBuild;
            }
        }
        return toBuild;
    }

    // private async Task<bool> IsIdExists()
    // {
    //     await using (var conn = await Utils.GetAndOpenConnectionFactory())
    //     {
    //         await using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM apartments WHERE id = @p1)", conn)
    //         {
    //             Parameters = {
    //                 new("p1", _id),
    //             }
    //         })
    //         {
    //             var reader =  await cmd.ExecuteReaderAsync();
    //             await reader.ReadAsync();
    //             return (bool)reader["exists"];
    //         }
    //     }
    // }

    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        Apartment? got = await GetById(_id, scope);
        if (got == null)
        {
            await using (var conn = await Utils.GetAndOpenConnectionFactory())
            {
                string cmdText = "SELECT id FROM apartments WHERE building = @p1 AND apartment_number = @p2";
                NpgsqlCommand cmd;
                if (scope != null){
                    cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
                }
                else{
                    cmd = new NpgsqlCommand(cmdText, conn);
                }
                
                cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _parentBuildingId));
                cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _untypedName));
                await using (cmd)
                {   
                    scope?.Capture();
                    using var reader = await cmd.ExecuteReaderAsync();
                    scope?.Release();
                    if (!reader.HasRows)
                    {
                        return null;
                    }
                    await reader.ReadAsync();
                    _id = (int)reader["id"];
                    return this;
                }
            }
        }
        else{
            return got;
        }
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.GetType() != typeof(Apartment))
        {
            return false;
        }
        var parsed = (Apartment)other;
        return parsed._id == _id && parsed._parentBuildingId == _parentBuildingId
            && parsed._apartmentType == _apartmentType
            && parsed._untypedName == _untypedName;

    }
}
