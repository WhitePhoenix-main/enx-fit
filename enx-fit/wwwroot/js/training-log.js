// A continuous workout log. Persistence, conflict handling and rest deadlines live in training.js.
(() => {
    'use strict';
    if (!document.body.classList.contains('training-log')) return;
    document.cookie = `enix-utc-offset=${-new Date().getTimezoneOffset()}; Path=/; SameSite=Lax; Max-Age=31536000`;
    const root = document.querySelector('[data-training-log]');
    const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[c]));
    const number = value => Number(value).toLocaleString('ru-RU', {maximumFractionDigits: 2});
    const open = dialog => {
        if (!dialog || dialog.open) return;
        document.querySelectorAll('dialog[open]').forEach(d => d.close());
        dialog.showModal();
    };
    let historyRequest = 0;
    async function showHistory(trigger) {
        const dialog = document.getElementById('exercise-history-dialog');
        const content = dialog.querySelector('[data-exercise-history-content]');
        const request = ++historyRequest;
        dialog.querySelector('.eyebrow').textContent = root?.dataset.coachMode === 'true' ? 'РЕЗУЛЬТАТЫ КЛИЕНТА' : 'ВАШИ РЕЗУЛЬТАТЫ';
        document.getElementById('exercise-history-title').textContent = trigger.closest('.log-exercise')?.querySelector('h2').textContent || trigger.closest('.log-record-card')?.querySelector('h2').textContent || 'История упражнения';
        content.textContent = 'Загружаем ваши результаты…';
        open(dialog);
        try {
            const url = root?.workoutRequestUrl ? `${root.workoutRequestUrl('History')}&exerciseId=${encodeURIComponent(trigger.dataset.exerciseHistory)}` : `/Training/Records?handler=Exercise&exerciseId=${encodeURIComponent(trigger.dataset.exerciseHistory)}${root ? `&beforeWorkoutId=${root.dataset.sessionId}` : ''}`;
            const response = await fetch(url, {cache:'no-store', redirect:'error'});
            if (!response.ok) throw new Error();
            const {performance: p} = await response.json();
            if (request !== historyRequest) return;
            if (!p) { content.innerHTML = '<div class="log-empty"><h3>Первый результат ещё впереди</h3><p>Здесь появятся рабочие подходы после завершения тренировки.</p></div>'; return; }
            const values = [p.bestWeight > 0 ? `<div><span>Максимальный вес</span><strong>${number(p.bestWeight)}<small> кг</small></strong></div>` : '', p.bestBodyweightReps > 0 ? `<div><span>Со своим весом</span><strong>${p.bestBodyweightReps}<small> повт.</small></strong></div>` : '', p.bestSetVolume > 0 ? `<div><span>Объём подхода</span><strong>${number(p.bestSetVolume)}<small> кг</small></strong></div>` : ''].join('');
            const points = p.history.slice(0, 12).reverse().map(h => ({date:h.date, value: Math.max(...h.sets.map(s => p.bestWeight > 0 ? (s.isBodyweight ? 0 : s.weight) : s.reps))}));
            const max = Math.max(...points.map(p => p.value), 1);
            content.innerHTML = `<div class="log-record-values">${values}</div><p class="eyebrow">${p.bestWeight > 0 ? 'МАКСИМАЛЬНЫЙ ВЕС · КГ' : 'ПОВТОРЕНИЯ СО СВОИМ ВЕСОМ'} · ПО ЗАНЯТИЯМ</p><div class="log-performance-chart" role="img" aria-label="Результаты последних ${points.length} тренировок">${points.map(p => `<div title="${esc(p.date)}: ${number(p.value)}"><span>${number(p.value)}</span><i style="height:${Math.max(2, p.value / max * 75)}px"></i><span>${new Date(`${p.date}T12:00:00`).toLocaleDateString('ru-RU', {day:'numeric',month:'numeric'})}</span></div>`).join('')}</div>${p.history.map(h => `<section class="log-history-session"><header><div><h3>${esc(h.title)}</h3><time>${new Date(`${h.date}T12:00:00`).toLocaleDateString('ru-RU', {day:'numeric',month:'long',year:'numeric'})}</time></div><span class="muted">${number(h.volume)} кг</span></header><ul>${h.sets.map(s => `<li><span>Подход ${s.number}</span><strong>${s.isBodyweight ? 'Свой вес' : number(s.weight) + ' кг'} × ${s.reps}</strong></li>`).join('')}</ul><a href="/Workouts/Details/${h.workoutId}${root?.dataset.coachMode === 'true' ? '?Coach=true' : ''}">Открыть тренировку ↗</a></section>`).join('')}`;
        } catch {
            if (request !== historyRequest) return;
            content.replaceChildren();
            const message = document.createElement('p'); message.textContent = 'Не удалось загрузить историю. Проверьте соединение.';
            const retry = document.createElement('button'); retry.className = 'text-button'; retry.textContent = 'Попробовать ещё раз'; retry.onclick = () => showHistory(trigger);
            content.append(message, retry);
        }
    }
    async function loadTemplates(dialog) {
        if (!dialog?.dataset.templatesUrl || dialog.dataset.loaded || dialog.dataset.loading) return;
        dialog.dataset.loading = 'true';
        const content = dialog.querySelector('[data-template-content]'), retry = dialog.querySelector('[data-template-retry]');
        retry.hidden = true;
        try {
            const response = await fetch(dialog.dataset.templatesUrl, {cache:'no-store', redirect:'error'});
            if (!response.ok) throw new Error();
            const html = new DOMParser().parseFromString(await response.text(), 'text/html');
            const options = html.querySelector('[data-template-options]');
            if (!options) throw new Error();
            content.replaceChildren(options); dialog.dataset.loaded = 'true';
        } catch { content.textContent = 'Не удалось загрузить шаблоны.'; retry.hidden = false; }
        finally { delete dialog.dataset.loading; }
    }
    document.addEventListener('click', event => {
        const history = event.target.closest('[data-exercise-history]');
        if (history) void showHistory(history);
        const trigger = event.target.closest('[data-dialog]');
        if (trigger) { event.preventDefault(); trigger.closest('details')?.removeAttribute('open'); const dialog = document.getElementById(trigger.dataset.dialog); open(dialog); void loadTemplates(dialog); }
        const close = event.target.closest('[data-close]'); if (close) close.closest('dialog')?.close();
        const retry = event.target.closest('[data-template-retry]'); if (retry) void loadTemplates(retry.closest('dialog'));
        if (event.target instanceof HTMLDialogElement) {
            const box = event.target.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
        }
        document.querySelectorAll('.log-menu[open],.log-set-options[open]').forEach(menu => { if (!menu.contains(event.target)) menu.open = false; });
    });
    if (!root) return;
    const field = (form, name) => form.elements.namedItem(name);
    const done = form => field(form, 'completed').checked && !form.classList.contains('is-skipped');
    const working = form => done(form) && !field(form, 'AddSet.IsWarmup').checked && Number(field(form, 'AddSet.Reps').value) > 0;
    const value = (form, name) => Number(field(form, name)?.value || 0);
    const volume = form => (field(form, 'AddSet.IsBodyweight').checked ? 0 : value(form, 'AddSet.Weight') * value(form, 'AddSet.Reps')) + [...form.querySelectorAll('[data-drop-index]')].reduce((sum,row) => sum + (row.querySelector('[data-bodyweight]').checked ? 0 : Number(row.querySelector('[data-drop-field="weight"]').value) * Number(row.querySelector('[data-drop-field="reps"]').value)),0);
    let state = JSON.parse(document.getElementById('workout-execution-state').textContent), toastTimer;
    function toast(text, form) {
        clearTimeout(toastTimer);
        const host = root.querySelector('[data-log-toast]'); host.replaceChildren(document.createTextNode(text));
        if (form) {
            const undo = document.createElement('button'); undo.className = 'text-button'; undo.textContent = 'Отменить';
            undo.onclick = () => { if (form.isConnected && state.status === 'InProgress') { const check = field(form,'completed'); check.checked = false; check.dispatchEvent(new Event('change',{bubbles:true})); } host.hidden = true; };
            host.append(undo);
        }
        host.hidden = false; toastTimer = setTimeout(() => host.hidden = true, 5000);
    }
    function render() {
        const cards = [...root.querySelectorAll('.session-exercise')], forms = [...root.querySelectorAll('.set-form')];
        root.querySelector('[data-log-volume]').textContent = number(forms.filter(working).reduce((sum,f) => sum + volume(f), 0));
        root.querySelectorAll('[data-log-done]').forEach(el => el.textContent = forms.filter(done).length);
        root.querySelector('[data-log-total]').textContent = forms.length;
        root.querySelector('[data-exercise-total]').textContent = cards.length;
        root.querySelector('[data-exercises-heading]').hidden = !cards.length;
        root.querySelector('.workout-empty')?.toggleAttribute('hidden', cards.length > 0);
        root.querySelectorAll('[data-finish]').forEach(finish => finish.disabled = !forms.some(working) || state.status === 'Paused');
        root.querySelectorAll('[data-add-exercise],[data-replace-exercise],[data-exercise-rest],.new-set-form button,.exercise-menu button,.qw-exercise-actions button').forEach(b => b.disabled = state.status === 'Paused' || b.dataset.structBusy === 'true' || b.dataset.orderDisabled === 'true');
        for (const [i,card] of cards.entries()) {
            card.querySelector('.log-exercise-number').textContent = root.hasAttribute('data-quick-workout') ? String(i+1) : String(i+1).padStart(2,'0');
            const rows = [...card.querySelectorAll('.set-form')];
            const next = rows.find(f => !done(f) && !f.classList.contains('is-skipped'));
            rows.forEach(f => f.classList.toggle('is-next-set', f === next));
            const results = rows.filter(working), badge = card.querySelector('[data-live-record]');
            let label = '';
            if (card.dataset.hasRecord === 'true') {
                if (results.some(f => !field(f,'AddSet.IsBodyweight').checked && value(f,'AddSet.Weight') > Number(card.dataset.recordWeight))) label = 'Новый рекорд веса';
                else if (results.some(f => field(f,'AddSet.IsBodyweight').checked && value(f,'AddSet.Reps') > Number(card.dataset.recordReps))) label = 'Новый рекорд повторений';
                else if (results.some(f => volume(f) > Number(card.dataset.recordVolume))) label = 'Новый рекорд объёма подхода';
            }
            badge.hidden = !label; badge.querySelector('span').textContent = label;
        }
        const outline = root.querySelector('[data-log-outline]');
        const markup = cards.map((c,i) => { const rows = [...c.querySelectorAll('.set-form')], count = rows.filter(done).length; return `<a class="log-outline-item ${rows.length && count === rows.length ? 'is-done' : ''}" href="#${c.id}"><span>${String(i+1).padStart(2,'0')}</span><span><strong>${esc(c.querySelector('h2').textContent)}</strong><small>${count} / ${rows.length} подходов</small></span></a>`; }).join('');
        if (outline.innerHTML !== markup) outline.innerHTML = markup;
    }
    root.addEventListener('workout-state', event => { state = event.detail; render(); });
    root.addEventListener('workout-exercises-changed', render);
    root.addEventListener('input', () => queueMicrotask(render));
    root.addEventListener('change', event => {
        const check = event.target;
        if (check.name === 'completed' && check.checked) {
            const form = check.closest('.set-form');
            queueMicrotask(() => { render(); const badge = form.closest('.session-exercise').querySelector('[data-live-record]'); toast(badge.hidden ? `Подход ${field(form,'AddSet.SetNumber').value} записан` : badge.textContent.trim(), form); });
        }
        if (check.name === 'completed' && !check.checked && Number(field(check.closest('form'),'setId').value) === state.restAfterSetId) root.querySelector('[data-control="skip-rest"]')?.click();
        queueMicrotask(render);
    });
    root.addEventListener('focusin', event => { if (event.target.matches('.log-set-main>input')) event.target.select(); });
    root.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || !event.target.matches('.log-set-main>input')) return;
        event.preventDefault(); const form = event.target.closest('form');
        (event.target.name === 'AddSet.Weight' ? field(form,'AddSet.Reps') : field(form,'completed')).focus();
    });
    document.addEventListener('submit', async event => {
        const form = event.target;
        if (!root.contains(form) || !form.matches('.new-set-form,[data-native-order]')) return;
        event.preventDefault(); event.stopImmediatePropagation();
        const button = event.submitter;
        if (button?.dataset.structBusy) return;
        if (button) { button.dataset.structBusy = 'true'; button.disabled = true; }
        const cardId = form.closest('.session-exercise').id, top = document.getElementById(cardId).getBoundingClientRect().top;
        if (form.matches('.new-set-form')) {
            const previous = [...form.closest('.session-exercise').querySelectorAll('.set-form')].at(-1);
            if (previous) for (const name of ['AddSet.Weight','AddSet.Reps','AddSet.RestSeconds','AddSet.IsBodyweight']) {
                let input = field(form,name);
                if (!input) { input = document.createElement('input'); input.type = 'hidden'; input.name = name; form.append(input); }
                input.value = name === 'AddSet.IsBodyweight' ? String(field(previous,name).checked) : field(previous,name).value;
            }
        }
        try { await root.saveWorkoutStructure(form, button); const card = document.getElementById(cardId); if (card) window.scrollBy(0, card.getBoundingClientRect().top - top); render(); }
        catch(error) { toast(error.message || 'Нет соединения. Повторите действие.'); }
        finally { if (button) { delete button.dataset.structBusy; button.disabled = false; } }
    }, true);
    root.addEventListener('change', async event => {
        const select = event.target.closest('[data-exercise-rest]'); if (!select) return;
        const card = select.closest('.session-exercise'), old = card.querySelector('.new-set-form [name="AddSet.RestSeconds"]').value;
        const form = document.createElement('form'); form.action = root.workoutRequestUrl('Rest');
        for (const [name,v] of Object.entries({ExerciseEntryId:card.dataset.entryId, seconds:select.value, __RequestVerificationToken:root.querySelector('[name="__RequestVerificationToken"]').value})) { const input = document.createElement('input'); input.name = name; input.value = v; form.append(input); }
        select.disabled = true; select.dataset.structBusy = 'true';
        try { await root.saveWorkoutStructure(form); render(); }
        catch(error) { select.value = old; toast(error.message || 'Не удалось сохранить отдых.'); }
        finally { select.disabled = false; delete select.dataset.structBusy; }
    });
    render();
})();
