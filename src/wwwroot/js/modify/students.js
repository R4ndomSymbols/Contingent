import { Utilities } from "../site.js";
const tag_select_postfix = "_tag"
const minimal_tag_id = 1
let currentTagId = minimal_tag_id;
let utils = new Utilities();
let tags = [];
let addressSearchLockCount = 0;
let studentId = utils.INVALID_ID;
let addressId = utils.INVALID_ID;
let russianCitizenshipId = utils.INVALID_ID;
let legalAddress = utils.INVALID_ID;


$(document).ready(function () {
    studentId = Number($("#StudentId").attr("assigned_id"))
    addressId = Number($("#AddressId").attr("assigned_id"))
    russianCitizenshipId = Number($("#RussianCitizenshipId").attr("assigned_id"))
    legalAddress = Number($("#RussianLegalAddressId").attr("assigned_id"))
    // получает все уровни образования, имеющиеся у студента
    if (studentId != utils.INVALID_ID) {
        utils.disableField("PaidAgreementType");
    }

    $.ajax({
        type: "GET",
        url: "/students/tags",
        dataType: "JSON",
        success: function (response) {
            $.each(response, function (index, obj) {
                tags.push(
                    {
                        label: obj["typeName"],
                        value: obj["type"]
                    }
                )
            });
        }
    });
});

function registerScheduledAddressSearch(searchFunc) {
    addressSearchLockCount += 1;
    var promise = new Promise(
        (resolve, reject) => {
            let now = addressSearchLockCount;
            setTimeout(
                () => {
                    if (now != addressSearchLockCount) {
                        resolve();
                    }
                    else {
                        searchFunc();
                        resolve();
                    }
                }, 400)
        }
    )
}

$("#ActualAddress").on("keyup", function () {
    registerScheduledAddressSearch(function () {
        var address = $("#ActualAddress").val();
        if (address.slice(-1) != " ") {
            return;
        }
        if (address.length > 3) {
            $.ajax({
                type: "GET",
                url: "/addresses/suggest/" + address,
                dataType: "JSON",
                success: function (response) {
                    var search = $("#ActualAddress").autocomplete({
                        source: response.map(x => address + " " + x)
                    });
                    search.autocomplete("search");
                }
            });
        }
    });

});
$("#LegalAddress").on("keyup", function () {
    registerScheduledAddressSearch(function () {
        var address = $("#LegalAddress").val();
        if (address.slice(-1) != " ") {
            return;
        }
        if (address.length > 3) {
            $.ajax({
                type: "GET",
                url: "/addresses/suggest/" + address,
                dataType: "JSON",
                success: function (response) {
                    $("#LegalAddress").autocomplete({
                        source: response.map(x => address + " " + x)
                    });
                }
            });
        }
    })
});

$("#about").click(function () {
    var address = $("#ActualAddress").val();
    $.ajax({
        type: "GET",
        url: "/addresses/explain/" + address,
        dataType: "JSON",
        success: function (response) {
            var about = document.getElementById("AboutAddress");
            about.innerHTML = "";
            about.innerHTML = response["addressState"];
        },
        error: function (xhr, a, b) {
            utils.readAndSetErrors(xhr, "AboutAddress")
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
            about.innerHTML = "";
            about.innerHTML = response["addressState"];
        },
        error: function (xhr, a, b) {
            utils.readAndSetErrors(xhr, "legal_address_info");
        }
    });
});
$("#add_education_level").click(function () {
    let innerSelect = "";
    let levels = $("#education_levels");
    currentTagId++
    innerSelect +=
        `
        <div class="d-flex flex-row flex-fill">
        <select id = "${String(currentTagId) + tag_select_postfix}" class="standard-select mb-1">
    
    `
    $.each(tags, function (index, value) {
        innerSelect +=
            `
        <option value = "${value.value}">${value.label}</option>
        `
    });
    innerSelect += "</select></div>";
    levels.append(innerSelect);

});

function getEducationLevels() {
    var resultArray = []
    for (let index = currentTagId; index > minimal_tag_id; index--) {
        resultArray.push(
            {
                Level: Number($("#" + String(index) + tag_select_postfix).val())
            }
        )

    }
    return resultArray;

}


$("#save").click(function () {
    var realAddress = $("#ActualAddress").val();
    var legalAddress = $("#LegalAddress").val();
    var giaMark = $("#GiaMark").val();
    var giaDemMark = $("#GiaDemoExamMark").val();
    var patr = $("#Patronymic").val();
    var paid = $("#PaidAgreementType");
    $.ajax({
        type: "POST",
        url: "/students/addcomplex",
        data: JSON.stringify(
            {

                Id: studentId,
                GradeBookNumber: $("#GradeBookNumber").val(),
                DateOfBirth: $("#DateOfBirth").val(),
                Gender: Number($("#Gender").val()),
                Snils: $("#Snils").val(),
                TargetAgreementType: Number($("#TargetAgreementType").val()),
                PaidAgreementType: Number(paid.val()),
                AdmissionScore: $("#AdmissionScore").val(),
                GiaMark: giaMark == "" ? null : giaMark,
                GiaDemoExamMark: giaDemMark == "" ? null : giaDemMark,
                PhysicalAddress: {
                    Address: realAddress,
                },
                RusCitizenship: {
                    Id: russianCitizenshipId,
                    Name: $("#Name").val(),
                    Surname: $("#Surname").val(),
                    Patronymic: patr == "" ? null : patr,
                    LegalAddress: {
                        Address: legalAddress,
                    },
                },
                Education: getEducationLevels()

            }),
        dataType: "JSON",
        success: function (response) {
            addressId = response["addressId"];
            studentId = response["studentId"];
            legalAddress = response["addressId"];
            russianCitizenshipId = response["russianCitizenshipId"];
            utils.notifySuccess();
        },
        error: function (xhr, a, b) {
            utils.readAndSetErrors(xhr);
        }
    });
});




