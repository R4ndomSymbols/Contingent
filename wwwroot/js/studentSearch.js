var currentPage = 1;
var previousOffset = 0;
var currentOffset = 0;
var lastFound = undefined;

$("#perform_search").on("click", function () {
    currentPage = 1;
    $("#current_page").empty();
    $("#current_page").append(String(currentPage));
    previousOffset = 0;
    currentOffset = 0;
    lastFound = undefined;
    searchStudents(0);
});

$("#previous_page").on("click", function () {
    moveBackwards();
});
$("#next_page").on("click", function () {
    moveForward();
});

function searchStudents(offset) {
    $.ajax({
        type: "POST",
        url: "/students/search/query",
        data: buildSearchQuery(offset),
        contentType: "application/json",
        success: function (response) {
            fillPage(response)
        }
    });
}


function fillPage(studentsFound) {
    $("#students_found").empty();
    lastFound = studentsFound;
    previousOffset = currentOffset;
    $.each(studentsFound, function (indexInArray, student) {
        $("#students_found").append(
            `
            <tr>
            <td>${student.gradeBookNumber}</td>
            <td>${student.studentFullName}</td>
            <td>${student.groupName}</td>
            <td>
                <a href="${student.linkToModify}">Изменить</a>
                <a href="${student.linkToView}">Детально</a>
            </td>
            </tr>
            `
        );
        currentOffset = student.globalOffset;
    });
}

function buildSearchQuery(offset) {
    var json = JSON.stringify(
        {
            Name: $("#name_input").attr("value"),
            GroupName: $("#group_input").attr("value"),
            PageSize: 20,
            GlobalOffset: offset
        }
    );
    return json;
}

function moveForward() {
    if (lastFound == undefined ||
        $.isEmptyObject(lastFound)) {
        return
    }
    $("#current_page").empty();
    currentPage++;
    $("#current_page").append(String(currentPage));
    searchStudents(currentOffset);
}

function moveBackwards() 
{
    if (currentPage <= 1){
        return;
    }
    $("#current_page").empty();
    currentPage--;
    $("#current_page").append(String(currentPage));
    searchStudents(previousOffset);
}
