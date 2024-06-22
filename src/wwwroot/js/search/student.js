import { Utilities } from "../site.js";
let utils = new Utilities();
let currentPage = 0;
let pageSize = 20;

init();

function init() {
    $("#perform_search").on("click", function () {
        currentPage = 0;
        changePage(currentPage);
        searchStudents();
    });
    $("#previous_page").on("click", function () {
        moveBackwards();
    });
    $("#next_page").on("click", function () {
        moveForward();
    });
}

function searchStudents() {
    $.ajax({
        type: "POST",
        url: "/students/search/find",
        data: buildSearchQuery(),
        contentType: "application/json",
        beforeSend: utils.setAuthHeader,
        success: function (response) {
            fillPage(response)
        }
    });
}


function fillPage(studentsFound) {
    $("#students_found").empty();

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
    });
}

function buildSearchQuery() {
    var json = JSON.stringify(
        {
            Name: $("#name_input").val(),
            GroupName: $("#group_input").val(),
            PageSize: pageSize,
            PageSkipCount: currentPage,
        }
    );
    return json;
}

function moveForward() {
    currentPage++;
    changePage(currentPage);
    searchStudents();

}

function moveBackwards() {
    if (currentPage <= 0) {
        return;
    }
    currentPage--;
    changePage(currentPage);
    searchStudents();
}

function changePage(pageNumber) {
    $("#current_page").empty();
    $("#current_page").append(String(pageNumber + 1));
}

