$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/specialities/add",
        data: getData(),
        contentType: "application/json",
        success: function (response) {
            var specialityId = response["id"];

            if (specialityId != undefined) {
                $("#Id").val(specialityId);
                alert("Специальность сохранена");
            }
            else {
                var errors = response["errors"]
                $.each(errors, function (index, value) {
                    var elem = document.getElementById(value.frontendFieldName + error_postfix);
                    if (elem != null) {

                        elem.innerHTML = value.messageForUser;
                        var parentInput = document.getElementById(value.frontendFieldName);
                        if (parentInput != null) {
                            $("#" + value.frontendFieldName).on("click", function () {
                                elem.innerHTML = "";
                            })
                        }
                    }
                });
            }
        }
    });
});

function getData() {

    let courseCount = $("#CourseCount");
    let fgosPrefix = $("#FgosPrefix");
    let qualPostfix = $("#QualificationPostfix");
    let eduIn = $("#EducationalLevelIn");
    let eduOut = $("#EducationalLevelOut");
    let id = $("#id_section");
    return JSON.stringify(
        {
            Id: id.length === 0 ? null : Number(id.attr("spec_id")),
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



