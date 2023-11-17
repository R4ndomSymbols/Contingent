using System.Text.RegularExpressions;
using Npgsql;
using Utilities;
using StudentTracking.Models.Domain.Misc;
namespace StudentTracking.Models.Domain.Address;

public class Street : InDbValidatedObject
{

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
    public static readonly IReadOnlyDictionary<Types, NameFormatting> Names = new Dictionary<Types, NameFormatting>(){
        {Types.NotMentioned, new NameFormatting("нет", "Не указано", NameFormatting.BEFORE)},
        {Types.Street, new NameFormatting("ул.", "Улица", NameFormatting.BEFORE)},
        {Types.Embankment, new NameFormatting("наб.", "Набережная", NameFormatting.BEFORE)},
        {Types.Avenue, new NameFormatting("пр-кт", "Проспект", NameFormatting.BEFORE)},
        {Types.DeadEnd, new NameFormatting("туп.", "Тупик", NameFormatting.BEFORE)},
        {Types.Alley, new NameFormatting("ал.", "Аллея", NameFormatting.BEFORE)},
        {Types.Square, new NameFormatting("пл.", "Площадь", NameFormatting.BEFORE)},
        {Types.Passage, new NameFormatting("пр-д", "Проезд", NameFormatting.BEFORE)},
        {Types.Highway, new NameFormatting("ш.", "Шоссе", NameFormatting.BEFORE)},
    };
    public enum Types
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
    private int _parentSettlementId;
    private string _untypedName;
    private Settlement? _parentSettlement;
    private Types _streetType;
    public int Id
    {
        get => _id;
        set
        {
            _id = value;
        }
    }
    public int SettlementParentId
    {
        get => _parentSettlementId;
        set
        {
            _parentSettlementId = value;
        }
    }
    public int StreetType
    {
        get => (int)_streetType;
        set
        {
            if (PerformValidation(
                () => Enum.TryParse(typeof(Types), value.ToString(), out object? res),
                new ValidationError(this,nameof(StreetType), "Неверно указан тип улицы")
            ))
            {
                _streetType = (Types)value;
            }

        }
    }
    public string UntypedName
    {
        get => _untypedName;
        set
        {
            if (PerformValidation(
                () => !ValidatorCollection.CheckStringPatterns(value, Restrictions),
                new ValidationError(this, nameof(UntypedName), "Название улицы содержит недопустимые слова")))
            {
                if (PerformValidation(
                    () => ValidatorCollection.CheckStringLength(value, 1, 200),
                    new ValidationError(this, nameof(UntypedName), "Название улицы вне допустимых пределов длины")))
                {
                    if (PerformValidation(
                        () => ValidatorCollection.CheckStringPattern(value, ValidatorCollection.OnlyRussianText),
                        new ValidationError(this, nameof(UntypedName), "Название улицы содержит недопустимые символы")))
                    {
                        _untypedName = value;
                    }
                }
            }
        }
    }


    protected Street(int id, int parentId, string name, Types type) : this()
    {
        _id = id;
        _parentSettlementId = parentId;
        _untypedName = name;
        _streetType = type;
    }
    protected Street(){
        _id = Utils.INVALID_ID;
        _untypedName = "";
        _streetType = Types.NotMentioned;
        _validationErrors = new List<ValidationError>();
        _parentSettlementId = Utils.INVALID_ID;
        _parentSettlement = null;
        _dbRelation = RelationTypes.UnboundInvalid;
    }
    public void Save()
    {
        if (CheckErrorsExist())
        {
            return;
        }
        UpdateObjectIntegrityState(GetById(_id));
        if (CurrentState != RelationTypes.Pending){
            return;
        }

        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand("INSERT INTO streets( " +
	            " settlement, street_type, full_name) " +
	            " VALUES (@p1, @p2, @p3); RETURNING id", conn) 
            {
                Parameters = {
                        new("p1", _parentSettlementId),
                        new("p2", _streetType),
                        new("p3", _untypedName),
                    }
            })
            {
                var reader = cmd.ExecuteReader();
                _id = (int)reader["id"];
                _dbRelation = RelationTypes.Bound;
            }
        }
    }
    public static List<Street>? GetAllStreetWithin(int settlementId)
    {
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM streets WHERE settlement = @p1", conn)
            {
                Parameters = {
                    new NpgsqlParameter("p1", settlementId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                else
                {
                    var result = new List<Street>();
                    while (reader.Read())
                    {
                        result.Add(new Street((int)reader["id"], settlementId, (string)reader["full_name"], (Types)reader["street_type"]));
                    }
                    return result;
                }
            }
        }
    }
    public static Street? GetById(int aId){
        using (var conn = Utils.GetConnectionFactory())
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand($"SELECT * FROM streets WHERE id = @p1", conn){
                Parameters = {
                    new ("p1", aId)
                }
            })
            {
                var reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    return null;
                }
                return new Street(
                        id: (int)reader["id"],
                        name: (string)reader["full_name"],
                        parentId: (int)reader["settlement"],
                        type: (Types)(int)reader["street_type"]);
            }
        }
    }
    public static Street? BuildByName(string? fullname, Settlement parent){
        if (fullname == null){
            return null;
        }
        NameToken? extracted;
        Street toBuild = new Street(); 
        foreach (var pair in Names){
            extracted = pair.Value.ExtractToken(fullname);
            if (extracted != null){
                toBuild.StreetType = (int)pair.Key;
                toBuild.UntypedName = extracted.Name;
                toBuild._parentSettlement = parent;
                return toBuild;
            } 
        }
        return null;
    }
}
