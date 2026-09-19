(() => {
    'use strict';
    const toggle = document.getElementById('admin-menu-toggle');
    const sidebar = document.getElementById('admin-sidebar');
    const backdrop = document.querySelector('.admin-backdrop');
    const mobile = window.matchMedia('(max-width: 767px)');
    function updateMenu(open) {
        if (mobile.matches) {
            document.body.classList.toggle('admin-mobile-open', open);
            document.body.classList.remove('admin-menu-collapsed');
            backdrop.hidden = !open;
        } else {
            document.body.classList.toggle('admin-menu-collapsed', !open);
            document.body.classList.remove('admin-mobile-open');
            backdrop.hidden = true;
        }
        sidebar.inert = !open;
        toggle.setAttribute('aria-expanded', String(open));
    }
    if (toggle && sidebar && backdrop) {
        updateMenu(!mobile.matches);
        toggle.addEventListener('click', () => {
            const open = toggle.getAttribute('aria-expanded') !== 'true';
            updateMenu(open);
            if (mobile.matches && open) sidebar.querySelector('a')?.focus();
        });
        backdrop.addEventListener('click', () => { updateMenu(false); toggle.focus(); });
        mobile.addEventListener('change', () => updateMenu(!mobile.matches));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && mobile.matches) { updateMenu(false); toggle.focus(); }
            if (event.key === 'Tab' && mobile.matches && document.body.classList.contains('admin-mobile-open')) {
                const items = [...sidebar.querySelectorAll('a,button'), toggle];
                const first = items[0], last = items[items.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
            }
        });
    }
    document.querySelectorAll('[data-auto-submit]').forEach(select => select.addEventListener('change', () => select.form.requestSubmit()));
    document.addEventListener('keydown', event => {
        if (event.key === '/' && !event.ctrlKey && !event.metaKey && !['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement.tagName) && !document.activeElement.isContentEditable) {
            event.preventDefault();
            document.querySelector('.admin-global-search input')?.focus();
        }
    });
})();
