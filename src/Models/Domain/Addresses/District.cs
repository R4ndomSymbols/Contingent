using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Contingent.Utilities;
namespace Contingent.Models.Domain.Address;
public class District : IAddressPart
{
    public const int ADDRESS_LEVEL = 2;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"округ", RegexOptions.IgnoreCase),
        new Regex(@"район", RegexOptions.IgnoreCase)
    };
    public static readonly IReadOnlyDictionary<DistrictTypes, AddressNameFormatting> Names = new Dictionary<DistrictTypes, AddressNameFormatting>(){
        {DistrictTypes.CityTerritory, new AddressNameFormatting("г.о.", "Городской округ", AddressNameFormatting.AFTER)},
        {DistrictTypes.MunicipalDistrict, new AddressNameFormatting("м.р-н", "Муниципальный район", AddressNameFormatting.AFTER)},
        {DistrictTypes.MunicipalTerritory, new AddressNameFormatting("м.о.", "Муниципальный округ", AddressNameFormatting.AFTER)},
    };
    public enum DistrictTypes
    {
        NotMentioned = -1,
        CityTerritory = 1, // городской округ
        MunicipalDistrict = 2, // муниципальный район
        MunicipalTerritory = 3, // муниципальный округ 
    }
    private int _id;
    private FederalSubject _parentFederalSubject;
    private AddressNameToken _districtName;
    private DistrictTypes _districtType;
    public int Id
    {
        get => _id;
    }
    public FederalSubject SubjectParentId
    {
        get => _parentFederalSubject;
    }
    public DistrictTypes DistrictType
    {
        get => _districtType;
    }
    private District(FederalSubject parent, DistrictTypes type, AddressNameToken name)
    {
        _id = Utils.INVALID_ID;
        _districtName = name;
        _parentFederalSubject = parent;
        _districtType = type;
    }
    private District(int id, FederalSubject parent, DistrictTypes type, AddressNameToken name)
    {
        _id = id;
        _districtName = name;
        _parentFederalSubject = parent;
        _districtType = type;
    }
    public static Result<District> Create(string addressPart, FederalSubject parent, ObservableTransaction? searchScope = null)
    {
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(','))
        {
            return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня указано неверно"));
        }
        AddressNameToken? foundDistrict = null;
        DistrictTypes subjectType = DistrictTypes.NotMentioned;
        foreach (var pair in Names)
        {
            foundDistrict = pair.Value.ExtractToken(addressPart, Restrictions);
            if (foundDistrict is not null)
            {
                subjectType = pair.Key;
                break;
            }
        }
        if (foundDistrict is null)
        {
            return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня не распознано"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundDistrict, (int)subjectType, ADDRESS_LEVEL, searchScope).Result;

        if (fromDb.Any())
        {
            if (fromDb.Count() != 1)
            {
                return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня не может быть однозначно распознано"));
            }
            else
            {
                var first = fromDb.First();
                return Result<District>.Success(new District(first.AddressPartId,
                     parent,
                     (DistrictTypes)first.ToponymType,
                     new AddressNameToken(first.AddressName, Names[(DistrictTypes)first.ToponymType])
                ));
            }
        }

        var got = new District(
            parent,
            subjectType,
            foundDistrict
        );
        return Result<District>.Success(got);
    }
    public static District? Create(AddressRecord source, FederalSubject parent)
    {

        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null)
        {
            return null;
        }
        return new District(source.AddressPartId,
            parent,
            (DistrictTypes)source.ToponymType,
            new AddressNameToken(source.AddressName, Names[(DistrictTypes)source.ToponymType])
        );
    }


    public async Task Save(ObservableTransaction? scope)
    {
        await _parentFederalSubject.Save(scope);
        if (!Utils.IsValidId(_id))
        {
            var desc = await AddressModel.FindRecords(
                    _parentFederalSubject.Id,
                    _districtName,
                    (int)_districtType,
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
            ParentId = _parentFederalSubject.Id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _districtName.UnformattedName,
            AddressPartId = _id,
            ToponymType = (int)_districtType
        };
    }
    public IEnumerable<IAddressPart> GetDescendants(ObservableTransaction? scope)
    {
        var foundUntyped = AddressModel.FindRecords(_id, scope).Result;
        return
        foundUntyped.Where(rec => rec.AddressLevelCode == Settlement.ADDRESS_LEVEL)
        .Select(rec => Settlement.Create(rec, this)!)
        .Concat(foundUntyped.Where(rec => rec.AddressLevelCode == SettlementArea.ADDRESS_LEVEL).Select(rec => (IAddressPart)SettlementArea.Create(rec, this)!));
    }

    public override string ToString()
    {
        return _districtName.FormattedName;
    }
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(District))
        {
            return false;
        }
        var toCompare = (District)obj;
        return toCompare._id == this._id;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
