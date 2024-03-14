using System.Text.RegularExpressions;
using Utilities;
namespace StudentTracking.Models.Domain.Address;
public class SettlementArea : IAddressPart
{
    public const int ADDRESS_LEVEL = 3;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"поселение"),
    };
    public static readonly IReadOnlyDictionary<SettlementAreaTypes, AddressNameFormatting> Names = new Dictionary<SettlementAreaTypes, AddressNameFormatting>(){
        {SettlementAreaTypes.NotMentioned, new AddressNameFormatting("нет", "Не указано", AddressNameFormatting.BEFORE)},
        {SettlementAreaTypes.CitySettlement, new AddressNameFormatting("г.п.", "Городское поселение", AddressNameFormatting.BEFORE)},
        {SettlementAreaTypes.CountysideDistrict, new AddressNameFormatting("с.п.", "Сельское поселение", AddressNameFormatting.BEFORE)},
    };
    public enum SettlementAreaTypes
    {
        NotMentioned = -1,
        CitySettlement = 1, // городской округ
        CountysideDistrict = 2, // муниципальный район
    }
    private int _id;
    private District _parentDistrict;
    private AddressNameToken _settlementAreaName;
    private SettlementAreaTypes _settlementAreaType;
    public int Id
    {
        get => _id;
    }
    public District DistrictParentId
    {
        get => _parentDistrict;
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
        AddressNameToken? foundSettlementArea = null;
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
        var fromDb = AddressModel.FindRecords(parent.Id, foundSettlementArea.UnformattedName, (int)settlementAreaType, ADDRESS_LEVEL, searchScope).Result;
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<SettlementArea>.Failure(new ValidationError(nameof(SettlementArea), "Поселение не может быть однозначно распознано"));
            }
            else{
                var first = fromDb.First();
                return Result<SettlementArea?>.Success(new SettlementArea(first.AddressPartId){
                    _parentDistrict = parent, 
                    _settlementAreaType = (SettlementAreaTypes)first.ToponymType,
                    _settlementAreaName = new AddressNameToken(first.AddressName, Names[(SettlementAreaTypes)first.ToponymType]),
                });
            }
        }
        
        var got = new SettlementArea(){
            _parentDistrict = parent,
            _settlementAreaType = settlementAreaType,
            _settlementAreaName = foundSettlementArea,
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
            _settlementAreaName = new AddressNameToken(source.AddressName, Names[(SettlementAreaTypes)source.ToponymType]),
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
            AddressName = _settlementAreaName.UnformattedName,
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
        return _settlementAreaName.FormattedName;
    }
}
