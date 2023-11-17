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
        set
        {
            _id = value;
        }
    }
    public int ParentBuildingId
    {
        get => _parentBuildingId;
        set
        {
            _parentBuildingId = value;
        }
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
                    _untypedName = value;
                }
            }
        }
    }
    public string LongTypedName {
        get => Names[_apartmentType].FormatLong(_untypedName); 
    }

    protected Apartment(int id, int parentId, string name) : this()
    {
        _id = id;
        _parentBuildingId = parentId;
        _untypedName = name;
    }
    protected Apartment() : base() {
        _untypedName = "";
        _apartmentType = Types.NotMentioned;
        _id = Utils.INVALID_ID;
    }
    public void Save()
    {
        if (IsIdExists()){
            return;
        }

        if (CurrentState != RelationTypes.Pending || !Building.IsIdExists(_parentBuildingId)){
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
                        new("p1", _parentBuildingId),
                        new("p2", _untypedName),
                    }
            })
            {
                var reader = cmd.ExecuteReader();
                _id = (int)reader["id"];
                SetBound();
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
                        var apt = new Apartment((int)reader["id"], buildingId, (string)reader["full_name"]);
                        apt.SetBound();
                        result.Add(apt);
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
                var apt = new Apartment(
                        id: (int)reader["id"],
                        name: (string)reader["apartmentNumber"],
                        parentId: (int)reader["building"]);
                apt.SetBound();
                return apt;
            }
        }
    }
    public static Apartment? BuildByName(string? fullname){
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
                return toBuild;
            } 
        }
        return null;
    }

    private bool IsIdExists(){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT id FROM apartments WHERE id = @p1)", conn) 
            {
                Parameters = {
                    new("p1", _id),
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
        if (other == null){
            return false;
        }
        if (other.GetType() != typeof(Apartment)){
            return false;
        }
        var parsed = (Apartment)other;
        return parsed._id == _id && parsed._parentBuildingId == _parentBuildingId 
            && parsed._apartmentType == _apartmentType 
            && parsed._untypedName == _untypedName;  
        
    }
}
