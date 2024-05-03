
var specialities = [];
var groups = [];
var lockCount = 0;
var creationYear = 0;
var ancestorGroupId = 0;
var specialityId = 0;
var financingType = 0;
var eduFormType = 0;
var courseOn = 0;
var groupCourse = [];
var created = false;
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
        findSpecialities();
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
        specialityId = -1;
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
                EduProgramId: specialityId,
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

$("#save").click(function () {
    if (created) {
        alert("Обновление не предусмотрено")
        return;
    }

    $.ajax({
        type: "POST",
        url: "/groups/add",
        data: JSON.stringify(
            {
                EduProgramId: specialityId,
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
            setErrorsByClass(response);
            alert("Сохранение провалилось");
        }
    });
});

function findSpecialities() {
    specialities = [];
    var searchFunction = () => $.ajax({
        type: "POST",
        url: "/specialities/search/query",
        data: JSON.stringify({
            SearchString: getSearchSpecialityData()
        }),
        contentType: "application/json",
        success: function (response) {
            $.each(response, function (index, speciality) {
                specialities.push({
                    value: speciality["id"],
                    label: speciality.fgosCode + " " + speciality.fgosName + " (" + speciality.qualificationName + ")"
                })
            });
            $(".edu_program_input").autocomplete({
                delay: 100,
                source: specialities,
                change: function (event, ui) {
                    specialityId = ui.item.value
                },
                select: function (event, ui) {
                    $(this).prop("value", ui.item.label)
                    event.preventDefault();
                }
            })
        }
    });
    registerSheduledSearch(searchFunction);
}
function findGroups() {
    groups = [];
    var searchFunction = () => $.ajax({
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
    registerSheduledSearch(searchFunction);
}

function getSearchSpecialityData() {
    return $(".edu_program_input").val();
}

function registerSheduledSearch(searchFunc) {
    lockCount += 1;
    var promise = new Promise(
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


