namespace StudentTracking.Models.Domain.Misc;

public class GroupSponsorship {
    
    public string RussianName {get; private init;}

    public string GroupNamePostfix {get; private init;}
    public GroupSponsorshipTypes TypeOfSponsorship {get; private init;}

    private GroupSponsorship(){

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
            TypeOfSponsorship = GroupSponsorshipTypes.LocalGovenmentSponsorship
        },
        new() {
            RussianName = "Внебюджет",
            GroupNamePostfix = "в",
            TypeOfSponsorship = GroupSponsorshipTypes.IndividualSponsorship
        },
    };

    public bool IsFree(){
        return TypeOfSponsorship != GroupSponsorshipTypes.IndividualSponsorship;
    }
    public bool IsPaid(){
        return TypeOfSponsorship == GroupSponsorshipTypes.IndividualSponsorship;
    }

    public static GroupSponsorship GetByTypeCode(int code) {
        return ListOfSponsorships.Where(x => (int)x.TypeOfSponsorship == code).First();
    } 
    public static bool TryGetByTypeCode(int code) {
        return ListOfSponsorships.Any(x => (int)x.TypeOfSponsorship == code);
    } 
}


public enum GroupSponsorshipTypes {
        NotMentioned = -1,
        FederalGovernmentSponsorship = 1,
        FederalSubjectGovernmentSponsorship = 2,
        LocalGovenmentSponsorship = 3,
        IndividualSponsorship = 4 
    }


