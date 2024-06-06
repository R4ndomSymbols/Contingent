namespace Contingent.Models.Domain.Address;

public class AddressNameToken
{
    public AddressNameFormatting Formatting { get; private set; }
    private string _normalizedName;
    public string FormattedName
    {
        get => Formatting.FormatLong(NormalizeTo(_normalizedName));
        private set
        {
            _normalizedName = NormalizeFrom(value);
        }
    }
    public string UnformattedName
    {
        get => _normalizedName;
    }

    public AddressNameToken(string nameOnly, AddressNameFormatting formatting)
    {
        _normalizedName = "";
        if (formatting is null)
        {
            throw new Exception("Форматирование должно быть определено");
        }
        FormattedName = nameOnly;
        Formatting = formatting;
    }

    private string NormalizeTo(string name)
    {
        var split = name.Split(' ');
        for (int i = 0; i < split.Length; i++)
        {
            split[i] = char.ToUpper(split[i][0]).ToString() + split[i][1..];
        }
        return string.Join(" ", split);
    }

    private string NormalizeFrom(string name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            throw new Exception("Нормализация невозможна");
        }
        string[] split = name.Split('-');
        if (split.Any(x => x == string.Empty))
        {
            throw new ArgumentException("Входной топоним не был в правильном формате");
        }
        string normalized = string.Join(" ", name.Split(" ").Where(x => x != string.Empty));
        return name.Trim().ToLower();
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != typeof(AddressNameToken))
        {
            return false;
        }
        var toCompare = (AddressNameToken)obj;
        return toCompare._normalizedName == this._normalizedName &&
                toCompare.Formatting.Equals(this.Formatting);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

}