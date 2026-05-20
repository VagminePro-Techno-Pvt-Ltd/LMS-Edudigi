var applicationUtil = {
    getAjaxRequest: function (url, data, success, fail, excluedLoader) {
        var globalRequest = true;
        if (excluedLoader != undefined && excluedLoader != null && excluedLoader == true)
            globalRequest = false;
        $.ajax({
            url: url,
            method: "GET",
            data: data,
            global: globalRequest,
            success: function (response) {
                if (success !== undefined && typeof success == 'function')
                    success(response);
            },
            error: function (a, b, c, d) {
                if (fail !== undefined && typeof fail == 'function')
                    fail(a, b, c, d);
            }
        });
    },
    postAjaxRequest: function (url, data, success, fail, excluedLoader) {
        var globalRequest = true;
        if (excluedLoader != undefined && excluedLoader != null && excluedLoader == true)
            globalRequest = false;
        $.ajax({
            url: url,
            method: "POST",
            data: data,
            global: globalRequest,
            success: function (response) {
                if (success !== undefined && typeof success == 'function')
                    success(response);
            },
            error: function (a, b, c, d) {
                if (fail !== undefined && typeof fail == 'function')
                    fail(a, b, c, d);
            }
        });
    },
    postAjaxRequestForm: function (url, data, success, fail, excluedLoader) {
        var globalRequest = true;
        if (excluedLoader != undefined && excluedLoader != null && excluedLoader == true)
            globalRequest = false;
        $.ajax({
            url: url,
            processData: false,
            contentType: false,
            method: "POST",
            data: data,
            global: globalRequest,
            success: function (response) {
                if (success !== undefined && typeof success == 'function')
                    success(response);
            },
            error: function (a, b, c, d) {
                if (fail !== undefined && typeof fail == 'function')
                    fail(a, b, c, d);
            }
        });
    },
    showToast: function (alertTitle, alertMessage, status) {
        let toast = {
            title: alertTitle,
            message: alertMessage,
            timeout: 5000
        }
        Toast.create(alertTitle, alertMessage, status, 5000);
    },
    serializeObject: function (obj) {
        var o = {};
        var a = $(obj).serializeArray();
        $.each(a, function () {
            if (o[this.name]) {
                if (!o[this.name].push) {
                    o[this.name] = [o[this.name]];
                }
                o[this.name].push(this.value || null);
            } else {
                o[this.name] = this.value || null;
            }
        });
        return o;
    },
    serializeFormObject: function (form) {
        var data = new FormData();
        //Form data
        var form_data = $(form).serializeArray();
        $.each(form_data, function (key, input) {
            data.append(input.name, input.value);
        });

        //File data
        $(form + ' input[type="file"]').each(function () {
            var file_data = $(this)[0].files;
            if (file_data.length > 0)
                for (var i = 0; i < file_data.length; i++) {
                    data.append($(this).attr('name'), file_data[i]);
                }
        });
        return data
    }
};



$(function () {
    var currentTick = (new Date()).getTime();
    showAppMessages();
    $(document).on('click', '.aLogout', function () {
        $('#frmLogout').submit();
    });
    $(document).ajaxStart(function () {
        showLoader();
    });
    $(document).ajaxComplete(function (event, xhr, options) {

        if (xhr.responseText === '#InvalidSessionProcess#') {
            window.location.href = appHostURL + 'Account/Login?se=y';
            return;
        }
        else if (xhr.responseText === '#UnauthorizedProcess#') {
            window.location.href = appHostURL + 'Account/UnAuthorized';
            return;
        }
        hideLoader();
        setTimeout(function () {
            currentTick = (new Date()).getTime();
            $('input').prop("autocomplete", currentTick);
            $('[data-toggle="tooltip"]').tooltip();
        }, 500);
    });
    $('form').not('form[target="_blank"],form.noclick').on('submit', function () {
        var that = this;
        showLoader();
        setTimeout(function () {
            if ($(that).find('span.field-validation-error').length > 0)
                hideLoader();
        }, 50);
    });
    $('a').not('a[target="_blank"],a[href = "#"],a[data-toggle="tab"],a[data-toggle="collapse"],a.noclick,.enhanced-sidebar a').on('click', function (event) {
        if (event.ctrlKey)
            return;
        showLoader();
    });
    $('input').prop("autocomplete", currentTick);
    $(document).on('keyup', '.modal .modal-body input[type="text"],.modal .modal-body input[type="number"],.modal .modal-body input[type="date"]',
        function (e) {
            e.preventDefault();
            e.stopPropagation();
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 13 && !$(this).hasClass('ignoreenter')) {
                $(this).closest('div.modal').find('div.modal-footer button.btn-primary').click();
            }
        });
    $(document).on('click', 'div.modal button.close', function () {
        $(this).closest('div.modal').modal('hide');
    });
    disableViewModeControls();
    resetMenuItems();
    if ($.fn.dataTable) {
        $('#tbl').dataTable();
    }
});

