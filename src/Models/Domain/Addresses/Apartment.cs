using System.Text.RegularExpressions;
using Contingent.Utilities;
namespace Contingent.Models.Domain.Address;
public class Apartment : IAddressPart
{
    public const int ADDRESS_LEVEL = 7;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"квартира", RegexOptions.IgnoreCase),
        new Regex(@"кв\u002E", RegexOptions.IgnoreCase),
    };
    public static readonly IReadOnlyDictionary<ApartmentTypes, AddressNameFormatting> Names = new Dictionary<ApartmentTypes, AddressNameFormatting>(){
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

    private Apartment(int id, Building parent, ApartmentTypes type, AddressNameToken name)
    {
        _id = id;
        _parentBuilding = parent;
        _apartmentType = type;
        _apartmentName = name;
    }
    private Apartment(Building parent, ApartmentTypes type, AddressNameToken name)
    {
        _id = Utils.INVALID_ID;
        _parentBuilding = parent;
        _apartmentType = type;
        _apartmentName = name;
    }
    public static Result<Apartment> Create(string addressPart, Building parent, ObservableTransaction? searchScope = null)
    {
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(','))
        {
            return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира указана неверно или не указана"));
        }
        AddressNameToken? foundApartment = null;
        ApartmentTypes apartmentType = ApartmentTypes.NotMentioned;
        foreach (var pair in Names)
        {
            foundApartment = pair.Value.ExtractToken(addressPart, Restrictions);
            if (foundApartment is not null)
            {
                apartmentType = pair.Key;
                break;
            }
        }
        if (foundApartment is null)
        {
            return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира не распознана"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundApartment, (int)apartmentType, ADDRESS_LEVEL, searchScope).Result;

        if (fromDb.Any())
        {
            if (fromDb.Count() != 1)
            {
                return Result<Apartment>.Failure(new ValidationError(nameof(Apartment), "Квартира не может быть однозначно распознана"));
            }
            else
            {
                var first = fromDb.First();
                return Result<Apartment>.Success(new Apartment(first.AddressPartId,
                    parent,
                    (ApartmentTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(ApartmentTypes)first.ToponymType])
                ));
            }
        }

        var got = new Apartment(
            parent,
            apartmentType,
            foundApartment
        );
        return Result<Apartment>.Success(got);
    }
    public static Apartment? Create(AddressRecord from, Building? parent)
    {
        if (from.AddressLevelCode != ADDRESS_LEVEL || parent is null)
        {
            return null;
        }
        return new Apartment(from.AddressPartId,
            parent,
            (ApartmentTypes)from.ToponymType,
            new AddressNameToken(from.AddressName, Names[(ApartmentTypes)from.ToponymType])
        );
    }
    public async Task Save(ObservableTransaction? scope)
    {
        await _parentBuilding.Save(scope);
        // итеративное сохранение всего дерева, чтобы не писать кучу ерунды
        if (!Utils.IsValidId(_id))
        {
            var desc = await AddressModel.FindRecords(
                _parentBuilding.Id,
                _apartmentName,
                (int)_apartmentType,
                ADDRESS_LEVEL,
                scope
            );
            if (desc.Any())
            {
                _id = desc.First().AddressPartId;
                return;
            }
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
        return new AddressRecord()
        {
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _apartmentName.UnformattedName,
            ToponymType = (int)_apartmentType,
            ParentId = _parentBuilding.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants(ObservableTransaction? scope)
    {
        return new List<IAddressPart>();
    }
    public override string ToString()
    {
        return _apartmentName.FormattedName;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
