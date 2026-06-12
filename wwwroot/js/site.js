// Site JavaScript - Factors Management System

$(document).ready(function() {
    // Auto-dismiss alerts after 5 seconds
    setTimeout(function() {
        $('.alert').alert('close');
    }, 5000);

    // Number formatting
    formatNumbers();

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (el) {
        return new bootstrap.Tooltip(el);
    });
});

function formatNumbers() {
    $('.format-number').each(function() {
        var num = parseFloat($(this).text().replace(/,/g, ''));
        if (!isNaN(num)) {
            $(this).text(num.toLocaleString());
        }
    });
}

// Persian number converter
function toPersianNumber(str) {
    var persianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
    return str.replace(/[0-9]/g, function(d) {
        return persianDigits[parseInt(d)];
    });
}

// Confirm dialog with Persian text
function confirmAction(message) {
    return confirm(message || 'آیا مطمئن هستید؟');
}

// AJAX helper
function ajaxPost(url, data, onSuccess, onError) {
    $.ajax({
        url: url,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function(result) {
            if (onSuccess) onSuccess(result);
        },
        error: function(xhr, status, error) {
            if (onError) {
                onError(xhr.responseText || error);
            } else {
                alert('خطا در ارتباط با سرور');
            }
        }
    });
}

// Date formatting helper
function formatPersianDate(dateStr) {
    if (!dateStr) return '-';
    return dateStr;
}
