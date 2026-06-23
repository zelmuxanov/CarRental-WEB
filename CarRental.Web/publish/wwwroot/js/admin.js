// Админ панель - основные скрипты

document.addEventListener('DOMContentLoaded', function() {
    console.log('Admin JS loaded');
    
    initMobileMenu();
    initDataTables();
    initFormConfirmations();
});

// Мобильное меню
function initMobileMenu() {
    const menuToggle = document.querySelector('.mobile-menu-toggle');
    const sidebar = document.querySelector('.admin-sidebar');
    
    if (menuToggle && sidebar) {
        menuToggle.addEventListener('click', function() {
            sidebar.classList.toggle('mobile-open');
            document.body.style.overflow = sidebar.classList.contains('mobile-open') ? 'hidden' : '';
        });
        
        // Закрытие по клику вне меню
        document.addEventListener('click', function(e) {
            if (sidebar.classList.contains('mobile-open') && 
                !sidebar.contains(e.target) && 
                !menuToggle.contains(e.target)) {
                sidebar.classList.remove('mobile-open');
                document.body.style.overflow = '';
            }
        });
        
        // Показать кнопку на мобильных
        if (window.innerWidth < 768) {
            menuToggle.style.display = 'block';
        }
        
        // Скрыть при ресайзе
        window.addEventListener('resize', function() {
            if (window.innerWidth >= 768) {
                sidebar.classList.remove('mobile-open');
                document.body.style.overflow = '';
            } else {
                menuToggle.style.display = 'block';
            }
        });
    }
}

// DataTables
function initDataTables() {
    if (typeof $.fn.DataTable !== 'undefined') {
        $('table:not(.no-datatables)').each(function() {
            if (!$.fn.DataTable.isDataTable(this)) {
                $(this).DataTable({
                    language: {
                        url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/ru.json'
                    },
                    pageLength: 25,
                    responsive: true,
                    dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                         '<"row"<"col-sm-12"tr>>' +
                         '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
                    initComplete: function() {
                        $('.dataTables_wrapper').addClass('admin-table-wrapper');
                    }
                });
            }
        });
    }
}

// Подтверждение форм
function initFormConfirmations() {
    const forms = document.querySelectorAll('form[onsubmit*="confirm"]');
    
    forms.forEach(form => {
        const originalOnsubmit = form.onsubmit;
        
        form.onsubmit = function(e) {
            const message = this.getAttribute('data-confirm') || 'Вы уверены?';
            
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
            
            if (originalOnsubmit) {
                return originalOnsubmit.call(this, e);
            }
            
            return true;
        };
    });
}

// Показать загрузчик
function showLoader() {
    const loader = document.getElementById('adminLoader');
    if (loader) loader.style.display = 'flex';
}

// Скрыть загрузчик
function hideLoader() {
    const loader = document.getElementById('adminLoader');
    if (loader) loader.style.display = 'none';
}

// Показать уведомление
function showToast(message, type = 'info') {
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    
    document.body.appendChild(toast);
    
    const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
    bsToast.show();
    
    toast.addEventListener('hidden.bs.toast', function() {
        toast.remove();
    });
}

// Экспорт функций
window.Admin = {
    showLoader,
    hideLoader,
    showToast
};

// Показываем кнопку меню на мобильных
window.addEventListener('load', function() {
    const menuToggle = document.querySelector('.mobile-menu-toggle');
    if (menuToggle && window.innerWidth < 768) {
        menuToggle.style.display = 'block';
    }
});