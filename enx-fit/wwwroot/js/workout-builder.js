(() => {
    'use strict';
    const form = document.getElementById('workout-builder');
    if (!form) return;
    const $ = id => document.getElementById(id);
    const data = JSON.parse($('wb-data').textContent);
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const number = value => new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 1 }).format(value);
    const plural = (n, forms) => `${n} ${forms[n % 100 >= 11 && n % 100 <= 19 ? 2 : n % 10 === 1 ? 0 : n % 10 >= 2 && n % 10 <= 4 ? 1 : 2]}`;
    const exerciseCount = n => plural(n, ['упражнение', 'упражнения', 'упражнений']);
    const setCount = n => plural(n, ['подход', 'подхода', 'подходов']);
    const paths = {
        plus: '<path d="M12 5v14M5 12h14"/>', close: '<path d="m6 6 12 12M6 18 18 6"/>',
        check: '<path d="m5 12 4 4L19 6"/>', chevron: '<path d="m9 5 7 7-7 7"/>',
        workout: '<path d="M3 9v6m3-9v12m12-12v12m3-9v6M6 12h12"/>',
        grip: '<path d="M8 5h.01M16 5h.01M8 12h.01M16 12h.01M8 19h.01M16 19h.01" stroke-width="3"/>',
        star: '<path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1 6.2-5.5-2.9L6.5 20l1-6.2L3 9.6l6.2-.9Z"/>',
        up: '<path d="M12 20V4m-6 6 6-6 6 6"/>', down: '<path d="M12 4v16m-6-6 6 6 6-6"/>',
        bars: '<path d="M5 20v-6m7 6V8m7 12V3" stroke-width="3"/>',
        layers: '<path d="m12 3 9 5-9 5-9-5Zm-9 10 9 5 9-5M3 18l9 5 9-5"/>',
        trophy: '<path d="M7 3h10v7a5 5 0 0 1-10 0Zm0 2H3v3a4 4 0 0 0 4 4m10-7h4v3a4 4 0 0 1-4 4m-5 3v6m-5 0h10"/>',
        clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
        info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v6m0-10v.1"/>',
        chest: '<circle cx="12" cy="4" r="2"/><path d="m8 8 4 2 4-2M8 8l-4 3-2-3m14 0 4 3 2-3M8 8v8l4 2 4-2V8M9 18l-1 4m7-4 1 4M12 10v6"/>',
        legs: '<circle cx="12" cy="3" r="2"/><path d="m9 7-1 7 5 2-2 6m4-15 1 7 4 3-3 5M8 9l-4 3m12-3 4 3M9 7h6"/>',
        back: '<circle cx="12" cy="4" r="2"/><path d="M3 3v5l5 3v6h8v-6l5-3V3M8 10l4 3 4-3m-4 3v4m-3 0-1 5m7-5 1 5M1 2h22"/>',
        shoulders: '<circle cx="12" cy="5" r="2"/><path d="M8 10h8v7H8Zm0 1L4 8V3m12 8 4-3V3M2 3h4m12 0h4M9 17l-1 5m7-5 1 5"/>',
        core: '<circle cx="12" cy="4" r="2"/><path d="M8 9h8l1 8H7l1-8Zm4 0v8m-3 1-1 4m7-4 1 4"/>'
    };
    const icon = name => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths[name] || paths.workout}</svg>`;
    // Localized labels for the existing seeded catalog; user-created exercise names remain unchanged.
    const catalogLabels = {
        'Bench Press': ['Жим лёжа', 'Грудь', 'Штанга'], 'Squat': ['Приседания со штангой', 'Ноги', 'Штанга'],
        'Deadlift': ['Становая тяга', 'Спина', 'Штанга'], 'Pull Up': ['Подтягивания', 'Спина', 'Турник'],
        'Overhead Press': ['Жим над головой', 'Плечи', 'Штанга'], 'Romanian Deadlift': ['Румынская тяга', 'Ноги', 'Штанга'],
        'Leg Press': ['Жим ногами', 'Ноги', 'Тренажёр'], 'Barbell Row': ['Тяга штанги в наклоне', 'Спина', 'Штанга']
    };
    const catalog = data.exercises.map(e => {
        const labels = catalogLabels[e.name];
        return { ...e, originalName: e.name, name: labels?.[0] || e.name, muscleGroup: e.muscleGroup || labels?.[1] || 'Другое', equipment: e.equipment || labels?.[2] || '' };
    });
    const byId = new Map(catalog.map(e => [e.id, e]));
    const muscleIcons = {Грудь:'chest',Спина:'back',Поясница:'back',Трапеции:'back',Плечи:'shoulders',Ноги:'legs',Квадрицепсы:'legs','Задняя поверхность бедра':'legs',Ягодицы:'legs',Икры:'legs','Приводящие мышцы бедра':'legs','Отводящие мышцы бедра':'legs',Пресс:'core','Косые мышцы живота':'core','Мышцы кора':'core',Кардио:'bars'};
    const thumb = e => `<div class="wb-thumb" data-group="${escape(e.muscleGroup)}" aria-hidden="true">${icon(muscleIcons[e.muscleGroup] || 'workout')}</div>`;
    const key = `enix-workout-draft:${form.dataset.viewer}:${data.ownerId}`;
    const favoritesKey = `enix-workout-favorites:${form.dataset.viewer}`;
    const read = key => { try { return JSON.parse(localStorage.getItem(key)); } catch { return null; } };
    const write = (key, value) => { try { localStorage.setItem(key, JSON.stringify(value)); return true; } catch { return false; } };
    const remove = key => { try { localStorage.removeItem(key); } catch { /* Storage can be disabled. */ } };
    const storedFavorites = read(favoritesKey);
    const favorites = new Set(Array.isArray(storedFavorites) ? storedFavorites.filter(id => byId.has(id)) : []);
    let state = { goal:'Набор массы', level:'intermediate', durationMinutes:60, cover:'athlete', autoDuration:true, showRecords:true,
        blocks:[{name:'Разминка',kind:'warmup',exercises:[]},{name:'Силовой блок',kind:'strength',exercises:[]},{name:'Заминка',kind:'cooldown',exercises:[]}] };
    let activeBlock = 1, tab = 'all', group = '', reorder = false, dirty = false, saving = false, dragId = null, lastLibraryTrigger;
    const collapsed = new Set();
    let toastTimer;
    function toast(message) { $('wb-live').textContent = message; $('wb-live').hidden = false; clearTimeout(toastTimer); toastTimer = setTimeout(() => { $('wb-live').hidden = true; }, 4200); }
    const all = () => state.blocks.flatMap(b => b.exercises);
    const find = id => all().find(e => e.exerciseId === id);
    const selected = id => all().some(e => e.exerciseId === id);
    const emptyInsight = (name, title, text) => `<div class="wb-insight-empty">${icon(name)}<div><strong>${escape(title)}</strong><p>${escape(text)}</p></div></div>`;
    function loadState(value) {
        if (!value || !Array.isArray(value.blocks) || !value.blocks.length || value.blocks.length > 12) return false;
        const ids = new Set();
        for (const b of value.blocks) {
            if (!b || typeof b.name !== 'string' || !b.name.trim() || b.name.length > 60 || !['warmup','strength','superset','cooldown'].includes(b.kind) || !Array.isArray(b.exercises)) return false;
            for (const e of b.exercises) {
                if (!e || ids.has(e.exerciseId) || !byId.has(e.exerciseId) || !Array.isArray(e.sets) || !e.sets.length || e.sets.length > 20) return false;
                ids.add(e.exerciseId);
                if (e.sets.some(s => !s || !Number.isFinite(s.weight) || !Number.isInteger(s.reps) || !Number.isInteger(s.restSeconds))) return false;
            }
        }
        if (ids.size > 40 || !['athlete','summit'].includes(value.cover) || typeof value.goal !== 'string' || !['beginner','intermediate','advanced'].includes(value.level) || !Number.isFinite(value.durationMinutes)) return false;
        state = value;
        activeBlock = Math.min(activeBlock, state.blocks.length - 1);
        return true;
    }
    if ($('BuilderJson').value) { try { loadState(JSON.parse($('BuilderJson').value)); } catch { /* Server validation remains visible. */ } }
    function syncControls() {
        $('wb-goal').value = state.goal; $('wb-level').value = state.level;
        $('wb-duration').value = state.durationMinutes; $('wb-auto-duration').checked = state.autoDuration;
        $('wb-show-records').checked = state.showRecords;
        $('wb-cover-image').src = `/images/landing/${state.cover}.webp`;
        $('wb-cover-image').alt = state.cover === 'athlete' ? 'Атлет в зале' : 'Горная вершина';
    }
    function renderBlocks() {
        $('wb-blocks').innerHTML = state.blocks.map((b,i) => `<div class="wb-block ${i === activeBlock ? 'active' : ''}" data-block="${i}" data-kind="${b.kind}">
            <div class="wb-block-header"><button type="button" data-select-block="${i}"><span>${i+1}.</span>${escape(b.name)}</button><button type="button" class="wb-icon" data-block-up="${i}" aria-label="Поднять блок ${escape(b.name)}" ${i === 0 ? 'disabled' : ''}>${icon('up')}</button><button type="button" class="wb-icon wb-danger" data-remove-block="${i}" aria-label="Удалить блок ${escape(b.name)}" ${b.exercises.length || state.blocks.length === 1 ? 'disabled' : ''}>${icon('close')}</button></div>
            <div class="wb-block-meta">${exerciseCount(b.exercises.length)} · ${setCount(b.exercises.reduce((n,e) => n+e.sets.length,0))}</div>
            ${b.exercises.map(e => `<div class="wb-structure-exercise" draggable="true" data-drag-id="${e.exerciseId}">${icon('grip')}<span>${escape(byId.get(e.exerciseId).name)}</span><small>${setCount(e.sets.length)}</small></div>`).join('')}
            <button type="button" class="wb-block-add" data-open-library="${i}">${icon('plus')}Добавить упражнение</button></div>`).join('');
        $('wb-library-target').innerHTML = state.blocks.map((b,i) => `<option value="${i}" ${i === activeBlock ? 'selected' : ''}>${escape(b.name)}</option>`).join('');
    }
    function renderExercises() {
        let order = 0;
        if (!all().length) {
            $('wb-exercises').innerHTML = `<div class="wb-empty"><div class="wb-empty-mark">${icon('workout')}</div><h3>Сильная тренировка<br>начинается здесь</h3><p>Выбери упражнения в библиотеке, настрой подходы и собери свой следующий шаг к цели.</p><button type="button" class="wb-button wb-primary" data-quick-start>${icon('plus')}Быстрый старт · Всё тело</button><small>3 базовых упражнения · веса выбираешь ты</small></div>`;
            return;
        }
        $('wb-exercises').innerHTML = state.blocks.map((b,bi) => !b.exercises.length ? '' : `<section class="wb-exercise-group ${b.kind === 'superset' ? 'is-superset' : ''}" aria-label="${escape(b.name)}"><div class="wb-group-title" data-kind="${b.kind}"><span>${escape(b.name)}${b.kind === 'superset' ? ' · суперсет' : ''}</span><span>${exerciseCount(b.exercises.length)}</span></div>${b.exercises.map((e,ei) => {
            const item = byId.get(e.exerciseId), id = e.exerciseId, closed = collapsed.has(id);
            return `<article class="wb-exercise-card ${closed ? 'collapsed' : ''}" data-exercise="${id}">
                <div class="wb-exercise-head"><span class="wb-number">${++order}</span>${thumb(item)}<div class="wb-exercise-copy"><strong>${escape(item.name)}</strong><small>${escape(item.muscleGroup)}${item.equipment ? ' · '+escape(item.equipment) : ''}</small></div><button type="button" class="wb-icon wb-chevron" data-collapse="${id}" aria-expanded="${!closed}" aria-controls="wb-body-${id}" aria-label="Подходы: ${escape(item.name)}">${icon('chevron')}</button><button type="button" class="wb-icon wb-danger" data-remove-exercise="${id}" aria-label="Удалить ${escape(item.name)}">${icon('close')}</button></div>
                <div class="wb-exercise-body" id="wb-body-${id}"><table class="wb-set-table"><thead><tr><th scope="col">Подход</th><th scope="col">Вес (кг)</th><th scope="col">Повторы</th><th scope="col">Отдых, с</th><th scope="col"><span class="wb-sr-only">Удалить</span></th></tr></thead><tbody>${e.sets.map((s,si) => `<tr><td>${si+1}</td>${[['weight',0,2000,'0.01','Вес'],['reps',1,1000,'1','Повторы'],['restSeconds',0,900,'1','Отдых']].map(([field,min,max,step,label]) => `<td><input type="number" required inputmode="${field === 'weight' ? 'decimal' : 'numeric'}" min="${min}" max="${max}" step="${step}" value="${s[field]}" data-set="${si}" data-field="${field}" data-id="${id}" aria-label="${escape(item.name)}, подход ${si+1}: ${label}" /></td>`).join('')}<td><button type="button" class="wb-icon wb-danger" data-remove-set="${id}:${si}" ${e.sets.length === 1 ? 'disabled' : ''} aria-label="Удалить подход ${si+1}: ${escape(item.name)}">${icon('close')}</button></td></tr>`).join('')}</tbody></table>
                <button type="button" class="wb-add-set" data-add-set="${id}" ${e.sets.length >= 20 ? 'disabled' : ''}>${icon('plus')}Добавить подход</button></div>
                <div class="wb-exercise-tools" ${reorder ? '' : 'hidden'}><button class="wb-icon" type="button" data-move="${id}:-1" ${ei === 0 ? 'disabled' : ''} aria-label="Поднять ${escape(item.name)}">${icon('up')}</button><button class="wb-icon" type="button" data-move="${id}:1" ${ei === b.exercises.length-1 ? 'disabled' : ''} aria-label="Опустить ${escape(item.name)}">${icon('down')}</button><label>Блок<select data-move-to="${id}">${state.blocks.map((block,i) => `<option value="${i}" ${i === bi ? 'selected' : ''}>${escape(block.name)}</option>`).join('')}</select></label></div></article>`;
        }).join('')}</section>`).join('');
    }
    function renderLibrary() {
        const query = $('wb-search').value.trim().toLocaleLowerCase('ru');
        const items = catalog.filter(e => (!group || e.muscleGroup === group) && (tab !== 'favorites' || favorites.has(e.id)) && (tab !== 'recent' || data.recentIds.includes(e.id)) && `${e.name} ${e.originalName} ${e.muscleGroup} ${e.equipment}`.toLocaleLowerCase('ru').includes(query));
        $('wb-library-count').textContent = String(items.length);
        $('wb-library-list').innerHTML = items.map(e => `<div class="wb-library-item">${thumb(e)}<div class="wb-exercise-copy"><strong>${escape(e.name)}</strong><small>${escape(e.muscleGroup)} · ${escape(e.equipment)}</small></div><a class="wb-icon" href="/Exercises/Details/${e.id}" target="_blank" rel="noopener" aria-label="Оборудование и описание: ${escape(e.name)} (откроется в новой вкладке)" title="Оборудование и описание">${icon('info')}</a><button class="wb-favorite ${favorites.has(e.id) ? 'active' : ''}" type="button" data-favorite="${e.id}" aria-pressed="${favorites.has(e.id)}" aria-label="В избранное: ${escape(e.name)}">${icon('star')}</button><button type="button" class="wb-icon" data-add="${e.id}" ${selected(e.id) ? 'disabled' : ''} aria-label="${selected(e.id) ? 'Добавлено' : 'Добавить'}: ${escape(e.name)}">${icon(selected(e.id) ? 'check' : 'plus')}</button></div>`).join('') || `<p class="wb-library-empty">${tab === 'favorites' && !query ? 'Отметь упражнения звёздочкой — они появятся здесь.' : tab === 'recent' && !query ? 'Здесь появятся упражнения из завершённых тренировок.' : 'Упражнений не найдено. Попробуй другую группу или запрос.'}</p>`;
    }
    function updateStats() {
        const entries = all(), sets = entries.flatMap(e => e.sets);
        const working = state.blocks.filter(b => b.kind !== 'warmup' && b.kind !== 'cooldown').flatMap(b => b.exercises);
        const workingSets = working.flatMap(e => e.sets);
        const volume = workingSets.reduce((sum,s) => sum + s.weight*s.reps,0);
        const duration = Math.max(5, Math.min(360, Math.ceil((sets.reduce((sum,s) => sum+s.reps*4+s.restSeconds,0)+entries.length*60)/60)));
        if (state.autoDuration && entries.length) state.durationMinutes = duration;
        $('wb-duration').value = state.durationMinutes; $('wb-duration').readOnly = state.autoDuration && !!entries.length;
        const muscles = [...new Set(entries.map(e => byId.get(e.exerciseId).muscleGroup))];
        $('wb-volume').innerHTML = `${number(volume)} <small>кг</small>`;
        $('wb-exercise-count').textContent = exerciseCount(entries.length);
        $('wb-forecast-count').textContent = entries.length ? `Выбрано: ${exerciseCount(entries.length)}` : 'Начни с первого упражнения';
        $('wb-focus').textContent = muscles.join(', ') || 'Твой выбор';
        $('wb-muscle-tags').innerHTML = muscles.length ? muscles.map(m => `<span class="wb-tag">${escape(m)}</span>`).join('') : '<span>Выбери упражнения — соберём фокус тренировки</span>';
        $('wb-mobile-summary').textContent = `${plural(state.blocks.length,['блок','блока','блоков'])} · ${setCount(sets.length)} · ~ ${entries.length ? state.durationMinutes : 0} мин`;
        const load = Math.min(10, Math.ceil(workingSets.length/3));
        $('wb-difficulty').innerHTML = `${load || '—'}<small>/10</small>`;
        $('wb-difficulty-meter').style.width = `${load*10}%`;
        $('wb-difficulty-note').textContent = load ? load < 4 ? 'Небольшой объём' : load < 7 ? 'Умеренный объём' : 'Высокий объём' : 'Оценка по числу подходов';
        const calories = entries.length && data.bodyWeight > 0 ? Math.round(state.durationMinutes*5*data.bodyWeight/60/10)*10 : null;
        $('wb-calories').innerHTML = `${calories === null ? '—' : '~ '+number(calories)} <small>ккал</small>`;
        $('wb-calories-note').textContent = !entries.length ? 'Добавь упражнения' : !data.bodyWeight ? 'Нужен вес из профиля' : 'Оценка по весу и времени';
        const previous = data.previous;
        $('wb-volume-change').textContent = previous?.volume > 0 && entries.length ? `${volume >= previous.volume ? '+' : ''}${number((volume/previous.volume-1)*100)}% к прошлой` : 'Без разминки и заминки';
        $('wb-comparison').innerHTML = !previous ? emptyInsight('bars','Новая точка отсчёта','После первой завершённой тренировки здесь появится сравнение объёма и подходов.') : `<div class="wb-deltas">${[['workout','Упражнения',entries.length,previous.exercises],['layers','Подходы',workingSets.length,previous.sets],['bars','Общий объём',volume,previous.volume]].map(([ic,label,value,old]) => `<div class="wb-delta">${icon(ic)}<span>${label}</span><strong>${value-old > 0 ? '+' : ''}${label === 'Общий объём' && old > 0 ? number((value/old-1)*100)+'%' : number(value-old)}</strong><small>Было ${number(old)} → ${number(value)}</small></div>`).join('')}</div>`;
        const records = state.showRecords ? entries.map(e => ({...e, record:data.records.find(r => r.exerciseId === e.exerciseId)?.weight || 0})).filter(e => e.record > 0) : [];
        $('wb-records').innerHTML = !state.showRecords ? emptyInsight('trophy','Сравнение с рекордами выключено','Включи его в настройках тренировки.') : !records.length ? emptyInsight('trophy','Твои рекорды ещё впереди','Выбери упражнение с записанными результатами — покажем твой лучший вес за последние 50 тренировок.') : records.slice(0,3).map(e => {
            const planned = Math.max(...e.sets.map(s => s.weight)), gain = planned-e.record;
            return `<div class="wb-record">${thumb(byId.get(e.exerciseId))}<div class="wb-exercise-copy"><strong>${escape(byId.get(e.exerciseId).name)}</strong><small>Лучший вес: ${number(e.record)} кг</small></div><strong>${gain > 0 ? '+'+number(gain) : number(e.record)} кг<small>${gain > 0 ? 'Возможный новый рекорд' : 'Твой ориентир'}</small></strong></div>`;
        }).join('');
        $('BuilderJson').value = JSON.stringify(state);
    }
    function render() { renderBlocks(); renderExercises(); renderLibrary(); updateStats(); }
    function changed() { dirty = true; updateStats(); }
    function mutate() { dirty = true; render(); }
    function addExercise(id) {
        if (selected(id) || !byId.has(id)) return;
        if (all().length >= 40) { toast('В одной тренировке может быть до 40 упражнений.'); return; }
        state.blocks[activeBlock].exercises.push({exerciseId:id,sets:Array.from({length:3},() => ({weight:0,reps:12,restSeconds:90}))});
        if (matchMedia('(max-width:760px)').matches && all().length > 1) collapsed.add(id);
        mutate(); toast(`${byId.get(id).name} — добавлено`);
    }
    function openLibrary(trigger, index) {
        if (Number.isInteger(index) && state.blocks[index]) activeBlock = index;
        renderBlocks(); lastLibraryTrigger = trigger;
        if (matchMedia('(max-width:760px)').matches) {
            $('wb-library').classList.add('open'); $('wb-library').setAttribute('role','dialog'); $('wb-library').setAttribute('aria-modal','true');
            document.body.style.overflow = 'hidden';
            [...form.children].filter(el => !el.contains($('wb-library')) && el.id !== 'wb-live').forEach(el => { el.inert = true; });
            document.querySelectorAll('.wb-structure,.wb-editor').forEach(el => { el.inert = true; });
        } else $('wb-library').scrollIntoView({behavior:'smooth',block:'nearest'});
        $('wb-search').focus();
    }
    function closeLibrary() {
        $('wb-library').classList.remove('open'); $('wb-library').removeAttribute('role'); $('wb-library').removeAttribute('aria-modal'); document.body.style.overflow = '';
        form.querySelectorAll('[inert]').forEach(el => { el.inert = false; }); lastLibraryTrigger?.focus();
    }
    function moveTo(id, index) {
        const source = state.blocks.find(b => b.exercises.some(e => e.exerciseId === id));
        if (!source || !state.blocks[index] || source === state.blocks[index]) return;
        const entry = source.exercises.find(e => e.exerciseId === id);
        source.exercises = source.exercises.filter(e => e !== entry); state.blocks[index].exercises.push(entry); activeBlock = index; mutate();
    }
    form.addEventListener('click', event => {
        const button = event.target.closest('button'); if (!button) return;
        const d = button.dataset;
        if ('add' in d) addExercise(Number(d.add));
        if ('favorite' in d) { const id = Number(d.favorite); favorites.has(id) ? favorites.delete(id) : favorites.add(id); if (!write(favoritesKey,[...favorites])) toast('Браузер не разрешил сохранить избранное.'); renderLibrary(); }
        if ('libraryTab' in d) { tab = d.libraryTab; form.querySelectorAll('[data-library-tab]').forEach(b => { b.classList.toggle('active',b.dataset.libraryTab === tab); b.setAttribute('aria-pressed', String(b.dataset.libraryTab === tab)); }); renderLibrary(); }
        if ('group' in d) { group = d.group; form.querySelectorAll('[data-group-filter]').forEach(b => { b.classList.toggle('active',b.dataset.group === group); b.setAttribute('aria-pressed',String(b.dataset.group === group)); }); renderLibrary(); }
        if ('openLibrary' in d) openLibrary(button, d.openLibrary === '' ? undefined : Number(d.openLibrary));
        if ('closeLibrary' in d) closeLibrary();
        if ('selectBlock' in d) { activeBlock = Number(d.selectBlock); renderBlocks(); }
        if ('removeBlock' in d) { const i = Number(d.removeBlock); if (state.blocks.length > 1 && !state.blocks[i].exercises.length) { state.blocks.splice(i,1); activeBlock = Math.min(activeBlock,state.blocks.length-1); mutate(); } }
        if ('blockUp' in d) { const i = Number(d.blockUp); if (i > 0) { [state.blocks[i-1],state.blocks[i]] = [state.blocks[i],state.blocks[i-1]]; activeBlock = i-1; mutate(); } }
        if ('addBlock' in d) { if (state.blocks.length >= 12) return toast('Можно добавить до 12 блоков.'); $('wb-block-dialog').showModal(); $('wb-block-name').focus(); }
        if ('collapse' in d) { const id = Number(d.collapse); collapsed.has(id) ? collapsed.delete(id) : collapsed.add(id); renderExercises(); form.querySelector(`[data-collapse="${id}"]`)?.focus(); }
        if ('removeExercise' in d) { const id = Number(d.removeExercise); state.blocks.forEach(b => { b.exercises = b.exercises.filter(e => e.exerciseId !== id); }); collapsed.delete(id); mutate(); }
        if ('addSet' in d) { const e = find(Number(d.addSet)); if (e.sets.length < 20) { e.sets.push({...e.sets.at(-1)}); mutate(); form.querySelector(`[data-add-set="${e.exerciseId}"]`)?.focus(); } }
        if ('removeSet' in d) { const [id,index] = d.removeSet.split(':').map(Number), e = find(id); if (e.sets.length > 1) { e.sets.splice(index,1); mutate(); } }
        if ('reorder' in d) { reorder = !reorder; form.classList.toggle('reordering',reorder); button.setAttribute('aria-pressed', String(reorder)); renderExercises(); if (reorder) toast('Используй стрелки и список блоков под упражнением.'); }
        if ('move' in d) { const [id,delta] = d.move.split(':').map(Number), b = state.blocks.find(b => b.exercises.some(e => e.exerciseId === id)), i = b.exercises.findIndex(e => e.exerciseId === id); if (b.exercises[i+delta]) { [b.exercises[i],b.exercises[i+delta]] = [b.exercises[i+delta],b.exercises[i]]; mutate(); } }
        if ('changeCover' in d) { state.cover = state.cover === 'athlete' ? 'summit' : 'athlete'; syncControls(); changed(); }
        if ('quickStart' in d) {
            activeBlock = state.blocks.findIndex(b => b.kind === 'strength'); if (activeBlock < 0) activeBlock = 0;
            ['Squat','Bench Press','Barbell Row'].forEach(name => { const e = catalog.find(e => e.originalName === name); if (e) addExercise(e.id); });
            if (!all().length) openLibrary(button); else { if (!$('Input_Title').value) $('Input_Title').value = 'Всё тело'; toast('План готов. Укажи рабочие веса и повторы.'); }
        }
        if ('saveDraft' in d) {
            const draft = {state, title:$('Input_Title').value, date:$('Input_Date').value, notes:$('Input_Notes').value};
            if (write(key,draft)) { dirty = false; $('wb-draft-notice').hidden = true; toast('Черновик сохранён на этом устройстве.'); } else toast('Браузер не разрешил сохранить черновик. Не закрывай страницу.');
        }
        if ('restoreDraft' in d) {
            const draft = read(key);
            if (draft && loadState(draft.state)) { $('Input_Title').value = typeof draft.title === 'string' ? draft.title : ''; $('Input_Date').value = typeof draft.date === 'string' ? draft.date : ''; $('Input_Notes').value = typeof draft.notes === 'string' ? draft.notes : ''; syncControls(); render(); dirty = false; $('wb-draft-notice').hidden = true; toast('Черновик восстановлен.'); }
            else toast('Черновик устарел: состав библиотеки изменился. Создай новый план.');
        }
        if ('discardDraft' in d) { remove(key); $('wb-draft-notice').hidden = true; }
    });
    form.addEventListener('input', event => {
        const input = event.target;
        if (input.id === 'wb-search') return renderLibrary();
        if (input.dataset.field) {
            const value = input.valueAsNumber;
            if (Number.isFinite(value)) find(Number(input.dataset.id)).sets[Number(input.dataset.set)][input.dataset.field] = value;
            changed(); renderBlocks(); return;
        }
        if (input.id === 'wb-duration') { const n = input.valueAsNumber; if (Number.isFinite(n)) state.durationMinutes = n; dirty = true; $('BuilderJson').value = JSON.stringify(state); return; }
        if (['Input_Title','Input_Date','Input_Notes'].includes(input.id)) dirty = true;
    });
    form.addEventListener('change', event => {
        const input = event.target;
        if (input.id === 'wb-goal') state.goal = input.value;
        else if (input.id === 'wb-level') state.level = input.value;
        else if (input.id === 'wb-auto-duration') state.autoDuration = input.checked;
        else if (input.id === 'wb-show-records') state.showRecords = input.checked;
        else if (input.id === 'wb-duration') { if (input.validity.valid) updateStats(); return; }
        else if (input.id === 'wb-library-target') { activeBlock = Number(input.value); renderBlocks(); return; }
        else if (input.dataset.moveTo) return moveTo(Number(input.dataset.moveTo),Number(input.value));
        else if (input.id === 'Input_OwnerId') { // Reload owner-specific history without mixing accounts.
            if (dirty && !confirm('Сменить пользователя? Несохранённый план будет сброшен.')) { input.value = data.ownerId; return; }
            dirty = false; location.assign(`/Workouts/Create?userId=${encodeURIComponent(input.value)}`); return;
        } else return;
        changed();
    });
    $('wb-block-form').addEventListener('submit', event => {
        event.preventDefault(); const name = $('wb-block-name').value.trim(); if (!name) return;
        state.blocks.push({name,kind:$('wb-block-kind').value,exercises:[]}); activeBlock = state.blocks.length-1;
        $('wb-block-dialog').close(); $('wb-block-form').reset(); mutate(); toast('Блок добавлен. Выбери для него упражнения.');
    });
    document.querySelector('[data-close-block]').addEventListener('click', () => $('wb-block-dialog').close());
    $('wb-block-kind').addEventListener('change', () => { if (!$('wb-block-name').value) $('wb-block-name').value = $('wb-block-kind').selectedOptions[0].text; });
    form.addEventListener('dragstart', e => { const row = e.target.closest('[data-drag-id]'); if (row) { dragId = Number(row.dataset.dragId); e.dataTransfer.setData('text/plain',String(dragId)); e.dataTransfer.effectAllowed = 'move'; } });
    form.addEventListener('dragover', e => { const block = e.target.closest('[data-block]'); if (block && dragId) { e.preventDefault(); block.classList.add('drag-over'); } });
    form.addEventListener('dragleave', e => e.target.closest('[data-block]')?.classList.remove('drag-over'));
    form.addEventListener('drop', e => { const block = e.target.closest('[data-block]'); if (block && dragId) { e.preventDefault(); moveTo(dragId,Number(block.dataset.block)); } dragId = null; });
    form.addEventListener('dragend', () => { dragId = null; form.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over')); });
    document.addEventListener('keydown', e => {
        if (!$('wb-library').classList.contains('open') || $('wb-block-dialog').open) return;
        if (e.key === 'Escape') { e.preventDefault(); closeLibrary(); }
        if (e.key === 'Tab') {
            const focusable = [...$('wb-library').querySelectorAll('button:not(:disabled),input,select')].filter(el => el.getClientRects().length);
            const first = focusable[0], last = focusable.at(-1);
            if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
            else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        }
    });
    matchMedia('(max-width:760px)').addEventListener('change', () => { if ($('wb-library').classList.contains('open')) closeLibrary(); });
    window.addEventListener('beforeunload', e => { if (dirty && !saving) { e.preventDefault(); e.returnValue = ''; } });
    form.addEventListener('invalid', event => {
        const card = event.target.closest('.wb-exercise-card');
        if (card) { collapsed.delete(Number(card.dataset.exercise)); card.classList.remove('collapsed'); card.querySelector('[data-collapse]').setAttribute('aria-expanded','true'); }
    }, true);
    form.addEventListener('submit', async event => {
        event.preventDefault(); if (saving) return;
        if (!all().length) { toast('Добавь хотя бы одно упражнение.'); openLibrary(form.querySelector('[data-open-library]')); return; }
        if (!form.reportValidity()) return;
        $('BuilderJson').value = JSON.stringify(state);
        saving = true; const buttons = form.querySelectorAll('button[type=submit]'); buttons.forEach(b => { b.disabled = true; });
        try {
            const response = await fetch(form.action, {method:'POST',body:new FormData(form),credentials:'same-origin'});
            if (response.redirected) {
                const destination = new URL(response.url);
                if (destination.origin === location.origin && destination.pathname.startsWith('/Workouts/Details/')) { remove(key); dirty = false; location.assign(destination.href); return; }
                toast('Сессия завершилась. Сохрани черновик и войди в аккаунт снова.');
            } else if (response.ok) {
                const html = new DOMParser().parseFromString(await response.text(),'text/html');
                const errors = [...html.querySelectorAll('.wb-validation li')].map(el => el.textContent.trim()).filter(Boolean);
                const summary = form.querySelector('.wb-validation'); summary.classList.remove('validation-summary-valid'); summary.textContent = errors.join(' ') || 'Не удалось сохранить тренировку. Проверь введённые данные.';
                summary.scrollIntoView({behavior:'smooth',block:'center'}); toast('Проверь параметры тренировки.');
            } else toast('Не удалось сохранить тренировку. План остался на странице — сохрани черновик.');
        } catch { toast('Связь прервалась. Сохрани черновик и проверь список тренировок перед повторной отправкой.'); }
        saving = false; buttons.forEach(b => { b.disabled = false; });
    });
    $('wb-filters').innerHTML = ['',...new Set(catalog.map(e => e.muscleGroup))].map(g => `<button type="button" data-group-filter data-group="${escape(g)}" class="${g ? '' : 'active'}" aria-pressed="${!g}">${escape(g || 'Все группы')}</button>`).join('');
    $('wb-draft-notice').hidden = !read(key) || data.hasErrors;
    syncControls(); render();
})();
