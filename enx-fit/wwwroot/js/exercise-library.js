(() => {
    'use strict';
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const plural = n => `${n} ${n % 100 >= 11 && n % 100 <= 19 ? 'упражнений' : n % 10 === 1 ? 'упражнение' : n % 10 >= 2 && n % 10 <= 4 ? 'упражнения' : 'упражнений'}`;
    class ExerciseLibrary {
        constructor(root) {
            this.root = root; root.exerciseLibrary = this;
            this.rows = [...root.querySelectorAll('[data-exercise-id]')];
            this.byId = new Map(this.rows.map(row => [Number(row.dataset.exerciseId), row]));
            this.search = root.querySelector('[data-library-search]'); this.group = root.querySelector('[data-library-group]');
            this.pending = new Map(); this.added = new Set(); this.tab = 'all'; this.single = false; this.busy = false; this.editing = null;
            this.results = root.dataset.prescription === 'results';
            this.key = `enix-workout-favorites:${root.dataset.viewer}`; this.favorites = this.readFavorites();
            this.search.addEventListener('input', () => this.filter());
            this.search.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
            this.group.addEventListener('change', () => this.filter());
            root.addEventListener('click', event => this.click(event));
            root.addEventListener('input', event => {
                const input = event.target;
                if (!input.dataset.pickField) return;
                const selection = this.pending.get(Number(input.closest('[data-exercise-id]').dataset.exerciseId));
                if (selection) { selection[input.dataset.pickField] = input.value; this.renderPrescription(input.closest('[data-exercise-id]'), selection); this.renderTray(); }
            });
            window.addEventListener('storage', event => { if (event.key === this.key) { this.favorites = this.readFavorites(); this.filter(); } });
            this.setSelected(this.rows.filter(row => row.querySelector('[data-library-add]').disabled).map(row => Number(row.dataset.exerciseId)));
            this.filter();
        }
        readFavorites() { try { const value = JSON.parse(localStorage.getItem(this.key)); return new Set(Array.isArray(value) ? value.filter(id => this.byId.has(id)) : []); } catch { return new Set(); } }
        icon(id) { return this.byId.get(Number(id))?.querySelector('[data-library-icon]')?.innerHTML || ''; }
        setContext(name) { this.root.querySelector('[data-library-context]').textContent = name || 'Текущая тренировка'; }
        setSingle(single) { if (this.single !== single) this.clear(); this.single = single; this.root.dataset.single = String(single); this.renderTray(); }
        setBusy(busy) { this.busy = busy; this.root.setAttribute('aria-busy', String(busy)); this.root.querySelectorAll('[data-library-add]').forEach(button => { button.disabled = busy || this.added.has(Number(button.value)); }); this.root.querySelectorAll('[data-pick-field],[data-clear-selection],[data-remove-pick]').forEach(button => button.disabled = busy); this.renderTray(); }
        message(text) { const status = this.root.querySelector('[data-library-status]'); status.textContent = text; status.hidden = !text; }
        setSelected(ids) {
            this.added = new Set(ids.map(Number));
            for (const id of this.added) this.pending.delete(id);
            this.rows.forEach(row => this.renderRow(row)); this.root.querySelector('[data-library-existing]').textContent = this.added.size; this.renderTray();
        }
        clear() { this.pending.clear(); this.editing = null; this.rows.forEach(row => this.renderRow(row)); this.renderTray(); }
        click(event) {
            const button = event.target.closest('button'); if (!button || !this.root.contains(button)) return;
            if (button.hasAttribute('data-library-tab')) { this.tab = button.dataset.libraryTab; this.filter(); return; }
            if (button.hasAttribute('data-muscle-filter')) { this.group.value = button.dataset.muscleFilter; this.filter(); return; }
            if (button.hasAttribute('data-clear-search')) { this.search.value = ''; this.filter(); this.search.focus(); return; }
            if (this.busy) return;
            if (button.hasAttribute('data-clear-selection')) { this.clear(); this.search.focus(); return; }
            if (button.hasAttribute('data-remove-pick')) { const id = Number(button.dataset.removePick); this.pending.delete(id); this.renderRow(this.byId.get(id)); this.renderTray(); this.root.querySelector('[data-library-commit]').focus(); return; }
            if (button.hasAttribute('data-library-commit')) { this.commit(); return; }
            const row = button.closest('[data-exercise-id]'); if (!row) return;
            const id = Number(row.dataset.exerciseId);
            if (button.hasAttribute('data-configure-pick')) { this.editing = this.editing === id ? null : id; this.rows.forEach(item => this.renderRow(item)); return; }
            if (button.hasAttribute('data-library-favorite')) { this.favorites.has(id) ? this.favorites.delete(id) : this.favorites.add(id); try { localStorage.setItem(this.key, JSON.stringify([...this.favorites])); } catch { this.message('Браузер не разрешил сохранить избранное.'); } this.filter(); }
            if (button.hasAttribute('data-library-add') && !button.disabled) {
                this.message('');
                if (this.pending.has(id)) this.pending.delete(id);
                else {
                    if (this.single) this.clear();
                    if (this.added.size + this.pending.size >= 40 && !this.single) { this.message('В тренировке может быть до 40 упражнений.'); return; }
                    this.pending.set(id, {exerciseId:id, setsCount:'3', weight:this.results?'':'0', reps:this.results?'':'12', restSeconds:'90'});
                    this.editing = id;
                }
                this.rows.forEach(item => this.renderRow(item)); this.renderTray();
            }
        }
        renderRow(row) {
            const id = Number(row.dataset.exerciseId), added = this.added.has(id), pick = this.pending.get(id), button = row.querySelector('[data-library-add]');
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
            button.disabled = this.busy; button.setAttribute('aria-expanded', String(this.editing === Number(row.dataset.exerciseId)));
            button.setAttribute('aria-label', `Настроить: ${row.dataset.name}`);
            if (pick) row.querySelector('[data-pick-summary]').textContent = `${pick.setsCount} × ${pick.reps || '—'} · ${pick.weight === '' ? 'вес не указан' : pick.weight+' кг'}${this.results?'':` · ${pick.restSeconds} с`}`;
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
            if (invalid) { this.search.value=''; this.group.value=''; this.tab='all'; this.editing = Number(invalid.closest('[data-exercise-id]').dataset.exerciseId); this.rows.forEach(row => this.renderRow(row)); this.filter(); this.byId.get(this.editing).querySelector(`[data-pick-field="${invalid.dataset.pickField}"]`).reportValidity(); return; }
            const exercises = [...this.pending.values()].map(p => ({exerciseId:p.exerciseId, setsCount:Number(p.setsCount), weight:p.weight===''?null:Number(p.weight), reps:p.reps===''?null:Number(p.reps), restSeconds:Number(p.restSeconds)}));
            this.root.dispatchEvent(new CustomEvent('exercises-selected', {detail:{exercises}}));
        }
        filter() {
            const normalize = value => value.toLocaleLowerCase('ru').replace(/ё/g, 'е'); const query = normalize(this.search.value.trim()); let count = 0;
            this.rows.forEach(row => {
                const id = Number(row.dataset.exerciseId), group = row.dataset.group;
                const muscle = !this.group.value || group === this.group.value || (this.group.value === 'Ноги' && ['Квадрицепсы','Задняя поверхность бедра','Ягодицы','Икры','Приводящие мышцы бедра','Отводящие мышцы бедра'].includes(group));
                const visible = muscle && (this.tab !== 'favorites' || this.favorites.has(id)) && (this.tab !== 'recent' || row.dataset.recent === 'true') && normalize(row.dataset.search).includes(query);
                row.hidden = !visible; if (visible) count++;
                row.querySelector('[data-library-favorite]').setAttribute('aria-pressed', String(this.favorites.has(id)));
            });
            this.root.querySelectorAll('[data-library-tab]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.libraryTab===this.tab)));
            this.root.querySelectorAll('[data-muscle-filter]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.muscleFilter===this.group.value)));
            this.root.querySelector('[data-clear-search]').hidden = !this.search.value;
            this.root.querySelector('[data-library-count]').textContent = count;
            const empty = this.root.querySelector('[data-library-empty]'); empty.hidden = count > 0;
            empty.textContent = this.tab === 'favorites' && !query ? 'Отметьте упражнения звёздочкой — они появятся здесь.' : this.tab === 'recent' && !query ? 'Здесь появятся упражнения из завершённых тренировок.' : 'Ничего не найдено. Попробуйте другую группу или запрос.';
        }
    }
    window.ExerciseLibrary = ExerciseLibrary;
    document.querySelectorAll('[data-exercise-library][data-mode="submit"]').forEach(root => new ExerciseLibrary(root));
})();
