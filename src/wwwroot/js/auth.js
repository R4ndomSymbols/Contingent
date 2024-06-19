import { Utilities } from "./site.js";
let utils = new Utilities();

$(document).ready(function () {
    $("#login_input").on("click", function() {
        $("#auth_err").empty();
    });
    $("#password_input").on("click", function() {
        $("#auth_err").empty();
    });
    $("#log_in_button").on("click", function () {
        $.ajax({
            type: "POST",
            url: "/login/token",
            data: JSON.stringify(
                {
                    Login: $("#login_input").val(),
                    Password: $("#password_input").val()
                }
            ),
            contentType: "application/json",
            success: function (response) {
                sessionStorage.setItem("jwt", response.jwt);
                window.location.replace("/");
            },
            error: function (xhr, status, error) {
                utils.readAndSetErrors(xhr);
            }
        });
    });
});



