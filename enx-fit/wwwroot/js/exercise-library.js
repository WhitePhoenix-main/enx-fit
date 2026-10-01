(() => {
    'use strict';
    const normalize = text => String(text).toLocaleLowerCase('ru').replaceAll('ё', 'е');

    class ExerciseLibrary {
        constructor(root) {
            this.root = root;
            this.rows = [...root.querySelectorAll('[data-exercise-id]')];
            this.byId = new Map(this.rows.map(row => [Number(row.dataset.exerciseId), row]));
            this.search = root.querySelector('[data-library-search]');
            this.group = root.querySelector('[data-library-group]');
            this.tab = 'all';
            this.key = `enix-workout-favorites:${root.dataset.viewer}`;
            this.favorites = new Set();
            try {
                const stored = JSON.parse(localStorage.getItem(this.key));
                if (Array.isArray(stored)) this.favorites = new Set(stored.filter(id => this.byId.has(id)));
            } catch { /* The library remains usable when browser storage is unavailable. */ }

            this.search.addEventListener('input', () => this.filter());
            this.search.addEventListener('keydown', event => { if (event.key === 'Enter') event.preventDefault(); });
            this.group.addEventListener('change', () => this.filter());
            root.addEventListener('click', event => {
                const button = event.target.closest('button');
                if (!button || !root.contains(button)) return;
                if (button.hasAttribute('data-library-tab')) {
                    this.tab = button.dataset.libraryTab;
                    root.querySelectorAll('[data-library-tab]').forEach(tab => tab.setAttribute('aria-pressed', String(tab.dataset.libraryTab === this.tab)));
                    this.filter();
                }
                const row = button.closest('[data-exercise-id]');
                if (!row) return;
                const id = Number(row.dataset.exerciseId);
                if (button.hasAttribute('data-library-favorite')) {
                    this.favorites.has(id) ? this.favorites.delete(id) : this.favorites.add(id);
                    try { localStorage.setItem(this.key, JSON.stringify([...this.favorites])); }
                    catch {
                        const status = root.querySelector('[data-library-status]');
                        status.hidden = false;
                        status.textContent = 'Браузер не разрешил сохранить избранное.';
                    }
                    this.filter();
                }
                if (button.hasAttribute('data-library-add') && !button.disabled && root.dataset.mode === 'builder') {
                    root.dispatchEvent(new CustomEvent('exercise-selected', { detail: { id } }));
                }
            });
            window.addEventListener('storage', event => {
                if (event.key !== this.key) return;
                try {
                    const stored = JSON.parse(event.newValue);
                    this.favorites = new Set(Array.isArray(stored) ? stored.filter(id => this.byId.has(id)) : []);
                    this.filter();
                } catch { /* Ignore malformed data from another tab. */ }
            });
            this.filter();
        }

        icon(id) { return this.byId.get(id)?.querySelector('[data-library-icon]').innerHTML || ''; }

        setSelected(ids) {
            const selected = new Set(ids);
            for (const [id, row] of this.byId) {
                const button = row.querySelector('[data-library-add]');
                const added = selected.has(id);
                button.disabled = added;
                button.setAttribute('aria-label', `${added ? 'Добавлено' : 'Добавить'}: ${row.dataset.name}`);
                button.title = added ? 'Уже в тренировке' : 'Добавить упражнение';
                button.querySelector('[data-add-icon]').hidden = added;
                button.querySelector('[data-selected-icon]').hidden = !added;
            }
        }

        filter() {
            const query = normalize(this.search.value.trim());
            let count = 0;
            for (const [id, row] of this.byId) {
                const favorite = this.favorites.has(id);
                row.querySelector('[data-library-favorite]').setAttribute('aria-pressed', String(favorite));
                row.hidden = !((!this.group.value || row.dataset.group === this.group.value) &&
                    (this.tab !== 'favorites' || favorite) && (this.tab !== 'recent' || row.dataset.recent === 'true') &&
                    normalize(row.dataset.search).includes(query));
                if (!row.hidden) count++;
            }
            this.root.querySelector('[data-library-count]').textContent = String(count);
            const empty = this.root.querySelector('[data-library-empty]');
            empty.hidden = count !== 0;
            empty.textContent = this.tab === 'favorites' && !query ? 'Отметьте упражнения звёздочкой — они появятся здесь.' :
                this.tab === 'recent' && !query ? 'Здесь появятся упражнения из завершённых тренировок.' :
                'Упражнений не найдено. Измените группу или поисковый запрос.';
        }
    }
    window.ExerciseLibrary = ExerciseLibrary;
    document.querySelectorAll('[data-exercise-library][data-mode="submit"]').forEach(root => new ExerciseLibrary(root));
})();
