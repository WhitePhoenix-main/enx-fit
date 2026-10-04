(() => {
    'use strict';
    const root = document.querySelector('[data-record-workout]');
    if (!root) return;
    const form = root.querySelector('[data-record-form]'), list = root.querySelector('[data-record-exercises]');
    const status = root.querySelector('[data-record-status]'), outline = root.querySelector('[data-record-outline]');
    const review = document.getElementById('record-review-dialog'), confirm = review.querySelector('[data-record-confirm]');
    const feedback = root.querySelector('[data-record-feedback]'), sourceHint = root.querySelector('[data-record-source-note]');
    const key = `enix-record-draft:${root.dataset.viewer}`;
    const mobile = matchMedia('(max-width:1023px)'), basics = root.querySelector('.record-basics');
    const libraryRoot = document.getElementById('record-library'), library = new window.ExerciseLibrary(libraryRoot);
    const field = name => form.elements.namedItem(`Input.${name}`);
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const number = value => Number(String(value).trim().replace(',', '.'));
    const format = value => new Intl.NumberFormat('ru-RU', {maximumFractionDigits:2}).format(value);
    const blankSet = () => ({weight:'', reps:'', rir:'', isWarmup:false, notes:''});
    const normalize = value => {
        if (!Array.isArray(value) || value.length > 40) throw new Error('Invalid draft');
        return value.map(e => {
            if (!e || !Number.isInteger(Number(e.exerciseId)) || !Array.isArray(e.sets) || !e.sets.length || e.sets.length > 20) throw new Error('Invalid draft');
            return {exerciseId:Number(e.exerciseId), sets:e.sets.map(s => {
                if (!s || typeof s !== 'object') throw new Error('Invalid set');
                return {...blankSet(), weight:s.weight??'', reps:s.reps??'', rir:s.rir??'', isWarmup:!!s.isWarmup, notes:s.notes??''};
            })};
        });
    };
    const serverError = !!root.querySelector('.validation-summary-errors');
    let exercises = [], selectedId, sourceNote = '', restored = false, dirty = false, storageAvailable = true;
    let undo, submitting = false, loading = false, version = 0;
    try { exercises = normalize(JSON.parse(field('ResultsJson').value)); } catch { /* Keep server errors visible. */ }
    if (!exercises.length && !serverError) {
        try {
            const draft = JSON.parse(localStorage.getItem(key));
            if (draft?.viewer === root.dataset.viewer && draft.fields && Array.isArray(draft.exercises)) {
                exercises = normalize(draft.exercises);
                for (const name of ['Title','Date','DurationMinutes','Notes','ClientRequestId'])
                    if (draft.fields[name] != null) field(name).value = draft.fields[name];
                selectedId = draft.selectedId; sourceNote = draft.sourceNote || ''; restored = true;
            }
        } catch { /* Storage may be unavailable or the draft unreadable. */ }
    }
    const localDay = new Date(Date.now() - new Date().getTimezoneOffset()*60000).toISOString().slice(0,10);
    field('UtcOffsetMinutes').value = -new Date().getTimezoneOffset(); field('Date').max = localDay;
    if (!restored && !serverError) field('Date').value = localDay;
    root.classList.add('record-enhanced');
    function message(text, canUndo = false) {
        feedback.hidden = false; feedback.querySelector('[data-record-feedback-text]').textContent = text;
        feedback.querySelector('[data-record-undo]').hidden = !canUndo;
    }
    function syncResults() {
        // Autofill and browser restoration may update values without firing input.
        for (const input of list.querySelectorAll('[data-set-field]')) {
            const exercise = exercises[Number(input.closest('[data-index]').dataset.index)];
            const set = exercise.sets[Number(input.closest('[data-set-index]').dataset.setIndex)];
            set[input.dataset.setField] = input.type === 'checkbox' ? input.checked : input.value;
        }
    }
    function save() {
        syncResults();
        const fields = Object.fromEntries(['Title','Date','DurationMinutes','Notes','ClientRequestId'].map(name => [name,field(name).value]));
        dirty = true;
        try {
            localStorage.setItem(key, JSON.stringify({viewer:root.dataset.viewer, fields, exercises, selectedId, sourceNote}));
            storageAvailable = true; status.textContent = 'Черновик сохранён на этом устройстве · ещё не в истории';
        } catch {
            storageAvailable = false; status.textContent = 'Черновик недоступен. Оставьте страницу открытой до сохранения записи.';
        }
    }
    function summary() {
        root.querySelector('[data-record-title]').textContent = field('Title').value.trim() || 'Без названия';
        const date = new Date(field('Date').value+'T12:00:00');
        root.querySelector('[data-record-date]').textContent = Number.isFinite(date.getTime()) ? new Intl.DateTimeFormat('ru-RU',{day:'numeric',month:'long',year:'numeric'}).format(date) : 'Выберите дату';
        root.querySelector('[data-record-optional-summary]').textContent =
            [field('DurationMinutes').value ? `${field('DurationMinutes').value} мин` : '', field('Notes').value.trim() ? 'есть заметка' : ''].filter(Boolean).join(' · ') || 'необязательно';
        const sets = exercises.flatMap(e => e.sets), working = sets.filter(s => !s.isWarmup);
        root.querySelector('[data-record-summary]').textContent = exercises.length ?
            `Упражнений: ${exercises.length} · рабочих подходов: ${working.length}${sets.length > working.length ? ` · разминка: ${sets.length-working.length}` : ''}` : 'Начните с упражнения';
    }
    function select(id) {
        selectedId = exercises.some(e => e.exerciseId === Number(id)) ? Number(id) : exercises[0]?.exerciseId;
        [...list.children].forEach(card => { card.hidden = Number(card.dataset.exerciseId) !== selectedId; });
        outline.querySelectorAll('button').forEach(button => button.setAttribute('aria-current', String(Number(button.dataset.recordSelect) === selectedId)));
        requestAnimationFrame(() => {
            const button = outline.querySelector('[aria-current="true"]'); if (!button) return;
            const bounds = outline.getBoundingClientRect(), item = button.getBoundingClientRect();
            if (item.left < bounds.left) outline.scrollLeft += item.left-bounds.left;
            else if (item.right > bounds.right) outline.scrollLeft += item.right-bounds.right;
        });
    }
    function render() {
        root.classList.toggle('record-has-exercises',exercises.length>0);
        basics.open = !mobile.matches || !exercises.length;
        root.querySelector('[data-record-empty]').hidden = exercises.length > 0;
        root.querySelector('[data-record-outline-host]').hidden = !exercises.length;
        root.querySelector('[data-record-count]').textContent = exercises.length;
        sourceHint.hidden = !sourceNote; sourceHint.textContent = sourceNote;
        library.setSelected(exercises.map(e => e.exerciseId));
        outline.innerHTML = exercises.map((e,i) => `<button type="button" data-record-select="${e.exerciseId}"><span>${i+1}</span><strong>${escape(library.byId.get(e.exerciseId)?.dataset.name || 'Упражнение недоступно')}</strong></button>`).join('');
        list.innerHTML = exercises.map((exercise,i) => {
            const row = library.byId.get(exercise.exerciseId);
            return `<section class="panel session-exercise record-exercise" data-index="${i}" data-exercise-id="${exercise.exerciseId}">
                <header><span class="exercise-order">${i+1}</span><div><h2>${escape(row?.dataset.name || 'Упражнение недоступно')}</h2><p>${escape(row?.dataset.group || '')} · подходов: ${exercise.sets.length}</p></div><details class="exercise-menu"><summary aria-label="Действия с упражнением">⋯</summary><div><button type="button" data-move-exercise="-1" ${i===0?'disabled':''}>Выше в списке</button><button type="button" data-move-exercise="1" ${i===exercises.length-1?'disabled':''}>Ниже в списке</button><button type="button" class="danger-text" data-remove-exercise>Удалить упражнение</button></div></details></header>
                <details class="record-batch"><summary>Одинаковый вес и повторения</summary><div><label>Вес для всех<input type="text" inputmode="decimal" data-batch-weight placeholder="кг" aria-label="Вес для всех подходов" /></label><label>Повторения<input type="number" min="1" max="1000" data-batch-reps placeholder="раз" aria-label="Повторения для всех подходов" /></label><button type="button" class="button secondary" data-apply-batch>Применить</button></div></details>
                <div class="record-set-labels"><span>№</span><span>Вес, кг</span><span>Повторения</span><span></span></div>
                ${exercise.sets.map((s,n) => `<div class="record-set" data-set-index="${n}"><div class="record-set-main"><span class="set-number">${n+1}</span><input type="text" inputmode="decimal" data-set-field="weight" value="${escape(s.weight)}" required aria-label="Вес подхода ${n+1}, кг" placeholder="0" /><input type="number" inputmode="numeric" data-set-field="reps" value="${escape(s.reps)}" min="1" max="1000" required aria-label="Повторения подхода ${n+1}" /><button type="button" class="text-button danger-text" data-remove-set aria-label="Удалить подход ${n+1}">×</button></div><details class="set-extra" ${s.isWarmup || s.notes || s.rir!==''?'open':''}><summary>Разминка, усилие и заметка</summary><div><label>Осталось повторов (RIR)<input type="number" min="0" max="10" inputmode="numeric" data-set-field="rir" value="${escape(s.rir)}" /></label><label class="set-note">Заметка<input maxlength="500" data-set-field="notes" value="${escape(s.notes)}" /></label><label class="warmup-check"><input type="checkbox" data-set-field="isWarmup" ${s.isWarmup?'checked':''} />Разминка</label></div></details></div>`).join('')}
                <p class="record-weight-hint">Для упражнения с собственным весом укажите 0 кг.</p>
                <footer class="record-exercise-footer"><button type="button" class="button secondary" data-add-set ${exercise.sets.length>=20?'disabled':''}>+ Подход</button><div>${i>0?`<button type="button" class="button secondary" data-record-neighbor="${exercises[i-1].exerciseId}">Назад</button>`:''}${i<exercises.length-1?`<button type="button" class="button secondary" data-record-neighbor="${exercises[i+1].exerciseId}">Следующее →</button>`:''}</div></footer>
            </section>`;
        }).join('');
        select(selectedId); summary();
    }
    function change(action, text) {
        syncResults();
        undo = {exercises:structuredClone(exercises), selectedId, sourceNote, title:field('Title').value, requestId:field('ClientRequestId').value};
        action(); version++; render(); save(); message(text,true);
    }
    root.querySelector('[data-record-undo]').addEventListener('click', () => {
        if (!undo) return;
        ({exercises,selectedId,sourceNote} = undo); field('Title').value = undo.title; field('ClientRequestId').value = undo.requestId; undo = null;
        version++; render(); save(); message('Изменение отменено.');
    });
    const updateInput = event => {
        const input = event.target;
        if (input.dataset.setField) {
            input.setCustomValidity('');
        }
        undo = null; feedback.hidden = true; version++; save(); summary();
    };
    form.addEventListener('input', updateInput);
    form.addEventListener('change', updateInput);
    outline.addEventListener('click', event => {
        const button = event.target.closest('[data-record-select]');
        if (button) { select(button.dataset.recordSelect); save(); if (mobile.matches) root.querySelector('[data-record-outline-host]').scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion:reduce)').matches?'instant':'smooth'}); }
    });
    mobile.addEventListener('change', () => { basics.open = !mobile.matches || !exercises.length; });
    list.addEventListener('click', event => {
        const button = event.target.closest('button'), section = button?.closest('[data-index]'); if (!section) return;
        const i = Number(section.dataset.index), exercise = exercises[i];
        if (button.hasAttribute('data-record-neighbor')) { select(button.dataset.recordNeighbor); save(); return; }
        if (button.hasAttribute('data-remove-exercise')) change(() => {
            exercises.splice(i,1); selectedId = exercises[Math.min(i,exercises.length-1)]?.exerciseId;
        },'Упражнение удалено.');
        else if (button.hasAttribute('data-move-exercise')) {
            const target = i+Number(button.dataset.moveExercise); if (target<0 || target>=exercises.length) return;
            change(() => { [exercises[i],exercises[target]] = [exercises[target],exercises[i]]; },'Порядок упражнений изменён.');
        } else if (button.hasAttribute('data-add-set') && exercise.sets.length<20) {
            change(() => exercise.sets.push({...exercise.sets.at(-1)||blankSet()}),'Подход добавлен. Проверьте вес и повторения.');
        } else if (button.hasAttribute('data-remove-set')) change(() => {
            exercise.sets.splice(Number(button.closest('[data-set-index]').dataset.setIndex),1);
            if (!exercise.sets.length) exercise.sets.push(blankSet());
        },'Подход удалён.');
        else if (button.hasAttribute('data-apply-batch')) {
            const weight = section.querySelector('[data-batch-weight]').value.trim(), reps = section.querySelector('[data-batch-reps]').value;
            if (weight!=='' && (!Number.isFinite(number(weight)) || number(weight)<0 || number(weight)>10000)) { message('Введите вес от 0 до 10000 кг.'); return; }
            if (reps!=='' && (!Number.isInteger(number(reps)) || number(reps)<1 || number(reps)>1000)) { message('Введите повторения от 1 до 1000.'); return; }
            if (weight==='' && reps==='') { message('Укажите вес или повторения для заполнения подходов.'); return; }
            change(() => exercise.sets.forEach(s => { if (weight!=='') s.weight=weight; if (reps!=='') s.reps=reps; }),'Значения применены ко всем подходам упражнения.');
        }
    });
    document.addEventListener('click', event => {
        if (event.target.closest('[data-dialog="record-exercise-dialog"]')) library.setContext(field('Title').value || 'Запись выполненной тренировки');
    });
    libraryRoot.addEventListener('exercises-selected', event => {
        const picks = event.detail.exercises.filter(p => !exercises.some(e => e.exerciseId===p.exerciseId));
        if (exercises.length+picks.length>40) { library.message('Максимум 40 упражнений в записи.'); return; }
        if (!picks.length) return;
        change(() => {
            picks.forEach(p => exercises.push({exerciseId:p.exerciseId,sets:Array.from({length:p.setsCount},() => ({...blankSet(),weight:p.weight??'',reps:p.reps??''}))}));
            selectedId = picks[0].exerciseId;
        },'Упражнения добавлены. Заполните фактические результаты.');
        library.clear(); document.getElementById('record-exercise-dialog').close();
        (mobile.matches ? root.querySelector('[data-record-outline-host]') : list.querySelector(`[data-exercise-id="${selectedId}"]`)).scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion:reduce)').matches?'instant':'smooth'});
    });
    document.addEventListener('click', async event => {
        const button = event.target.closest('[data-record-source]'); if (!button || loading) return;
        if (exercises.length && !window.confirm('Заменить упражнения выбранной основой? Изменение можно отменить.')) return;
        loading = true; button.disabled = true; const initialVersion = version;
        try {
            const url = new URL(location.href); url.search = new URLSearchParams({handler:'Source',source:button.dataset.recordSource,templateId:button.dataset.templateId||''});
            const response = await fetch(url,{cache:'no-store'}), data = await response.json();
            if (!response.ok) throw new Error(data.error || 'Не удалось загрузить основу.');
            const loaded = normalize(data.exercises);
            if (version!==initialVersion) throw new Error('Черновик изменился во время загрузки. Повторите выбор основы.');
            change(() => {
                exercises = loaded; selectedId = loaded[0]?.exerciseId; field('Title').value = data.title;
                field('ClientRequestId').value = crypto.randomUUID();
                sourceNote = button.dataset.recordSource==='template' ? 'Основа из шаблона: проверьте плановые значения и замените их фактическими результатами.' : 'Основа из последней тренировки: проверьте результаты для новой даты.';
            },'Основа загружена. Проверьте каждый подход.');
            document.getElementById('record-template-dialog').close();
        } catch (error) { message(error.message); } finally { loading = false; button.disabled = false; }
    });
    root.querySelector('[data-reset-draft]').addEventListener('click', () => {
        if ((dirty || restored || exercises.length) && !window.confirm('Удалить текущий черновик и начать пустую запись?')) return;
        form.reset(); exercises = []; selectedId = undefined; sourceNote = ''; undo = null; version++;
        field('Date').value = localDay; field('ClientRequestId').value = crypto.randomUUID();
        root.querySelector('.record-optional').open = false; feedback.hidden = true; render(); save();
    });
    const reveal = input => {
        if (review.open) review.close();
        const card = input.closest('[data-exercise-id]'); if (card) select(card.dataset.exerciseId);
        for (let parent=input.parentElement; parent && parent!==form; parent=parent.parentElement)
            if (parent instanceof HTMLDetailsElement) parent.open=true;
        input.focus(); input.reportValidity();
    };
    function validate() {
        syncResults();
        for (const input of list.querySelectorAll('[data-set-field="weight"]'))
            input.setCustomValidity(input.value.trim()!=='' && Number.isFinite(number(input.value)) && number(input.value)>=0 && number(input.value)<=10000 ? '' : 'Введите вес от 0 до 10000 кг. Для собственного веса укажите 0.');
        const invalid = [...form.querySelectorAll('input:invalid,textarea:invalid')].find(input => !input.matches('[data-batch-weight],[data-batch-reps]'));
        if (invalid) { message('Проверьте выделенное поле.'); reveal(invalid); return null; }
        if (!exercises.length) { message('Добавьте хотя бы одно упражнение.'); return null; }
        if (exercises.some(e => !library.byId.has(e.exerciseId))) { message('Упражнение недоступно. Удалите его и выберите другое из библиотеки.'); return null; }
        const payload = exercises.map(e => ({exerciseId:e.exerciseId,sets:e.sets.map(s => ({weight:number(s.weight),reps:number(s.reps),rir:s.rir===''||s.rir==null?null:number(s.rir),isWarmup:!!s.isWarmup,notes:s.notes||null}))}));
        if (!payload.some(e => e.sets.some(s => !s.isWarmup))) { message('Добавьте хотя бы один рабочий подход. Сейчас указана только разминка.'); return null; }
        return payload;
    }
    form.addEventListener('submit', event => {
        event.preventDefault();
        if (submitting || loading) return;
        const payload = validate(); if (!payload) return;
        if (!event.submitter?.hasAttribute('data-record-confirm')) {
            review.querySelector('[data-record-review-status]').hidden = true;
            const sets = payload.flatMap(e => e.sets), working = sets.filter(s => !s.isWarmup);
            const date = new Intl.DateTimeFormat('ru-RU',{day:'numeric',month:'long',year:'numeric'}).format(new Date(field('Date').value+'T12:00:00'));
            review.querySelector('[data-record-review-content]').innerHTML = `<h3>${escape(field('Title').value)}</h3><p class="record-review-date">${escape(date)} · ${field('DurationMinutes').value?escape(field('DurationMinutes').value)+' мин':'Длительность не указана'}</p><div class="record-review-stats"><div><span>Рабочих подходов</span><strong>${working.length}</strong></div><div><span>Объём, кг</span><strong>${format(working.reduce((sum,s) => sum+s.weight*s.reps,0))}</strong></div></div><ul class="record-review-list">${payload.map(e => `<li><strong>${escape(library.byId.get(e.exerciseId).dataset.name)}</strong><span>${e.sets.length} подх.</span></li>`).join('')}</ul>${sets.some(s => s.isWarmup)?`<p class="record-review-date">Разминочных подходов: ${sets.length-working.length}</p>`:''}`;
            save(); review.showModal(); return;
        }
        if (!navigator.onLine) {
            save();
            const notice = review.querySelector('[data-record-review-status]'); notice.hidden = false;
            notice.textContent = 'Нет соединения. Черновик сохранён на этом устройстве. Отправьте запись, когда появится сеть.';
            if (!storageAvailable) notice.textContent = 'Нет соединения, и браузер не сохранил черновик. Оставьте страницу открытой и повторите отправку после подключения.';
            return;
        }
        field('ResultsJson').value = JSON.stringify(payload); field('UtcOffsetMinutes').value = -new Date().getTimezoneOffset(); save();
        submitting = true; confirm.disabled = true; confirm.textContent = 'Сохраняем…';
        // Keep the existing server form, antiforgery and request identity.
        HTMLFormElement.prototype.submit.call(form);
    });
    window.addEventListener('beforeunload', event => { if (dirty && !storageAvailable && !submitting) { event.preventDefault(); event.returnValue=''; } });
    window.addEventListener('pageshow', () => { submitting=false; confirm.disabled=false; confirm.textContent='Сохранить в историю'; });
    let viewportHeight = window.visualViewport?.height || innerHeight;
    const keyboardLayout = () => {
        const viewport = window.visualViewport; if (!viewport) return;
        const input = document.activeElement;
        const editing = matchMedia('(max-width:1023px)').matches && form.contains(input) && input?.matches('input:not([type="hidden"]),textarea');
        const inset = Math.max(0,innerHeight-viewport.height-viewport.offsetTop);
        const open = editing && (inset>120 || viewportHeight-viewport.height>120);
        document.body.classList.toggle('record-keyboard-open',!!open);
        const bounds = form.getBoundingClientRect();
        root.style.setProperty('--record-keyboard-inset',`${open?inset:0}px`);
        root.style.setProperty('--record-save-left',`${bounds.left}px`); root.style.setProperty('--record-save-width',`${bounds.width}px`);
        if (open) {
            const inputBounds = input.getBoundingClientRect(), bar = root.querySelector('.record-savebar').getBoundingClientRect();
            if (inputBounds.bottom>bar.top-16) window.scrollBy({top:inputBounds.bottom-bar.top+16,behavior:'instant'});
        }
        if (!editing) viewportHeight=viewport.height;
    };
    window.visualViewport?.addEventListener('resize',keyboardLayout); window.visualViewport?.addEventListener('scroll',keyboardLayout);
    document.addEventListener('focusin',keyboardLayout); document.addEventListener('focusout',() => setTimeout(keyboardLayout,0));
    render();
    if (restored) status.textContent='Черновик восстановлен. Проверьте результаты перед сохранением.';
    const invalidServerField = root.querySelector('.input-validation-error:not([type="hidden"])') ||
        (serverError ? [...form.querySelectorAll('input:invalid,textarea:invalid')].find(input => input.type!=='hidden') : null);
    if (invalidServerField) reveal(invalidServerField);
})();
