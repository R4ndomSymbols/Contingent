using System.Text.RegularExpressions;
using Contingent.Utilities;
namespace Contingent.Models.Domain.Address;
public class Street : IAddressPart
{
    public const int ADDRESS_LEVEL = 5;

    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"улица", RegexOptions.IgnoreCase),
        new Regex(@"набережная", RegexOptions.IgnoreCase),
        new Regex(@"проспект",RegexOptions.IgnoreCase),
        new Regex(@"тупик",RegexOptions.IgnoreCase),
        new Regex(@"аллея", RegexOptions.IgnoreCase),
        new Regex(@"площадь", RegexOptions.IgnoreCase),
        new Regex(@"проезд",RegexOptions.IgnoreCase),
        new Regex(@"шоссе",RegexOptions.IgnoreCase),
    };

    public static readonly IReadOnlyDictionary<StreetTypes, AddressNameFormatting> Names = new Dictionary<StreetTypes, AddressNameFormatting>(){
        {StreetTypes.Street, new AddressNameFormatting("ул.", "Улица", AddressNameFormatting.BEFORE)},
        {StreetTypes.Embankment, new AddressNameFormatting("наб.", "Набережная", AddressNameFormatting.BEFORE)},
        {StreetTypes.Avenue, new AddressNameFormatting("пр-кт", "Проспект", AddressNameFormatting.BEFORE)},
        {StreetTypes.DeadEnd, new AddressNameFormatting("туп.", "Тупик", AddressNameFormatting.BEFORE)},
        {StreetTypes.Alley, new AddressNameFormatting("ал.", "Аллея", AddressNameFormatting.BEFORE)},
        {StreetTypes.Square, new AddressNameFormatting("пл.", "Площадь", AddressNameFormatting.BEFORE)},
        {StreetTypes.Passage, new AddressNameFormatting("пр-д", "Проезд", AddressNameFormatting.BEFORE)},
        {StreetTypes.Highway, new AddressNameFormatting("ш.", "Шоссе", AddressNameFormatting.BEFORE)},
    };
    public enum StreetTypes
    {
        NotMentioned = -1,
        Street = 1, // улица
        Embankment = 2, // набережная
        Avenue = 3, // проспект 
        DeadEnd = 4, // тупик
        Alley = 5, // аллея
        Square = 6, // площадь
        Passage = 7, // проезд 
        Highway = 8, // шоссе 
    }
    private int _id;
    private Settlement _parentSettlement;
    private AddressNameToken _streetName;
    private StreetTypes _streetType;
    public int Id
    {
        get => _id;
    }
    public Settlement SettlementParentId
    {
        get => _parentSettlement;
    }
    private Street(int id, Settlement parent, StreetTypes type, AddressNameToken name)
    {
        _id = id;
        _parentSettlement = parent;
        _streetType = type;
        _streetName = name;
    }
    private Street(Settlement parent, StreetTypes type, AddressNameToken name)
    {
        _id = Utils.INVALID_ID;
        _parentSettlement = parent;
        _streetType = type;
        _streetName = name;
    }
    public static Result<Street> Create(string addressPart, Settlement parent, ObservableTransaction? searchScope = null)
    {
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(','))
        {
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не указан или указан неверно"));
        }
        AddressNameToken? foundStreet = null;
        StreetTypes streetType = StreetTypes.NotMentioned;
        foreach (var pair in Names)
        {
            foundStreet = pair.Value.ExtractToken(addressPart, Restrictions);
            if (foundStreet is not null)
            {
                streetType = pair.Key;
                break;
            }
        }
        if (foundStreet is null)
        {
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не распознан"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundStreet, (int)streetType, ADDRESS_LEVEL, searchScope).Result;

        if (fromDb.Any())
        {
            if (fromDb.Count() != 1)
            {
                return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не может быть однозначно распознан"));
            }
            else
            {
                var first = fromDb.First();
                return Result<Street>.Success(new Street(first.AddressPartId,
                    parent,
                    (StreetTypes)first.ToponymType,
                    new AddressNameToken(first.AddressName, Names[(StreetTypes)first.ToponymType])
                ));
            }
        }

        var got = new Street(
             parent,
             streetType,
             foundStreet
        );
        return Result<Street>.Success(got);
    }
    public static Street Create(AddressRecord source, Settlement parent)
    {
        return new Street(source.AddressPartId,
            parent,
            (StreetTypes)source.ToponymType,
            new AddressNameToken(source.AddressName, Names[(StreetTypes)source.ToponymType])
        );
    }
    public async Task Save(ObservableTransaction? scope = null)
    {
        await _parentSettlement.Save(scope);
        if (!Utils.IsValidId(_id))
        {
            var desc = await AddressModel.FindRecords(
               _parentSettlement.Id,
               _streetName,
               (int)_streetType,
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
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord()
        {
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _streetName.UnformattedName,
            ToponymType = (int)_streetType,
            ParentId = _parentSettlement.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants(ObservableTransaction? scope)
    {
        var found = AddressModel.FindRecords(_id, scope).Result;
        return found.Select(rec => Building.Create(rec, this)!);
    }
    public override string ToString()
    {
        return _streetName.FormattedName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(Street))
        {
            return false;
        }
        var toCompare = (Street)obj;
        return toCompare._id == this._id;
    }


}
