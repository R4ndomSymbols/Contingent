const numberOnlyRegex = /^[0-9]+$/
const numberAndDotOnlyRegex = /^[0-9\.]+$/
const russianLettersOnly = /^[\u0410-\u044f]+$/
const dateStrict = /^[0-9]{4}-[0-9]{2}-[0-9]{2}$/
const russianLettersAndSpace = /^[\u0410-\u044f\s]+$/
const russianLettersNumbersAndSpace = /^[\u0410-\u044f\s0-9]+$/
const fgos = /^[0-9]{2}\.[0-9]{2}\.[0-9]{2}$/
var validationZeroLengthIndicator = "";

function flexValidation(fieldId, errFieldId, errMsg, validateFunc, uniqueString = "_", callback = function() { }) {
    $("#" + String(fieldId)).on("change", function () {
        var value = $("#" + String(fieldId)).val();
        var elem = document.getElementById(errFieldId);
        if (validateFunc(value)) {
            elem.innerHTML = "";
            validationZeroLengthIndicator = validationZeroLengthIndicator.replaceAll(uniqueString, "");
        }
        else {
            elem.innerHTML = errMsg;
            validationZeroLengthIndicator = uniqueString.concat(validationZeroLengthIndicator);
        }
        callback();
    });
}
function invokeAllValidation(fieldNames = [""]){
    for (i in fieldNames){
        $("#"+fieldNames[i]).trigger("change");
    }
}

function validateRangeLength(lengthMin, lengthMax, toValidate){
    if (String(toValidate) == undefined || String(toValidate) == ""){
        return false;
    }
    var asString = String(toValidate);
    return asString.length >= lengthMin && asString.length <= lengthMax;
}
function validateNumber(toValidate){
    if(!validateNotEmpty(toValidate)){
        return false;
    }
    if (toValidate === "false" || toValidate === "true"){
        return false;
    }
    var match = toValidate.match(numberOnlyRegex);
    if (match == null){
        return false;
    }
    
    return match.length != 0;
}
function validateLetters(toValidate){
    var match = toValidate.match(russianLettersOnly);
    if (match == null){
        return false;
    }
    return match.length != 0 && validateNotEmpty(toValidate);
}
function validateDate(toValidate){
    
    var match = toValidate.match(dateStrict);
    return match != null && Date.parse(toValidate).valueOf() != NaN;
}
function validateValue(min, max, value){
    if (!validateDecimalNumber(value)){
        return false;
    }
    var number = Number(value); 
    if (number!=NaN){
        return number >= Number(min) && number <= Number(max);
    }
    else{
        return false;
    }
}
function validateNotEmpty(toValidate){
    if (toValidate == undefined || toValidate == ""){
        return false;
    }
    return true;
}
function validateWordsOnly(toValidate){
    if (!validateNotEmpty(toValidate)){
        return false;
    }
    var match = toValidate.match(russianLettersAndSpace);
    return match != null;
}
function validateWordsAndNumbersOnly(toValidate){
    if (!validateNotEmpty(toValidate)){
        return false;
    }
    var match = toValidate.match(russianLettersNumbersAndSpace);
    return match != null;
}
function validateDecimalNumber(toValidate){
    if (!validateNotEmpty(toValidate)){
        return false;
    }
    var match = toValidate.match(numberAndDotOnlyRegex);
    return match != null;
}
function validateNonNegative(toValidate){
    if (!validateNumber(toValidate)){
        return false;
    }
    return Number(toValidate) > 0;
}
function validateFGOS(toValidate){
    if (!validateNotEmpty(toValidate)){
        return false;
    }
    else{
        var match = String(toValidate).match(fgos);
        if (match!=null){
            return true;
        }
        else{
            return false;
        }
    }
}
