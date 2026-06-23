// Theme Management
class ThemeManager {
    constructor() {
        this.themeToggle = document.getElementById('themeToggle');
        this.themeIcon = document.getElementById('themeIcon');
        this.init();
    }

    init() {
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);
        
        if (this.themeToggle) {
            this.themeToggle.addEventListener('click', () => this.toggleTheme());
        }
    }

    setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        
        if (this.themeIcon) {
            this.themeIcon.className = theme === 'dark' ? 'fas fa-sun' : 'fas fa-moon';
        }
    }

    toggleTheme() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
    }
}

// Form Validation Helper
class FormValidator {
    static initPhoneMask() {
        $('[data-phone-mask]').inputmask({
            mask: '+7 (999) 999-99-99',
            placeholder: '_',
            clearIncomplete: true,
            showMaskOnHover: true
        });
    }

    static validatePasswordStrength(password) {
        const requirements = {
            length: password.length >= 6,
            uppercase: /[A-Z]/.test(password),
            lowercase: /[a-z]/.test(password),
            digit: /\d/.test(password)
        };

        const strength = Object.values(requirements).filter(Boolean).length;
        return {
            strength,
            requirements,
            isValid: strength >= 3
        };
    }

    static initPasswordStrengthIndicator(inputId, indicatorId) {
        const input = document.getElementById(inputId);
        const indicator = document.getElementById(indicatorId);

        if (input && indicator) {
            input.addEventListener('input', (e) => {
                const result = this.validatePasswordStrength(e.target.value);
                indicator.style.width = `${result.strength * 25}%`;
                indicator.className = `progress-bar bg-${result.isValid ? 'success' : 'warning'}`;
            });
        }
    }
}

// Car Navigation (for Details page)
class CarNavigation {
    constructor() {
        this.currentCarId = null;
        this.init();
    }

    init() {
        this.currentCarId = document.querySelector('[data-car-id]')?.dataset.carId;
        
        // Load next/previous car data if available
        this.loadNavigation();
    }

    loadNavigation() {
        // This would typically make an API call to get adjacent cars
        // For now, we'll implement basic functionality
        const prevBtn = document.querySelector('[data-car-prev]');
        const nextBtn = document.querySelector('[data-car-next]');

        if (prevBtn) {
            prevBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.navigateToAdjacent('prev');
            });
        }

        if (nextBtn) {
            nextBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.navigateToAdjacent('next');
            });
        }
    }

    navigateToAdjacent(direction) {
        // Implement navigation logic based on your application structure
        console.log(`Navigate ${direction} from car ${this.currentCarId}`);
        // You would typically fetch the adjacent car ID and redirect
    }
}

// Price Calculator for Booking
class PriceCalculator {
    constructor() {
        this.pricePerDay = 0;
        this.init();
    }

    init() {
        const priceElement = document.querySelector('[data-price-per-day]');
        if (priceElement) {
            this.pricePerDay = parseFloat(priceElement.dataset.pricePerDay) || 0;
            this.bindEvents();
        }
    }

    bindEvents() {
        const startDate = document.getElementById('StartDate');
        const endDate = document.getElementById('EndDate');
        const options = document.querySelectorAll('.booking-option');

        if (startDate) startDate.addEventListener('change', () => this.calculate());
        if (endDate) endDate.addEventListener('change', () => this.calculate());
        options.forEach(option => {
            option.addEventListener('change', () => this.calculate());
        });
    }

    calculate() {
        const startDate = new Date(document.getElementById('StartDate')?.value);
        const endDate = new Date(document.getElementById('EndDate')?.value);
        
        if (!startDate || !endDate || isNaN(startDate) || isNaN(endDate)) return;

        const days = Math.ceil((endDate - startDate) / (1000 * 60 * 60 * 24));
        const validDays = Math.max(days, 3); // Minimum 3 days
        
        let total = this.pricePerDay * validDays;

        // Add option prices
        document.querySelectorAll('.booking-option:checked').forEach(option => {
            const price = parseFloat(option.dataset.price) || 0;
            total += option.dataset.calculation === 'daily' ? price * validDays : price;
        });

        this.updateDisplay(total, validDays);
    }

