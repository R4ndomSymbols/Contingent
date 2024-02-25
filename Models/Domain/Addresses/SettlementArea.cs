using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;

public class SettlementArea : IAddressRecord
{
    private const int _addressLevel = 3;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"поселение"),
    };
    public static readonly IReadOnlyDictionary<SettlementAreaTypes, NameFormatting> Names = new Dictionary<SettlementAreaTypes, NameFormatting>(){
        {SettlementAreaTypes.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {SettlementAreaTypes.CitySettlement, new NameFormatting("г.п.", "Городское поселение", NameFormatting.BEFORE)},
        {SettlementAreaTypes.CountysideDistrict, new NameFormatting("с.п.", "Сельское поселение", NameFormatting.BEFORE)},
    };
    public enum SettlementAreaTypes
    {
        NotMentioned = -1,
        CitySettlement = 1, // городской округ
        CountysideDistrict = 2, // муниципальный район
    }

    private int _id;
    private District _parentDistrict;
    private string _untypedName;
    private SettlementAreaTypes _settlementAreaType;
    public int Id
    {
        get => _id;
    }
    public District DistrictParentId
    {
        get => _parentDistrict;
    }
    public int SettlementAreaType
    {
        get => (int)_settlementAreaType;
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_untypedName);
    }
    public string LongTypedName
    {
        get
        {
            return Names[_settlementAreaType].FormatLong(UntypedName);
        }
    }

    private SettlementArea(int id)
    {
        _id = id;
    }
    
    private SettlementArea()
    {
        _id = Utils.INVALID_ID;
    }   
    // добавить ограничение на создание 
    // исходя из типа родителя
    public static Result<SettlementArea?> Create(string addressPart, District scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение указано неверно"));
        }

        NameToken? foundSettlementArea = null;
        SettlementAreaTypes settlementAreaType = SettlementAreaTypes.NotMentioned;  
        foreach (var pair in Names){
            foundSettlementArea = pair.Value.ExtractToken(addressPart); 
            if (foundSettlementArea is not null){
                settlementAreaType = pair.Key;
                break;
            }
        }
        if (foundSettlementArea is null){
            return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не распознано"));
        }
        var fromDb = AddressModel.FindRecords(scope.Id, foundSettlementArea.Name, (int)settlementAreaType, _addressLevel);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<SettlementArea?>.Success(new SettlementArea(first.AddressPartId){
                    _parentDistrict = scope, 
                    _settlementAreaType = (SettlementAreaTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new SettlementArea(){
            _parentDistrict = scope,
            _settlementAreaType = settlementAreaType,
            _untypedName = foundSettlementArea.Name,
        };
        return Result<SettlementArea?>.Success(got);
    }

    public async Task Save(ObservableTransaction? scope = null)
    {   await _parentDistrict.Save(scope);
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, scope); 
        }
    }
   
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(SettlementArea))
        {
            return false;
        }
        var unboxed = (SettlementArea)obj;
        return _id == unboxed._id;
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = _addressLevel,
            AddressName = _untypedName,
            ToponymType = (int)_settlementAreaType,
            ParentId = _parentDistrict.Id
        };
    }
}
