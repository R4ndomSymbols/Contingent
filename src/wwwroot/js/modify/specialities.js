import { Utilities } from "../site";
let utils = new Utilities();
let id = null;

$(document).ready(function () {
    let possible = Number($("#Id").val());
    if (possible !== NaN && possible !== 0 && possible !== undefined){
        id = possible
    }
});

$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/specialities/add",
        data: getData(),
        contentType: "application/json",
        success: function (response) {
            id = Number(response["id"]);
            alert("Специальность сохранена");
        },
        error: function(xhr, a, b) {
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
            CourseCount: courseCount.length === 0 ? -1 :Number(courseCount.val()),
            FgosPrefix: fgosPrefix.length === 0 ? "undefined" : fgosPrefix.val(),
            QualificationPostfix: qualPostfix.length === 0 ? "undefined" : qualPostfix.val(),
            EducationalLevelIn: eduIn.length === 0 ? -1 : Number(eduIn.val()),
            EducationalLevelOut: eduOut.length === 0 ? -1 : Number(eduOut.val()),
            TeachingLevel: Number($("#TeachingLevel").val()),
            ProgramType: Number($("#ProgramType").val())
        }
    )

}



