const error_postfix = "_err"
var addresses = [];

$(document).ready(function () {

});

function setAutocompleteCountryRegions() {
    $("#country_region").autocomplete({
        source: countryRegions.map((x) => x.name)
    });
};

$("#ActualAddress").on("change", function () {
    var address = $("#ActualAddress").val();
    if (address.length > 3){
        $.ajax({
            type: "GET",
            url: "/addresses/suggest/" + address,
            dataType: "JSON",
            success: function (response) {
                $("#country_region").autocomplete({
                    source: response
                });
            }
        });
    }
});

$("#about").click(function() {
    var address = $("#ActualAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
            var about = document.getElementById("AboutAddress");
            if (about != null){
                about.innerHTML = response["aboutAddress"];
            }
        }
    });


});

$("#save").click(function () {  
    $.ajax({
        type: "POST",
        url: "/students/add",
        dataType: "json",
        data: JSON.stringify(
            {
                GradeBookNumber: $("#GradeBookNumber").val(),
                DateOfBirth: $("#DateOfBirth").val(),
                Gender: Number($("#Gender").val()),
                Snils: $("#Snils").val(),
                Inn: $("#Inn").val(),
                TargetAgreementType: Number($("#TargetAgreementType").val()),
                PaidAgreementType: Number($("#PaidAgreementType").val()),  
                AdmissionScore: $("#AdmissionScore").val(),
                GiaMark: $("#GiaMark").val(),
                GiaDemoExamMark: $("#GiaDemoExamMark").val(),
            }),
        success: function (data) {
            if (data.length == 0){
                alert("Студент занесен в базу");
                return; 
            }
            data.forEach(element => {
                var elem = document.getElementById(element["Field"] + error_postfix);
                if (elem != null){
                    elem.innerHTML = element["Err"];
                    var parentInput = document.getElementById(element["Field"]);
                    if (parentInput!=null){
                        $("#" + element["Field"]).on("click", function () {
                            elem.innerHTML = "";
                        })
                    }
                }
                
            });
        }
    });
});




