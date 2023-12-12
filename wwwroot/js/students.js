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
    if (address.length > 3) {
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

$("#about").click(function () {
    var address = $("#ActualAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
            var about = document.getElementById("AboutAddress");
            if (about != null) {
                about.innerHTML = response["aboutAddress"];
            }
        }
    });
});
$("#about_legal_address").click(function () {
    var address = $("#LegalAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
            var about = document.getElementById("legal_address_info");
            if (about != null) {
                about.innerHTML = response["aboutAddress"];
            }
        }
    });
});

$("#save").click(function () {
    var realAddress = $("#ActualAddress").val();
    var legalAddress = $("#LegalAddress").val();
    document.getElementById("ActualAddress_err").innerHTML = "";
    document.getElementById("LegalAddress_err").innerHTML = "";

    $.ajax({
        type: "POST",
        url: "/students/addcomplex",
        data: JSON.stringify(
        {
            actAddress: {
                Address : realAddress,
            },
            student : {
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
                RussianCitizenshipId : null
            },
            factAddress: {
                Address : legalAddress,
            },
            rusCitizenship:{
                Name: $("#Name").val(),
                Surname: $("#Surname").val(),
                Patronymic: $("#Patronymic").val(),
                PassportNumber: $("#PassportNumber").val(),
                PassportSeries: $("#PassportSeries").val(),
            }

        }),
        dataType: "JSON",
        success: function (response) {
            var realAddressId = response["addressId"];
            var studentId = response["studentId"];
            var legalAddressId = response["addressId"];
            var rusId = response["russianCitizenshipId"];

            if (realAddressId != undefined && studentId!=undefined && legalAddressId!=undefined && rusId != undefined) {
                $("#AddressId").val(realAddressId);
                $("#StudentId").val(studentId);
                $("#RussianLegalAddressId").val(legalAddressId);
                $("#RussianCitizenshipId").val(rusId);
            }
            else {
                $.each(response, function (index, value) { 
                    var elem = document.getElementById(value.field + error_postfix);
                    if (elem != null) {
                        
                        elem.innerHTML = value.err;
                        var parentInput = document.getElementById(value.field);
                        if (parentInput != null) {
                            $("#" + value.field).on("click", function () {
                                elem.innerHTML = "";
                            })
                        }
                    }
                }); 
            }
        }
    });
});




