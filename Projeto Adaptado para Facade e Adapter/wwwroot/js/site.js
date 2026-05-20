/* ═══════════════════════════════════════════════════════════════════════════
   PCBuilder — site.js
   ═══════════════════════════════════════════════════════════════════════════ */

// ── Theme Toggle ────────────────────────────────────────────────────────────
(function () {
    const html = document.documentElement;
    const btn  = document.getElementById('themeToggle');
    const stored = localStorage.getItem('pcb-theme') || 'dark';

    html.setAttribute('data-theme', stored);
    if (btn) btn.querySelector('.theme-icon').textContent = stored === 'dark' ? '◐' : '●';

    if (btn) {
        btn.addEventListener('click', () => {
            const current = html.getAttribute('data-theme');
            const next    = current === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-theme', next);
            localStorage.setItem('pcb-theme', next);
            btn.querySelector('.theme-icon').textContent = next === 'dark' ? '◐' : '●';
        });
    }
})();

// ── Mobile Nav ──────────────────────────────────────────────────────────────
(function () {
    const burger = document.getElementById('navBurger');
    const links  = document.querySelector('.nav-links');
    if (!burger || !links) return;

    burger.addEventListener('click', () => {
        links.classList.toggle('open');
    });

    // Close on outside click
    document.addEventListener('click', (e) => {
        if (!burger.contains(e.target) && !links.contains(e.target)) {
            links.classList.remove('open');
        }
    });
})();

// ── Toast Notification System ────────────────────────────────────────────────
window.showToast = function (message, type = 'success', duration = 4000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : '⚠';
    toast.innerHTML = `<span style="color:var(--${type === 'success' ? 'accent' : type === 'error' ? 'error' : 'warn'})">${icon}</span> ${message}`;

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideInToast 0.3s ease reverse';
        setTimeout(() => toast.remove(), 280);
    }, duration);
};

// ── Fade-in on scroll ────────────────────────────────────────────────────────
(function () {
    const els = document.querySelectorAll('.build-card, .category-card, .flow-step, .comp-section');
    if (!('IntersectionObserver' in window)) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.08 });

    els.forEach((el, i) => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(16px)';
        el.style.transition = `opacity 0.4s ease ${i * 0.05}s, transform 0.4s ease ${i * 0.05}s`;
        observer.observe(el);
    });
})();

// ── PC Diagram hover labels ──────────────────────────────────────────────────
(function () {
    document.querySelectorAll('.pc-part').forEach(part => {
        part.addEventListener('mouseenter', () => {
            window.showToast(part.dataset.label + ' — Clique para personalizar', 'success', 2000);
        });
    });
    const cpuPart = document.querySelector('.pc-part.cpu');
    if (cpuPart) {
        cpuPart.style.cursor = 'pointer';
        cpuPart.addEventListener('click', () => {
            window.location.href = '/Build/Customize';
        });
    }
})();

// ── Generic form submit loader ────────────────────────────────────────────────
(function () {
    document.querySelectorAll('form').forEach(form => {
        // Skip the customize form (handled inline)
        if (form.id === 'customizeForm' || form.id === 'checkoutForm') return;

        form.addEventListener('submit', function () {
            const btns = this.querySelectorAll('button[type="submit"]');
            btns.forEach(btn => {
                btn.disabled = true;
                btn.innerHTML = '<div class="spinner spinner-sm"></div>';
            });
        });
    });
})();

// ── Currency selection on customize page ──────────────────────────────────────
(function () {
    const currencySelect = document.getElementById('currencySelect');
    if (!currencySelect) return;
    currencySelect.addEventListener('change', function () {
        // Re-triggers live price fetch with new currency
        const event = new Event('change');
        document.querySelectorAll('.comp-radio:checked').forEach(r => r.dispatchEvent(event));
    });
})();
