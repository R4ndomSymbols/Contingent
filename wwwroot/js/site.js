const error_postfix = "_err";
function setErrors(response){
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

