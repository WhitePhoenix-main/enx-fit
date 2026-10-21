(() => {
    'use strict';
    const search = document.querySelector('[data-client-search]');
    search?.addEventListener('input', () => {
        const query = search.value.trim().toLocaleLowerCase('ru');
        let visible = 0;
        document.querySelectorAll('[data-client-name]').forEach(row => {
            row.hidden = !row.dataset.clientName.toLocaleLowerCase('ru').includes(query);
            if (!row.hidden) visible++;
        });
        document.querySelector('[data-client-search-empty]').hidden = visible > 0;
    });
    const tabs = document.querySelector('[data-progress-tabs]');
    if (!tabs) return;
    const links = [...tabs.querySelectorAll('a[href^="#"]')];
    const sections = links.map(link => document.querySelector(link.getAttribute('href'))).filter(Boolean);
    let scheduled = false;
    const update = () => {
        scheduled = false;
        const edge = tabs.getBoundingClientRect().bottom + 48;
        let current = sections[0];
        sections.forEach(section => { if (section.getBoundingClientRect().top <= edge) current = section; });
        links.forEach(link => {
            if (link.hash === '#' + current?.id) link.setAttribute('aria-current', 'true');
            else link.removeAttribute('aria-current');
        });
    };
    window.addEventListener('scroll', () => { if (!scheduled) { scheduled = true; requestAnimationFrame(update); } }, { passive: true });
    update();
})();
