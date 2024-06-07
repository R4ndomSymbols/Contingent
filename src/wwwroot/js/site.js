export class Utilities {
    constructor() { }
    ERROR_COLLECTION_NAME = "errors";
    ERROR_FIELD_NAME = "frontendFieldName";
    ERROR_VALUE_FIELD_NAME = "messageForUser";
    ERROR_ON_PAGE_DISPLAY_ELEMENT_NAME_POSTFIX = "_err";
    SELECTOR_CLASS = "."
    SELECTOR_ID = "#"
    INVALID_ID = -1
    locks = new Map();
    // контейнер ошибок - это перечисление, содержащее ошибки с полями
    // формат ошибок определен на бекенде
    readAndSetErrors(errorsContainer, customName = undefined, selector = this.SELECTOR_ID) {
        if (errorsContainer === undefined) {
            return;
        }
        let errors = undefined;
        // получение ошибок из xhr токена
        if (errorsContainer.hasOwnProperty("responseText")) {
            errors = JSON.parse(errorsContainer.responseText)
            if (errors.hasOwnProperty(this.ERROR_COLLECTION_NAME)) {
                errors = errors[this.ERROR_COLLECTION_NAME];
            }
            else {
                return;
            }
        }
        else if (errorsContainer.hasOwnProperty(this.ERROR_COLLECTION_NAME)) {
            errors = errorsContainer[this.ERROR_COLLECTION_NAME];
        }
        else {
            // если в объекте нет нужного свойства, то прекратить выполнение
            return
        }
        if (!Array.isArray(errors) || errors === undefined) {
            alert("here")
            return;
        }
        if (errors.length == 1) {
            let errorObject = errors[0];
            let errorName = String(errorObject[this.ERROR_FIELD_NAME]);
            let errorText = String(errorObject[this.ERROR_VALUE_FIELD_NAME]);
            if (errorName === undefined || errorText === undefined) {
                return;
            }
            if (errorName === "CRITICAL_ERROR") {
                alert("КРИТИЧЕСКАЯ ОШИБКА:\n" + errorText)
                return;
            }
            if (errorName === "GENERAL_ERROR") {
                alert("Произошла неотслеживаемая ошибка: \n" + errorText + "\n Обратитесь к администратору");
                return;
            }
            if (errorName === "NULL_RECEIVED_ERROR") {
                alert("Ошибка содержимого запроса: \n" + errorText);
                return;
            }
        }
        let currentObj = this;

        if (customName !== undefined) {
            let errorFieldName = String(customName);
            $.each(errors, function (index, errorJSON) {
                let errorText = String(errorJSON[currentObj.ERROR_VALUE_FIELD_NAME]);
                if (errorFieldName === undefined || errorText === undefined) {
                    return;
                }
                // получение объекта (как правило, текстового элемента)
                // для отображения текста ошибки
                let errorDisplays = $(selector + errorFieldName);
                $.each(errorDisplays, function (index, htmlElement) {
                    htmlElement.innerHTML += errorText;
                    // получение всех родителей указанного элемента
                    let parents = $(selector + errorFieldName);
                    parents.on("click.errorDisplay", function () {
                        htmlElement.innerHTML = "";
                        // отключение события после клика
                        parents.off("click.errorDisplay");
                    });
                });
            });
        }
        else {
            $.each(errors, function (index, errorJSON) {
                let errorFieldName = String(errorJSON[currentObj.ERROR_FIELD_NAME]);
                let errorText = String(errorJSON[currentObj.ERROR_VALUE_FIELD_NAME]);
                if (errorFieldName === undefined || errorText === undefined) {
                    return;
                }
                // получение объекта (как правило, текстового элемента)
                // для отображения текста ошибки
                let errorDisplays = $(selector + errorFieldName + currentObj.ERROR_ON_PAGE_DISPLAY_ELEMENT_NAME_POSTFIX);
                $.each(errorDisplays, function (index, htmlElement) {
                    htmlElement.innerHTML = errorText;
                    // получение всех родителей указанного элемента
                    let parents = $(selector + errorFieldName);
                    parents.on("click.errorDisplay", function () {
                        htmlElement.innerHTML = "";
                        // отключение события после клика
                        parents.off("click.errorDisplay");
                    });
                });
            });
        }
    }
    notifySuccess(message = "Сохранение прошло успешно") {
        alert(message)
    }
    registerScheduledQuery(executed, queryIdentity = 1) {
        if (this.locks.has(queryIdentity)) {
            this.locks.set(
                queryIdentity,
                this.locks.get(queryIdentity) + 1
            )
        }
        else {
            this.locks.set(queryIdentity, 1)
        }
        let promise = new Promise(
            (resolve, reject) => {
                let now = this.locks.get(queryIdentity);
                setTimeout(
                    () => {
                        if (now != this.locks.get(queryIdentity)) {
                            resolve();
                        }
                        else {
                            executed();
                            resolve();
                        }
                    }, 400)
            }
        )
    }
    disableField(name, selector = this.SELECTOR_ID) {
        $(selector + name).attr("disabled", "disabled")
    }
    enableField(name, selector = this.SELECTOR_ID) {
        $(selector + name).removeAttr("disabled")
    }
}

