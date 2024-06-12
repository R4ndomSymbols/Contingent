import { Utilities } from "../site.js";
let utils = new Utilities();
let id = null;

init();

function init() {
    let possible = Number($("#id_section").attr("spec_id"));
    if (possible !== NaN && possible !== 0 && possible !== undefined) {
        id = possible
        utils.disableField("CourseCount")
        utils.disableField("FgosPrefix")
        utils.disableField("QualificationPostfix")
        utils.disableField("EducationalLevelIn")
        utils.disableField("EducationalLevelOut")
    }
}

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/specialties/add",
        data: getData(),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            id = Number(response["id"]);
            utils.notifySuccess();
        },
        error: function (xhr, a, b) {
            utils.readAndSetErrors(xhr)
        }
    });
});

function getData() {

    let courseCount = $("#CourseCount");
    let fgosPrefix = $("#FgosPrefix");
    let qualPostfix = $("#QualificationPostfix");
    let eduIn = $("#EducationalLevelIn");
    let eduOut = $("#EducationalLevelOut");
    return JSON.stringify(
        {
            Id: id,
            FgosCode: $("#FgosCode").val(),
            FgosName: $("#FgosName").val(),
            Qualification: $("#Qualification").val(),
            CourseCount: Number(courseCount.val()),
            FgosPrefix: fgosPrefix.val(),
            QualificationPostfix: qualPostfix.val(),
            EducationalLevelIn: Number(eduIn.val()),
            EducationalLevelOut: Number(eduOut.val()),
            TeachingLevel: Number($("#TeachingLevel").val()),
            ProgramType: Number($("#ProgramType").val())
        }
    )

}



