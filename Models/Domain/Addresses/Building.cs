using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class Building : IAddressPart
{
    public const int ADDRESS_LEVEL = 6;

    private static List<Building> _duplicationBuffer;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"дом", RegexOptions.IgnoreCase),
        new Regex(@"д\u002E", RegexOptions.IgnoreCase),
    };
    static Building(){
        _duplicationBuffer = new List<Building>();
    }
    private static IEnumerable<Building> GetDuplicates(Building building){
        return _duplicationBuffer.Where(
            b => b._parentStreet.Equals(building._parentStreet)
            && b._buildingName.Equals(building._buildingName) 
            && b._buildingType == building._buildingType 
        );
    } 

    public static readonly IReadOnlyDictionary<BuildingTypes, AddressNameFormatting> Names = new Dictionary<BuildingTypes, AddressNameFormatting>(){
        {BuildingTypes.Building, new AddressNameFormatting("д.", "Дом", AddressNameFormatting.BEFORE)},
    };
    public enum BuildingTypes
    {
        NotMentioned = -1,
        Building = 1
    }
    private int _id;
    private Street _parentStreet;
    private BuildingTypes _buildingType;
    private AddressNameToken _buildingName;
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
    private Building(int id, Street parent, BuildingTypes type, AddressNameToken name)
    {
        _id = id;
        _buildingName = name;
        _parentStreet = parent;
        _buildingType = type;
    }
    protected Building(Street parent, BuildingTypes type, AddressNameToken name)
    {
        _id = Utils.INVALID_ID;
        _buildingName = name;
        _parentStreet = parent;
        _buildingType = type;
        _duplicationBuffer.Add(this);
    }
    public static Result<Building> Create(string addressPart, Street parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание указано неверно или не указано"));
        }
        AddressNameToken? foundBuilding = null;
        BuildingTypes buildingType = BuildingTypes.NotMentioned;  
        foreach (var pair in Names){
            foundBuilding = pair.Value.ExtractToken(addressPart, Restrictions); 
            if (foundBuilding is not null){
                buildingType = pair.Key;
                break;
            }
        }
        if (foundBuilding is null){
            return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание не распознано"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundBuilding.UnformattedName, (int)buildingType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Building>.Failure(new ValidationError(nameof(Building), "Здание не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<Building>.Success(new Building(
                    first.AddressPartId,
                    parent, 
                    (BuildingTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(BuildingTypes)first.ToponymType])
                ));
            }
        }
        
        var got = new Building(
            parent,
            buildingType,
            foundBuilding
        );
        return Result<Building>.Success(got);
    }
    public static Building? Create(AddressRecord source, Street parent){
        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new Building(source.AddressPartId,
            parent,
            (BuildingTypes)source.ToponymType,
            new AddressNameToken(source.AddressName, Names[(BuildingTypes)source.ToponymType])
        );
    }
    public async Task Save(ObservableTransaction? scope = null){
        await _parentStreet.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope);
        }
        var duplicates = GetDuplicates(this);
        foreach(var d in duplicates){
            d._id = this._id;
        }
        _duplicationBuffer.RemoveAll(d => d._id == this._id);
    }
    
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _buildingName.UnformattedName,
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
        return _buildingName.FormattedName;
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(Building)){
            return false;
        }
        var toCompare = (Building)obj;
        return toCompare._id == this._id;
    }
}
