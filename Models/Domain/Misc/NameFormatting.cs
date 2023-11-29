using System.Text.RegularExpressions;
using Utilities;

namespace StudentTracking.Models.Domain.Misc;

public class NameFormatting {

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
    public NameFormatting(string shortN, string longN, bool outPositioning){
        _shortName = shortN;
        _longName = longN;
        _positioning = outPositioning;
        _tokenOcurrences = new List<Regex>();
    }

    public NameToken? ExtractToken(string? tokenContainer){
       if (string.IsNullOrEmpty(tokenContainer)){
            return null;
       }
       if (string.IsNullOrWhiteSpace(tokenContainer)){
            return null;
       }
       else{
            string token = tokenContainer.Trim();
            int shortFound = token.IndexOf(_shortName, StringComparison.OrdinalIgnoreCase);
            if (shortFound != -1){
                if (shortFound == 0 || shortFound == token.Length - _shortName.Length){
                    return new NameToken(token.Remove(shortFound, _shortName.Length).Trim(), _shortName);
                }
                else{
                    return null;
                }
            }
            int longFound = token.IndexOf(_longName, StringComparison.OrdinalIgnoreCase);
            if (longFound != -1){
                if (longFound == 0 || longFound == token.Length - _longName.Length){
                    return new NameToken(token.Remove(longFound, _longName.Length).Trim(), _longName);
                }
                else{
                    return null;
                }
            }
            return null;
       }   
    }


    public string FormatLong(string toponym){
        if (_positioning == BEFORE){
            return _longName + " " + Utils.FormatToponymName(toponym);
        }
        if (_positioning == AFTER){
            return Utils.FormatToponymName(toponym) + " " + _longName;
        }
        return string.Empty;
    }
     public string FormatShort(string toponym){
        if (_positioning == BEFORE){
            return _shortName + " " + Utils.FormatToponymName(toponym);
        }
        if (_positioning == AFTER){
            return Utils.FormatToponymName(toponym) + " " + _shortName;
        }
        return string.Empty;
    }


}
