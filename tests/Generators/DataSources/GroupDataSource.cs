
using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Specialties;

namespace Tests;

public class GroupRowDataSource : IRowSource
{
    private static string[] _headers = new[]{
        GroupInDTO.EduProgramIdFieldName,
        GroupInDTO.EduFormatCodeFieldName,
        GroupInDTO.SponsorshipTypeCodeFieldName,
        GroupInDTO.CreationYearFieldName,
        GroupInDTO.AutogenerateNameFieldName
    };
    private string[] _data;
    private IList<int> _eduPrograms;
    private IList<int> _formats;
    private IList<int> _sponsorships;
    public int ColumnCount => _headers.Length;
    private Random _rng;

    public GroupRowDataSource()
    {
        _eduPrograms = SpecialtyModel.FindSpecialties(new Contingent.SQL.QueryLimits(0, 1000)).Result.Select(x => (int)x.Id).ToList();
        _data = new string[_headers.Length];
        _formats = GroupEducationFormat.ListOfFormats.Where(x => x.IsDefined()).Select(x => (int)x.FormatType).ToList();
        _sponsorships = GroupSponsorship.ListOfSponsorships.Where(x => x.IsDefined()).Select(x => (int)x.TypeOfSponsorship).ToList();
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
        _data[0] = RandomPicker<int>.Pick(_eduPrograms).ToString();
        _data[1] = RandomPicker<int>.Pick(_formats).ToString();
        _data[2] = RandomPicker<int>.Pick(_sponsorships).ToString();
        _data[3] = _rng.Next(2020, 2025).ToString();
        _data[4] = "да";
    }
}
