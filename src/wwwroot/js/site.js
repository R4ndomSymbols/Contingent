const error_postfix = "_err";
function setErrors(response){
    response = response.errors;
    $.each(response, function (index, value) { 
        var elem = document.getElementById(value.frontendFieldName + error_postfix);
        if (elem != null) {
            elem.innerHTML = value.messageForUser;
            var parentInput = document.getElementById(value.frontendFieldName);
            if (parentInput != null) {
                $("#" + value.frontendFieldName).on("click", function () {
                    elem.innerHTML = "";
                })
            }
        }
    }); 
}

