
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Specialties;

namespace Tests;

public class GroupRowDataSource : IRowSource
{
    private static string[] _headers = new[]{
        GroupInDTO.EduProgramNameFieldName,
        GroupInDTO.QualificationFieldName,
        GroupInDTO.EduFormatFieldName,
        GroupInDTO.SponsorshipTypeCodeFieldName,
        GroupInDTO.CreationYearFieldName,
        GroupInDTO.AutogenerateNameFieldName
    };
    private string[] _data;
    private IList<(string, string)> _eduPrograms;
    private IList<string> _formats;
    private IList<string> _sponsorships;
    public int ColumnCount => _headers.Length;
    private Random _rng;

    public GroupRowDataSource()
    {
        _eduPrograms = SpecialtyModel.FindSpecialties(new Contingent.SQL.QueryLimits(0, 1000)).Result.Select(x => (x.FgosName, x.Qualification)).ToList();
        _data = new string[_headers.Length];
        _formats = GroupEducationFormat.ListOfFormats.Where(x => x.IsDefined()).Select(x => x.RussianName).ToList();
        _sponsorships = GroupSponsorship.ListOfSponsorships.Where(x => x.IsDefined()).Select(x => x.RussianName).ToList();
        _rng = new Random();
        UpdateState();
    }

    public string GetData(int pos)
    {
        return _data[pos];
    }

    public string? GetHeader(int pos)
    {
        return _headers[pos];
    }

    public void UpdateState()
    {
        _data = new string[_headers.Length];
        var spec = RandomPicker<(string, string)>.Pick(_eduPrograms);
        _data[0] = spec.Item1;
        _data[1] = spec.Item2;
        _data[2] = RandomPicker<string>.Pick(_formats);
        _data[3] = RandomPicker<string>.Pick(_sponsorships);
        _data[4] = _rng.Next(2010, 2025).ToString();
        _data[5] = "да";
    }
}
