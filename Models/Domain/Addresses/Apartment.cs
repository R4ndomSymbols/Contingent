using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
namespace StudentTracking.Models.Domain.Address;

public class Apartment : ValidatedObject<Apartment>
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
    private int _buildingId;
    private Building? _parentBuiding;
    private Types _apartmentType;
    private string _untypedName;
    public int Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public int BuildingParentId
    {
        get => _buildingId;
        set
        {
            _buildingId = value;
        }
    }
    public int ApartmentType
    {
        get => (int)_apartmentType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError<Apartment>(nameof(ApartmentType), "Неверно указан тип квартиры")
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
                new ValidationError<Apartment>(nameof(UntypedName), "Номер квартиры содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 10),
                    new ValidationError<Apartment>(nameof(UntypedName), "Название квартиры слишком длинное или короткое")))
                {
                    _untypedName = value;
                }
            }
        }
    }
    public string LongTypedName {
        get => Names[_apartmentType].FormatLong(_untypedName); 
    }

    protected Apartment(int id, int parentId, string name)
    {
        _id = id;
        _buildingId = parentId;
        _untypedName = name;
        _validationErrors = new List<ValidationError<Apartment>>();
    }
    protected Apartment(){
        _untypedName = "";
        _apartmentType = Types.NotMentioned;
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
            using (var cmd = new NpgsqlCommand("INSERT INTO apartments( " +
	                " building, apartment_number) " +
	                " VALUES (@p1, @p2) RETURNING id", conn) 
            {
                Parameters = {
                        new("p1", _buildingId),
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
                    AddError(new ValidationError<Apartment>(nameof(BuildingParentId), "Неверно указан родитель"));
                }
            }
        }
    }
    public static List<Apartment>? GetAllApartmentsIn(int buildingId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM apartments WHERE building = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", buildingId)
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
                    var result = new List<Apartment>();
                    while (reader.Read())
                    {
                        result.Add(new Apartment((int)reader["id"], buildingId, (string)reader["full_name"]));
                    }
                    return result;
                }
            }
        }
    }
    public static Apartment? GetById(int aId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM apartments WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", aId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                return new Apartment(
                        id: (int)reader["id"],
                        name: (string)reader["apartmentNumber"],
                        parentId: (int)reader["building"]);
            }
        }
    }
    public static Apartment? BuildByName(string? fullname, Building parent){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        Apartment toBuild = new Apartment(); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.ApartmentType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                toBuild._parentBuiding = parent;
                return toBuild;
            } 
        }
        return null;
    }
}
