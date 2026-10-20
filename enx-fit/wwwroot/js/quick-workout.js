// Quick-workout presentation. The shared execution queue owns all server mutations.
(() => {
    'use strict';
    const root = document.querySelector('[data-quick-workout]');
    if (!root) return;
    const initial = JSON.parse(document.getElementById('workout-execution-state').textContent);
    const libraryRoot = document.getElementById('workout-library');
    const library = libraryRoot.exerciseLibrary;
    const storageKey = `enix-quick-timer:${root.dataset.viewer}:${root.dataset.sessionId}`;
    let saved = {};
    try { saved = JSON.parse(localStorage.getItem(storageKey)) || {}; } catch { /* Server timers remain available. */ }
    let restSeconds = Math.max(0, Math.min(900, Number(saved.restSeconds ?? root.dataset.defaultRest ?? 60)));
    let mode = saved.mode === 'stopwatch' ? 'stopwatch' : 'timer';
    let stopwatch = {elapsed: Math.max(0, Number(saved.stopwatch?.elapsed) || 0), startedAt: Number(saved.stopwatch?.startedAt) || null};
    let resumeStopwatch = false, state = initial, settingRest = false, adding = false, clearing = false;
    let tab = 'recommended', limit = 8;
    const rows = [...root.querySelectorAll('[data-quick-id]')];
    const search = root.querySelector('[data-qw-search]');
    const play = root.querySelector('[data-qw-timer-toggle]');
    const skip = root.querySelector('[data-qw-timer-skip]');
    const clock = seconds => `${String(Math.floor(Math.max(0, seconds) / 60)).padStart(2, '0')}:${String(Math.floor(Math.max(0, seconds) % 60)).padStart(2, '0')}`;
    const stopwatchSeconds = () => (stopwatch.elapsed + (stopwatch.startedAt ? Date.now() - stopwatch.startedAt : 0)) / 1000;
    const normalize = text => String(text).toLocaleLowerCase('ru').replace(/ё/g, 'е');
    function persist() {
        try { localStorage.setItem(storageKey, JSON.stringify({restSeconds, mode, stopwatch})); } catch { /* Optional local preferences only. */ }
    }
    function message(host, text) { if(!host)return; host.textContent = text || ''; host.hidden = !text; }
    function updateSuggestions() {
        if(!search)return;
        const added = new Set([...root.querySelectorAll('.session-exercise')].map(card => card.dataset.catalogId));
        const words = normalize(search.value).trim().split(/\s+/).filter(Boolean);
        const favorites = library.favorites;
        let count = 0;
        for (const row of rows) {
            const id = row.dataset.quickId;
            const match = !added.has(id) && (tab !== 'recent' || row.dataset.recent === 'true') &&
                (tab !== 'favorites' || favorites.has(id)) && words.every(word => normalize(row.dataset.search).includes(word));
            row.hidden = !match || ++count > limit;
            row.querySelector('[data-qw-favorite]').setAttribute('aria-pressed', String(favorites.has(id)));
            row.querySelector('[data-qw-add]').disabled = adding || library.busy || state.status !== 'InProgress';
        }
        root.querySelector('[data-qw-empty]').hidden = count > 0;
        root.querySelector('[data-qw-more]').hidden = count <= limit;
        root.querySelectorAll('[data-qw-tab]').forEach(button => button.setAttribute('aria-pressed', String(tab === button.dataset.qwTab)));
    }
    const playIcon = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 3 15 9-15 9Z" fill="currentColor"/></svg>';
    const pauseIcon = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 4v16M17 4v16" stroke="currentColor" stroke-width="5"/></svg>';
    function renderTimer() {
        const paused = state.status === 'Paused';
        const remaining = state.remainingRestSeconds ?? state.restRemainingSeconds ??
            (state.restEndsAtUtc ? Math.max(0, Math.ceil((Date.parse(state.restEndsAtUtc) - Date.parse(state.serverNowUtc)) / 1000)) : null);
        const hasRest = !!state.restEndsAtUtc || remaining != null;
        const running = mode === 'stopwatch' ? !!stopwatch.startedAt : !!state.restEndsAtUtc && (remaining ?? 0) > 0;
        const seconds = mode === 'stopwatch' ? stopwatchSeconds() : hasRest ? remaining ?? 0 : restSeconds;
        root.querySelector('[data-rest-clock]').textContent = clock(seconds);
        root.querySelector('[data-rest-caption]').textContent = paused ? 'На паузе' : mode === 'stopwatch' ? 'Секундомер' :
            hasRest && !running && seconds > 0 ? 'Пауза' : hasRest && seconds <= 0 ? 'Можно продолжать' : restSeconds === 0 ? 'Отдых выключен' : 'Отдых';
        root.querySelector('[data-qw-ring]').style.setProperty('--rest-progress', `${mode === 'stopwatch' ? (seconds % 60) / 60 * 75 : Math.min(1, Math.max(0, seconds / Math.max(restSeconds, 1))) * 75}%`);
        play.innerHTML = running ? pauseIcon : playIcon;
        play.disabled = paused || settingRest || (mode === 'timer' && !hasRest && restSeconds === 0);
        play.setAttribute('aria-label', mode === 'stopwatch' ? running ? 'Остановить секундомер' : 'Запустить секундомер' : running ? 'Приостановить таймер отдыха' : 'Запустить таймер отдыха');
        play.dataset.control = running ? 'pause-rest' : hasRest && seconds > 0 ? 'resume-rest' : 'start-rest';
        play.dataset.seconds = String(restSeconds);
        skip.disabled = paused;
        skip.setAttribute('aria-label', mode === 'stopwatch' ? 'Сбросить секундомер' : 'Пропустить отдых');
        skip.querySelector('span').textContent = mode === 'stopwatch' ? 'Сбросить' : 'Пропустить';
        root.querySelectorAll('[data-qw-mode]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.qwMode === mode)));
        root.querySelectorAll('[data-qw-preset]').forEach(button => {
            button.setAttribute('aria-pressed', String(Number(button.dataset.qwPreset) === restSeconds));
            button.disabled = paused || settingRest || mode === 'stopwatch';
        });
        root.querySelectorAll('[data-qw-adjust]').forEach(button => button.disabled = paused || settingRest || mode === 'stopwatch');
        root.querySelector('[data-qw-rest-form]')?.querySelector('button[type="submit"]')?.toggleAttribute('disabled', paused || settingRest);
    }
    function renderStats() {
        const cards = root.querySelectorAll('.session-exercise');
        const forms = [...root.querySelectorAll('.set-form')];
        root.querySelectorAll('[data-qw-exercise-count]').forEach(el => el.textContent = cards.length);
        root.querySelectorAll('[data-qw-working-count]').forEach(el => el.textContent = forms.filter(form => !form.elements.namedItem('AddSet.IsWarmup').checked && !form.classList.contains('is-skipped')).length);
        root.querySelectorAll('[data-qw-clear-trigger]').forEach(button => button.disabled = !cards.length || state.status !== 'InProgress' || clearing);
    }
    async function setRest(seconds, form = null) {
        if (settingRest || state.status !== 'InProgress') return;
        seconds = Math.max(0, Math.min(900, seconds));
        settingRest = true; renderTimer();
        const status = document.querySelector('[data-qw-rest-status]');
        try {
            const request = form || document.createElement('form');
            request.action = `${location.pathname}?handler=SessionRest`;
            for (const [name, value] of Object.entries({seconds, __RequestVerificationToken: document.querySelector('[name="__RequestVerificationToken"]').value})) {
                let input = request.elements.namedItem(name);
                if (!input) { input = document.createElement('input'); input.type = 'hidden'; input.name = name; request.append(input); }
                input.value = String(value);
            }
            await root.saveWorkoutStructure(request);
            restSeconds = seconds; persist();
            document.querySelector('[data-qw-rest-form] [name="seconds"]').value = String(seconds);
            if (state.restEndsAtUtc || state.restRemainingSeconds != null) root.controlWorkout('set-rest', seconds);
            message(status, '');
            if (form) document.getElementById('quick-rest-settings-dialog').close();
        } catch (error) {
            message(status, error.message);
            message(root.querySelector('[data-qw-add-status]'), error.message);
        } finally { settingRest = false; renderTimer(); updateSuggestions(); }
    }
    root.addEventListener('workout-state', event => {
        const next = event.detail;
        if (next.restAfterSetId && next.restAfterSetId !== state.restAfterSetId && mode === 'stopwatch') {
            stopwatch = {elapsed: stopwatchSeconds() * 1000, startedAt: null}; mode = 'timer'; persist();
        }
        if (next.status === 'Paused' && stopwatch.startedAt) {
            stopwatch = {elapsed: stopwatchSeconds() * 1000, startedAt: null}; resumeStopwatch = true; persist();
        } else if (next.status === 'InProgress' && state.status === 'Paused' && resumeStopwatch) {
            stopwatch.startedAt = Date.now(); resumeStopwatch = false; persist();
        }
        state = next; renderTimer(); renderStats();
    });
    root.addEventListener('workout-exercises-changed', () => { updateSuggestions(); renderStats(); });
    root.addEventListener('change', renderStats);
    search?.addEventListener('input', () => { limit = 8; updateSuggestions(); });
    root.addEventListener('click', async event => {
        const target = event.target.closest('button');
        if (!target) return;
        if (target.hasAttribute('data-qw-mode')) { mode = target.dataset.qwMode; persist(); renderTimer(); }
        if (target.hasAttribute('data-qw-preset')) await setRest(Number(target.dataset.qwPreset));
        if (target.hasAttribute('data-qw-adjust')) {
            if (state.restEndsAtUtc || state.restRemainingSeconds != null) root.controlWorkout('adjust-rest', Number(target.dataset.qwAdjust));
            else await setRest(restSeconds + Number(target.dataset.qwAdjust));
        }
        if (target === play && mode === 'stopwatch') {
            event.stopPropagation();
            stopwatch = stopwatch.startedAt ? {elapsed: stopwatchSeconds() * 1000, startedAt: null} : {elapsed: stopwatch.elapsed, startedAt: Date.now()};
            persist(); renderTimer();
        }
        if (target === skip && mode === 'stopwatch') { event.stopPropagation(); stopwatch = {elapsed: 0, startedAt: null}; persist(); renderTimer(); }
        if (target.hasAttribute('data-qw-tab')) { tab = target.dataset.qwTab; limit = 8; updateSuggestions(); }
        if (target.hasAttribute('data-qw-more')) { limit += 8; updateSuggestions(); }
        if (target.hasAttribute('data-qw-favorite')) {
            const id = target.dataset.qwFavorite;
            library.favorites.has(id) ? library.favorites.delete(id) : library.favorites.add(id);
            try { localStorage.setItem(library.key, JSON.stringify([...library.favorites])); } catch { /* In-memory favorites remain usable. */ }
            library.filter(); updateSuggestions();
        }
        if (target.hasAttribute('data-qw-add') && !adding && !library.busy && state.status === 'InProgress') {
            adding = true; updateSuggestions();
            const status = root.querySelector('[data-qw-add-status]'); message(status, 'Добавляем упражнение…');
            try {
                const id = target.dataset.qwAdd;
                await root.pickWorkoutExercises([{exerciseId: id, setsCount: 3, weight: 0, reps: 8, restSeconds}]);
                message(status, 'Упражнение добавлено. Укажите рабочий вес и повторения.');
            } catch (error) { message(status, error.message); }
            finally { adding = false; updateSuggestions(); }
        }
    });
    document.addEventListener('submit', async event => {
        const form = event.target;
        if (!form.matches('[data-qw-clear-form],[data-qw-rest-form]')) return;
        event.preventDefault(); event.stopImmediatePropagation();
        if (form.matches('[data-qw-rest-form]')) { if (form.reportValidity()) await setRest(Number(form.elements.namedItem('seconds').value), form); return; }
        if (clearing) return;
        clearing = true; renderStats();
        const status = document.querySelector('[data-qw-clear-status]');
        const submit = form.querySelector('[type="submit"]'); submit.disabled = true;
        try { await root.saveWorkoutStructure(form, submit); document.getElementById('quick-clear-dialog').close(); message(status, ''); }
        catch (error) { message(status, error.message); }
        finally { clearing = false; submit.disabled = false; renderStats(); updateSuggestions(); }
    }, true);
    libraryRoot.addEventListener('click', () => queueMicrotask(updateSuggestions));
    window.addEventListener('storage', event => { if (event.key === library.key) updateSuggestions(); });
    const stopwatchTick = setInterval(() => { if (!root.isConnected) clearInterval(stopwatchTick); else if (mode === 'stopwatch') renderTimer(); }, 250);
    renderTimer(); renderStats(); updateSuggestions();
})();
