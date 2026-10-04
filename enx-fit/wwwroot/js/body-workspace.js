(() => {
    'use strict';
    const root = document.querySelector('.body-workspace');
    if (!root) return;
    root.classList.add('is-enhanced');
    const chart = root.querySelector('[data-body-chart]');
    if (chart) {
        const links = [...chart.querySelectorAll('[data-body-point]')];
        const picker = chart.querySelector('[name="Point"]');
        const select = id => {
            const point = links.find(link => link.dataset.bodyPoint === String(id));
            if (!point) return;
            links.forEach(link => { link.classList.toggle('is-selected', link === point); link.setAttribute('aria-pressed', String(link === point)); });
            picker.value = point.dataset.bodyPoint;
            chart.querySelector('[data-body-selected-date]').textContent = point.dataset.bodyDate;
            chart.querySelector('[data-body-selected-value]').textContent = point.dataset.bodyValue;
            chart.querySelector('[data-body-selected-edit]').href = point.getAttribute('href');
            const address = new URL(location.href); address.searchParams.set('Point', point.dataset.bodyPoint);
            history.replaceState(history.state, '', address);
        };
        links.forEach(link => {
            link.setAttribute('role', 'button'); link.setAttribute('aria-pressed', String(link.classList.contains('is-selected')));
            link.setAttribute('aria-label', link.getAttribute('aria-label').replace('Изменить замер', 'Выбрать замер'));
            link.addEventListener('keydown', event => { if (event.key === ' ') { event.preventDefault(); link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true })); } });
            link.addEventListener('click', event => {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
            event.preventDefault(); select(link.dataset.bodyPoint);
            });
        });
        picker.addEventListener('change', () => select(picker.value));
        chart.querySelector('[data-body-point-picker]').addEventListener('submit', event => { event.preventDefault(); select(picker.value); });
    }
    root.querySelectorAll('[data-body-filters] select').forEach(select => select.addEventListener('change', () => select.form.requestSubmit()));
    const form = root.querySelector('[data-body-form]');
    if (form) {
        const feedback = form.querySelector('[data-body-feedback]');
        const save = form.querySelector('[data-body-save]');
        const caption = save.textContent;
        const numbers = [...form.querySelectorAll('[data-body-number]')];
        const validate = field => {
            const value = field.value.trim().replace(',', '.');
            let error = '';
            if (!value && field.required) error = 'Укажите значение.';
            else if (value && (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(value) || !Number.isFinite(Number(value)))) error = 'Введите число, например 80,5.';
            else if (value && (Number(value) < Number(field.dataset.min) || Number(value) > Number(field.dataset.max))) error = 'Укажите значение от ' + field.dataset.min + ' до ' + field.dataset.max + '.';
            field.setCustomValidity(error);
            return !error;
        };
        numbers.forEach(field => field.addEventListener('input', () => { validate(field); feedback.textContent = ''; }));
        const notes = form.querySelector('[data-body-notes]');
        notes?.addEventListener('input', () => { form.querySelector('[data-body-note-count]').textContent = notes.value.length + ' / 1000'; });
        const recover = () => { save.disabled = false; save.removeAttribute('aria-busy'); save.textContent = caption; };
        form.addEventListener('submit', event => {
            numbers.forEach(validate);
            const invalid = [...form.querySelectorAll('input,select,textarea')].find(field => !field.checkValidity());
            if (invalid) {
                event.preventDefault();
                const details = invalid.closest('details'); if (details) details.open = true;
                invalid.focus(); invalid.reportValidity(); return;
            }
            if (!navigator.onLine) { event.preventDefault(); recover(); feedback.textContent = 'Сейчас нет сети. Данные остались в форме; повторите сохранение после подключения.'; return; }
            save.disabled = true; save.setAttribute('aria-busy', 'true'); save.textContent = save.classList.contains('danger') ? 'Удаляем…' : 'Сохраняем…';
        });
        // Validate hidden optional inputs before native constraint validation tries to focus them.
        form.addEventListener('invalid', event => { const details = event.target.closest('details'); if (details) details.open = true; }, true);
        window.addEventListener('online', () => { if (feedback.textContent) feedback.textContent = 'Соединение восстановлено. Повторите сохранение.'; });
        window.addEventListener('pageshow', recover);
    }
    const plans = [...root.querySelectorAll('[data-plan]')];
    if (plans.length) {
        const desktop = matchMedia('(min-width: 900px)');
        const layout = () => plans.forEach(plan => { plan.open = desktop.matches || plan.dataset.plan === root.dataset.currentPlan; });
        desktop.addEventListener('change', layout); layout();
    }
})();
