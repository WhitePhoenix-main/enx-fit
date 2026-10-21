(() => {
    document.addEventListener('click', event => {
        document.querySelectorAll('[data-account-menu][open]').forEach(menu => {
            if (!menu.contains(event.target)) menu.open = false;
        });
    });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        document.querySelectorAll('[data-account-menu][open]').forEach(menu => {
            menu.open = false; menu.querySelector('summary')?.focus();
        });
    });
})();
