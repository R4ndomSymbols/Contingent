using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Contingent.Models.Domain.Address;

public class AddressNameFormatting
{

    public const bool AFTER = false;
    public const bool BEFORE = true;
    private string _shortTypeName;
    private string _longTypeName;
    private bool _positioning;
    private List<Regex> _tokenOcurrences;

    public string ShortName
    {
        get => _shortTypeName;
    }
    public string LongName
    {
        get => _longTypeName;
    }
    public AddressNameFormatting(string shortN, string longN, bool outPositioning)
    {
        _shortTypeName = shortN;
        _longTypeName = longN;
        _positioning = outPositioning;
        _tokenOcurrences = new List<Regex>();
    }

    public AddressNameToken? ExtractToken(string? tokenContainer, IReadOnlyCollection<Regex> restrictions)
    {
        if (string.IsNullOrEmpty(tokenContainer))
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(tokenContainer))
        {
            return null;
        }
        else
        {
            string token = tokenContainer.Trim();
            var ext = Find(token, _longTypeName) ?? Find(token, _shortTypeName);
            return ext;

            AddressNameToken? Find(string token, string toFind)
            {
                try
                {
                    int found = token.IndexOf(toFind, StringComparison.OrdinalIgnoreCase);
                    if (found != -1)
                    {
                        if (found == 0 || found == token.Length - toFind.Length)
                        {
                            var extracted = token.Remove(found, toFind.Length);
                            if (restrictions.Any(
                                t => t.IsMatch(extracted)
                            ))
                            {
                                return null;
                            }
                            return new AddressNameToken(extracted, this);
                        }
                    }
                }
                catch { }
                return null;
            }

        }
    }
    // нормализацию напишу потом, необходимо проверять токен на предмет наличия запрещенных токенов
    // еще, конечно, было бы прекрасно фильтровать извлеченный токен на наличие запрещенных сокращений
    // в зависимости от типа

    public string FormatLong(string toponym)
    {
        if (_positioning == BEFORE)
        {
            return _longTypeName + " " + toponym;
        }
        if (_positioning == AFTER)
        {
            return toponym + " " + _longTypeName;
        }
        return string.Empty;
    }
    public string FormatShort(string toponym)
    {
        if (_positioning == BEFORE)
        {
            return _shortTypeName + " " + toponym;
        }
        if (_positioning == AFTER)
        {
            return toponym + " " + _shortTypeName;
        }
        return string.Empty;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(AddressNameFormatting))
        {
            return false;
        }
        var toCompare = (AddressNameFormatting)obj;
        return
            toCompare._longTypeName == this._longTypeName &&
            toCompare._shortTypeName == this._shortTypeName &&
            toCompare._positioning == this._positioning;
    }


}
