import { Utilities } from "../site";
let utils = new Utilities();
let specialties = [];
let groups = [];
let lockCount = 0;
let creationYear = 0;
let ancestorGroupId = 0;
let specialtyId = 0;
let financingType = 0;
let eduFormType = 0;
let courseOn = 0;
let groupCourse = [];
let created = false;
$(document).ready(function () {

    $(".data_changer").blur(
        function () {
            updateName();
        }
    );
    $("#ancestor_group").on("input", function () {
        findGroups();
    });
    $(".edu_program_input").on("input", function () {
        findSpecialties();
    });
    $(".CreationYear").on("input", function () {
        var num = Number($(this).val());
        creationYear = isNaN(num) ? creationYear : num;
    });
    $(".SponsorshipType").on("change", function () {
        var num = Number($(this).val());
        financingType = num != -1 ? num : financingType
    });
    $(".EducationalForm").on("change", function () {
        var num = Number($(this).val());
        eduFormType = num != -1 ? num : eduFormType
    });

    $("#AutogenerateName").on("click", function () {
        var generateName = Boolean(document.getElementById("AutogenerateName").checked);
        if (generateName) {
            $("#no_gen").css("display", "none");
            $("#auto_gen").css("display", "block");
        }
        else {
            $("#no_gen").css("display", "block");
            $("#auto_gen").css("display", "none");
        }
        creationYear = 0;
        ancestorGroupId = -1;
        specialtyId = -1;
        eduFormType = -1;
        financingType = -1;
        $(".edu_program_input").val(undefined);
        $(".creationYear").val(undefined);
        $('.EducationalForm').prop('selectedIndex', 0);
        $('.SponsorshipType').prop('selectedIndex', 0);

    });
    $("#AutogenerateName").trigger("click")

});
function updateName() {
    var generateName = Boolean(document.getElementById("AutogenerateName").checked);
    if (!generateName) {
        return;
    }
    $.ajax({
        type: "POST",
        url: "/groups/getname",
        data: JSON.stringify(
            {
                EduProgramId: specialtyId,
                EduFormatCode: eduFormType,
                SponsorshipTypeCode: financingType,
                CreationYear: creationYear,
                AutogenerateName: true,
            }
        ),
        dataType: "JSON",
        success: function (response) {
            document.getElementById("GroupName").innerText = response["groupName"];
        }
    });
}

$("#save").on("click", function () {
    if (created) {
        alert("Обновление не предусмотрено")
        return;
    }

    $.ajax({
        type: "POST",
        url: "/groups/add",
        data: JSON.stringify(
            {
                EduProgramId: specialtyId,
                EduFormatCode: eduFormType,
                SponsorshipTypeCode: financingType,
                CreationYear: creationYear,
                AutogenerateName: Boolean(document.getElementById("AutogenerateName").checked),
                GroupName: $("#GroupNameInput").val(),
                CourseOn: courseOn,
                PreviousGroupId: ancestorGroupId


            }),
        dataType: "JSON",
        contentType: "application/json",
        success: function (response) {
            alert("Сохранение прошло успешно")
            created = true
        },
        error: function (response, a, b) {
            utils.readAndSetErrors(response)
            alert("Сохранение провалилось");
        }
    });
});

function findSpecialties() {
    specialties = [];
    let searchFunction = () => $.ajax({
        type: "POST",
        url: "/specialities/search/query",
        data: JSON.stringify({
            SearchString: getSearchSpecialtyData()
        }),
        contentType: "application/json",
        success: function (response) {
            $.each(response, function (index, specialty) {
                specialties.push({
                    value: specialty["id"],
                    label: specialty.fgosCode + " " + specialty.fgosName + " (" + specialty.qualificationName + ")"
                })
            });
            $(".edu_program_input").autocomplete({
                delay: 100,
                source: specialties,
                change: function (event, ui) {
                    specialtyId = ui.item.value
                },
                select: function (event, ui) {
                    $(this).prop("value", ui.item.label)
                    event.preventDefault();
                }
            })
        }
    });
    registerScheduledSearch(searchFunction);
}
function findGroups() {
    groups = [];
    let searchFunction = () => $.ajax({
        type: "POST",
        url: "/groups/search/query",
        data: JSON.stringify({
            GroupName: $("#ancestor_group").val(),
            IsActive: true
        }),
        contentType: "application/json",
        success: function (response) {
            $.each(response, function (index, group) {
                groups.push({
                    value: group["groupId"],
                    label: group.groupName
                });
                groupCourse.push({
                    id: group["groupId"],
                    course: group.courseOn
                });
            });
            $("#ancestor_group").autocomplete({
                delay: 100,
                source: groups,
                change: function (event, ui) {
                    ancestorGroupId = ui.item.value
                    courseOn = Number(groupCourse.find((x) => x.id == ui.item.value).course) + 1
                },
                select: function (event, ui) {
                    $(this).prop("value", ui.item.label)
                    event.preventDefault();
                }
            })
        }
    });
    registerScheduledSearch(searchFunction);
}

function getSearchSpecialtyData() {
    return $(".edu_program_input").val();
}

function registerScheduledSearch(searchFunc) {
    lockCount += 1;
    let promise = new Promise(
        (resolve, reject) => {
            let now = lockCount;
            setTimeout(
                () => {
                    if (now != lockCount) {
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


