(() => {
    'use strict';
    const exerciseId = window.ExerciseIds.normalize;
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const plural = n => `${n} ${n % 100 >= 11 && n % 100 <= 19 ? 'упражнений' : n % 10 === 1 ? 'упражнение' : n % 10 >= 2 && n % 10 <= 4 ? 'упражнения' : 'упражнений'}`;
    const setCount = n => `${n} ${n % 100 >= 11 && n % 100 <= 19 ? 'подходов' : n % 10 === 1 ? 'подход' : n % 10 >= 2 && n % 10 <= 4 ? 'подхода' : 'подходов'}`;
    class ExerciseLibrary {
        constructor(root) {
            this.root = root; root.exerciseLibrary = this;
            this.rows = [...root.querySelectorAll('[data-exercise-id]')];
            this.byId = new Map(this.rows.map(row => [exerciseId(row.dataset.exerciseId), row]));
            this.search = root.querySelector('[data-library-search]'); this.group = root.querySelector('[data-library-group]');
            this.equipment = root.querySelector('[data-library-equipment]');
            this.pending = new Map(); this.added = new Set(); this.tab = 'all'; this.single = false; this.busy = false; this.editing = null;
            this.results = root.dataset.prescription === 'results';
            this.key = `enix-exercise-favorites:${root.dataset.viewer}:live`; this.legacyKey = `enix-workout-favorites:${root.dataset.viewer}`; this.favorites = this.readFavorites();
            this.search.addEventListener('input', () => { if (this.search.value.trim()) this.tab = 'all'; this.filter(); });
            this.search.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
            this.group.addEventListener('change', () => this.filter());
            this.equipment.addEventListener('change', () => this.filter());
            root.addEventListener('click', event => this.click(event));
            const updatePick = event => {
                const input = event.target;
                if (!input.dataset.pickField) return;
                const selection = this.pending.get(exerciseId(input.closest('[data-exercise-id]').dataset.exerciseId));
                if (selection) { selection[input.dataset.pickField] = input.value; this.renderPrescription(input.closest('[data-exercise-id]'), selection); this.renderTray(); }
            };
            root.addEventListener('input', updatePick); root.addEventListener('change', updatePick);
            window.addEventListener('storage', event => { if (event.key === this.key) { this.favorites = this.readFavorites(); this.filter(); } });
            this.setSelected(this.rows.filter(row => row.querySelector('[data-library-add]').disabled).map(row => exerciseId(row.dataset.exerciseId)));
            this.tab = this.rows.some(row => row.dataset.recent === 'true' && !this.added.has(exerciseId(row.dataset.exerciseId))) ? 'recent' :
                [...this.favorites].some(id => !this.added.has(id)) ? 'favorites' : 'all';
            this.filter();
            let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
            const keyboardLayout = () => {
                const viewport = window.visualViewport; if (!viewport) return;
                if (viewportWidth !== innerWidth) { viewportWidth = innerWidth; viewportHeight = viewport.height; }
                const interacting = root.contains(document.activeElement);
                const focused = interacting && document.activeElement.matches('input,select');
                const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
                const open = matchMedia('(max-width:1023px)').matches && interacting && (focused || root.dataset.keyboardOpen === 'true') &&
                    (inset > 120 || viewportHeight - viewport.height > 120);
                root.dataset.keyboardOpen = String(open);
                const surface = root.closest('.exercise-picker-dialog') || root;
                surface.style.setProperty('--el-viewport-height', `${viewport.height}px`);
                surface.style.setProperty('--el-viewport-bottom', `${open ? inset : 0}px`);
                surface.dataset.libraryKeyboardOpen = String(open);
                if (open) requestAnimationFrame(() => document.activeElement?.closest('[data-pick-settings]')?.scrollIntoView({block:'nearest',behavior:'instant'}));
                if (!interacting) viewportHeight = viewport.height;
            };
            window.visualViewport?.addEventListener('resize', keyboardLayout); window.visualViewport?.addEventListener('scroll', keyboardLayout);
            document.addEventListener('focusin', keyboardLayout); document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
        }
        readFavorites() {
            const read = key => { try { const value = JSON.parse(localStorage.getItem(key)); return Array.isArray(value) ? value : []; } catch { return []; } };
            const legacy = read(this.legacyKey), ids = new Set([...read(this.key), ...legacy].map(exerciseId).filter(id => this.byId.has(id)));
            if (legacy.length) try { localStorage.setItem(this.key, JSON.stringify([...ids])); localStorage.removeItem(this.legacyKey); } catch { this.message('Браузер не разрешил перенести избранное.'); }
            return ids;
        }
        icon(id) { return this.byId.get(exerciseId(id))?.querySelector('[data-library-icon]')?.innerHTML || ''; }
        setContext(name) { this.root.querySelector('[data-library-context]').textContent = name || 'Текущая тренировка'; }
        setSingle(single) { if (this.single !== single) this.clear(); this.single = single; this.root.dataset.single = String(single); this.renderTray(); }
        setBusy(busy) { this.busy = busy; this.root.setAttribute('aria-busy', String(busy)); this.root.querySelectorAll('[data-library-add]').forEach(button => { button.disabled = busy || this.added.has(exerciseId(button.value)); }); this.root.querySelectorAll('[data-pick-field],[data-clear-selection],[data-remove-pick]').forEach(button => button.disabled = busy); this.renderTray(); }
        message(text) { const status = this.root.querySelector('[data-library-status]'); status.textContent = text; status.hidden = !text; }
        setSelected(ids) {
            this.added = new Set(ids.map(exerciseId));
            for (const id of this.added) this.pending.delete(id);
            this.rows.forEach(row => this.renderRow(row)); this.root.querySelector('[data-library-existing]').textContent = this.added.size; this.renderTray();
        }
        clear() { this.pending.clear(); this.editing = null; this.rows.forEach(row => this.renderRow(row)); this.renderTray(); }
        click(event) {
            const button = event.target.closest('button'); if (!button || !this.root.contains(button)) return;
            if (button.hasAttribute('data-library-tab')) { this.tab = button.dataset.libraryTab; this.filter(); return; }
            if (button.hasAttribute('data-muscle-filter')) { this.group.value = button.dataset.muscleFilter; this.filter(); return; }
            if (button.hasAttribute('data-clear-search')) { this.search.value = ''; this.filter(); this.search.focus(); return; }
            if (button.hasAttribute('data-clear-filters') || button.hasAttribute('data-reset-library')) {
                this.group.value = ''; this.equipment.value = '';
                if (button.hasAttribute('data-reset-library')) { this.search.value = ''; this.tab = 'all'; }
                this.filter(); this.search.focus(); return;
            }
            if (this.busy) return;
            if (button.hasAttribute('data-clear-selection')) { this.clear(); this.search.focus(); return; }
            if (button.hasAttribute('data-remove-pick')) { const id = exerciseId(button.dataset.removePick); this.pending.delete(id); this.renderRow(this.byId.get(id)); this.renderTray(); (this.pending.size ? this.root.querySelector('[data-library-commit]') : this.search).focus(); return; }
            if (button.hasAttribute('data-library-commit')) { this.commit(); return; }
            const row = button.closest('[data-exercise-id]'); if (!row) return;
            const id = exerciseId(row.dataset.exerciseId);
            if (button.hasAttribute('data-configure-pick')) { this.editing = this.editing === id ? null : id; this.rows.forEach(item => this.renderRow(item)); return; }
            if (button.hasAttribute('data-library-favorite')) { this.favorites.has(id) ? this.favorites.delete(id) : this.favorites.add(id); try { localStorage.setItem(this.key, JSON.stringify([...this.favorites])); } catch { this.message('Браузер не разрешил сохранить избранное.'); } this.filter(); }
            if (button.hasAttribute('data-library-add') && !button.disabled) {
                this.message('');
                if (this.pending.has(id)) this.pending.delete(id);
                else {
                    if (this.single) this.clear();
                    if (this.added.size + this.pending.size >= 40 && !this.single) { this.message('В тренировке может быть до 40 упражнений.'); return; }
                    this.pending.set(id, {exerciseId:id, setsCount:'3', weight:this.results?'':'0', reps:this.results?'':'12', restSeconds:'90'});
                }
                this.editing = null;
                this.rows.forEach(item => this.renderRow(item)); this.renderTray();
            }
        }
        renderRow(row) {
            const id = exerciseId(row.dataset.exerciseId), added = this.added.has(id), pick = this.pending.get(id), button = row.querySelector('[data-library-add]');
            row.classList.toggle('is-added', added); row.classList.toggle('is-picked', !!pick);
            button.disabled = added || this.busy; button.setAttribute('aria-pressed', String(!!pick));
            button.setAttribute('aria-label', `${added ? 'Добавлено' : pick ? 'Убрать из выбора' : 'Добавить'}: ${row.dataset.name}`);
            row.querySelector('[data-added-label]').hidden = !added;
            row.querySelector('[data-add-icon]').hidden = added || !!pick; row.querySelector('[data-selected-icon]').hidden = !added && !pick;
            this.renderPrescription(row, pick);
            const settings = row.querySelector('[data-pick-settings]'); settings.hidden = !pick || this.single || this.editing !== id;
            if (!pick || this.single) { settings.innerHTML = ''; return; }
            const fields = [['setsCount','Подходы',1,20],['reps','Повторы',1,1000],['weight','Вес, кг',0,2000],...(!this.results?[['restSeconds','Отдых, с',0,900]]:[])];
            settings.innerHTML = fields.map(([key,label,min,max]) => `<label>${label}<input type="number" inputmode="${key==='weight'?'decimal':'numeric'}" min="${min}" max="${max}" step="${key==='weight'?'0.01':'1'}" data-pick-field="${key}" value="${escape(pick[key])}" ${!this.results || key==='setsCount'?'required':''} placeholder="${key==='weight'?'0':'—'}" aria-label="${escape(row.dataset.name)}: ${label}" /></label>`).join('');
        }
        renderPrescription(row, pick) {
            const button = row.querySelector('[data-configure-pick]'); button.hidden = !pick || this.single;
            button.disabled = this.busy; button.setAttribute('aria-expanded', String(this.editing === exerciseId(row.dataset.exerciseId)));
            button.setAttribute('aria-controls', row.querySelector('[data-pick-settings]').id);
            button.setAttribute('aria-label', `Настроить: ${row.dataset.name}`);
            if (pick) row.querySelector('[data-pick-summary]').textContent = this.results && (!pick.reps || pick.weight === '') ? `${setCount(Number(pick.setsCount))} · результаты после добавления` :
                `${pick.setsCount} × ${pick.reps || '—'} · ${pick.weight === '' ? 'вес не указан' : !this.results && Number(pick.weight) === 0 ? 'вес на выбор' : pick.weight+' кг'}`;
        }
        renderTray() {
            const picks = [...this.pending.values()], count = picks.length, total = picks.reduce((n,p) => n + (Number(p.setsCount)||0), 0);
            this.root.querySelector('[data-selection-summary]').textContent = count ? `Выбрано ${count}${this.single?'':` · ${total} подходов`}` : 'Выберите упражнения';
            this.root.querySelector('[data-clear-selection]').hidden = !count;
            const chips = this.root.querySelector('[data-selection-chips]'); chips.hidden = !count;
            chips.innerHTML = picks.map(p => `<button type="button" data-remove-pick="${p.exerciseId}" ${this.busy?'disabled':''} aria-label="Убрать из выбора: ${escape(this.byId.get(p.exerciseId).dataset.name)}">${escape(this.byId.get(p.exerciseId).dataset.name)}<span aria-hidden="true">×</span></button>`).join('');
            const commit = this.root.querySelector('[data-library-commit]'); commit.disabled = !count || this.busy;
            commit.querySelector('span').textContent = this.busy ? 'Добавляем…' : this.single ? 'Заменить упражнение' : count ? `Добавить ${plural(count)}` : 'Добавить в тренировку';
            this.root.querySelector('[data-selection-hint]').hidden = this.single;
        }
        commit() {
            if (this.busy || !this.pending.size) return;
            const invalid = [...this.root.querySelectorAll('[data-pick-field]')].find(input => !input.checkValidity());
            if (invalid) { this.search.value=''; this.group.value=''; this.equipment.value=''; this.tab='all'; this.editing = exerciseId(invalid.closest('[data-exercise-id]').dataset.exerciseId); this.rows.forEach(row => this.renderRow(row)); this.filter(); const field = this.byId.get(this.editing).querySelector(`[data-pick-field="${invalid.dataset.pickField}"]`); field.scrollIntoView({block:'nearest',behavior:'instant'}); field.focus(); field.reportValidity(); return; }
            const exercises = [...this.pending.values()].map(p => ({exerciseId:p.exerciseId, setsCount:Number(p.setsCount), weight:p.weight===''?null:Number(p.weight), reps:p.reps===''?null:Number(p.reps), restSeconds:Number(p.restSeconds)}));
            this.root.dispatchEvent(new CustomEvent('exercises-selected', {detail:{exercises}}));
        }
        filter() {
            const normalize = value => value.toLocaleLowerCase('ru').replace(/ё/g, 'е'); const query = normalize(this.search.value.trim()); const words = query.split(/\s+/).filter(Boolean); let count = 0;
            const recentCount = this.rows.filter(row => row.dataset.recent === 'true').length;
            if ((this.tab === 'favorites' && !this.favorites.size) || (this.tab === 'recent' && !recentCount)) this.tab = 'all';
            this.rows.forEach(row => {
                const id = exerciseId(row.dataset.exerciseId), group = row.dataset.group;
                const muscle = !this.group.value || group === this.group.value || (this.group.value === 'Ноги' && ['Квадрицепсы','Задняя поверхность бедра','Ягодицы','Икры','Приводящие мышцы бедра','Отводящие мышцы бедра'].includes(group));
                const visible = muscle && (!this.equipment.value || row.dataset.equipment === this.equipment.value) &&
                    (this.tab !== 'favorites' || this.favorites.has(id)) && (this.tab !== 'recent' || row.dataset.recent === 'true') && words.every(word => normalize(row.dataset.search).includes(word));
                row.hidden = !visible; if (visible) count++;
                row.querySelector('[data-library-favorite]').setAttribute('aria-pressed', String(this.favorites.has(id)));
                row.querySelector('[data-library-favorite]').setAttribute('aria-label', `${this.favorites.has(id) ? 'Убрать из избранного' : 'В избранное'}: ${row.dataset.name}`);
            });
            const order = this.tab === 'recent' ? 'recent' : 'catalog';
            if (this.order !== order) {
                const rows = order === 'recent' ? [...this.rows].sort((a,b) => Number(a.dataset.recentOrder) - Number(b.dataset.recentOrder)) : this.rows;
                this.root.querySelector('[data-library-list]').append(...rows); this.root.querySelector('[data-library-browse]').scrollTop = 0; this.order = order;
            }
            this.root.querySelectorAll('[data-library-tab]').forEach(button => {
                button.setAttribute('aria-pressed', String(button.dataset.libraryTab===this.tab));
                if (button.dataset.libraryTab === 'recent') button.hidden = !recentCount;
                if (button.dataset.libraryTab === 'favorites') button.hidden = !this.favorites.size;
            });
            this.root.querySelector('[data-tab-count="recent"]').textContent = recentCount;
            this.root.querySelector('[data-tab-count="favorites"]').textContent = this.favorites.size;
            this.root.querySelectorAll('[data-muscle-filter]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.muscleFilter===this.group.value)));
            this.root.querySelector('[data-clear-search]').hidden = !this.search.value;
            this.root.querySelector('[data-library-count]').textContent = count;
            const filters = Number(!!this.group.value) + Number(!!this.equipment.value);
            this.root.querySelector('[data-filter-count]').textContent = filters; this.root.querySelector('[data-filter-count]').hidden = !filters;
            this.root.querySelector('[data-clear-filters]').hidden = !filters;
            const empty = this.root.querySelector('[data-library-empty]'); empty.hidden = count > 0;
            this.root.querySelector('[data-empty-message]').textContent = !this.rows.length ? 'В библиотеке пока нет упражнений.' :
                'Ничего не найдено. Попробуйте другое название или сбросьте условия поиска.';
            this.root.querySelector('[data-reset-library]').hidden = !this.rows.length;
        }
    }
    window.ExerciseLibrary = ExerciseLibrary;
    document.querySelectorAll('[data-exercise-library][data-mode="submit"]').forEach(root => new ExerciseLibrary(root));
})();