    updateDisplay(total, days) {
        const totalElement = document.getElementById('totalPrice');
        const daysElement = document.getElementById('rentalPeriod');

        if (totalElement) {
            totalElement.textContent = new Intl.NumberFormat('ru-RU', {
                style: 'currency',
                currency: 'RUB',
                minimumFractionDigits: 0
            }).format(total);
        }

        if (daysElement) {
            daysElement.textContent = `${days} ${this.getDayWord(days)}`;
        }
    }

    getDayWord(days) {
        if (days % 10 === 1 && days % 100 !== 11) return 'день';
        if ([2,3,4].includes(days % 10) && ![12,13,14].includes(days % 100)) return 'дня';
        return 'дней';
    }
}

// Main Application Initialization
class CarRentalApp {
    constructor() {
        this.themeManager = new ThemeManager();
        this.priceCalculator = new PriceCalculator();
        this.carNavigation = new CarNavigation();
        this.init();
    }

    init() {
        console.log('CarRental application initialized');
        
        // Initialize components
        FormValidator.initPhoneMask();
        FormValidator.initPasswordStrengthIndicator('NewPassword', 'passwordStrengthBar');
        
        // Initialize tooltips
        this.initTooltips();
        
        // Initialize smooth scrolling
        this.initSmoothScroll();
        
        // Form submission prevention for development
        this.initFormHandlers();
    }

    initTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (href === '#') return;
                
                e.preventDefault();
                const targetElement = document.querySelector(href);
                if (targetElement) {
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    initFormHandlers() {
        document.querySelectorAll('form[data-ajax]').forEach(form => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const formData = new FormData(form);
                const submitBtn = form.querySelector('[type="submit"]');
                const originalText = submitBtn.textContent;
                
                // Show loading state
                submitBtn.disabled = true;
                submitBtn.textContent = 'Отправка...';
                
                try {
                    const response = await fetch(form.action, {
                        method: form.method,
                        body: formData,
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    });
                    
                    const result = await response.json();
                    
                    if (result.success) {
                        this.showNotification(result.message, 'success');
                        if (result.redirectUrl) {
                            setTimeout(() => {
                                window.location.href = result.redirectUrl;
                            }, 1500);
                        }
                    } else {
                        this.showNotification(result.message, 'error');
                    }
                } catch (error) {
                    console.error('Form submission error:', error);
                    this.showNotification('Произошла ошибка при отправке формы', 'error');
                } finally {
                    submitBtn.disabled = false;
                    submitBtn.textContent = originalText;
                }
            });
        });
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        // Add to page
        const container = document.querySelector('.container') || document.body;
        container.prepend(notification);
        
        // Auto remove after 5 seconds
        setTimeout(() => {
            notification.remove();
        }, 5000);
    }
}

// Initialize application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.CarRentalApp = new CarRentalApp();
});

// Utility functions
window.formatPrice = (price) => {
    return new Intl.NumberFormat('ru-RU', {
        style: 'currency',
        currency: 'RUB',
        minimumFractionDigits: 0
    }).format(price);
};

window.showLoader = (show = true) => {
    const loader = document.getElementById('global-loader');
    if (loader) {
        loader.style.display = show ? 'flex' : 'none';
    }
};

// Handle AJAX form submissions globally
$(document).on('submit', 'form[data-ajax]', function(e) {
    e.preventDefault();
    const form = $(this);
    const submitBtn = form.find('[type="submit"]');
    const originalText = submitBtn.text();
    
    submitBtn.prop('disabled', true).text('Отправка...');
    
    $.ajax({
        url: form.attr('action'),
        method: form.attr('method'),
        data: form.serialize(),
        success: function(response) {
            if (response.success) {
                window.CarRentalApp.showNotification(response.message, 'success');
                if (response.redirectUrl) {
                    setTimeout(() => {
                        window.location.href = response.redirectUrl;
                    }, 1500);
                }
            } else {
                window.CarRentalApp.showNotification(response.message, 'error');
            }
        },
        error: function() {
            window.CarRentalApp.showNotification('Произошла ошибка при отправке формы', 'error');
        },
        complete: function() {
            submitBtn.prop('disabled', false).text(originalText);
        }
    });
});