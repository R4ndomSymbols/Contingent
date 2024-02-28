using System.ComponentModel;
using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;
public class Building : IAddressPart
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
    public static Result<Building?> Create(string addressPart, Street parent, ObservableTransaction? searchScope = null){
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
        var fromDb = AddressModel.FindRecords(parent.Id, foundBuilding.Name, (int)buildingType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<Building?>.Success(new Building(first.AddressPartId){
                    _parentStreet = parent, 
                    _buildingType = (BuildingTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Building(){
            _parentStreet = parent,
            _buildingType = buildingType,
            _untypedName = foundBuilding.Name,
        };
        return Result<Building?>.Success(got);
    }
    public static Building? Create(AddressRecord source, Street parent){
        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new Building(source.AddressPartId){
            _parentStreet = parent,
            _buildingType = (BuildingTypes)source.ToponymType,
            _untypedName = source.AddressName
        };
    }
    public async Task Save(ObservableTransaction? scope = null){
        await _parentStreet.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope);
        }
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
    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id).Result;
        return found.Select(d => Apartment.Create(d, this));    
    }
    public override string ToString(){
        return LongTypedName;
    }
}
