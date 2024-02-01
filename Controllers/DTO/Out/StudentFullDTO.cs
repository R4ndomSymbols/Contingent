using System.Text.Json.Serialization;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Misc;
using Utilities;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public sealed class StudentFullDTO
{
    public int Id { get; private init; }
    public string Snils { get; private init; }
    public string Inn { get; private init; }
    public string DateOfBirth { get; private init; }
    public decimal AdmissionScore { get; private init; }
    public string GradeBookNumber { get; private init; }
    public int Gender { get; private init; }
    public string GenderName {get; private init;}
    public int TargetAgreementType { get; private init; }
    public int? GiaMark { get; private init; }
    public int? GiaDemoExamMark { get; private init; }
    public int PaidAgreementType { get; private init; }
    public int? RussianCitizenshipId { get; private init; }
    public int? ActualAddressId { get; private init; }
    [JsonIgnore]
    public readonly bool IsEmpty;

    public StudentFullDTO(StudentModel model)
    {
        if (model is null)
        {
           throw new ArgumentNullException(nameof(model));
        }
        Id = model.Id;
        Snils = model.Snils;
        Inn = model.Inn;
        DateOfBirth = Utils.FormatDateTime(model.DateOfBirth);
        AdmissionScore = model.AdmissionScore;
        GradeBookNumber = model.GradeBookNumber;
        Gender = (int)model.Gender;
        TargetAgreementType = (int)model.TargetAgreementType.AgreementType;
        GiaMark = model.GiaMark;
        GiaDemoExamMark = model.GiaDemoExamMark;
        PaidAgreementType = (int)model.PaidAgreementType.AgreementType;
        RussianCitizenshipId = model.RussianCitizenshipId;
        ActualAddressId = model.ActualAddressId;
        GenderName = Genders.Names[model.Gender];
        IsEmpty = false;
    }
    public StudentFullDTO(){
         Id = Utils.INVALID_ID;
            Snils = string.Empty;
            Inn = string.Empty;
            DateOfBirth = string.Empty;
            AdmissionScore = 0;
            GradeBookNumber = string.Empty;
            Gender = (int)Genders.GenderCodes.Undefined;
            TargetAgreementType = (int)TypesOfEducationAgreement.NotMentioned;
            GiaMark = null;
            GiaDemoExamMark = null;
            PaidAgreementType = (int)PaidEducationAgreementTypes.NotMentioned;
            RussianCitizenshipId = Utils.INVALID_ID;
            ActualAddressId = Utils.INVALID_ID;
            GenderName = Genders.Names[Genders.GenderCodes.Undefined];
            IsEmpty = true;
    }


}
