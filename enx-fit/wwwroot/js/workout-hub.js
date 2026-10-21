(() => {
    'use strict';
    const open = id => { const dialog = document.getElementById(id); if (dialog instanceof HTMLDialogElement && !dialog.open) dialog.showModal(); };
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-hub-dialog]');
        if (trigger) open(trigger.dataset.hubDialog);
        const close = event.target.closest('[data-hub-close]');
        if (close) close.closest('dialog')?.close();
        const assignment = event.target.closest('[data-assign-template]');
        if (assignment) {
            const select = document.querySelector('[name="Appointment.WorkoutId"]');
            if (select) select.value = assignment.dataset.assignTemplate;
            open('hub-appointment');
        }
    });
    document.querySelectorAll('dialog.wh-dialog').forEach(dialog => dialog.addEventListener('click', event => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close();
    }));
    document.querySelectorAll('[data-hub-submit]').forEach(select => select.addEventListener('change', () => select.form?.requestSubmit()));
    const clocks = [...document.querySelectorAll('[data-hub-clock]')].map(node => ({ node, initial: Number(node.dataset.hubClock), start: Date.now(), paused: node.dataset.hubPaused === 'true' }));
    if (clocks.some(clock => !clock.paused)) setInterval(() => clocks.forEach(clock => {
        const seconds = clock.initial + (clock.paused ? 0 : Math.floor((Date.now() - clock.start) / 1000));
        clock.node.textContent = `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
    }), 1000);
    let category = 'all';
    const search = document.querySelector('[data-hub-template-search]');
    const cards = [...document.querySelectorAll('[data-template-name]')];
    const filter = () => {
        const term = search?.value.trim().toLocaleLowerCase('ru') || '';
        let visible = 0;
        cards.forEach(card => { card.hidden = !(category === 'all' || card.dataset.templateOwner === category) || !card.dataset.templateName.toLocaleLowerCase('ru').includes(term); if (!card.hidden) visible++; });
        const empty = document.querySelector('[data-hub-templates-empty]');
        if (empty) empty.hidden = visible > 0 || cards.length === 0;
    };
    search?.addEventListener('input', filter);
    document.querySelectorAll('[data-hub-template-filter]').forEach(button => button.addEventListener('click', () => {
        category = button.dataset.hubTemplateFilter;
        document.querySelectorAll('[data-hub-template-filter]').forEach(item => item.setAttribute('aria-pressed', String(item === button)));
        filter();
    }));
    window.addEventListener('pageshow', event => { if (event.persisted) location.reload(); });
})();
