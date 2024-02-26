using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class Apartment : IAddressPart
{
    public const int ADDRESS_LEVEL = 7;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"квартира"),
        new Regex(@"кв\u002E"),
    };
    public static readonly IReadOnlyDictionary<ApartmentTypes, NameFormatting> Names = new Dictionary<ApartmentTypes, NameFormatting>(){
        {ApartmentTypes.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {ApartmentTypes.Apartment, new NameFormatting("кв.", "Квартира", NameFormatting.BEFORE)},
    };
    public enum ApartmentTypes
    {
        NotMentioned = -1,
        Apartment = 1
    }

    private int _id;
    private Building _parentBuilding;
    private ApartmentTypes _apartmentType;
    private string _untypedName;
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
    public string UntypedName
    {
        get => _untypedName;
    }
    public string LongTypedName
    {
        get => Names[_apartmentType].FormatLong(_untypedName);
    }

    private Apartment(int id)
    {
        _id = id;
    }
    protected Apartment()
    {
        _id = Utils.INVALID_ID;
    }
    public static Result<Apartment?> Create(string addressPart, Building scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира указана неверно или не указана"));
        }
        NameToken? foundApartment = null;
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
        var fromDb = AddressModel.FindRecords(scope.Id, foundApartment.Name, (int)apartmentType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира не может быть однозначно распознана"));
            }
            else{
                var first = fromDb.First();
                return Result<Apartment?>.Success(new Apartment(first.AddressPartId){
                    _parentBuilding = scope, 
                    _apartmentType = (ApartmentTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Apartment(){
            _parentBuilding = scope,
            _apartmentType = apartmentType,
            _untypedName = foundApartment.Name,
        };
        return Result<Apartment?>.Success(got);
    }

    public static Apartment Create(AddressRecord from, Building parent){
        return new Apartment(from.AddressPartId){
            _apartmentType = (ApartmentTypes)from.ToponymType,
            _parentBuilding = parent,
            _untypedName = from.AddressName
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
            AddressName = _untypedName,
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
        return LongTypedName;
    }
}
