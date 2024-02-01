$("#save").click(function () {
    $.ajax({
        type: "POST",
        url: "/specialities/add",
        data: JSON.stringify(
        {
            FgosCode: $("#FgosCode").val(),
            FgosName: $("#FgosName").val(),
            Qualification: $("#Qualification").val(),
            CourseCount: Number($("#CourseCount").val()),
            FgosPrefix: $("#FgosPrefix").val(),
            QualificationPostfix: $("#QualificationPostfix").val(),
            EducationalLevelIn: Number($("#EducationalLevelIn").val()),
            EducationalLevelOut: Number($("#EducationalLevelOut").val()),
            TeachingLevel: Number($("#TeachingLevel").val()),
        }),
        dataType: "JSON",
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



