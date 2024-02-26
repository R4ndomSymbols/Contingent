using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql;
using StudentTracking.Models.Domain.Misc;
using Utilities;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Address;


public class FederalSubject : IAddressPart
{
    public const int ADDRESS_LEVEL = 1;
    private static readonly IReadOnlyList<Regex> Restrictions = new List<Regex>(){
        new Regex(@"республик(а|и)"),
        new Regex(@"(федеральн|город)"),
        new Regex(@"кра(й|я)"),
        new Regex(@"округ(и|а)"),
        new Regex(@"област(ь|и)")
    };

    private int _id;
    private string _subjectUntypedName;
    private FederalSubjectTypes _federalSubjectType;

    public int Id {
        get => _id;
    }
    public int SubjectType
    {
        get => (int)_federalSubjectType;
    }
    public string UntypedName
    {
        get => Utils.FormatToponymName(_subjectUntypedName);
    }
    public string LongTypedName
    {
        get
        {
            return Names[_federalSubjectType].FormatLong(UntypedName);
        }
    }

    private FederalSubject()
    {
        _id = Utils.INVALID_ID;
    }
    private FederalSubject(int id){
        _id = id;
    }

    public enum FederalSubjectTypes
    {
        NotMentioned = -1,
        Republic = 1,
        FederalCity = 2,
        Edge = 3, // край
        Autonomy = 4, // автономная область
        AutomomyDistrict = 5, // автономный округ
        Region = 6, // область
    }

    public static readonly IReadOnlyDictionary<FederalSubjectTypes, NameFormatting> Names = new Dictionary<FederalSubjectTypes, NameFormatting>(){
        {FederalSubjectTypes.NotMentioned, new NameFormatting("Нет", "Не указано", NameFormatting.BEFORE)},
        {FederalSubjectTypes.Republic, new NameFormatting("респ.", "Республика", NameFormatting.BEFORE)},
        {FederalSubjectTypes.FederalCity, new NameFormatting("г.ф.з.", "Город федерального значения", NameFormatting.BEFORE)},
        {FederalSubjectTypes.Edge, new NameFormatting("край", "Край", NameFormatting.BEFORE)},
        {FederalSubjectTypes.Autonomy, new NameFormatting("а.обл.", "Автономная область", NameFormatting.BEFORE)},
        {FederalSubjectTypes.AutomomyDistrict, new NameFormatting("а.окр", "Автономный округ", NameFormatting.BEFORE)},
        {FederalSubjectTypes.Region, new NameFormatting("обл.", "Область", NameFormatting.BEFORE)},
    };
    // кода у субъекта не будет
    // метод одновременно ищет и в базе адресов
    public static Result<FederalSubject?> Create(string addressPart){
        IEnumerable<ValidationError> errors = new List<ValidationError>();
        if (string.IsNullOrEmpty(addressPart) || addressPart.Contains(',')){
            return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации указан неверно"));
        }

        NameToken? found = null;
        FederalSubjectTypes subjectType = FederalSubjectTypes.NotMentioned;  
        foreach (var pair in Names){
            found = pair.Value.ExtractToken(addressPart); 
            if (found is not null){
                subjectType = pair.Key;
                break;
            }
        }
        if (found is null){
            return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации не распознан"));
        }
        var fromDb = AddressModel.FindRecords(found.Name, (int)subjectType, ADDRESS_LEVEL);
        
        if (fromDb.Any()){
            if (fromDb.Count() != 1){
                return Result<FederalSubject>.Failure(new ValidationError(nameof(FederalSubject), "Субъект федерации не может быть однозначно распознан"));
            }
            else{
                var first = fromDb.First();
                return Result<FederalSubject?>.Success(new FederalSubject(first.AddressPartId){
                    _federalSubjectType = (FederalSubjectTypes)first.ToponymType,
                    _subjectUntypedName = first.AddressName
                });
            }
        }
        
        var got = new FederalSubject(){
            _federalSubjectType = subjectType,
            _subjectUntypedName = found.Name,
        };
        return Result<FederalSubject?>.Success(got);
    }

    public static FederalSubject? Create(AddressRecord source){
        if (source.AddressLevelCode != ADDRESS_LEVEL){
            return null;
        }
        return new FederalSubject(source.AddressPartId){
            _federalSubjectType = (FederalSubjectTypes)source.ToponymType,
            _subjectUntypedName = source.AddressName
        };
    } 

    public async Task Save(ObservableTransaction? transaction = null){
        if (_id == Utils.INVALID_ID){
            _id = await AddressModel.SaveRecord(this, transaction);
        }
        
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj.GetType() != typeof(FederalSubject))
        {
            return false;
        }
        var unboxed = (FederalSubject)obj;
        return 
        _id== unboxed._id;
    }

    public AddressRecord ToAddressRecord()
    {
        return new AddressRecord(){
            ParentId = null,
            AddressPartId = _id,
            AddressLevelCode = ADDRESS_LEVEL,
            AddressName =  _subjectUntypedName,
            ToponymType = (int)_federalSubjectType
        };
    }

    public IEnumerable<IAddressPart> GetDescendants()
    {
        var found = AddressModel.FindRecords(_id);
        return found.Select(rec => District.Create(rec, this));
    }

    public override string ToString()
    {
        return LongTypedName;
    }
}