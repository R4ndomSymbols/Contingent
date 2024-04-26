var currentPage = 1;
var pageSize = 20;
var lastFound = undefined;
var offsets = [0];

$("#perform_search").on("click", function () {
    currentPage = 0;
    changePage(currentPage);
    lastFound = undefined;
    $.each(offsets, function (indexInArray, valueOfElement) { 
         offsets[indexInArray] = 0
    });
    searchStudents();
});

$("#previous_page").on("click", function () {
    moveBackwards();
});
$("#next_page").on("click", function () {
    moveForward();
});

function searchStudents() {
    $.ajax({
        type: "POST",
        url: "/students/search/query",
        data: buildSearchQuery(),
        contentType: "application/json",
        success: function (response) {
            fillPage(response)
        }
    });
}


function fillPage(studentsFound) {
    $("#students_found").empty();
    lastFound = (studentsFound === undefined || studentsFound.length === 0) ? undefined : studentsFound;
     
    $.each(studentsFound, function (indexInArray, student) {
        if (currentPage == offsets.length){
            offsets.push(0);
        }
        offsets[currentPage] = student.requiredOffset
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
    });
}

function buildSearchQuery() {
    var json = JSON.stringify(
        {
            Name: $("#name_input").val(),
            GroupName: $("#group_input").val(),
            PageSize: pageSize,
            PageSkipCount: currentPage,
            PreciseOffset: currentPage - 1 < 0 ? 0 : offsets[currentPage-1]
        }
    );
    return json;
}

function moveForward() {
    if (lastFound == undefined ||
        $.isEmptyObject(lastFound)) {
        return
    }
    currentPage++;
    changePage(currentPage);
    searchStudents();
    
}

function moveBackwards() 
{
    if (currentPage <= 0){
        return;
    }
    currentPage--;
    changePage(currentPage);
    searchStudents();
}

function changePage(pageNumber) {
    $("#current_page").empty();
    $("#current_page").append("<h3>" +String(pageNumber+1)+"</h3>");
}