function showAppMessages() {
    var successMessage = $('div.appMessage div.success').html();
    var errorMessage = $('div.appMessage div.error').html();

    if (successMessage)
        $('div.appMessage div.success').toast('show');
    if (errorMessage)
        $('div.appMessage div.error').toast('show');
}

function showLoader() {
    $('div.loader-wrapper').show();
}
function hideLoader() {
    $('div.loader-wrapper').hide();
}

function showLoaderContainer(containerId) {
    $('#' + containerId).html('<img src="' + appHostURL + '/app-assets/images/loader.svg" class="inlineLoader" />');
}

function disableViewModeControls() {
    if ($('#divViewContentRenderBody form').length > 0) {
        if ($('#divViewContentRenderBody form button[type=submit]').length == 0) {
            $('#divViewContentRenderBody form input,#divViewContentRenderBody form select').not('input[type=hidden]').prop('disabled', true);
        }
    }
}

function changeToUpperCase(ele) {
    var p = ele.selectionStart;
    ele.value = ele.value.toUpperCase();
    ele.setSelectionRange(p, p);
}

function resetMenuItems() {
    if ($('.sidebar-list a.active').length === 0)
        return;
    $('.sidebar-list li').removeClass('open');
    $('.sidebar-list a.active').closest('ul').show();
    $('.sidebar-list a.active').closest('ul').closest('li').addClass('open');
}
//function previewPDF() {
//    $('#FileName').change();
//    alert("hello")

//}
//function bindImagePreview(fileSelector, imageSelector) {
//    $(document).on('change', fileSelector, function () {
//        var files = $(this)[0].files;
//        if (files && files.length > 0) {
//            $(imageSelector)[0].src = URL.createObjectURL(files[0]);
//            $(imageSelector).removeClass('d-none');
//        }
//    });
//}
function bindImagePreview(buttonSelector,fileSelector, pdfContainerSelector) {
    $(document).on('click', buttonSelector, function (event) {
        event.preventDefault();
        var files = $(fileSelector)[0].files;
        if (files && files.length > 0) {
            $(pdfContainerSelector)[0].src = URL.createObjectURL(files[0]);
            $(pdfContainerSelector).removeClass('d-none');
        }
    });
}

$.fn.formData = function (values) {
    var form = $(this);
    var inputs = $(':input', form).get();
    var hasNewValues = typeof values == 'object';

    if (hasNewValues) {
        $.each(inputs, function () {
            var input = $(this);
            var value = values[this.name];

            if (values.hasOwnProperty(this.name)) {
                switch (this.type) {
                    case 'checkbox':
                        input.prop('checked', value !== null && value);
                        break;
                    case 'radio':
                        if (value === null) {
                            input.prop('checked', false);
                        } else if (input.val() == value) {
                            input.prop("checked", true);
                        }
                        break;
                    default:
                        input.val(value);
                }
            }
        });
        return form;
    } else {
        values = {};
        $.each(inputs, function () {
            var input = $(this);
            var value;
            switch (this.type) {
                case 'checkbox':
                case 'radio':
                    value = input.is(':checked') ? input.val() : null;
                    break;
                default:
                    value = $(this).val();
            }
            values[this.name] = value;
        });
        return values;
    }
};