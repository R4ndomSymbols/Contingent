using System.Text.RegularExpressions;
using Npgsql;
using Utilities;
using StudentTracking.Models.Domain.Misc;
using Utilities.Validation;
using StudentTracking.Models.JSON;
namespace StudentTracking.Models.Domain.Address;

public class Street : IAddressRecord
{
    public const int ADDRESS_LEVEL = 5;  
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"улица"),
        new Regex(@"набережная"),
        new Regex(@"проспект"),
        new Regex(@"тупик"),
        new Regex(@"аллея"),
        new Regex(@"площадь"),
        new Regex(@"проезд"),
        new Regex(@"шоссе"),
    };
    public static readonly IReadOnlyDictionary<StreetTypes, NameFormatting> Names = new Dictionary<StreetTypes, NameFormatting>(){
        {StreetTypes.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {StreetTypes.Street, new NameFormatting("ул.", "Улица", NameFormatting.BEFORE)},
        {StreetTypes.Embankment, new NameFormatting("наб.", "Набережная", NameFormatting.BEFORE)},
        {StreetTypes.Avenue, new NameFormatting("пр-кт", "Проспект", NameFormatting.BEFORE)},
        {StreetTypes.DeadEnd, new NameFormatting("туп.", "Тупик", NameFormatting.BEFORE)},
        {StreetTypes.Alley, new NameFormatting("ал.", "Аллея", NameFormatting.BEFORE)},
        {StreetTypes.Square, new NameFormatting("пл.", "Площадь", NameFormatting.BEFORE)},
        {StreetTypes.Passage, new NameFormatting("пр-д", "Проезд", NameFormatting.BEFORE)},
        {StreetTypes.Highway, new NameFormatting("ш.", "Шоссе", NameFormatting.BEFORE)},
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
    private string _untypedName;
    private StreetTypes _streetType;
    public int Id
    {
        get => _id;
    }
    public Settlement SettlementParentId
    {
        get => _parentSettlement;
    }

    public string UntypedName
    {
        get => Utils.FormatToponymName(_untypedName);
    }
    public string LongTypedName
    {
        get
        {
            return Names[_streetType].FormatLong(UntypedName);
        }
    }


    protected Street(int id)
    {
        _id = id;
    }
    protected Street() : base()
    {
        _id = Utils.INVALID_ID;
    }

    public static Result<Street?> Create(string addressPart, Settlement scope){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не указан или указан неверно"));
        }

        NameToken? foundStreet = null;
        StreetTypes streetType = StreetTypes.NotMentioned;  
        foreach (var pair in Names){
            foundStreet = pair.Value.ExtractToken(addressPart); 
            if (foundStreet is not null){
                streetType = pair.Key;
                break;
            }
        }
        if (foundStreet is null){
            return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не распознан"));
        }
        var fromDb = AddressModel.FindRecords(scope.Id, foundStreet.Name, (int)streetType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<Street>.Failure(new ValidationError(nameof(Street), "Объект дорожной инфраструктуры не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<Street?>.Success(new Street(first.AddressPartId){
                    _parentSettlement = scope, 
                    _streetType = (StreetTypes)first.ToponymType,
                    _untypedName = first.AddressName
                });
            }
        }
        
        var got = new Street(){
            _parentSettlement = scope,
            _streetType = streetType,
            _untypedName = foundStreet.Name,
        };
        return Result<Street?>.Success(got);
    }
    public async Task Save(ObservableTransaction? scope = null)
    {   
        await _parentSettlement.Save(scope);
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
        if (other.GetType() != typeof(Street))
        {
            return false;
        }
        var parsed = (Street)other;
        return parsed._id == _id;
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName = _untypedName,
            ToponymType = (int)_streetType,
            ParentId = _parentSettlement.Id
        };
    }
}
