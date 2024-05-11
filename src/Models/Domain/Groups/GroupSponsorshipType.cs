namespace Contingent.Models.Domain.Groups;

public class GroupSponsorship
{

    public string RussianName { get; private init; }

    public string GroupNamePostfix { get; private init; }
    public GroupSponsorshipTypes TypeOfSponsorship { get; private init; }

    private GroupSponsorship()
    {

    }

    public static IReadOnlyCollection<GroupSponsorship> ListOfSponsorships => new List<GroupSponsorship> {
        new() {
            RussianName = "Не указано",
            GroupNamePostfix = "",
            TypeOfSponsorship = GroupSponsorshipTypes.NotMentioned
        },
        new() {
            RussianName = "Бюджетное финансирование",
            GroupNamePostfix = "",
            TypeOfSponsorship = GroupSponsorshipTypes.FederalGovernmentSponsorship
        },
        new() {
            RussianName = "Бюджет субъекта",
            GroupNamePostfix = "",
            TypeOfSponsorship = GroupSponsorshipTypes.FederalSubjectGovernmentSponsorship
        },
        new() {
            RussianName = "Местный бюджет",
            GroupNamePostfix = "",
            TypeOfSponsorship = GroupSponsorshipTypes.LocalGovernmentSponsorship
        },
        new() {
            RussianName = "Внебюджет",
            GroupNamePostfix = "в",
            TypeOfSponsorship = GroupSponsorshipTypes.IndividualSponsorship
        },
    };

    public bool IsFree()
    {
        return
        TypeOfSponsorship == GroupSponsorshipTypes.FederalGovernmentSponsorship ||
        TypeOfSponsorship == GroupSponsorshipTypes.FederalSubjectGovernmentSponsorship ||
        TypeOfSponsorship == GroupSponsorshipTypes.LocalGovernmentSponsorship;
    }
    public bool IsPaid()
    {
        return TypeOfSponsorship == GroupSponsorshipTypes.IndividualSponsorship;
    }

    public static GroupSponsorship GetByTypeCode(int code)
    {
        return ListOfSponsorships.Where(x => (int)x.TypeOfSponsorship == code).First();
    }
    public static bool TryGetByTypeCode(int code, out GroupSponsorship? type)
    {
        type = ListOfSponsorships.FirstOrDefault(x => (int)x.TypeOfSponsorship == code, null);
        return type is not null;
    }
    public bool IsDefined()
    {
        return TypeOfSponsorship != GroupSponsorshipTypes.NotMentioned;
    }
    public static bool operator ==(GroupSponsorship left, GroupSponsorship right)
    {
        return left.TypeOfSponsorship == right.TypeOfSponsorship;
    }
    public static bool operator !=(GroupSponsorship left, GroupSponsorship right)
    {
        return left.TypeOfSponsorship != right.TypeOfSponsorship;
    }

}


public enum GroupSponsorshipTypes
{
    NotMentioned = -1,
    FederalGovernmentSponsorship = 1,
    FederalSubjectGovernmentSponsorship = 2,
    LocalGovernmentSponsorship = 3,
    IndividualSponsorship = 4
}


