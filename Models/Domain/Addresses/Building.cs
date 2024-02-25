using System.ComponentModel;
using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Building : IAddressRecord
{
    public const int ADDRESS_LEVEL = 6;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"дом"),
        new Regex(@"д\u002E"),
    };
    public static readonly IReadOnlyDictionary<BuildingTypes, NameFormatting> Names = new Dictionary<BuildingTypes, NameFormatting>(){
        {BuildingTypes.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {BuildingTypes.Building, new NameFormatting("д.", "Дом", NameFormatting.BEFORE)},
    };
    public enum BuildingTypes
    {
        NotMentioned = -1,
        Building = 1
    }

    private int _id;
    private Street _parentStreet;
    private BuildingTypes _buildingType;
    private string _untypedName;
    public int Id
    {
        get => _id;
    }

    public Street StreetParentId
    {
        get => _parentStreet;
    }
    public int BuildingType
    {
        get => (int)_buildingType;
    }
    public string UntypedName
    {
        get => _untypedName;
    }
    public string LongTypedName
    {
        get
        {
            return Names[_buildingType].FormatLong(_untypedName);
        }
    }

    private Building(int id)
    {
        _id = id;
    }
    protected Building()
    {
       _id = Utils.INVALID_ID;
    }
    public static Result<Building?> Create(string addressPart, Street scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание указано неверно или не указано"));
        }

        NameToken? foundBuilding = null;
        BuildingTypes buildingType = BuildingTypes.NotMentioned;  
        foreach (var pair in Names){
            foundBuilding = pair.Value.ExtractToken(addressPart); 
            if (foundBuilding is not null){
                buildingType = pair.Key;
                break;
            }
        }
        if (foundBuilding is null){
            return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание не распознано"));
        }
        var fromDb = AddressModel.FindRecords(scope.Id, foundBuilding.Name, (int)buildingType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<Building?>.Success(new Building(first.AddressPartId){
                    _parentStreet = scope, 
                    _buildingType = (BuildingTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Building(){
            _parentStreet = scope,
            _buildingType = buildingType,
            _untypedName = foundBuilding.Name,
        };
        return Result<Building?>.Success(got);
    }

    public async Task Save(ObservableTransaction? scope = null){
        await _parentStreet.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope);
        }
    }
    
    public override bool Equals(object? other)
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
        return parsed._id == _id;

    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _untypedName,
            ToponymType = (int)_buildingType,
            ParentId = _parentStreet.Id
        };
    }
}
