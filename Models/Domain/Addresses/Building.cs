using System.ComponentModel;
using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Building : DbValidatedObject
{
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"дом"),
        new Regex(@"д\u002E"),
    };
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Building, new NameFormatting("д.", "Дом", NameFormatting.BEFORE)},
    };
    public enum Types
    {
        NotMentioned = -1,
        Building = 1
    }

    private int _id;
    private int _parentStreetId;
    private Types _buildingType;
    private string _untypedName;
    public int Id
    {
        get => _id;
    }

    public async Task SetParentStreet(int id, ObservableTransaction? scope){
        bool exists = await Street.IsIdExists(id, scope);
        if (PerformValidation(
            () => exists,
            new DbIntegrityValidationError(nameof(StreetParentId), "ID улицы должен быть указан")
        )){
            _parentStreetId = id;
        }
    }
    public int StreetParentId
    {
        get => _parentStreetId;
    }
    public int BuildingType
    {
        get => (int)_buildingType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(nameof(BuildingType), "Неверно указан тип дома")
            ))
            {
                _buildingType = (Types)value;
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
                new ValidationError(nameof(UntypedName), "Название дома содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 10),
                    new ValidationError(nameof(UntypedName), "Название дома вне допустимых пределов длины")))
                {
                    _untypedName = value.ToLower();
                }
            }
        }
    }
    public string LongTypedName
    {
        get
        {
            return Names[_buildingType].FormatLong(_untypedName);
        }
    }

    private Building(int id) : base(RelationTypes.Bound)
    {
        _id = id;
        _untypedName = "";
    }
    protected Building() : base()
    {
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(BuildingType));
        RegisterProperty(nameof(StreetParentId));
        _untypedName = "";
        _buildingType = Types.NotMentioned;
    }

    public async Task Save(ObservableTransaction? scope){
        var conn = await Utils.GetAndOpenConnectionFactory();
        if (await GetCurrentState(scope) != RelationTypes.Pending){
            return;
        }
        NpgsqlCommand? command = null;
        string cmdText = 
        "INSERT INTO buildings( " +
                    " street, full_name) " +
                    " VALUES (@p1, @p2) RETURNING id";
        if (scope!=null){
            command = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            scope.OnRollbackSubscribe(new EventHandler((obj, args) => this._id = Utils.INVALID_ID));
        }
        else{
            command = new NpgsqlCommand(cmdText, conn);
        }
        command.Parameters.Add(new NpgsqlParameter<int>("p1", _parentStreetId));
        command.Parameters.Add(new NpgsqlParameter<string> ("p2", _untypedName));
        await using (conn)
        await using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _id = (int)reader["id"];
            NotifyStateChanged();
        }
    }
    
    // public static List<Building>? GetAllBuildingsOn(int streetId)
    // {
    //     using (var conn = Utils.GetAndOpenConnectionFactory())
    //     {
    //         using (var cmd = new NpgsqlCommand($"SELECT * FROM buildings WHERE street = @p1", conn)
    //         {
    //             Parameters = {
    //                 new NpgsqlParameter("p1", streetId)
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
    //                 var result = new List<Building>();
    //                 while (reader.Read())
    //                 {
    //                     var b = new Building((int)reader["id"]);
    //                     b._parentStreetId = streetId;
    //                     b._untypedName = (string)reader["full_name"];
    //                     result.Add(b);
    //                 }
    //                 return result;
    //             }
    //         }
    //     }
    // }
    public static async Task<Building?> GetById(int bId, ObservableTransaction? scope)
    {
        NpgsqlConnection conn = await Utils.GetAndOpenConnectionFactory();
        string query = "SELECT * FROM buildings WHERE id = @p1";
        NpgsqlCommand cmd;
        if (scope == null){
            cmd = new NpgsqlCommand(query, conn);
        }
        else{
            cmd = new NpgsqlCommand(query, scope.Connection, scope.Transaction);
        }
        cmd.Parameters.Add(new NpgsqlParameter<int>("p1", bId));
        using (conn)
        using (cmd)
        {
            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            if (!reader.HasRows)
            {
                return null;
            }
            var b = new Building((int)reader["id"])
            {
                _untypedName = (string)reader["full_name"],
                _parentStreetId = (int)reader["street"],
                _buildingType = Types.Building,
            };
            return b;
        }
       
    }
    public static Building? BuildByName(string? fullname)
    {
        if (fullname == null)
        {
            return null;
        }
        NameToken? extracted;
        Building toBuild = new Building();
        foreach (var pair in Names)
        {
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null)
            {
                toBuild.BuildingType = (int)pair.Key;
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
            string cmdText = "SELECT EXISTS(SELECT id FROM buildings WHERE id = @p1)";
            NpgsqlCommand? cmd = null;
            if (scope!=null){
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else{
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", id));
            await using (cmd)
            {   
                using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();
                return (bool)reader["exists"];
            }
        }
    }
    public override async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {
        Building? got = await GetById(_id, scope);
        if (got == null)
        {
            if (await BindId(scope)){
                return this;
            }
            else{
                return null;
            }
        }
        else{
            return got;
        }
    }

    private async Task<bool> BindId(ObservableTransaction? scope){
        await using (var conn = await Utils.GetAndOpenConnectionFactory())
        {
            string cmdText = "SELECT id FROM buildings WHERE street = @p1 AND full_name = @p2";
            NpgsqlCommand? cmd;
            if (scope != null){
                cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
            }
            else{
                cmd = new NpgsqlCommand(cmdText, conn);
            }
            cmd.Parameters.Add(new NpgsqlParameter<int>("p1", _parentStreetId));
            cmd.Parameters.Add(new NpgsqlParameter<string>("p2", _untypedName));
            using (cmd)
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return false;
                }
                await reader.ReadAsync();
                _id = (int)reader["id"];
                NotifyStateChanged();
                return true;
            }
        }
    }

    public override bool Equals(IDbObjectValidated? other)
    {
        if (other == null)
        {
            return false;
        }
        if (other.GetType() != typeof(Building))
        {
            return false;
        }
        var parsed = (Building)other;
        return parsed._id == _id 
            && parsed._parentStreetId == _parentStreetId
            && parsed._buildingType == _buildingType
            && parsed._untypedName == _untypedName;

    }

}
