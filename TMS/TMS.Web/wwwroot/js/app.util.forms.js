
function reOrderContainer(items) {
    var regexName = /\[([0-9])*\]/g;
    var regexId = /\_([0-9])*\_/g;
    $(items).each(function (i, cont) {
        $(cont).find('input,textarea').each(function (j, ele) {
            var name = $(ele).attr('name');
            var id = $(ele).attr('id');
            $(ele).attr('name', name.replace(regexName, '[' + i + ']'));
            $(ele).attr('id', id.replace(regexId, '_' + i + '_'));
        });


        $(cont).find('select').each(function (j, ele) {
            var name = $(ele).attr('name');
            var id = $(ele).attr('id');
            $(ele).attr('name', name.replace(regexName, '[' + i + ']'));
            $(ele).attr('id', id.replace(regexId, '_' + i + '_'));
            $(ele).attr('data-select2-id', id.replace(regexId, '_' + i + '_'));
        });
        $(cont).find('span[data-valmsg-for]').each(function (j, ele) {
            var varFor = $(ele).attr('data-valmsg-for');
            $(ele).attr('data-valmsg-for', varFor.replace(regexName, '[' + i + ']'));
        });
        $(cont).find('label[for]').each(function (j, ele) {
            var varFor = $(ele).attr('for');
            $(ele).attr('for', varFor.replace(regexId, '_' + i + '_'));
        });
        $(cont).find('.spCount').text(i + 1);
    });
}
function reOrderContainerObject(object, newIndex) {
    var regexName = /\[([0-9])*\]/g;
    var regexId = /\_([0-9])*\_/g;
    $(object).find('input,textarea').each(function (j, ele) {
        var name = $(ele).attr('name');
        var id = $(ele).attr('id');
        $(ele).attr('name', name.replace(regexName, '[' + newIndex + ']'));
        $(ele).attr('id', id.replace(regexId, '_' + newIndex + '_'));
    });
    $(object).find('select').each(function (j, ele) {
        var name = $(ele).attr('name');
        var id = $(ele).attr('id');
        $(ele).attr('name', name.replace(regexName, '[' + newIndex + ']'));
        $(ele).attr('id', id.replace(regexId, '_' + newIndex + '_'));
        $(ele).attr('data-select2-id', id.replace(regexId, '_' + newIndex + '_'));
    });
    $(object).find('span[data-valmsg-for]').each(function (j, ele) {
        var varFor = $(ele).attr('data-valmsg-for');
        $(ele).attr('data-valmsg-for', varFor.replace(regexName, '[' + newIndex + ']'));
    });
    $(object).find('label[for]').each(function (j, ele) {
        var varFor = $(ele).attr('for');
        $(ele).attr('for', varFor.replace(regexId, '_' + newIndex + '_'));
    });
    $(object).find('.spCount').text(newIndex + 1);
    return object;
}

function resetFormValidation(formId) {
    $(formId).removeData("validator");
    $(formId).removeData("unobtrusiveValidation");
    $.validator.unobtrusive.parse(formId);
}