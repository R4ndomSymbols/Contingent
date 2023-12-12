namespace StudentTracking.Models.Domain.Misc;

public class GroupSponsorshipType {
    public enum Types {
        NotMentioned = -1,
        FederalGovernmentSponsorship = 1,
        FederalSubjectGovernmentSponsorship = 2,
        LocalGovenmentSponsorship = 3,
        IndividualSponsorship = 4 
    }

    public static readonly Dictionary<Types, (string name, string postfix)> Names = new Dictionary<Types, (string name, string postfix)> {
        {Types.NotMentioned, ("Не указано", "")},
        {Types.FederalGovernmentSponsorship, ("Бюджетное финансирование", "")},
        {Types.FederalSubjectGovernmentSponsorship, ("Бюджет субъекта", "")},
        {Types.LocalGovenmentSponsorship, ("Местный бюджет", "")},
        {Types.IndividualSponsorship, ("Внебюджет", "в")}
    };
}
