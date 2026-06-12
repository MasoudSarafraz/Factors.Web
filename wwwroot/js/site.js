/* ===========================
   Factors System - Site JS
   =========================== */

$(document).ready(function () {
    // ---- Sidebar Toggle ----
    $('#sidebarToggle').on('click', function () {
        if ($(window).width() <= 992) {
            // Mobile: show/hide sidebar with overlay
            $('body').toggleClass('sidebar-open');
            $('#sidebar').toggleClass('show');
            $('#sidebarOverlay').toggle();
        } else {
            // Desktop: collapse/expand sidebar
            $('body').toggleClass('sidebar-collapsed');
        }
    });

    // Close sidebar on overlay click (mobile)
    $('#sidebarOverlay').on('click', function () {
        $('body').removeClass('sidebar-open');
        $('#sidebar').removeClass('show');
        $(this).hide();
    });

    // ---- Initialize Persian Datepickers ----
    initPersianDatepickers();

    // ---- Auto-dismiss alerts ----
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);
});

function initPersianDatepickers() {
    if (typeof $.fn.persianDatepicker === 'undefined') return;

    $('.persian-datepicker').each(function () {
        var $input = $(this);
        var altFieldId = $input.data('alt-field');

        var options = {
            format: 'YYYY/MM/DD',
            autoClose: true,
            position: 'auto',
            calendarType: 'persian',
            navigator: {
                enabled: true,
                scroll: {
                    enabled: false
                }
            },
            toolbox: {
                enabled: true,
                calendarSwitch: {
                    enabled: false
                }
            },
            initialValue: false,
            observer: true,
            altFormat: 'YYYY-MM-DD',
            altField: altFieldId ? '#' + altFieldId : undefined
        };

        // If the input already has a value, set it as initial
        var existingVal = $input.val();
        if (existingVal && existingVal.length >= 8) {
            var parts = existingVal.replace(/-/g, '/').split('/');
            if (parts.length === 3) {
                try {
                    options.initialValue = false;
                    $input.persianDatepicker(options);
                    var pd = new persianDate([parseInt(parts[0]), parseInt(parts[1]), parseInt(parts[2])]);
                    $input.val(pd.format('YYYY/MM/DD'));
                    return;
                } catch (e) {
                    // fallback
                }
            }
        }

        $input.persianDatepicker(options);
    });
}
