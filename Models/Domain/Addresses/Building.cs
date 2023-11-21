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
        set
        {
            _id = value;
        }
    }
    public int StreetParentId
    {
        get => _parentStreetId;
        set
        {
            _parentStreetId = value;
        }
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
                    _untypedName = value;
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

    protected Building(int id, int parentId, string name) : this()
    {
        _id = id;
        _parentStreetId = parentId;
        _untypedName = name;
    }
    protected Building() : base()
    {
        RegisterProperty(nameof(UntypedName));
        RegisterProperty(nameof(BuildingType));
        _untypedName = "";
        _buildingType = Types.NotMentioned;
    }

    public bool Save()
    {
        if (CurrentState != RelationTypes.Pending || !Street.IsIdExists(_parentStreetId))
        {
            return false;
        }

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO buildings( " +
                    " street, full_name) " +
                    " VALUES (@p1, @p2) RETURNING id", conn)
            {
                Parameters = {
                        new("p1", _parentStreetId),
                        new("p2", _untypedName),
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
                        var b = new Building((int)reader["id"], streetId, (string)reader["full_name"]);
                        b.SetBound();
                        result.Add(b);
                    }
                    return result;
                }
            }
        }
    }
    public static Building? GetById(int bId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM buildings WHERE id = @p1", conn)
            {
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
                var b = new Building(
                        id: (int)reader["id"],
                        name: (string)reader["full_name"],
                        parentId: (int)reader["street"]);
                b.SetBound();
                return b;
            }
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

    public static bool IsIdExists(int id)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM buildings WHERE id = @p1)", conn)
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
        return parsed._id == _id && parsed._parentStreetId == _parentStreetId
            && parsed._buildingType == _buildingType
            && parsed._untypedName == _untypedName;

    }

}
