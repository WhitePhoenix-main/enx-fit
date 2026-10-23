// One active-session presentation for empty, template, program and supervised workouts.
// training.js remains the owner of persistence and deadlines.
(() => {
    'use strict';
    const root = document.querySelector('[data-workout-session]');
    if (!root) return;
    const libraryRoot = document.getElementById('workout-library'), library = libraryRoot.exerciseLibrary;
    const pickerDialog = document.getElementById('exercise-dialog');
    const restDialog = document.getElementById('quick-rest-dialog');
    const key = `enix-quick-open:${root.dataset.viewer}:${root.dataset.sessionId}`;
    let state = JSON.parse(document.getElementById('workout-execution-state').textContent);
    let openId, nextForm = null, adding = false;
    try { openId = localStorage.getItem(key); } catch { /* A presentation preference only. */ }
    const forms = host => [...host.querySelectorAll('.set-form')];
    const field = (form, name) => form?.elements.namedItem(name);
    const working = form => field(form, 'completed').checked && !field(form, 'AddSet.IsWarmup').checked &&
        !form.classList.contains('is-skipped') && Number(field(form, 'AddSet.Reps').value) > 0;
    const pending = form => !field(form, 'completed').checked && !form.classList.contains('is-skipped');
    const clock = seconds => `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(Math.floor(seconds % 60)).padStart(2, '0')}`;
    const number = value => Number(value).toLocaleString('ru-RU', {maximumFractionDigits: 2});
    const setDialog = document.getElementById('session-set-dialog');
    const rirDialog = document.getElementById('session-rir-dialog');
    const parametersDialog = document.getElementById('session-parameters-dialog');
    let selectedForm = null, rirDraft = '';
    function text(element, value) { if (element.textContent !== value) element.textContent = value; }
    function setContext(form) {
        const name = form.closest('.session-exercise').querySelector('h2').textContent.trim();
        return `${name} · подход ${field(form, 'AddSet.SetNumber').value}`;
    }
    function result(form) {
        return `${field(form, 'AddSet.IsBodyweight').checked ? 'Свой вес' : `${number(field(form, 'AddSet.Weight').value)} кг`} × ${field(form, 'AddSet.Reps').value}`;
    }
    function openSheet(dialog, form) {
        if (!form?.isConnected || state.status !== 'InProgress') return false;
        document.querySelectorAll('dialog[open]').forEach(sheet => sheet.close());
        root.querySelectorAll('.log-menu[open]').forEach(menu => { menu.open = false; });
        selectedForm = form;
        dialog.showModal();
        return true;
    }
    function renderRirChoice() {
        const input = rirDialog.querySelector('[data-session-rir-input]');
        input.value = rirDraft;
        const value = rirDraft === '' ? null : Number(rirDraft);
        rirDialog.querySelectorAll('[data-session-rir-choice]').forEach(button => {
            const choice = Number(button.dataset.sessionRirChoice);
            button.setAttribute('aria-pressed', String(value != null && (choice === 5 ? value >= 5 : value === choice)));
        });
        text(rirDialog.querySelector('[data-session-rir-description]'), value == null ? 'RIR не указан' : value === 0 ? 'Без запаса повторений' : `${value} ${value === 1 ? 'повтор' : value < 5 ? 'повтора' : 'повторов'} в запасе`);
        const save = rirDialog.querySelector('[data-session-rir-save]');
        save.textContent = value == null ? 'Записать RIR' : `Записать RIR ${value}`;
        save.disabled = state.status !== 'InProgress' || value == null || !input.validity.valid;
    }
    function openRir(form) {
        if (!openSheet(rirDialog, form)) return;
        rirDraft = field(form, 'AddSet.Rir').value;
        text(rirDialog.querySelector('[data-session-rir-context]'), setContext(form));
        text(rirDialog.querySelector('[data-session-rir-result]'), `${result(form)} повторов`);
        text(rirDialog.querySelector('[data-session-rir-plan]'), !form.dataset.targetRir ? 'План: RIR не задан' : `По плану: RIR ${form.dataset.targetRir}`);
        text(rirDialog.querySelector('[data-session-rir-previous]'), !form.dataset.previousRir ? 'Ранее: RIR не указан' : `В прошлый раз: RIR ${form.dataset.previousRir}`);
        rirDialog.querySelector('.qs-rir-custom').hidden = rirDraft === '' || Number(rirDraft) < 5;
        renderRirChoice();
    }
    function openSetMenu(form) {
        if (!openSheet(setDialog, form)) return;
        text(setDialog.querySelector('h2'), `Подход ${field(form, 'AddSet.SetNumber').value}`);
        text(setDialog.querySelector('[data-session-set-context]'), `${form.closest('.session-exercise').querySelector('h2').textContent.trim()} · ${result(form)}`);
        const warmup = field(form, 'AddSet.IsWarmup').checked;
        setDialog.querySelectorAll('[data-session-set-kind]').forEach(button => button.setAttribute('aria-pressed', String((button.dataset.sessionSetKind === 'warmup') === warmup)));
        setDialog.querySelector('[data-session-menu-drop]').disabled = form.querySelectorAll('[data-drop-index]').length >= 10;
        const plan = form.querySelector('[data-session-target]')?.textContent.trim() || '';
        text(setDialog.querySelector('[data-session-set-plan]'), plan);
        setDialog.querySelector('[data-session-set-plan]').hidden = !plan;
    }
    function openParameters(form, focusNote = false) {
        if (!openSheet(parametersDialog, form)) return;
        text(parametersDialog.querySelector('h2'), `Параметры подхода ${field(form, 'AddSet.SetNumber').value}`);
        text(parametersDialog.querySelector('[data-session-parameters-context]'), `${form.closest('.session-exercise').querySelector('h2').textContent.trim()} · ${result(form)}`);
        parametersDialog.querySelector('[data-session-bodyweight]').checked = field(form, 'AddSet.IsBodyweight').checked;
        parametersDialog.querySelector('[data-session-rest-input]').value = field(form, 'AddSet.RestSeconds').value;
        parametersDialog.querySelector('[data-session-note-input]').value = field(form, 'AddSet.Notes').value;
        const plan = form.querySelector('[data-session-target]')?.textContent.trim() || '';
        text(parametersDialog.querySelector('[data-session-parameters-plan]'), plan);
        parametersDialog.querySelector('[data-session-parameters-plan]').hidden = !plan;
        const copy = parametersDialog.querySelector('[data-session-copy-previous]');
        const prior = form.querySelector('[data-copy-previous]');
        copy.hidden = !prior;
        if (prior) copy.textContent = `Повторить прошлый: ${prior.dataset.previousBodyweight === 'true' ? 'свой вес' : `${number(prior.dataset.weight)} кг`} × ${prior.dataset.reps}`;
        if (focusNote) parametersDialog.querySelector('[data-session-note-input]').focus();
    }
    // Drop fields are also rebuilt by the shared draft/queue client. Decorate both paths identically.
    function renderSet(form) {
        if (!form.matches('.qs-set')) return;
        text(form.querySelector('[data-session-set-number]'), field(form, 'AddSet.IsWarmup').checked ? 'Р' : field(form, 'AddSet.SetNumber').value);
        const rir = field(form, 'AddSet.Rir').value;
        const picker = form.querySelector('[data-session-rir]');
        text(picker.querySelector('span'), rir || '—');
        picker.setAttribute('aria-label', `Запас повторений в подходе ${field(form, 'AddSet.SetNumber').value}: ${rir || 'не указан'}`);
        const drops = [...form.querySelectorAll('[data-drop-index]')];
        const caption = form.querySelector('[data-session-set-caption]');
        const labels = [];
        if (form.classList.contains('is-skipped')) labels.push('Пропущен');
        else if (form.classList.contains('is-next-set')) labels.push('Текущий');
        if (field(form, 'AddSet.IsWarmup').checked) labels.push('Разминка');
        if (drops.length) labels.push(`Дропсет, ${drops.length} ${drops.length === 1 ? 'ступень' : drops.length < 5 ? 'ступени' : 'ступеней'}`);
        if (field(form, 'AddSet.Notes').value.trim()) labels.push('Есть заметка');
        text(caption, labels.join(' · '));
        caption.hidden = labels.length === 0;
        form.querySelector('.qs-drop-footer').hidden = drops.length === 0;
        form.querySelector('[data-add-drop]').disabled = state.status !== 'InProgress' || drops.length >= 10;
        let previous = [];
        try { previous = JSON.parse(form.dataset.previousDrops) || []; } catch { /* No history for a new set. */ }
        drops.forEach((row, index) => {
            const main = row.querySelector('.drop-main');
            text(main.querySelector('.drop-number'), `Д${index + 1}`);
            let prior = main.querySelector('[data-drop-previous]');
            if (!prior) {
                prior = document.createElement('span');
                prior.className = 'qs-drop-previous';
                prior.setAttribute('data-drop-previous', '');
                main.querySelector('.drop-number').after(prior);
            }
            const saved = previous[index];
            text(prior, saved ? `${saved.isBodyweight ? 'СВ' : number(saved.weight)} × ${saved.reps}` : '—');
            if (!main.querySelector('.qs-drop-effort')) {
                const effort = document.createElement('span');
                effort.className = 'qs-drop-effort'; effort.textContent = '—'; effort.setAttribute('aria-hidden', 'true');
                main.querySelector('[data-drop-field="reps"]').after(effort);
            }
            if (!main.querySelector('.qs-drop-menu')) {
                const menu = document.createElement('details'); menu.className = 'log-menu qs-drop-menu';
                const summary = document.createElement('summary'); summary.textContent = '⋯';
                summary.setAttribute('aria-label', `Действия дропа ${index + 1}`);
                const content = document.createElement('div');
                const remove = main.querySelector('[data-remove-drop]');
                const bodyweight = row.querySelector('label');
                remove.textContent = 'Удалить ступень';
                content.append(bodyweight, remove); menu.append(summary, content); main.append(menu);
            }
        });
    }
    function renderProgress() {
        const eligible = forms(root).filter(form => !field(form, 'AddSet.IsWarmup').checked && !form.classList.contains('is-skipped'));
        text(root.querySelector('[data-session-set-total]'), String(eligible.length));
        text(root.querySelector('[data-qw-working-count]'), String(eligible.filter(working).length));
        if (selectedForm && !selectedForm.isConnected) {
            [setDialog, rirDialog, parametersDialog].forEach(dialog => { if (dialog.open) dialog.close(); });
            selectedForm = null;
        }
        [setDialog, rirDialog, parametersDialog].forEach(dialog => {
            dialog.querySelectorAll('button:not([data-close]),input,textarea').forEach(input => input.disabled = state.status !== 'InProgress');
        });
        if (rirDialog.open) {
            const input = rirDialog.querySelector('[data-session-rir-input]');
            rirDialog.querySelector('[data-session-rir-save]').disabled = state.status !== 'InProgress' || input.value === '' || !input.validity.valid;
        }
        if (setDialog.open && selectedForm) setDialog.querySelector('[data-session-menu-drop]').disabled = state.status !== 'InProgress' || selectedForm.querySelectorAll('[data-drop-index]').length >= 10;
    }
    function remember() { try { localStorage.setItem(key, openId || 'closed'); } catch { /* Optional. */ } }
    function renderCards() {
        const cards = [...root.querySelectorAll('.qw-exercise')];
        if (openId !== 'closed' && !cards.some(card => card.id === openId))
            openId = cards.find(card => forms(card).some(pending))?.id || cards[0]?.id;
        cards.forEach(card => {
            const expanded = card.id === openId, count = forms(card).filter(working).length;
            card.dataset.qwExpanded = String(expanded);
            card.querySelector('.qw-exercise-body').hidden = !expanded;
            card.querySelectorAll('[data-qw-toggle]').forEach(button => button.setAttribute('aria-expanded', String(expanded)));
            card.querySelector('.qs-exercise-chevron').setAttribute('aria-label', `${expanded ? 'Свернуть' : 'Развернуть'}: ${card.querySelector('h2').textContent.trim()}`);
            const rows = forms(card).filter(form => !field(form, 'AddSet.IsWarmup').checked && !form.classList.contains('is-skipped'));
            const next = rows.find(pending) || rows.at(-1);
            const target = next ? ` · ${result(next)}` : '';
            text(card.querySelector('[data-qw-card-summary]'), `${count} из ${rows.length} подходов${target}`);
            text(card.querySelector('[data-session-weight-heading]'), rows.length && rows.every(form => field(form, 'AddSet.IsBodyweight').checked) ? 'Вес' : 'кг');
            forms(card).forEach(renderSet);
        });
        root.querySelector('.qs-add-exercise').hidden = !cards.length;
        renderProgress();
    }
    function renderNext() {
        const all = forms(root), after = all.find(form => Number(field(form, 'setId').value) === state.restAfterSetId);
        nextForm = (after && forms(after.closest('.session-exercise')).find(pending)) || all.find(pending) || null;
        const host = root.querySelector('[data-qw-next-set]');
        host.hidden = !nextForm;
        const context = root.querySelector('[data-qw-rest-context]');
        context.textContent = after ? `После подхода ${field(after, 'AddSet.SetNumber').value} · ${after.closest('.session-exercise').querySelector('h2').textContent.trim()}` : 'Между подходами';
        if (!nextForm) return;
        root.querySelector('[data-qw-next-name]').textContent = nextForm.closest('.session-exercise').querySelector('h2').textContent.trim();
        const bodyweight = field(nextForm, 'AddSet.IsBodyweight').checked;
        for (const [selector, name] of [['[data-qw-next-weight]', 'AddSet.Weight'], ['[data-qw-next-reps]', 'AddSet.Reps']]) {
            const input = root.querySelector(selector);
            if (document.activeElement !== input) input.value = field(nextForm, name).value;
            input.disabled = state.status !== 'InProgress';
        }
        root.querySelector('[data-qw-next-weight]').hidden = bodyweight;
        root.querySelector('[data-qw-next-weight-unit]').hidden = bodyweight;
        root.querySelector('[data-qw-next-bodyweight]').hidden = !bodyweight;
        text(root.querySelector('[data-session-dock-next]'), `Далее: ${nextForm.closest('.session-exercise').querySelector('h2').textContent.trim()} · подход ${field(nextForm, 'AddSet.SetNumber').value} · ${result(nextForm)}`);
    }
    function renderDock() {
        const clockText = root.querySelector('[data-rest-clock]').textContent;
        root.querySelector('[data-qw-mini-clock]').textContent = clockText;
        const all = forms(root), after = all.find(form => Number(field(form, 'setId').value) === state.restAfterSetId);
        const rest = Number(after ? field(after, 'AddSet.RestSeconds').value : root.querySelector('[data-qw-rest-form] [name="seconds"]').value) || 0;
        const mode = root.querySelector('[data-qw-mode="stopwatch"]').getAttribute('aria-pressed') === 'true' ? 'stopwatch' : 'timer';
        root.querySelector('#quick-rest-title').textContent = mode === 'stopwatch' ? 'Секундомер' : 'Отдых';
        const remaining = state.remainingRestSeconds ?? state.restRemainingSeconds;
        const activeRest = state.restEndsAtUtc || remaining != null;
        const stopwatchActive = mode === 'stopwatch' && (clockText !== '00:00' || root.querySelector('[data-qw-timer-toggle]').dataset.control === 'pause-rest');
        const visible = mode === 'stopwatch' ? stopwatchActive : !!activeRest && remaining > 0;
        root.querySelector('[data-session-controls]').hidden = !visible;
        document.body.classList.toggle('qs-rest-visible', visible);
        if (!nextForm) root.querySelector('[data-session-dock-next]').textContent = 'Все подходы разобраны';
        const restPaused = state.status === 'Paused' || (mode === 'timer' && activeRest && !state.restEndsAtUtc && remaining > 0);
        root.dataset.qwRestPaused = String(restPaused);
        const total = mode === 'timer' ? `из ${clock(rest)}` : 'Секундомер';
        root.querySelector('[data-qw-rest-total]').textContent = total;
        root.querySelector('[data-qw-mini-caption]').textContent = restPaused ? 'Отдых на паузе' : mode === 'stopwatch' ? 'Секундомер' :
            activeRest && remaining <= 0 ? 'Можно продолжать' : `Отдых · ${rest} с`;
        const percentage = mode === 'stopwatch' ? 0 : activeRest ? Math.min(100, Math.max(0, (remaining ?? rest) / Math.max(rest, 1) * 100)) : 100;
        root.querySelector('[data-qw-mini-progress]').style.width = `${percentage}%`;
        root.querySelector('[data-qw-ring]').style.setProperty('--rest-progress', `${percentage}%`);
        root.querySelector('[data-qw-timer-skip] span').textContent = mode === 'stopwatch' ? 'Сбросить секундомер' : 'Пропустить отдых';
        root.querySelector('[data-qw-mini-progress]').closest('.qs-mini-track').hidden = mode === 'stopwatch';
    }
    root.addEventListener('click', event => {
        const notes = event.target.closest('[data-session-notes]');
        if (notes) {
            event.stopPropagation();
            const card = notes.closest('.session-exercise');
            const form = forms(card).find(pending) || forms(card)[0];
            if (form) {
                notes.closest('details')?.removeAttribute('open');
                root.querySelectorAll('.log-set-options[open]').forEach(menu => { menu.open = false; });
                openParameters(form, true);
            }
        }
        const toggle = event.target.closest('[data-qw-toggle]');
        if (toggle) {
            const card = toggle.closest('.qw-exercise');
            openId = openId === card.id ? 'closed' : card.id;
            remember(); renderCards();
        }
        if (event.target.closest('[data-qw-timer-skip]')) restDialog.close();
        if (event.target.closest('[data-session-rest-skip]')) root.querySelector('[data-qw-timer-skip]').click();
        if (event.target.closest('[data-qw-open-rest]')) {
            event.target.closest('details')?.removeAttribute('open');
            document.querySelectorAll('dialog[open]').forEach(dialog => dialog.close());
            restDialog.showModal();
        }
        const setMenu = event.target.closest('[data-session-set-menu]');
        if (setMenu) openSetMenu(setMenu.closest('.set-form'));
        const rir = event.target.closest('[data-session-rir]');
        if (rir) openRir(rir.closest('.set-form'));
        if (selectedForm?.isConnected && state.status === 'InProgress') {
            const kind = event.target.closest('[data-session-set-kind]');
            if (kind) {
                field(selectedForm, 'AddSet.IsWarmup').checked = kind.dataset.sessionSetKind === 'warmup';
                setDialog.close();
                field(selectedForm, 'AddSet.IsWarmup').dispatchEvent(new Event('change', {bubbles: true}));
            }
            if (event.target.closest('[data-session-menu-drop]')) {
                setDialog.close(); selectedForm.querySelector('[data-add-drop]').click();
            }
            if (event.target.closest('[data-session-more]')) openParameters(selectedForm);
            const choice = event.target.closest('[data-session-rir-choice]');
            if (choice) {
                rirDraft = choice.dataset.sessionRirChoice;
                rirDialog.querySelector('.qs-rir-custom').hidden = Number(rirDraft) < 5;
                renderRirChoice();
            }
            if (event.target.closest('[data-session-rir-custom]')) {
                rirDialog.querySelector('.qs-rir-custom').hidden = false;
                rirDialog.querySelector('[data-session-rir-input]').focus();
            }
            const clearRir = event.target.closest('[data-session-rir-clear]');
            if (clearRir || event.target.closest('[data-session-rir-save]')) {
                const input = rirDialog.querySelector('[data-session-rir-input]');
                if (clearRir || input.value !== '' && input.reportValidity()) {
                    field(selectedForm, 'AddSet.Rir').value = clearRir ? '' : input.value;
                    rirDialog.close();
                    field(selectedForm, 'AddSet.Rir').dispatchEvent(new Event('change', {bubbles: true}));
                }
            }
            if (event.target.closest('[data-session-parameters-save]')) {
                const rest = parametersDialog.querySelector('[data-session-rest-input]');
                const note = parametersDialog.querySelector('[data-session-note-input]');
                if (rest.reportValidity() && note.reportValidity()) {
                    field(selectedForm, 'AddSet.RestSeconds').value = rest.value;
                    field(selectedForm, 'AddSet.Notes').value = note.value;
                    field(selectedForm, 'AddSet.IsBodyweight').checked = parametersDialog.querySelector('[data-session-bodyweight]').checked;
                    parametersDialog.close();
                    field(selectedForm, 'AddSet.IsBodyweight').dispatchEvent(new Event('change', {bubbles: true}));
                }
            }
            if (event.target.closest('[data-session-copy-previous]')) {
                const copy = selectedForm.querySelector('[data-copy-previous]');
                if (copy) { parametersDialog.close(); copy.click(); }
            }
            if (event.target.closest('[data-session-skip]')) { parametersDialog.close(); selectedForm.querySelector('[data-skip-set]').click(); }
            if (event.target.closest('[data-session-remove]')) { parametersDialog.close(); selectedForm.requestSubmit(selectedForm.querySelector('[name="remove"]')); }
        }
        if (event.target.closest('[data-qw-mode],[data-qw-preset],[data-qw-adjust]')) queueMicrotask(renderDock);
    });
    root.addEventListener('workout-state', event => { state = event.detail; renderCards(); renderNext(); renderDock(); });
    root.addEventListener('workout-exercises-changed', event => {
        if (event.detail?.exerciseId) openId = event.detail.exerciseId;
        renderCards(); renderNext(); renderDock(); remember();
    });
    root.addEventListener('input', event => {
        const input = event.target;
        if (input.matches('[data-session-rir-input]')) {
            rirDraft = input.value;
            renderRirChoice();
        }
        if (input.matches('[data-qw-next-weight],[data-qw-next-reps]') && nextForm && state.status === 'InProgress') {
            const target = field(nextForm, input.hasAttribute('data-qw-next-weight') ? 'AddSet.Weight' : 'AddSet.Reps');
            target.value = input.value;
            target.dispatchEvent(new Event('input', {bubbles: true}));
        } else queueMicrotask(() => { renderCards(); renderNext(); });
    });
    root.addEventListener('change', () => queueMicrotask(() => { renderCards(); renderNext(); renderDock(); }));
    // Immediate addition uses the same serialized, idempotent picker command as the full library.
    libraryRoot.addEventListener('click', async event => {
        const button = event.target.closest('[data-library-add]');
        if (!button || button.disabled) return;
        event.preventDefault(); event.stopImmediatePropagation();
        if (adding || library.busy || state.status !== 'InProgress') return;
        adding = true;
        try {
            const seconds = Number(root.querySelector('[data-qw-rest-form] [name="seconds"]').value);
            await root.pickWorkoutExercises([{exerciseId: button.value, setsCount: 3, weight: 0, reps: 8, restSeconds: seconds}], Number(document.querySelector('[data-exercise-entry]').value) || 0);
        } catch (error) { library.message(error.message); }
        finally { adding = false; }
    }, true);
    pickerDialog.addEventListener('close', () => library.message(''));
    // Avoid covering focused set inputs with the fixed action bar while a phone keyboard is open.
    function keyboard() {
        const viewport = window.visualViewport;
        const inset = viewport ? Math.max(0, innerHeight - viewport.height - viewport.offsetTop) : 0;
        const editing = root.contains(document.activeElement) && document.activeElement.matches('.set-form input');
        document.body.classList.toggle('qs-keyboard', editing && inset > 120);
    }
    window.visualViewport?.addEventListener('resize', keyboard);
    document.addEventListener('focusin', keyboard);
    document.addEventListener('focusout', () => setTimeout(keyboard, 0));
    renderCards(); renderNext(); renderDock();
})();
