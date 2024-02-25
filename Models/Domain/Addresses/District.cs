using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class District : IAddressRecord
{
    private const int _addressLevel = 2;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"округ"),
        new Regex(@"район")
    };
    public static readonly IReadOnlyDictionary<DistrictTypes, NameFormatting> Names = new Dictionary<DistrictTypes, NameFormatting>(){
        {DistrictTypes.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.AFTER)},
        {DistrictTypes.CityTerritory, new NameFormatting("г.о.", "Городской округ", NameFormatting.AFTER)},
        {DistrictTypes.MunicipalDistrict, new NameFormatting("м.р-н", "Муниципальный район", NameFormatting.AFTER)},
        {DistrictTypes.MunicipalTerritory, new NameFormatting("м.о.", "Муниципальный округ", NameFormatting.AFTER)},
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
    private string _untypedName;
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
    public string UntypedName
    {
        get => Utils.FormatToponymName(_untypedName);
    }

    public string LongTypedName
    {
        get => Names[_districtType].FormatLong(UntypedName);
    }


    private District()
    {
        _id = Utils.INVALID_ID;
    }
    private District(int id){
        _id = id;
    }

    public static Result<District?> Create(string addressPart, FederalSubject scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня указано неверно"));
        }

        NameToken? foundDistrict = null;
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
        var fromDb = AddressModel.FindRecords(scope.Id, foundDistrict.Name, (int)subjectType, _addressLevel);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<District>.Failure(new ValidationError(nameof(District), "Муниципальное образование верхнего уровня не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<District?>.Success(new District(first.AddressPartId){
                    _parentFederalSubject = scope, 
                    _districtType = (DistrictTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new District(){
            _parentFederalSubject = scope,
            _districtType = subjectType,
            _untypedName = foundDistrict.Name,
        };
        return Result<District?>.Success(got);
    }

    

    public async Task Save(ObservableTransaction? scope = null)
    {
        await _parentFederalSubject.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope); 
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        else
        {
            if (obj.GetType() != typeof(District))
            {
                return false;
            }
            else
            {
                var right = (District)obj;
                return _id == right._id;
            }
        }
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            ParentId = _parentFederalSubject.Id,
            AddressLevelCode = _addressLevel,
            AddressName = _untypedName,
            AddressPartId = _id,
            ToponymType = (int)_districtType
        };
    }
}
