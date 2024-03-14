using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class District : IAddressPart
{
    public const int ADDRESS_LEVEL = 2;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"округ"),
        new Regex(@"район")
    };
    public static readonly IReadOnlyDictionary<DistrictTypes, AddressNameFormatting> Names = new Dictionary<DistrictTypes, AddressNameFormatting>(){
        {DistrictTypes.NotMentioned, new AddressNameFormatting("Нет", "Не указано", AddressNameFormatting.AFTER)},
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
    private District()
    {
        _id = Utils.INVALID_ID;
    }
    private District(int id){
        _id = id;
    }
    public static Result<District?> Create(string addressPart, FederalSubject parent, ObservableTransaction? searchScope = null){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня указано неверно"));
        }
        AddressNameToken? foundDistrict = null;
        DistrictTypes subjectType = DistrictTypes.NotMentioned;  
        foreach (var pair in Names){
            foundDistrict = pair.Value.ExtractToken(addressPart); 
            if (foundDistrict is not null){
                subjectType = pair.Key;
                break;
            }
        }
        if (foundDistrict is null){
            return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня не распознано"));
        }
        var fromDb = AddressModel.FindRecords(parent.Id, foundDistrict.UnformattedName, (int)subjectType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<District?>.Success(new District(first.AddressPartId){
                    _parentFederalSubject = parent, 
                    _districtType = (DistrictTypes)first.ToponymType,
                    _districtName = new AddressNameToken(first.AddressName, Names[(DistrictTypes)first.ToponymType]),
                });
            }
        }
        
        var got = new District(){
            _parentFederalSubject = parent,
            _districtType = subjectType,
            _districtName = foundDistrict,
        };
        return Result<District?>.Success(got);
    }
    public static District? Create(AddressRecord source, FederalSubject parent){
        
        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new District(source.AddressPartId){
            _districtType = (DistrictTypes)source.ToponymType,
            _parentFederalSubject = parent,
            _districtName = new AddressNameToken(source.AddressName, Names[(DistrictTypes)source.ToponymType]),
        };
    }
    
    
    public async Task Save(ObservableTransaction? scope = null)
    {
        await _parentFederalSubject.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope); 
        }
    }
    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            ParentId = _parentFederalSubject.Id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _districtName.UnformattedName,
            AddressPartId = _id,
            ToponymType = (int)_districtType
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        var foundUntyped = AddressModel.FindRecords(_id).Result;
        return foundUntyped.Where(rec => rec.AddressLevelCode == Settlement.ADDRESS_LEVEL).Select(rec => (IAddressPart)Settlement.Create(rec, this))
        .Concat(foundUntyped.Where(rec => rec.AddressLevelCode == SettlementArea.ADDRESS_LEVEL).Select(rec => (IAddressPart)SettlementArea.Create(rec, this)));
    }

    public override string ToString()
    {
        return _districtName.FormattedName;
    }
}
