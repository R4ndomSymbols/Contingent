using System.Text.RegularExpressions;

namespace StudentTracking.Models.Domain.Address;

public class AddressNameFormatting {

    public const bool AFTER = false;
    public const bool BEFORE = true;
    private string _shortName;
    private string _longName;
    private bool _positioning;
    private List<Regex> _tokenOcurrences;

    public string ShortName {
        get => _shortName;
    } 
    public string LongName {
        get => _longName;
    }
    public AddressNameFormatting(string shortN, string longN, bool outPositioning){
        _shortName = shortN;
        _longName = longN;
        _positioning = outPositioning;
        _tokenOcurrences = new List<Regex>();
    }

    public AddressNameToken? ExtractToken(string? tokenContainer){
       if (string.IsNullOrEmpty(tokenContainer)){
            return null;
       }
       if (string.IsNullOrWhiteSpace(tokenContainer)){
            return null;
       }
       else{
            try{
                string token = tokenContainer.Trim();
                int shortFound = token.IndexOf(_shortName, StringComparison.OrdinalIgnoreCase);
                if (shortFound != -1){
                    if (shortFound == 0 || shortFound == token.Length - _shortName.Length){
                        return new AddressNameToken(token.Remove(shortFound, _shortName.Length), this);
                    }
                    else{
                        return null;
                    }
                }
                int longFound = token.IndexOf(_longName, StringComparison.OrdinalIgnoreCase);
                if (longFound != -1){
                    if (longFound == 0 || longFound == token.Length - _longName.Length){
                        return new AddressNameToken(token.Remove(longFound, _longName.Length), this);
                    }
                    else{
                        return null;
                    }
                }
                return null;
            }
            catch {
                return null;
            }
       }   
    }
    // нормализацию напишу потом, необходимо проверять токен на предмет наличия запрещенных токенов
    // еще, конечно, было бы прекрасно фильтровать извлеченный токен на наличие запрещенных сокращений
    // в зависимости от типа

    public string FormatLong(string toponym){
        if (_positioning == BEFORE){
            return _longName + " " + toponym;
        }
        if (_positioning == AFTER){
            return toponym + " " + _longName;
        }
        return string.Empty;
    }
     public string FormatShort(string toponym){
        if (_positioning == BEFORE){
            return _shortName + " " + toponym;
        }
        if (_positioning == AFTER){
            return toponym + " " + _shortName;
        }
        return string.Empty;
    }


}
