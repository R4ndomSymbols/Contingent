using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class Apartment : IAddressPart
{
    public const int ADDRESS_LEVEL = 7;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"квартира"),
        new Regex(@"кв\u002E"),
    };
    public static readonly IReadOnlyDictionary<ApartmentTypes, AddressNameFormatting> Names = new Dictionary<ApartmentTypes, AddressNameFormatting>(){
        {ApartmentTypes.NotMentioned, new AddressNameFormatting("нет", "Не указано", AddressNameFormatting.BEFORE)},
        {ApartmentTypes.Apartment, new AddressNameFormatting("кв.", "Квартира", AddressNameFormatting.BEFORE)},
    };
    public enum ApartmentTypes
    {
        NotMentioned = -1,
        Apartment = 1
    }
    private int _id;
    private Building _parentBuilding;
    private ApartmentTypes _apartmentType;
    private AddressNameToken _apartmentName;
    public int Id
    {
        get => _id;
    }
    public Building ParentBuildingId
    {
        get => _parentBuilding;
    }
    public int ApartmentType
    {
        get => (int)_apartmentType;
    }

    private Apartment(int id)
    {
        _id = id;
    }
    protected Apartment()
    {
        _id = Utils.INVALID_ID;
    }
    public static Result<Apartment?> Create(string addressPart, Building parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира указана неверно или не указана"));
        }
        AddressNameToken? foundApartment = null;
        ApartmentTypes apartmentType = ApartmentTypes.NotMentioned;  
        foreach (var pair in Names){
            foundApartment = pair.Value.ExtractToken(addressPart); 
            if (foundApartment is not null){
                apartmentType = pair.Key;
                break;
            }
        }
        if (foundApartment is null){
            return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира не распознана"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundApartment.UnformattedName, (int)apartmentType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира не может быть однозначно распознана"));
            }
            else{
                var first = fromDb.First();
                return Result<Apartment?>.Success(new Apartment(first.AddressPartId){
                    _parentBuilding = parent, 
                    _apartmentType = (ApartmentTypes)first.ToponymType,
                    _apartmentName = new AddressNameToken(first.AddressName, Names[(ApartmentTypes)first.ToponymType])
                });
            }
        }
        
        var got = new Apartment(){
            _parentBuilding = parent,
            _apartmentType = apartmentType,
            _apartmentName = foundApartment,
        };
        return Result<Apartment?>.Success(got);
    }
    public static Apartment? Create(AddressRecord from, Building parent){
        if (from.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        } 
        return new Apartment(from.AddressPartId){
            _apartmentType = (ApartmentTypes)from.ToponymType,
            _parentBuilding = parent,
            _apartmentName = new AddressNameToken(from.AddressName, Names[(ApartmentTypes)from.ToponymType])
        };
    }
    public async Task Save(ObservableTransaction? scope = null)
    {
        // итеративное сохрание всего дерева, чтобы не писать кучу ерунды
        await _parentBuilding.Save(scope);
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
        if (other.GetType() != typeof(Apartment))
        {
            return false;
        }
        var parsed = (Apartment)other;
        return parsed._id == _id;
    }
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _apartmentName.UnformattedName,
            ToponymType = (int)_apartmentType,
            ParentId = _parentBuilding.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        return new List<IAddressPart>();
    }
    public override string ToString()
    {
        return _apartmentName.FormattedName;
    }
}
