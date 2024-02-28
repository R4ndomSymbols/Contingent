using System.Text.RegularExpressions;
using System.Xml;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;
namespace StudentTracking.Models.Domain.Address;
public class SettlementArea : IAddressPart
{
    public const int ADDRESS_LEVEL = 3;
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
    public static Result<SettlementArea?> Create(string addressPart, District parent, ObservableTransaction? searchScope = null){
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
        var fromDb = AddressModel.FindRecords(parent.Id, foundSettlementArea.Name, (int)settlementAreaType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<SettlementArea?>.Success(new SettlementArea(first.AddressPartId){
                    _parentDistrict = parent, 
                    _settlementAreaType = (SettlementAreaTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new SettlementArea(){
            _parentDistrict = parent,
            _settlementAreaType = settlementAreaType,
            _untypedName = foundSettlementArea.Name,
        };
        return Result<SettlementArea?>.Success(got);
    }
    public static SettlementArea? Create(AddressRecord source, District parent){
        if (source.AddressLevelCode != ADDRESS_LEVEL || parent is null){
            return null;
        }
        return new SettlementArea(source.AddressPartId){
            _parentDistrict = parent,
            _settlementAreaType = (SettlementAreaTypes)source.ToponymType,
            _untypedName = source.AddressName
        };
    }
    public async Task Save(ObservableTransaction? scope = null)
    {   await _parentDistrict.Save(scope);
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
            ToponymType = (int)_settlementAreaType,
            ParentId = _parentDistrict.Id
        };
    }
    public IEnumerable<IAddressPart> GetDescendants()
    {
        IEnumerable<AddressRecord> foundUntyped = AddressModel.FindRecords(_id).Result;
        return foundUntyped.Select(rec => Settlement.Create(rec, this)) ?? new List<Settlement>();
    }
    public override string ToString(){
        return LongTypedName;
    }
}
