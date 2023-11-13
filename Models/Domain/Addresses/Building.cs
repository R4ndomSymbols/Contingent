using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class Building : ValidatedObject<Building>
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
    private int _streetId;
    private Types _buildingType;
    private Street? _streetParent;
    private string _untypedName;
    public int Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public int StreetParentId
    {
        get => _streetId;
        set
        {
            _streetId = value;
        }
    }
    public int BuildingType
    {
        get => (int)_buildingType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError<Building>(nameof(BuildingType), "Неверно указан тип дома")
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
                new ValidationError<Building>(nameof(UntypedName), "Название дома содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 10),
                    new ValidationError<Building>(nameof(UntypedName), "Название дома вне допустимых пределов длины")))
                {
                    _untypedName = value;
                }
            }
        }
    }
    public string LongTypedName {
        get => "дом" + " " + _untypedName;
    }

    protected Building(int id, int parentId, string name)
    {
        _id = id;
        _streetId = parentId;
        _untypedName = name;
        _validationErrors = new List<ValidationError<Building>>();
    }
    protected Building(){
        _untypedName = "";
        _buildingType = Types.NotMentioned;
        AddError(new ValidationError<Building>(nameof(UntypedName), "Номер дома должен быть указан"));
        AddError(new ValidationError<Building>(nameof(BuildingType), "Тип дома должен быть указан"));
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
            using (var cmd = new NpgsqlCommand("INSERT INTO buildings( " +
	                " street, full_name) " +
	                " VALUES (@p1, @p2) RETURNING id", conn) 
            {
                Parameters = {
                        new("p1", _streetId),
                        new("p2", _untypedName),
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
                    AddError(new ValidationError<Building>(nameof(StreetParentId), "Неверно указан родитель"));
                }
            }
        }
    }
    public static List<Building>? GetAllBuildingsOn(int streetId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM buildings WHERE street = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", streetId)
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
                    var result = new List<Building>();
                    while (reader.Read())
                    {
                        result.Add(new Building((int)reader["id"], streetId, (string)reader["full_name"]));
                    }
                    return result;
                }
            }
        }
    }
    public static Building? GetById(int bId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM buildings WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", bId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                return new Building(
                        id: (int)reader["id"],
                        name: (string)reader["full_name"],
                        parentId: (int)reader["street"]);
            }
        }
    }
    public static Building? BuildByName(string? fullname, Street parent){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        Building toBuild = new Building(); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.BuildingType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                toBuild._streetParent = parent;
                return toBuild;
            } 
        }
        return null;
    }
}
