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

    var actualAddressSaved = false;
    var studentSaved = false;
    var legalAddressSaved = false;
    var rusSaved = false;

    $.ajax({
        type: "POST",
        url: "/addresses/save/" + realAddress,
        dataType: "JSON",
        success: function (response) {
            var realAddressId = response["addressId"];
            if (realAddressId != undefined && realAddressId != null && realAddressId != "") {
                $("#AddressId").val(realAddressId);
                actualAddressSaved = true;
            }
            else {
                document.getElementById("ActualAddress_err").innerHTML = "Ошибка при сохранении адреса";
            }
        },
        complete: function (a, b) {
            callbackOne();
        }
    });
    function callbackOne() {
        if (actualAddressSaved) {
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
                        ActualAddressId: Number($("#AddressId").val()),
                        RussianCitizenshipId: null
                    }),
                success: function (data) {
                    var studentId = data["studentId"];
                    if (studentId != null && studentId != undefined) {
                        studentSaved = true;
                        $("#StudentId").val(studentId);
                        alert("student saved")
                    }
                    else {
                        for (element in data) {
                            var elem = document.getElementById(element["Field"] + error_postfix);
                            if (elem != null) {
                                elem.innerHTML = element["Err"];
                                var parentInput = document.getElementById(element["Field"]);
                                if (parentInput != null) {
                                    $("#" + element["Field"]).on("click", function () {
                                        elem.innerHTML = "";
                                    })
                                }
                            }

                        }
                    }
                },
                complete: function (a, b) {
                    callbackSecond();
                }
            });
        }
    }

    function callbackSecond() {
        if (studentSaved) {
            $.ajax({
                type: "POST",
                url: "/addresses/save/" + legalAddress,
                dataType: "JSON",
                success: function (response) {
                    var legalAddressId = response["addressId"];
                    if (legalAddressId != undefined && legalAddressId != null) {
                        $("#RussianLegalAddressId").val(legalAddressId);
                        legalAddressSaved = true;
                    }
                    else {
                        document.getElementById("legal_address_err").innerHTML = "Ошибка при сохранении прописки";
                    }
                },
                complete: function (a, b) {
                    callbackThird();
                }
            });
        }
    }

    function callbackThird() {
    if (legalAddressSaved) {
        $.ajax({
            type: "POST",
            url: "/students/rus/new",
            dataType: "JSON",
            data: JSON.stringify(
                {
                    Name: $("#Name").val(),
                    Surname: $("#Surname").val(),
                    Patronymic: $("#Patronymic").val(),
                    PassportNumber: $("#PassportNumber").val(),
                    PassportSeries: $("#PassportSeries").val(),
                    StudentId: Number($("#StudentId").val()),
                    LegalAddressId: Number( $("#RussianLegalAddressId").val())

                }),
            success: function (data) {
                var rusId = data["russianCitizenshipId"];
                if (rusId != null && rusId != undefined) {
                    rusSaved = true;
                    $("#RussianCitizenshipId").val(rusId);
                }
                else {
                    for (element in data) {
                        var elem = document.getElementById(element["Field"] + error_postfix);
                        if (elem != null) {
                            elem.innerHTML = element["Err"];
                            var parentInput = document.getElementById(element["Field"]);
                            if (parentInput != null) {
                                $("#" + element["Field"]).on("click", function () {
                                    elem.innerHTML = "";
                                })
                            }
                        }

                    }
                }
            }
        });
    }
    }



});




