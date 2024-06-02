using Contingent.Models.Domain.Specialties;

namespace Contingent.Controllers.DTO.Out;


public class SpecialtySearchResultDTO
{

    public int? Id { get; set; }
    public string FgosName { get; set; }
    public string FgosCode { get; set; }
    public string QualificationName { get; set; }
    public string LinkToModify { get; set; }
    public string LinkToView { get; set; }
    public SpecialtySearchResultDTO(SpecialtyModel model)
    {
        Id = model.Id;
        FgosName = model.FgosName;
        FgosCode = model.FgosCode;
        QualificationName = model.Qualification;
        if (model.Id is not null)
        {
            LinkToModify = "/specialities/modify/" + Id.ToString();
            LinkToView = "/specialities/about/" + Id.ToString();
        }
        else
        {
            LinkToModify = "";
            LinkToView = "";
        }
    }
}