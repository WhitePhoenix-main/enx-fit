// Exercise picker shared by workout workflows.
(() => {
    'use strict';
    const exerciseId = window.ExerciseIds.normalize;
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[c]));
    const plural = n => `${n} ${n % 100 >= 11 && n % 100 <= 19 ? 'упражнений' : n % 10 === 1 ? 'упражнение' : n % 10 >= 2 && n % 10 <= 4 ? 'упражнения' : 'упражнений'}`;
    const setCount = n => `${n} ${n % 100 >= 11 && n % 100 <= 19 ? 'подходов' : n % 10 === 1 ? 'подход' : n % 10 >= 2 && n % 10 <= 4 ? 'подхода' : 'подходов'}`;

    class ExerciseLibrary {
        constructor(root) {
            this.root = root;
            root.exerciseLibrary = this;
            this.rows = [...root.querySelectorAll('[data-exercise-id]')];
            this.byId = new Map(this.rows.map(row => [exerciseId(row.dataset.exerciseId), row]));
            this.search = root.querySelector('[data-library-search]');
            this.group = root.querySelector('[data-library-group]');
            this.equipment = root.querySelector('[data-library-equipment]');
            this.pending = new Map();
            this.added = new Set();
            this.tab = 'all';
            this.single = false;
            this.busy = false;
            this.editing = null;
            this.results = root.dataset.prescription === 'results';
            this.key = `enix-exercise-favorites:${root.dataset.viewer}:live`;
            this.legacyKey = `enix-workout-favorites:${root.dataset.viewer}`;
            this.favorites = this.readFavorites();
            this.search.addEventListener('input', () => {
                if (this.search.value.trim()) this.tab = 'all';
                this.filter();
            });
            this.search.addEventListener('keydown', event => {
                if (event.key === 'Enter') event.preventDefault();
            });
            this.group.addEventListener('change', () => {
                // Muscle filters search the full catalog, just like a text query.
                this.tab = 'all';
                this.filter();
            });
            this.equipment.addEventListener('change', () => this.filter());
            root.addEventListener('click', event => this.click(event));
            const updatePick = event => {
                const input = event.target;
                if (!input.dataset.pickField) return;
                const selection = this.pending.get(exerciseId(input.closest('[data-exercise-id]').dataset.exerciseId));
                if (selection) {
                    selection[input.dataset.pickField] = input.value;
                    this.renderPrescription(input.closest('[data-exercise-id]'), selection);
                    this.renderTray();
                }
            };
            root.addEventListener('input', updatePick);
            root.addEventListener('change', updatePick);
            window.addEventListener('storage', event => {
                if (event.key === this.key) {
                    this.favorites = this.readFavorites();
                    this.filter();
                }
            });
            this.setSelected(this.rows.filter(row => row.querySelector('[data-library-add]').disabled).map(row => exerciseId(row.dataset.exerciseId)));
            this.tab = this.rows.some(row => row.dataset.recent === 'true' && !this.added.has(exerciseId(row.dataset.exerciseId))) ? 'recent' :
                [...this.favorites].some(id => !this.added.has(id)) ? 'favorites' : 'all';
            this.filter();
            let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
            const keyboardLayout = () => {
                const viewport = window.visualViewport;
                if (!viewport) return;
                if (viewportWidth !== innerWidth) {
                    viewportWidth = innerWidth;
                    viewportHeight = viewport.height;
                }
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
                if (open) requestAnimationFrame(() => document.activeElement?.closest('[data-pick-settings]')?.scrollIntoView({
                    block: 'nearest',
                    behavior: 'instant'
                }));
                if (!interacting) viewportHeight = viewport.height;
            };
            window.visualViewport?.addEventListener('resize', keyboardLayout);
            window.visualViewport?.addEventListener('scroll', keyboardLayout);
            document.addEventListener('focusin', keyboardLayout);
            document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
        }

        readFavorites() {
            const read = key => {
                try {
                    const value = JSON.parse(localStorage.getItem(key));
                    return Array.isArray(value) ? value : [];
                } catch {
                    return [];
                }
            };
            const legacy = read(this.legacyKey),
                ids = new Set([...read(this.key), ...legacy].map(exerciseId).filter(id => this.byId.has(id)));
            if (legacy.length) try {
                localStorage.setItem(this.key, JSON.stringify([...ids]));
                localStorage.removeItem(this.legacyKey);
            } catch {
                this.message('Браузер не разрешил перенести избранное.');
            }
            return ids;
        }

        icon(id) {
            return this.byId.get(exerciseId(id))?.querySelector('[data-library-icon]')?.innerHTML || '';
        }

        setContext(name) {
            this.root.querySelector('[data-library-context]').textContent = name || 'Текущая тренировка';
        }

        setSingle(single) {
            if (this.single !== single) this.clear();
            this.single = single;
            this.root.dataset.single = String(single);
            this.renderTray();
        }

        setBusy(busy) {
            this.busy = busy;
            this.root.setAttribute('aria-busy', String(busy));
            this.root.querySelectorAll('[data-library-add]').forEach(button => {
                button.disabled = busy || this.added.has(exerciseId(button.value));
            });
            this.root.querySelectorAll('[data-pick-field],[data-clear-selection],[data-remove-pick]').forEach(button => button.disabled = busy);
            this.renderTray();
        }

        message(text) {
            const status = this.root.querySelector('[data-library-status]');
            status.textContent = text;
            status.hidden = !text;
        }

        setSelected(ids) {
            this.added = new Set(ids.map(exerciseId));
            for (const id of this.added) this.pending.delete(id);
            this.rows.forEach(row => this.renderRow(row));
            this.root.querySelector('[data-library-existing]').textContent = this.added.size;
            this.renderTray();
        }

        clear() {
            this.pending.clear();
            this.editing = null;
            this.rows.forEach(row => this.renderRow(row));
            this.renderTray();
        }

        click(event) {
            const button = event.target.closest('button');
            if (!button || !this.root.contains(button)) return;
            if (button.hasAttribute('data-library-tab')) {
                this.tab = button.dataset.libraryTab;
                this.filter();
                return;
            }
            if (button.hasAttribute('data-muscle-filter')) {
                this.group.value = button.dataset.muscleFilter;
                this.tab = 'all';
                this.filter();
                return;
            }
            if (button.hasAttribute('data-clear-search')) {
                this.search.value = '';
                this.filter();
                this.search.focus();
                return;
            }
            if (button.hasAttribute('data-clear-filters') || button.hasAttribute('data-reset-library')) {
                this.group.value = '';
                this.equipment.value = '';
                if (button.hasAttribute('data-reset-library')) {
                    this.search.value = '';
                    this.tab = 'all';
                }
                this.filter();
                this.search.focus();
                return;
            }
            if (this.busy) return;
            if (button.hasAttribute('data-clear-selection')) {
                this.clear();
                this.search.focus();
                return;
            }
            if (button.hasAttribute('data-remove-pick')) {
                const id = exerciseId(button.dataset.removePick);
                this.pending.delete(id);
                this.renderRow(this.byId.get(id));
                this.renderTray();
                (this.pending.size ? this.root.querySelector('[data-library-commit]') : this.search).focus();
                return;
            }
            if (button.hasAttribute('data-library-commit')) {
                this.commit();
                return;
            }
            const row = button.closest('[data-exercise-id]');
            if (!row) return;
            const id = exerciseId(row.dataset.exerciseId);
            if (button.hasAttribute('data-configure-pick')) {
                this.editing = this.editing === id ? null : id;
                this.rows.forEach(item => this.renderRow(item));
                return;
            }
            if (button.hasAttribute('data-library-favorite')) {
                this.favorites.has(id) ? this.favorites.delete(id) : this.favorites.add(id);
                try {
                    localStorage.setItem(this.key, JSON.stringify([...this.favorites]));
                } catch {
                    this.message('Браузер не разрешил сохранить избранное.');
                }
                this.filter();
            }
            if (button.hasAttribute('data-library-add') && !button.disabled) {
                this.message('');
                if (this.pending.has(id)) this.pending.delete(id);
                else {
                    if (this.single) this.clear();
                    if (this.added.size + this.pending.size >= 40 && !this.single) {
                        this.message('В тренировке может быть до 40 упражнений.');
                        return;
                    }
                    this.pending.set(id, {
                        exerciseId: id,
                        setsCount: '3',
                        weight: this.results ? '' : '0',
                        reps: this.results ? '' : '12',
                        restSeconds: '90'
                    });
                }
                this.editing = null;
                this.rows.forEach(item => this.renderRow(item));
                this.renderTray();
            }
        }

        renderRow(row) {
            const id = exerciseId(row.dataset.exerciseId), added = this.added.has(id), pick = this.pending.get(id),
                button = row.querySelector('[data-library-add]');
            row.classList.toggle('is-added', added);
            row.classList.toggle('is-picked', !!pick);
            button.disabled = added || this.busy;
            button.setAttribute('aria-pressed', String(!!pick));
            button.setAttribute('aria-label', `${added ? 'Добавлено' : pick ? 'Убрать из выбора' : 'Добавить'}: ${row.dataset.name}`);
            row.querySelector('[data-added-label]').hidden = !added;
            row.querySelector('[data-add-icon]').hidden = added || !!pick;
            row.querySelector('[data-selected-icon]').hidden = !added && !pick;
            this.renderPrescription(row, pick);
            const settings = row.querySelector('[data-pick-settings]');
            settings.hidden = !pick || this.single || this.editing !== id;
            if (!pick || this.single) {
                settings.innerHTML = '';
                return;
            }
            const fields = [['setsCount', 'Подходы', 1, 20], ['reps', 'Повторы', 1, 1000], ['weight', 'Вес, кг', 0, 2000], ...(!this.results ? [['restSeconds', 'Отдых, с', 0, 900]] : [])];
            settings.innerHTML = fields.map(([key, label, min, max]) => `<label>${label}<input type="number" inputmode="${key === 'weight' ? 'decimal' : 'numeric'}" min="${min}" max="${max}" step="${key === 'weight' ? '0.01' : '1'}" data-pick-field="${key}" value="${escape(pick[key])}" ${!this.results || key === 'setsCount' ? 'required' : ''} placeholder="${key === 'weight' ? '0' : '—'}" aria-label="${escape(row.dataset.name)}: ${label}" /></label>`).join('');
        }

        renderPrescription(row, pick) {
            const button = row.querySelector('[data-configure-pick]');
            button.hidden = !pick || this.single;
            button.disabled = this.busy;
            button.setAttribute('aria-expanded', String(this.editing === exerciseId(row.dataset.exerciseId)));
            button.setAttribute('aria-controls', row.querySelector('[data-pick-settings]').id);
            button.setAttribute('aria-label', `Настроить: ${row.dataset.name}`);
            if (pick) row.querySelector('[data-pick-summary]').textContent = this.results && (!pick.reps || pick.weight === '') ? `${setCount(Number(pick.setsCount))} · результаты после добавления` :
                `${pick.setsCount} × ${pick.reps || '—'} · ${pick.weight === '' ? 'вес не указан' : !this.results && Number(pick.weight) === 0 ? 'вес на выбор' : pick.weight + ' кг'}`;
        }

        renderTray() {
            const picks = [...this.pending.values()], count = picks.length,
                total = picks.reduce((n, p) => n + (Number(p.setsCount) || 0), 0);
            this.root.querySelector('[data-selection-summary]').textContent = count ? `Выбрано ${count}${this.single ? '' : ` · ${total} подходов`}` : 'Выберите упражнения';
            this.root.querySelector('[data-clear-selection]').hidden = !count;
            const chips = this.root.querySelector('[data-selection-chips]');
            chips.hidden = !count;
            chips.innerHTML = picks.map(p => `<button type="button" data-remove-pick="${p.exerciseId}" ${this.busy ? 'disabled' : ''} aria-label="Убрать из выбора: ${escape(this.byId.get(p.exerciseId).dataset.name)}">${escape(this.byId.get(p.exerciseId).dataset.name)}<span aria-hidden="true">×</span></button>`).join('');
            const commit = this.root.querySelector('[data-library-commit]');
            commit.disabled = !count || this.busy;
            commit.querySelector('span').textContent = this.busy ? 'Добавляем…' : this.single ? 'Заменить упражнение' : count ? `Добавить ${plural(count)}` : 'Добавить в тренировку';
            this.root.querySelector('[data-selection-hint]').hidden = this.single;
        }

        commit() {
            if (this.busy || !this.pending.size) return;
            const invalid = [...this.root.querySelectorAll('[data-pick-field]')].find(input => !input.checkValidity());
            if (invalid) {
                this.search.value = '';
                this.group.value = '';
                this.equipment.value = '';
                this.tab = 'all';
                this.editing = exerciseId(invalid.closest('[data-exercise-id]').dataset.exerciseId);
                this.rows.forEach(row => this.renderRow(row));
                this.filter();
                const field = this.byId.get(this.editing).querySelector(`[data-pick-field="${invalid.dataset.pickField}"]`);
                field.scrollIntoView({block: 'nearest', behavior: 'instant'});
                field.focus();
                field.reportValidity();
                return;
            }
            const exercises = [...this.pending.values()].map(p => ({
                exerciseId: p.exerciseId,
                setsCount: Number(p.setsCount),
                weight: p.weight === '' ? null : Number(p.weight),
                reps: p.reps === '' ? null : Number(p.reps),
                restSeconds: Number(p.restSeconds)
            }));
            this.root.dispatchEvent(new CustomEvent('exercises-selected', {detail: {exercises}}));
        }

        filter() {
            const normalize = value => value.toLocaleLowerCase('ru').replace(/ё/g, 'е');
            const query = normalize(this.search.value.trim());
            const words = query.split(/\s+/).filter(Boolean);
            let count = 0;
            const recentCount = this.rows.filter(row => row.dataset.recent === 'true').length;
            if ((this.tab === 'favorites' && !this.favorites.size) || (this.tab === 'recent' && !recentCount)) this.tab = 'all';
            this.rows.forEach(row => {
                const id = exerciseId(row.dataset.exerciseId), group = row.dataset.group;
                const muscle = !this.group.value || group === this.group.value || (this.group.value === 'Ноги' && ['Квадрицепсы', 'Задняя поверхность бедра', 'Ягодицы', 'Икры', 'Приводящие мышцы бедра', 'Отводящие мышцы бедра'].includes(group));
                const visible = muscle && (!this.equipment.value || row.dataset.equipment === this.equipment.value) &&
                    (this.tab !== 'favorites' || this.favorites.has(id)) && (this.tab !== 'recent' || row.dataset.recent === 'true') && words.every(word => normalize(row.dataset.search).includes(word));
                row.hidden = !visible;
                if (visible) count++;
                row.querySelector('[data-library-favorite]').setAttribute('aria-pressed', String(this.favorites.has(id)));
                row.querySelector('[data-library-favorite]').setAttribute('aria-label', `${this.favorites.has(id) ? 'Убрать из избранного' : 'В избранное'}: ${row.dataset.name}`);
            });
            const order = this.tab === 'recent' ? 'recent' : 'catalog';
            if (this.order !== order) {
                const rows = order === 'recent' ? [...this.rows].sort((a, b) => Number(a.dataset.recentOrder) - Number(b.dataset.recentOrder)) : this.rows;
                this.root.querySelector('[data-library-list]').append(...rows);
                this.root.querySelector('[data-library-browse]').scrollTop = 0;
                this.order = order;
            }
            this.root.querySelectorAll('[data-library-tab]').forEach(button => {
                button.setAttribute('aria-pressed', String(button.dataset.libraryTab === this.tab));
                if (button.dataset.libraryTab === 'recent') button.hidden = !recentCount;
                if (button.dataset.libraryTab === 'favorites') button.hidden = !this.favorites.size;
            });
            this.root.querySelector('[data-tab-count="recent"]').textContent = recentCount;
            this.root.querySelector('[data-tab-count="favorites"]').textContent = this.favorites.size;
            this.root.querySelectorAll('[data-muscle-filter]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.muscleFilter === this.group.value)));
            this.root.querySelector('[data-clear-search]').hidden = !this.search.value;
            this.root.querySelector('[data-library-count]').textContent = count;
            const filters = Number(!!this.group.value) + Number(!!this.equipment.value);
            this.root.querySelector('[data-filter-count]').textContent = filters;
            this.root.querySelector('[data-filter-count]').hidden = !filters;
            this.root.querySelector('[data-clear-filters]').hidden = !filters;
            const empty = this.root.querySelector('[data-library-empty]');
            empty.hidden = count > 0;
            this.root.querySelector('[data-empty-message]').textContent = !this.rows.length ? 'В библиотеке пока нет упражнений.' :
                'Ничего не найдено. Попробуйте другое название или сбросьте условия поиска.';
            this.root.querySelector('[data-reset-library]').hidden = !this.rows.length;
        }
    }

    window.ExerciseLibrary = ExerciseLibrary;
    document.querySelectorAll('[data-exercise-library][data-mode="submit"]').forEach(root => new ExerciseLibrary(root));
})();

// Program catalog, schedule and assignment.
if (document.querySelector('.programs-page, .ep-heading')) {
    // Program navigation, forms and template filters.
    (() => {
        'use strict';
        document.addEventListener('click', event => {
            document.querySelectorAll('.program-menu[open]').forEach(menu => {
                if (!menu.contains(event.target)) menu.open = false;
            });
        });
        document.querySelectorAll('form[data-busy]').forEach(form => form.addEventListener('submit', event => {
            if (!form.checkValidity()) return;
            const button = event.submitter;
            if (button) {
                // Keep the submitter's name/value in the POST.
                button.setAttribute('aria-busy', 'true');
                setTimeout(() => {
                    button.disabled = true;
                }, 0);
            }
        }));
        window.addEventListener('pageshow', () => document.querySelectorAll('[aria-busy=true]').forEach(button => {
            button.disabled = false;
            button.removeAttribute('aria-busy');
        }));
        const editor = document.querySelector('[data-program-editor]');
        if (editor && !document.querySelector('.program-builder')) {
            const key = `program-editor-scroll:${location.pathname}`;
            try {
                const y = sessionStorage.getItem(key);
                if (y) {
                    window.scrollTo(0, Number(y));
                    sessionStorage.removeItem(key);
                }
            } catch {
            }
            editor.addEventListener('submit', event => {
                if (event.submitter?.formAction.includes('handler=Structure')) {
                    try {
                        sessionStorage.setItem(key, String(window.scrollY));
                    } catch {
                    }
                }
            });
        }
        document.querySelectorAll('[data-template-filters]').forEach(form => {
            // A sheet applies several choices together; selecting one field keeps it open.
            if (form.closest('.native-filter-sheet')) return;
            form.classList.add('plan-filter-enhanced');
            form.querySelectorAll('select,input[type="checkbox"]').forEach(control => control.addEventListener('change', () => form.requestSubmit()));
        });
    })();

    // Program calendar and rescheduling.
    (() => {
        'use strict';
        const root = document.querySelector('[data-program-calendar]');
        if (!root) return;
        const content = root.querySelector('[data-schedule-content]');
        const host = root.querySelector('[data-schedule-details-host]');
        const groups = [...content.querySelectorAll('[data-details-day]')];
        const triggers = [...root.querySelectorAll('[data-schedule-day]')];
        const dialog = document.getElementById('schedule-day-dialog');
        const mobile = matchMedia('(max-width:1023px)');
        const canDialog = typeof dialog?.showModal === 'function';
        const week = root.querySelector('.schedule-week');
        let day = root.dataset.selectedDay || root.querySelector('.schedule-day.has-training.is-today')?.dataset.scheduleDay ||
            root.querySelector('.schedule-day.has-training')?.dataset.scheduleDay || root.dataset.today;
        if (!groups.some(group => group.dataset.detailsDay === day)) day = groups[0]?.dataset.detailsDay;
        const revealSelectedDay = () => requestAnimationFrame(() => {
            const selected = week.querySelector('.is-selected');
            if (!selected) return;
            const bounds = week.getBoundingClientRect(), item = selected.getBoundingClientRect();
            if (item.left < bounds.left) week.scrollLeft += item.left - bounds.left;
            else if (item.right > bounds.right) week.scrollLeft += item.right - bounds.right;
        });
        const select = value => {
            day = value;
            groups.forEach(group => {
                group.hidden = group.dataset.detailsDay !== day;
            });
            triggers.forEach(trigger => {
                const active = trigger.dataset.scheduleDay === day;
                trigger.classList.toggle('is-selected', active);
                if (active) trigger.setAttribute('aria-current', 'true'); else trigger.removeAttribute('aria-current');
            });
            revealSelectedDay();
        };
        const layout = () => {
            if (mobile.matches && canDialog) {
                dialog.querySelector('[data-schedule-dialog-host]').append(content);
                host.hidden = true;
            } else {
                if (dialog?.open) dialog.close();
                host.append(content);
                host.hidden = false;
            }
        };
        triggers.forEach(trigger => trigger.addEventListener('click', event => {
            event.preventDefault();
            select(trigger.dataset.scheduleDay);
            if (mobile.matches && canDialog) dialog.showModal();
        }));
        mobile.addEventListener('change', layout);
        window.addEventListener('resize', revealSelectedDay);
        root.classList.add('schedule-enhanced');
        select(day);
        layout();
        if (root.dataset.selectedDay === day && new URL(location.href).searchParams.get('OpenDay')?.toLowerCase() === 'true' && mobile.matches && canDialog) dialog.showModal();
        document.querySelectorAll('.schedule-move form').forEach(form => {
            const input = form.querySelector('input[name="date"]');
            const choices = [...form.querySelectorAll('[data-date-choice]')];
            const summary = form.querySelector('[data-date-summary]');
            const original = form.closest('[data-appointment-date]').dataset.appointmentDate;
            const update = () => {
                choices.forEach(choice => choice.setAttribute('aria-pressed', String(choice.dataset.dateChoice === input.value)));
                summary.hidden = !input.value || !input.validity.valid || input.value === original;
                if (!summary.hidden) summary.textContent = 'Новая дата: ' + new Intl.DateTimeFormat('ru-RU', {
                    weekday: 'long',
                    day: 'numeric',
                    month: 'long'
                }).format(new Date(input.value + 'T12:00:00'));
            };
            choices.forEach(choice => choice.addEventListener('click', () => {
                input.value = choice.dataset.dateChoice;
                update();
            }));
            input.addEventListener('change', update);
            update();
        });
    })();

    // Program assignment.
    (() => {
        'use strict';
        const form = document.querySelector('[data-assignment-form]');
        if (!form) return;
        const save = form.querySelector('[data-assignment-save]'),
            feedback = form.querySelector('[data-assignment-feedback]');
        let busy = false;
        const notice = state => window.AppComponents?.setNoticeState(feedback, state);
        const recover = () => {
            busy = false;
            save.disabled = false;
            save.removeAttribute('aria-busy');
        };
        form.addEventListener('input', () => {
            feedback.textContent = 'Есть несохранённые изменения назначения.';
            notice('pending');
        });
        form.addEventListener('submit', event => {
            if (busy) {
                event.preventDefault();
                return;
            }
            if (!form.checkValidity()) {
                event.preventDefault();
                form.reportValidity();
                return;
            }
            if (!navigator.onLine) {
                event.preventDefault();
                feedback.textContent = 'Сейчас нет сети. Получатель, дата и дни остались в форме; повторите после подключения.';
                notice('error');
                return;
            }
            busy = true;
            save.setAttribute('aria-busy', 'true');
            setTimeout(() => {
                if (busy) save.disabled = true;
            }, 0);
            feedback.textContent = 'Ожидаем подтверждения назначения…';
            notice('pending');
        });
        addEventListener('online', () => {
            if (feedback.dataset.appState === 'error') {
                feedback.textContent = 'Подключение восстановлено. Проверьте назначение и повторите сохранение.';
                notice('pending');
            }
        });
        addEventListener('pageshow', () => {
            const wasBusy = busy;
            recover();
            if (wasBusy) {
                feedback.textContent = 'Проверьте назначение перед повторным сохранением.';
                notice('pending');
            }
        });
    })();
}

// Program structure editor.
(() => {
    'use strict';
    const form = document.querySelector('.program-builder [data-program-editor]');
    if (!form) return;
    const panels = [...form.querySelectorAll('[data-editor-workout]')];
    const buttons = [...form.querySelectorAll('[data-select-workout]')];
    const selected = form.querySelector('[data-selected-workout]');
    const status = form.querySelector('[data-plan-save-status]');
    const initialStatus = {text: status.textContent, state: status.dataset.appState};
    const scrollKey = `plan-editor-scroll:${location.pathname}`;
    const about = form.querySelector('[data-plan-about]');
    about.addEventListener('toggle', () => {
        form.querySelector('[data-about-expanded]').value = String(about.open);
    });
    let dirty = form.dataset.unsaved === 'true', submitting = false;
    form.classList.add('plan-enhanced');
    form.noValidate = true;
    form.querySelector('[data-editor-navigation]').hidden = panels.length === 0;
    const selectDay = (index, focus = false) => {
        const value = Math.max(0, Math.min(panels.length - 1, Number(index) || 0));
        selected.value = String(value);
        panels.forEach((panel, i) => {
            panel.hidden = i !== value;
        });
        buttons.forEach((button, i) => {
            button.classList.toggle('is-current', i === value);
            button.setAttribute('aria-current', i === value ? 'true' : 'false');
        });
        const outline = form.querySelector('[data-editor-outline]'), button = buttons[value];
        if (button) {
            const listBounds = outline.getBoundingClientRect(), buttonBounds = button.getBoundingClientRect();
            if (buttonBounds.left < listBounds.left) outline.scrollLeft -= listBounds.left - buttonBounds.left;
            else if (buttonBounds.right > listBounds.right) outline.scrollLeft += buttonBounds.right - listBounds.right;
        }
        if (focus && panels[value]) panels[value].querySelector('[data-day-name]').focus({preventScroll: true});
    };
    buttons.forEach(button => button.addEventListener('click', () => selectDay(button.dataset.selectWorkout)));
    selectDay(selected.value);
    const update = () => {
        let count = 0;
        const days = [];
        panels.forEach((panel, index) => {
            const name = panel.querySelector('[data-day-name]').value.trim() || 'Новая тренировка';
            const day = panel.querySelector('[data-day-schedule]');
            const exercises = panel.querySelectorAll('[data-plan-exercise]').length;
            const duration = panel.querySelector('[data-day-duration]').value;
            count += exercises;
            if (day.value) days.push(day.value);
            panel.querySelector('[data-workout-title]').textContent = name;
            buttons[index].querySelector('[data-day-title]').textContent = name;
            buttons[index].querySelector('[data-day-meta]').textContent = `${day.value ? day.selectedOptions[0].text : 'Без дня'} · ${exercises} упр. · ${duration || '—'} мин`;
        });
        form.querySelector('[data-plan-summary]').textContent = `Тренировок: ${panels.length} · упражнений: ${count}. Каждая тренировка повторяется в выбранный день недели.`;
        const schedule = form.querySelector('[data-plan-schedule]');
        const frequency = Number(form.querySelector('[name="Input.DaysPerWeek"]').value);
        const repeated = new Set(days).size !== days.length;
        schedule.hidden = days.length === 0;
        schedule.classList.toggle('is-warning', repeated || (days.length > 0 && days.length !== frequency));
        schedule.textContent = repeated ? 'В один день можно назначить одну тренировку. Выберите другой день для повторяющихся.' :
            days.length !== frequency ? `Дней в расписании: ${days.length}, тренировок в неделю: ${frequency}. Согласуйте их перед сохранением.` : `Расписание готово. Тренировочных дней в неделю: ${days.length}. Остальные дни — отдых.`;
    };
    const setDirty = () => {
        dirty = true;
        status.textContent = 'Изменения не сохранены';
        status.classList.add('is-dirty');
        window.AppComponents?.setNoticeState(status, 'pending');
        update();
    };
    form.addEventListener('input', setDirty);
    form.addEventListener('change', setDirty);
    if (dirty) status.classList.add('is-dirty');
    update();
    const revealField = field => {
        const panel = field.closest('[data-editor-workout]');
        if (panel) selectDay(panel.dataset.workoutIndex);
        for (let parent = field.parentElement; parent && parent !== form; parent = parent.parentElement)
            if (parent instanceof HTMLDetailsElement) parent.open = true;
        field.focus();
    };
    form.addEventListener('submit', event => {
        const button = event.submitter;
        const structure = button && new URL(button.formAction).searchParams.get('handler') === 'Structure';
        if (!structure) {
            const invalid = form.querySelector('input:invalid,select:invalid,textarea:invalid');
            if (invalid) {
                event.preventDefault();
                revealField(invalid);
                invalid.reportValidity();
                return;
            }
        } else {
            try {
                sessionStorage.setItem(scrollKey, JSON.stringify({
                    y: window.scrollY,
                    at: Date.now(),
                    day: selected.value
                }));
            } catch {
            }
        }
        submitting = true;
        status.textContent = structure ? 'Обновляем состав…' : 'Сохраняем программу…';
        window.AppComponents?.setNoticeState(status, 'pending');
        if (button) button.setAttribute('aria-busy', 'true');
    });
    window.addEventListener('beforeunload', event => {
        if (dirty && !submitting) {
            event.preventDefault();
            event.returnValue = '';
        }
    });
    window.addEventListener('pageshow', () => {
        submitting = false;
        form.querySelectorAll('[aria-busy="true"]').forEach(button => button.removeAttribute('aria-busy'));
        status.textContent = dirty ? 'Изменения не сохранены' : initialStatus.text;
        window.AppComponents?.setNoticeState(status, dirty ? 'pending' : initialStatus.state);
    });
    try {
        const previous = JSON.parse(sessionStorage.getItem(scrollKey) || 'null');
        sessionStorage.removeItem(scrollKey);
        if (previous && Date.now() - previous.at < 60000)
            requestAnimationFrame(() => {
                if (previous.day === selected.value) window.scrollTo({top: previous.y, behavior: 'instant'});
                else panels[Number(selected.value)]?.scrollIntoView({block: 'start', behavior: 'instant'});
            });
    } catch {
    }
    const serverError = form.querySelector('.input-validation-error:not([type="hidden"])');
    if (serverError) revealField(serverError);
    else if (form.querySelector('.validation-summary-errors')) form.querySelector('.validation-summary-errors').scrollIntoView({
        block: 'start',
        behavior: 'instant'
    });

    const savebar = form.querySelector('.plan-savebar');
    let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
    const keyboardLayout = () => {
        const viewport = window.visualViewport;
        if (!viewport) return;
        if (viewportWidth !== innerWidth) {
            viewportWidth = innerWidth;
            viewportHeight = viewport.height;
        }
        const input = document.activeElement;
        const editing = matchMedia('(max-width:1023px)').matches && form.contains(input) && !input?.closest('dialog') &&
            input?.matches('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),textarea');
        const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
        const open = editing && (inset > 120 || viewportHeight - viewport.height > 120);
        document.body.classList.toggle('plan-keyboard-open', !!open);
        const bounds = form.getBoundingClientRect();
        form.style.setProperty('--plan-keyboard-inset', `${open ? inset : 0}px`);
        form.style.setProperty('--plan-save-left', `${bounds.left}px`);
        form.style.setProperty('--plan-save-width', `${bounds.width}px`);
        if (open) {
            const inputBounds = input.getBoundingClientRect(), saveBounds = savebar.getBoundingClientRect();
            if (inputBounds.bottom > saveBounds.top - 16) window.scrollBy({
                top: inputBounds.bottom - saveBounds.top + 16,
                behavior: 'instant'
            });
        }
        if (!editing) viewportHeight = viewport.height;
    };
    window.visualViewport?.addEventListener('resize', keyboardLayout);
    window.visualViewport?.addEventListener('scroll', keyboardLayout);
    document.addEventListener('focusin', keyboardLayout);
    document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));

    const dialog = document.getElementById('plan-exercise-picker');
    if (!dialog || typeof dialog.showModal !== 'function') return;
    const search = dialog.querySelector('[data-plan-search]');
    const equipment = dialog.querySelector('[data-plan-equipment]');
    const choices = [...dialog.querySelectorAll('[data-plan-pick]')];
    const commit = dialog.querySelector('[data-plan-pick-commit]');
    const picked = new Set();
    let addButton, capacity = 0;
    const normalize = value => value.toLocaleLowerCase('ru-RU').replaceAll('ё', 'е').trim();
    const filter = () => {
        const query = normalize(search.value);
        let visible = 0;
        choices.forEach(choice => {
            choice.hidden = !normalize(`${choice.dataset.name} ${choice.dataset.muscle}`).includes(query) ||
                (equipment.value !== '' && choice.dataset.equipment !== equipment.value);
            if (!choice.hidden) visible++;
        });
        dialog.querySelector('[data-plan-search-empty]').hidden = visible > 0;
        dialog.querySelector('[data-picker-found]').textContent = `Найдено: ${visible}`;
    };
    const updatePicked = () => {
        choices.forEach(choice => {
            const active = picked.has(choice.dataset.planPick);
            choice.setAttribute('aria-pressed', String(active));
            choice.querySelector('[data-pick-mark]').textContent = choice.dataset.added === 'true' ? '✓' : active ? '✓' : '+';
            choice.disabled = choice.dataset.added === 'true' || (!active && picked.size >= capacity);
        });
        commit.disabled = picked.size === 0;
        commit.textContent = picked.size ? `Добавить (${picked.size})` : 'Добавить';
        dialog.querySelector('[data-picker-selected]').textContent = picked.size ? `Выбрано: ${picked.size}` : `Можно добавить: ${capacity}`;
    };
    form.querySelectorAll('[data-plan-add-exercise]').forEach(button => button.addEventListener('click', event => {
        event.preventDefault();
        addButton = button;
        const panel = button.closest('[data-editor-workout]');
        const existing = [...panel.querySelectorAll('[data-plan-exercise]')].map(input => input.value);
        capacity = Math.max(0, 30 - existing.length);
        picked.clear();
        search.value = '';
        equipment.value = '';
        choices.forEach(choice => {
            choice.dataset.added = String(existing.includes(choice.dataset.planPick));
        });
        dialog.querySelector('[data-picker-day]').textContent = panel.querySelector('[data-day-name]').value;
        updatePicked();
        filter();
        dialog.showModal();
        if (matchMedia('(min-width: 900px)').matches) search.focus();
    }));
    choices.forEach(choice => choice.addEventListener('click', () => {
        const id = choice.dataset.planPick;
        if (picked.has(id)) picked.delete(id); else if (picked.size < capacity) picked.add(id);
        updatePicked();
    }));
    search.addEventListener('input', filter);
    equipment.addEventListener('change', filter);
    commit.addEventListener('click', () => {
        if (!addButton || picked.size === 0) return;
        form.querySelectorAll('[name="selectedExerciseIds"]').forEach(input => input.remove());
        picked.forEach(id => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'selectedExerciseIds';
            input.value = id;
            form.append(input);
        });
        dialog.close();
        form.requestSubmit(addButton);
    });
})();

// Workout builder.
(() => {
    'use strict';
    const form = document.getElementById('workout-builder');
    if (!form) return;
    const $ = id => document.getElementById(id);
    const data = JSON.parse($('wb-data').textContent);
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[c]));
    const number = value => new Intl.NumberFormat('ru-RU', {maximumFractionDigits: 1}).format(value);
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
        'Bench Press': ['Жим лёжа', 'Грудь', 'Штанга'],
        'Squat': ['Приседания со штангой', 'Ноги', 'Штанга'],
        'Deadlift': ['Становая тяга', 'Спина', 'Штанга'],
        'Pull Up': ['Подтягивания', 'Спина', 'Турник'],
        'Overhead Press': ['Жим над головой', 'Плечи', 'Штанга'],
        'Romanian Deadlift': ['Румынская тяга', 'Ноги', 'Штанга'],
        'Leg Press': ['Жим ногами', 'Ноги', 'Тренажёр'],
        'Barbell Row': ['Тяга штанги в наклоне', 'Спина', 'Штанга']
    };
    const catalog = data.exercises.map(e => {
        const originalName = e.originalName || e.name;
        const labels = catalogLabels[originalName];
        return {
            ...e,
            originalName,
            name: e.name || labels?.[0] || originalName,
            muscleGroup: e.muscleGroup || labels?.[1] || 'Другое',
            equipment: e.equipment || labels?.[2] || ''
        };
    });
    const exerciseId = window.ExerciseIds.normalize;
    const byId = new Map(catalog.map(e => [e.id, e]));
    const muscleIcons = {
        Грудь: 'chest',
        Спина: 'back',
        Поясница: 'back',
        Трапеции: 'back',
        Плечи: 'shoulders',
        Ноги: 'legs',
        Квадрицепсы: 'legs',
        'Задняя поверхность бедра': 'legs',
        Ягодицы: 'legs',
        Икры: 'legs',
        'Приводящие мышцы бедра': 'legs',
        'Отводящие мышцы бедра': 'legs',
        Пресс: 'core',
        'Косые мышцы живота': 'core',
        'Мышцы кора': 'core',
        Кардио: 'bars'
    };
    const thumb = e => `<div class="wb-thumb" data-group="${escape(e.muscleGroup)}" aria-hidden="true">${icon(muscleIcons[e.muscleGroup] || 'workout')}</div>`;
    const key = `enix-workout-draft:${form.dataset.viewer}:${data.ownerId}`;
    const read = key => {
        try {
            return JSON.parse(localStorage.getItem(key));
        } catch {
            return null;
        }
    };
    const write = (key, value) => {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch {
            return false;
        }
    };
    const remove = key => {
        try {
            localStorage.removeItem(key);
        } catch { /* Storage can be disabled. */
        }
    };
    let state = {
        goal: 'Набор массы',
        level: 'intermediate',
        durationMinutes: 60,
        cover: 'athlete',
        autoDuration: true,
        showRecords: true,
        blocks: [{name: 'Основные упражнения', kind: 'strength', exercises: []}]
    };
    let activeBlock = 0, reorder = false, dirty = false, saving = false, dragId = null, lastLibraryTrigger;
    let selectedExerciseId = null;
    const mobileLayout = matchMedia('(max-width:1023px)');
    const optionalPanels = [...form.querySelectorAll('[data-mobile-disclosure]')];
    const syncPanels = () => optionalPanels.forEach(panel => {
        panel.open = !mobileLayout.matches || (data.hasErrors && panel.classList.contains('wb-advanced'));
    });
    syncPanels();
    let exerciseLibrary;
    const collapsed = new Set();
    const rawSets = new WeakMap();
    const blockTypes = {strength: 'Силовой блок', warmup: 'Разминка', superset: 'Суперсет', cooldown: 'Заминка'};
    const blockHelp = {
        strength: 'Основные упражнения с отдельными подходами и отдыхом.',
        warmup: 'Подготовительные упражнения. Подходы этого блока отмечаются как разминка.',
        superset: 'Связанные упражнения в одном блоке. Подходы отмечаются отдельно; автоматического чередования кругов пока нет.',
        cooldown: 'Упражнения в конце занятия. Значения и отметки подходов задаёте вы.'
    };
    const setValue = (set, field) => rawSets.get(set)?.[field] ?? set[field];

    function rememberField(input) {
        const set = find(exerciseId(input.dataset.id))?.sets[Number(input.dataset.set)];
        if (!set) return;
        rawSets.set(set, {...rawSets.get(set), [input.dataset.field]: input.value});
        if (input.validity.valid && Number.isFinite(input.valueAsNumber)) set[input.dataset.field] = input.valueAsNumber;
        changed();
        renderBlocks();
    }

    function orderMode(value, rebuild = true) {
        reorder = value;
        form.classList.toggle('reordering', value);
        form.querySelector('[data-reorder]').setAttribute('aria-pressed', String(value));
        form.querySelector('[data-reorder-label]').textContent = value ? 'Готово' : 'Порядок';
        form.querySelector('[data-reorder-hint]').hidden = !value;
        if (rebuild) renderExercises();
        else form.querySelectorAll('.wb-exercise-tools').forEach(el => {
            el.hidden = !value;
        });
    }

    let toastTimer;

    function toast(message, state = 'info') {
        $('wb-live').textContent = message;
        window.AppComponents?.setNoticeState($('wb-live'), state);
        $('wb-live').hidden = false;
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => {
            $('wb-live').hidden = true;
        }, 4200);
    }

    const all = () => state.blocks.flatMap(b => b.exercises);
    const find = id => all().find(e => e.exerciseId === id);
    const selected = id => all().some(e => e.exerciseId === id);

    function selectExercise(id) {
        selectedExerciseId = id;
        if (!mobileLayout.matches) return;
        all().forEach(e => collapsed.add(e.exerciseId));
        if (id != null) collapsed.delete(id);
    }

    const emptyInsight = (name, title, text) => `<div class="wb-insight-empty">${icon(name)}<div><strong>${escape(title)}</strong><p>${escape(text)}</p></div></div>`;

    function loadState(value) {
        if (!value || !Array.isArray(value.blocks) || !value.blocks.length || value.blocks.length > 12) return false;
        const ids = new Set();
        for (const b of value.blocks) {
            if (!b || typeof b.name !== 'string' || !b.name.trim() || b.name.length > 60 || !['warmup', 'strength', 'superset', 'cooldown'].includes(b.kind) || !Array.isArray(b.exercises)) return false;
            for (const e of b.exercises) {
                if (e) e.exerciseId = exerciseId(e.exerciseId);
                if (!e || ids.has(e.exerciseId) || !byId.has(e.exerciseId) || !Array.isArray(e.sets) || !e.sets.length || e.sets.length > 20) return false;
                ids.add(e.exerciseId);
                if (e.sets.some(s => !s || !Number.isFinite(s.weight) || !Number.isInteger(s.reps) || !Number.isInteger(s.restSeconds))) return false;
            }
        }
        if (ids.size > 40 || !['athlete', 'summit'].includes(value.cover) || typeof value.goal !== 'string' || !['beginner', 'intermediate', 'advanced'].includes(value.level) || !Number.isFinite(value.durationMinutes)) return false;
        state = value;
        activeBlock = Math.min(activeBlock, state.blocks.length - 1);
        return true;
    }

    if ($('BuilderJson').value) {
        try {
            loadState(JSON.parse($('BuilderJson').value));
        } catch { /* Server validation remains visible. */
        }
    }

    function syncControls() {
        $('wb-goal').value = state.goal;
        $('wb-level').value = state.level;
        $('wb-duration').value = state.durationMinutes;
        $('wb-auto-duration').checked = state.autoDuration;
        $('wb-show-records').checked = state.showRecords;
        $('wb-cover-image').src = `/images/landing/${state.cover}.webp`;
        $('wb-cover-image').alt = state.cover === 'athlete' ? 'Атлет в зале' : 'Горная вершина';
    }

    function renderBlocks() {
        $('wb-blocks').innerHTML = state.blocks.map((b, i) => `<div class="wb-block ${i === activeBlock ? 'active' : ''}" data-block="${i}" data-kind="${b.kind}">
            <div class="wb-block-header"><button type="button" data-select-block="${i}"><span>${i + 1}.</span>${escape(b.name)}</button><button type="button" class="wb-icon" data-block-up="${i}" aria-label="Поднять блок ${escape(b.name)}" ${i === 0 ? 'disabled' : ''}>${icon('up')}</button><button type="button" class="wb-icon" data-block-down="${i}" aria-label="Опустить блок ${escape(b.name)}" ${i === state.blocks.length - 1 ? 'disabled' : ''}>${icon('down')}</button><button type="button" class="wb-icon wb-danger" data-remove-block="${i}" aria-label="Удалить блок ${escape(b.name)}" ${b.exercises.length || state.blocks.length === 1 ? 'disabled' : ''}>${icon('close')}</button></div>
            <div class="wb-block-meta">${blockTypes[b.kind]} · ${exerciseCount(b.exercises.length)} · ${setCount(b.exercises.reduce((n, e) => n + e.sets.length, 0))}</div>
            ${b.exercises.map(e => `<div class="wb-structure-exercise" draggable="true" data-order-key="${e.exerciseId}" data-drag-id="${e.exerciseId}">${icon('grip')}<span>${escape(byId.get(e.exerciseId).name)}</span><small>${setCount(e.sets.length)}</small></div>`).join('')}
            <button type="button" class="wb-block-add" data-open-library="${i}">${icon('plus')}Добавить упражнение</button></div>`).join('');
        $('wb-library-target').innerHTML = state.blocks.map((b, i) => `<option value="${i}" ${i === activeBlock ? 'selected' : ''}>${escape(b.name)}</option>`).join('');
        exerciseLibrary?.setContext(`${$('Input_Title').value || 'Новая тренировка'} → ${state.blocks[activeBlock].name}`);
    }

    function renderExercises() {
        let order = 0;
        if (!all().length) {
            $('wb-exercises').innerHTML = `<div class="wb-empty"><div class="wb-empty-mark">${icon('workout')}</div><h3>Добавьте первое упражнение</h3><p>Выберите одно или несколько упражнений в библиотеке. Подходы и нагрузку можно изменить после добавления.</p><button type="button" class="wb-button" data-quick-start>${icon('plus')}Готовый состав · Всё тело</button><small>3 базовых упражнения · рабочий вес задаёте вы</small></div>`;
            return;
        }
        $('wb-exercises').innerHTML = state.blocks.map((b, bi) => !b.exercises.length ? '' : `<section class="wb-exercise-group ${b.kind === 'superset' ? 'is-superset' : ''}" aria-label="${escape(b.name)}"><div class="wb-group-title" data-kind="${b.kind}"><span>${escape(b.name)} · ${blockTypes[b.kind]}</span><span>${exerciseCount(b.exercises.length)}</span></div>${b.exercises.map((e, ei) => {
            const item = byId.get(e.exerciseId), id = e.exerciseId, closed = collapsed.has(id);
            return `<article class="wb-exercise-card ${closed ? 'collapsed' : ''}" data-exercise="${id}" data-order-key="${id}">
                <div class="wb-exercise-head"><span class="wb-number">${++order}</span>${thumb(item)}<div class="wb-exercise-copy"><strong>${escape(item.name)}</strong><small>${escape(item.muscleGroup)}${item.equipment ? ' · ' + escape(item.equipment) : ''}</small></div><button type="button" class="wb-icon wb-chevron" data-collapse="${id}" aria-expanded="${!closed}" aria-controls="wb-body-${id}" aria-label="Подходы: ${escape(item.name)}">${icon('chevron')}</button><button type="button" class="wb-icon wb-danger" data-remove-exercise="${id}" aria-label="Удалить ${escape(item.name)}">${icon('close')}</button></div>
                <div class="wb-exercise-body" id="wb-body-${id}"><table class="wb-set-table"><thead><tr><th scope="col">Подход</th><th scope="col">Вес (кг)</th><th scope="col">Повторы</th><th scope="col">Отдых, с</th><th scope="col"><span class="wb-sr-only">Удалить</span></th></tr></thead><tbody>${e.sets.map((s, si) => `<tr><td>${si + 1}</td>${[['weight', 0, 2000, '0.01', 'Вес'], ['reps', 1, 1000, '1', 'Повторы'], ['restSeconds', 0, 900, '1', 'Отдых']].map(([field, min, max, step, label]) => `<td><input type="number" required inputmode="${field === 'weight' ? 'decimal' : 'numeric'}" min="${min}" max="${max}" step="${step}" value="${escape(setValue(s, field))}" data-set="${si}" data-field="${field}" data-id="${id}" aria-label="${escape(item.name)}, подход ${si + 1}: ${label}" /></td>`).join('')}<td><button type="button" class="wb-icon wb-danger" data-remove-set="${id}:${si}" ${e.sets.length === 1 ? 'disabled' : ''} aria-label="Удалить подход ${si + 1}: ${escape(item.name)}">${icon('close')}</button></td></tr>`).join('')}</tbody></table>
                <button type="button" class="wb-add-set" data-add-set="${id}" ${e.sets.length >= 20 ? 'disabled' : ''}>${icon('plus')}Добавить подход</button></div>
                <div class="wb-exercise-tools" ${reorder ? '' : 'hidden'}><button class="wb-icon" type="button" data-move="${id}:-1" ${ei === 0 ? 'disabled' : ''} aria-label="Поднять ${escape(item.name)}">${icon('up')}</button><button class="wb-icon" type="button" data-move="${id}:1" ${ei === b.exercises.length - 1 ? 'disabled' : ''} aria-label="Опустить ${escape(item.name)}">${icon('down')}</button><label>Блок<select data-move-to="${id}">${state.blocks.map((block, i) => `<option value="${i}" ${i === bi ? 'selected' : ''}>${escape(block.name)}</option>`).join('')}</select></label></div></article>`;
        }).join('')}</section>`).join('');
    }

    function renderLibrary() {
        exerciseLibrary?.setSelected(all().map(e => e.exerciseId));
    }

    function updateStats() {
        const entries = all(), sets = entries.flatMap(e => e.sets);
        const working = state.blocks.filter(b => b.kind !== 'warmup' && b.kind !== 'cooldown').flatMap(b => b.exercises);
        const workingSets = working.flatMap(e => e.sets);
        const volume = workingSets.reduce((sum, s) => sum + s.weight * s.reps, 0);
        const duration = Math.max(5, Math.min(360, Math.ceil((sets.reduce((sum, s) => sum + s.reps * 4 + s.restSeconds, 0) + entries.length * 60) / 60)));
        if (state.autoDuration && entries.length) state.durationMinutes = duration;
        $('wb-duration').value = state.durationMinutes;
        $('wb-duration').readOnly = state.autoDuration && !!entries.length;
        const muscles = [...new Set(entries.map(e => byId.get(e.exerciseId).muscleGroup))];
        $('wb-volume').innerHTML = `${number(volume)} <small>кг</small>`;
        $('wb-exercise-count').textContent = exerciseCount(entries.length);
        $('wb-forecast-count').textContent = entries.length ? `Выбрано: ${exerciseCount(entries.length)}` : 'Начни с первого упражнения';
        $('wb-focus').textContent = muscles.join(', ') || 'Твой выбор';
        $('wb-muscle-tags').innerHTML = muscles.length ? muscles.map(m => `<span class="wb-tag">${escape(m)}</span>`).join('') : '<span>Выбери упражнения — соберём фокус тренировки</span>';
        $('wb-muscle-tags').hidden = !entries.length;
        $('wb-mobile-summary').textContent = `${plural(state.blocks.length, ['блок', 'блока', 'блоков'])} · ${setCount(sets.length)} · ~ ${entries.length ? state.durationMinutes : 0} мин`;
        $('wb-mobile-summary').hidden = !entries.length;
        $('wb-block-summary').textContent = `${plural(state.blocks.length, ['блок', 'блока', 'блоков'])} · ${exerciseCount(entries.length)}`;
        $('wb-plan-brief').textContent = entries.length ? `${setCount(workingSets.length)} · ≈ ${state.durationMinutes} мин` : 'Появится после выбора упражнений';
        const load = Math.min(10, Math.ceil(workingSets.length / 3));
        $('wb-difficulty').innerHTML = `${load || '—'}<small>/10</small>`;
        $('wb-difficulty-meter').style.width = `${load * 10}%`;
        $('wb-difficulty-note').textContent = load ? load < 4 ? 'Небольшой объём' : load < 7 ? 'Умеренный объём' : 'Высокий объём' : 'Оценка по числу подходов';
        const calories = entries.length && data.bodyWeight > 0 ? Math.round(state.durationMinutes * 5 * data.bodyWeight / 60 / 10) * 10 : null;
        $('wb-calories').innerHTML = `${calories === null ? '—' : '~ ' + number(calories)} <small>ккал</small>`;
        $('wb-calories-note').textContent = !entries.length ? 'Добавь упражнения' : !data.bodyWeight ? 'Нужен вес из профиля' : 'Оценка по весу и времени';
        const previous = data.previous;
        $('wb-volume-change').textContent = previous?.volume > 0 && entries.length ? `${volume >= previous.volume ? '+' : ''}${number((volume / previous.volume - 1) * 100)}% к прошлой` : 'Без разминки и заминки';
        $('wb-comparison').innerHTML = !previous ? emptyInsight('bars', 'Новая точка отсчёта', 'После первой завершённой тренировки здесь появится сравнение объёма и подходов.') : `<div class="wb-deltas">${[['workout', 'Упражнения', entries.length, previous.exercises], ['layers', 'Подходы', workingSets.length, previous.sets], ['bars', 'Общий объём', volume, previous.volume]].map(([ic, label, value, old]) => `<div class="wb-delta">${icon(ic)}<span>${label}</span><strong>${value - old > 0 ? '+' : ''}${label === 'Общий объём' && old > 0 ? number((value / old - 1) * 100) + '%' : number(value - old)}</strong><small>Было ${number(old)} → ${number(value)}</small></div>`).join('')}</div>`;
        const records = state.showRecords ? entries.map(e => ({
            ...e,
            record: data.records.find(r => r.exerciseId === e.exerciseId)?.weight || 0
        })).filter(e => e.record > 0) : [];
        $('wb-records').innerHTML = !state.showRecords ? emptyInsight('trophy', 'Сравнение с рекордами выключено', 'Включи его в настройках тренировки.') : !records.length ? emptyInsight('trophy', 'Твои рекорды ещё впереди', 'Выбери упражнение с записанными результатами — покажем твой лучший вес за последние 50 тренировок.') : records.slice(0, 3).map(e => {
            const planned = Math.max(...e.sets.map(s => s.weight)), gain = planned - e.record;
            return `<div class="wb-record">${thumb(byId.get(e.exerciseId))}<div class="wb-exercise-copy"><strong>${escape(byId.get(e.exerciseId).name)}</strong><small>Лучший вес: ${number(e.record)} кг</small></div><strong>${gain > 0 ? '+' + number(gain) : number(e.record)} кг<small>${gain > 0 ? 'Возможный новый рекорд' : 'Твой ориентир'}</small></strong></div>`;
        }).join('');
        $('BuilderJson').value = JSON.stringify(state);
    }

    function render() {
        renderBlocks();
        renderExercises();
        renderLibrary();
        updateStats();
    }

    function changed() {
        dirty = true;
        updateStats();
    }

    function mutate() {
        const editor = window.ReorderMotion?.capture($('wb-exercises')),
            blocks = window.ReorderMotion?.capture($('wb-blocks'));
        dirty = true;
        render();
        window.ReorderMotion?.play($('wb-exercises'), editor);
        window.ReorderMotion?.play($('wb-blocks'), blocks);
    }

    function addExercise(id) {
        if (selected(id) || !byId.has(id)) return;
        if (all().length >= 40) {
            toast('В одной тренировке может быть до 40 упражнений.');
            return;
        }
        state.blocks[activeBlock].exercises.push({
            exerciseId: id,
            sets: Array.from({length: 3}, () => ({weight: 0, reps: 12, restSeconds: 90}))
        });
        selectExercise(id);
        mutate();
        toast(`${byId.get(id).name} — добавлено`);
    }

    function openLibrary(trigger, index) {
        if (Number.isInteger(index) && state.blocks[index]) activeBlock = index;
        renderBlocks();
        lastLibraryTrigger = trigger;
        if (mobileLayout.matches) {
            $('wb-library').classList.add('open');
            $('wb-library').setAttribute('role', 'dialog');
            $('wb-library').setAttribute('aria-modal', 'true');
            document.body.style.overflow = 'hidden';
            [...form.children].filter(el => !el.contains($('wb-library')) && el.id !== 'wb-live').forEach(el => {
                el.inert = true;
            });
            document.querySelectorAll('.wb-structure,.wb-editor').forEach(el => {
                el.inert = true;
            });
        } else $('wb-library').scrollIntoView({behavior: 'smooth', block: 'nearest'});
        $('wb-search').focus();
    }

    function closeLibrary() {
        $('wb-library').classList.remove('open');
        $('wb-library').removeAttribute('role');
        $('wb-library').removeAttribute('aria-modal');
        document.body.style.overflow = '';
        form.querySelectorAll('[inert]').forEach(el => {
            el.inert = false;
        });
        lastLibraryTrigger?.focus();
    }

    function moveTo(id, index, beforeId = null) {
        const source = state.blocks.find(b => b.exercises.some(e => e.exerciseId === id));
        if (!source || !state.blocks[index] || beforeId === id || (source === state.blocks[index] && !beforeId)) return;
        const entry = source.exercises.find(e => e.exerciseId === id);
        source.exercises = source.exercises.filter(e => e !== entry);
        const destination = state.blocks[index],
            target = destination.exercises.findIndex(e => e.exerciseId === beforeId);
        destination.exercises.splice(target < 0 ? destination.exercises.length : target, 0, entry);
        activeBlock = index;
        mutate();
        toast(`${byId.get(id).name} → ${destination.name}`);
        form.querySelector(`[data-move-to="${id}"]`)?.focus();
    }

    form.addEventListener('click', event => {
        const button = event.target.closest('button');
        if (!button) return;
        const d = button.dataset;
        if ('add' in d) addExercise(exerciseId(d.add));
        if ('openLibrary' in d) openLibrary(button, d.openLibrary === '' ? undefined : Number(d.openLibrary));
        if ('closeLibrary' in d) closeLibrary();
        if ('selectBlock' in d) {
            activeBlock = Number(d.selectBlock);
            renderBlocks();
        }
        if ('removeBlock' in d) {
            const i = Number(d.removeBlock);
            if (state.blocks.length > 1 && !state.blocks[i].exercises.length) {
                state.blocks.splice(i, 1);
                activeBlock = Math.min(activeBlock, state.blocks.length - 1);
                mutate();
            }
        }
        if ('blockUp' in d) {
            const i = Number(d.blockUp);
            if (i > 0) {
                [state.blocks[i - 1], state.blocks[i]] = [state.blocks[i], state.blocks[i - 1]];
                activeBlock = i - 1;
                mutate();
                form.querySelector(`[data-block-down="${i - 1}"]`)?.focus();
                toast(`${state.blocks[i - 1].name} · блок ${i}`);
            }
        }
        if ('blockDown' in d) {
            const i = Number(d.blockDown);
            if (i < state.blocks.length - 1) {
                [state.blocks[i + 1], state.blocks[i]] = [state.blocks[i], state.blocks[i + 1]];
                activeBlock = i + 1;
                mutate();
                form.querySelector(`[data-block-up="${i + 1}"]`)?.focus();
            }
        }
        if ('addBlock' in d) {
            if (state.blocks.length >= 12) return toast('Можно добавить до 12 блоков.');
            $('wb-block-dialog').showModal();
            $('wb-block-name').focus();
        }
        if ('collapse' in d) {
            const id = exerciseId(d.collapse);
            if (collapsed.has(id)) {
                selectExercise(id);
                collapsed.delete(id);
            } else {
                collapsed.add(id);
                selectedExerciseId = null;
            }
            renderExercises();
            form.querySelector(`[data-collapse="${id}"]`)?.focus();
        }
        if ('removeExercise' in d) {
            const id = exerciseId(d.removeExercise);
            state.blocks.forEach(b => {
                b.exercises = b.exercises.filter(e => e.exerciseId !== id);
            });
            collapsed.delete(id);
            if (selectedExerciseId === id) selectExercise(all()[0]?.exerciseId ?? null);
            mutate();
        }
        if ('addSet' in d) {
            const e = find(exerciseId(d.addSet));
            if (e.sets.length < 20) {
                e.sets.push({...e.sets.at(-1)});
                mutate();
                form.querySelector(`[data-add-set="${e.exerciseId}"]`)?.focus();
            }
        }
        if ('removeSet' in d) {
            const [rawId, rawIndex] = d.removeSet.split(':'), id = exerciseId(rawId), index = Number(rawIndex),
                e = find(id);
            if (e.sets.length > 1) {
                e.sets.splice(index, 1);
                mutate();
            }
        }
        if ('reorder' in d) {
            orderMode(!reorder);
            if (reorder) toast('Изменяйте порядок стрелками и выбирайте блок под упражнением.');
        }
        if ('move' in d) {
            const [rawId, rawDelta] = d.move.split(':'), id = exerciseId(rawId), delta = Number(rawDelta),
                b = state.blocks.find(b => b.exercises.some(e => e.exerciseId === id)),
                i = b.exercises.findIndex(e => e.exerciseId === id);
            if (b.exercises[i + delta]) {
                [b.exercises[i], b.exercises[i + delta]] = [b.exercises[i + delta], b.exercises[i]];
                mutate();
                form.querySelector(`[data-move="${id}:${-delta}"]`)?.focus();
                toast(`${byId.get(id).name} · позиция ${all().findIndex(e => e.exerciseId === id) + 1}`);
            }
        }
        if ('changeCover' in d) {
            state.cover = state.cover === 'athlete' ? 'summit' : 'athlete';
            syncControls();
            changed();
        }
        if ('quickStart' in d) {
            activeBlock = state.blocks.findIndex(b => b.kind === 'strength');
            if (activeBlock < 0) activeBlock = 0;
            ['Squat', 'Bench Press', 'Barbell Row'].forEach(name => {
                const e = catalog.find(e => e.originalName === name);
                if (e) addExercise(e.id);
            });
            if (!all().length) openLibrary(button); else {
                if (!$('Input_Title').value) $('Input_Title').value = 'Всё тело';
                toast('План готов. Укажи рабочие веса и повторы.');
            }
        }
        if ('saveDraft' in d) {
            const draft = {
                state,
                raw: all().map(e => ({id: e.exerciseId, sets: e.sets.map(s => rawSets.get(s) || {})})),
                title: $('Input_Title').value,
                date: $('Input_Date').value,
                notes: $('Input_Notes').value,
                selectedExerciseId
            };
            if (write(key, draft)) {
                dirty = false;
                $('wb-draft-notice').hidden = true;
                toast('Черновик сохранён на этом устройстве.', 'local');
            } else toast('Браузер не разрешил сохранить черновик. Не закрывай страницу.', 'error');
        }
        if ('restoreDraft' in d) {
            const draft = read(key);
            if (draft && loadState(draft.state)) {
                if (Array.isArray(draft.raw)) for (const entry of draft.raw) {
                    const exercise = find(exerciseId(entry?.id));
                    if (exercise && Array.isArray(entry.sets)) entry.sets.forEach((values, i) => {
                        if (!exercise.sets[i] || !values) return;
                        rawSets.set(exercise.sets[i], Object.fromEntries(['weight', 'reps', 'restSeconds'].filter(f => typeof values[f] === 'string' && values[f].length < 24).map(f => [f, values[f]])));
                    });
                }
                $('Input_Title').value = typeof draft.title === 'string' ? draft.title : '';
                $('Input_Date').value = typeof draft.date === 'string' ? draft.date : '';
                $('Input_Notes').value = typeof draft.notes === 'string' ? draft.notes : '';
                selectExercise(find(exerciseId(draft.selectedExerciseId)) ? exerciseId(draft.selectedExerciseId) : all()[0]?.exerciseId ?? null);
                syncControls();
                render();
                dirty = false;
                $('wb-draft-notice').hidden = true;
                toast('Черновик восстановлен.', 'local');
            } else toast('Черновик устарел: состав библиотеки изменился. Создай новый план.');
        }
        if ('discardDraft' in d) {
            remove(key);
            $('wb-draft-notice').hidden = true;
        }
    });
    form.addEventListener('input', event => {
        const input = event.target;
        if (input.id === 'wb-search') return;
        if (input.dataset.field) {
            rememberField(input);
            return;
        }
        if (input.id === 'wb-duration') {
            const n = input.valueAsNumber;
            if (Number.isFinite(n)) state.durationMinutes = n;
            dirty = true;
            $('BuilderJson').value = JSON.stringify(state);
            return;
        }
        if (['Input_Title', 'Input_Date', 'Input_Notes'].includes(input.id)) {
            dirty = true;
            if (input.id === 'Input_Title') exerciseLibrary?.setContext(`${input.value || 'Новая тренировка'} → ${state.blocks[activeBlock].name}`);
        }
    });
    form.addEventListener('change', event => {
        const input = event.target;
        if (input.dataset.field) {
            rememberField(input);
            return;
        }
        if (input.id === 'wb-goal') state.goal = input.value;
        else if (input.id === 'wb-level') state.level = input.value;
        else if (input.id === 'wb-auto-duration') state.autoDuration = input.checked;
        else if (input.id === 'wb-show-records') state.showRecords = input.checked;
        else if (input.id === 'wb-duration') {
            if (input.validity.valid) updateStats();
            return;
        } else if (input.id === 'wb-library-target') {
            activeBlock = Number(input.value);
            renderBlocks();
            return;
        } else if (input.dataset.moveTo) return moveTo(exerciseId(input.dataset.moveTo), Number(input.value));
        else if (input.id === 'Input_OwnerId') { // Reload owner-specific history without mixing accounts.
            if (dirty && !confirm('Сменить пользователя? Несохранённый план будет сброшен.')) {
                input.value = data.ownerId;
                return;
            }
            dirty = false;
            location.assign(`/Workouts/Create?userId=${encodeURIComponent(input.value)}`);
            return;
        } else return;
        changed();
    });
    $('wb-block-form').addEventListener('submit', event => {
        event.preventDefault();
        const name = $('wb-block-name').value.trim();
        if (!name) return;
        state.blocks.push({name, kind: $('wb-block-kind').value, exercises: []});
        activeBlock = state.blocks.length - 1;
        $('wb-block-dialog').close();
        $('wb-block-form').reset();
        mutate();
        toast('Блок добавлен. Выбери для него упражнения.');
    });
    document.querySelector('[data-close-block]').addEventListener('click', () => $('wb-block-dialog').close());
    $('wb-block-kind').addEventListener('change', () => {
        if (!$('wb-block-name').value) $('wb-block-name').value = $('wb-block-kind').selectedOptions[0].text;
    });
    const explainBlock = () => {
        $('wb-block-help').textContent = blockHelp[$('wb-block-kind').value];
    };
    $('wb-block-kind').addEventListener('change', explainBlock);
    $('wb-block-form').addEventListener('reset', () => queueMicrotask(explainBlock));
    explainBlock();
    form.addEventListener('dragstart', e => {
        const row = e.target.closest('[data-drag-id]');
        if (row) {
            dragId = exerciseId(row.dataset.dragId);
            e.dataTransfer.setData('text/plain', String(dragId));
            e.dataTransfer.effectAllowed = 'move';
        }
    });
    form.addEventListener('dragover', e => {
        const block = e.target.closest('[data-block]');
        if (block && dragId) {
            e.preventDefault();
            block.classList.add('drag-over');
        }
    });
    form.addEventListener('dragleave', e => e.target.closest('[data-block]')?.classList.remove('drag-over'));
    form.addEventListener('drop', e => {
        const block = e.target.closest('[data-block]');
        if (block && dragId) {
            e.preventDefault();
            moveTo(dragId, Number(block.dataset.block), e.target.closest('[data-drag-id]')?.dataset.dragId);
        }
        dragId = null;
        form.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
    });
    form.addEventListener('dragend', () => {
        dragId = null;
        form.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
    });
    document.addEventListener('keydown', e => {
        if (!$('wb-library').classList.contains('open') || $('wb-block-dialog').open) return;
        if (e.key === 'Escape') {
            e.preventDefault();
            closeLibrary();
        }
        if (e.key === 'Tab') {
            const focusable = [...$('wb-library').querySelectorAll('button:not(:disabled),input,select')].filter(el => el.getClientRects().length);
            const first = focusable[0], last = focusable.at(-1);
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    });
    mobileLayout.addEventListener('change', () => {
        if ($('wb-library').classList.contains('open')) closeLibrary();
        syncPanels();
        selectExercise(selectedExerciseId ?? all()[0]?.exerciseId ?? null);
        renderExercises();
    });
    window.addEventListener('beforeunload', e => {
        if (dirty && !saving) {
            e.preventDefault();
            e.returnValue = '';
        }
    });
    window.addEventListener('pageshow', event => {
        if (!event.persisted) return;
        saving = false;
        form.querySelectorAll('button[type=submit]').forEach(button => {
            button.disabled = false;
            button.removeAttribute('aria-busy');
        });
    });
    form.addEventListener('invalid', event => {
        if (reorder) orderMode(false, false);
        for (let parent = event.target.parentElement; parent && parent !== form; parent = parent.parentElement) if (parent.tagName === 'DETAILS') parent.open = true;
        const card = event.target.closest('.wb-exercise-card');
        if (card) {
            collapsed.delete(exerciseId(card.dataset.exercise));
            card.classList.remove('collapsed');
            card.querySelector('[data-collapse]').setAttribute('aria-expanded', 'true');
        }
    }, true);
    form.addEventListener('submit', async event => {
        event.preventDefault();
        if (saving) return;
        if (!all().length) {
            toast('Добавь хотя бы одно упражнение.');
            openLibrary(form.querySelector('[data-open-library]'));
            return;
        }
        if (!form.reportValidity()) return;
        $('BuilderJson').value = JSON.stringify(state);
        saving = true;
        const buttons = form.querySelectorAll('button[type=submit]');
        buttons.forEach(b => {
            b.disabled = true;
            b.setAttribute('aria-busy', 'true');
        });
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                credentials: 'same-origin'
            });
            if (response.redirected) {
                const destination = new URL(response.url);
                if (destination.origin === location.origin && destination.pathname.startsWith('/Workouts/Details/')) {
                    remove(key);
                    dirty = false;
                    location.assign(destination.href);
                    return;
                }
                toast('Сессия завершилась. Сохрани черновик и войди в аккаунт снова.', 'error');
            } else if (response.ok) {
                const html = new DOMParser().parseFromString(await response.text(), 'text/html');
                const errors = [...html.querySelectorAll('.wb-validation li')].map(el => el.textContent.trim()).filter(Boolean);
                const summary = form.querySelector('.wb-validation');
                summary.classList.remove('validation-summary-valid');
                summary.textContent = errors.join(' ') || 'Не удалось сохранить тренировку. Проверь введённые данные.';
                summary.scrollIntoView({behavior: 'smooth', block: 'center'});
                toast('Проверь параметры тренировки.', 'error');
            } else toast('Не удалось сохранить тренировку. План остался на странице — сохрани черновик.', 'error');
        } catch {
            toast('Связь прервалась. Сохрани черновик и проверь список тренировок перед повторной отправкой.', 'error');
        }
        saving = false;
        buttons.forEach(b => {
            b.disabled = false;
            b.removeAttribute('aria-busy');
        });
    });
    const libraryRoot = $('wb-library');
    if (window.ExerciseLibrary && libraryRoot) {
        exerciseLibrary = new window.ExerciseLibrary(libraryRoot);
        libraryRoot.addEventListener('exercises-selected', event => {
            const picks = event.detail.exercises.filter(p => !selected(p.exerciseId) && byId.has(p.exerciseId));
            if (all().length + picks.length > 40) {
                exerciseLibrary.message('В тренировке может быть до 40 упражнений.');
                return;
            }
            picks.forEach(p => {
                state.blocks[activeBlock].exercises.push({
                    exerciseId: p.exerciseId,
                    sets: Array.from({length: p.setsCount}, () => ({
                        weight: p.weight,
                        reps: p.reps,
                        restSeconds: p.restSeconds
                    }))
                });
                collapsed.delete(p.exerciseId);
            });
            selectExercise(picks[0]?.exerciseId ?? null);
            mutate();
            exerciseLibrary.clear();
            closeLibrary();
            toast(`${exerciseCount(picks.length)} → ${state.blocks[activeBlock].name}`);
            const card = form.querySelector(`[data-exercise="${picks[0]?.exerciseId}"]`);
            card?.classList.add('exercise-just-added');
            card?.scrollIntoView({behavior: 'smooth', block: 'center'});
            card?.querySelector('input')?.focus({preventScroll: true});
        });
    }
    $('wb-draft-notice').hidden = !read(key) || data.hasErrors;
    selectExercise(all()[0]?.exerciseId ?? null);
    syncControls();
    render();
    let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
    const keyboardLayout = () => {
        const viewport = window.visualViewport;
        if (!viewport) return;
        if (viewportWidth !== innerWidth) {
            viewportWidth = innerWidth;
            viewportHeight = viewport.height;
        }
        const input = document.activeElement;
        const editing = form.contains(input) && input.matches('input:not([type=hidden]):not([type=checkbox]):not([type=radio]),textarea,select') &&
            !input.closest('.wb-library,dialog');
        if (!editing) viewportHeight = viewport.height;
        const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
        const open = mobileLayout.matches && editing && (inset > 120 || viewportHeight - viewport.height > 120);
        document.body.classList.toggle('wb-keyboard-open', !!open);
        const box = form.getBoundingClientRect();
        form.style.setProperty('--wb-keyboard-inset', `${open ? inset : 0}px`);
        form.style.setProperty('--wb-save-left', `${box.left}px`);
        form.style.setProperty('--wb-save-width', `${box.width}px`);
        if (open) {
            const input = document.activeElement, field = input.getBoundingClientRect(),
                footer = form.querySelector('.wb-footer').getBoundingClientRect();
            if (field.bottom > footer.top - 16) window.scrollBy({
                top: field.bottom - footer.top + 16,
                behavior: 'instant'
            });
            else if (field.top < viewport.offsetTop + 16) window.scrollBy({
                top: field.top - viewport.offsetTop - 16,
                behavior: 'instant'
            });
        }
    };
    window.visualViewport?.addEventListener('resize', keyboardLayout);
    window.visualViewport?.addEventListener('scroll', keyboardLayout);
    document.addEventListener('focusin', keyboardLayout);
    document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
})();

// Workout execution and synchronization.
// Workout dialogs also run in layouts that do not load dashboard.js.
(() => {
    'use strict';
    if (!document.querySelector('[data-active-workout]')) return;
    const ids = new Set(['exercise-dialog', 'load-template-dialog', 'workout-outline-dialog', 'finish-workout-dialog', 'cancel-workout-dialog']);
    document.addEventListener('click', event => {
        const trigger = event.target.closest('[data-dialog]');
        if (trigger && ids.has(trigger.dataset.dialog)) {
            const dialog = document.getElementById(trigger.dataset.dialog);
            trigger.closest('details')?.removeAttribute('open');
            if (!dialog || dialog.open) return;
            document.querySelectorAll('dialog[open]').forEach(item => item.close());
            dialog.showModal();
        }
        const close = event.target.closest('[data-close]');
        if (close && ids.has(close.closest('dialog')?.id)) close.closest('dialog').close();
        if (event.target instanceof HTMLDialogElement && ids.has(event.target.id)) {
            const box = event.target.getBoundingClientRect();
            if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
        }
    });
})();

// Bodyweight and the additional stages of a drop set share the set's completion.
(() => {
    'use strict';
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;'}[c]));
    const dropMarkup = (drop, index, record = false) => {
        const field = name => record ? `data-set-field="${name}"` : `name="AddSet.Drops[${index}].${name[0].toUpperCase() + name.slice(1)}" data-drop-field="${name}"`;
        return `<div class="set-drop" data-drop-index="${index}"><div class="drop-main"><span class="drop-number">Дроп ${index + 1}</span><input type="${record ? 'text' : 'number'}" inputmode="decimal" min="0" max="10000" step="0.01" ${field('weight')} value="${escape(drop.weight)}" aria-label="Вес дропа ${index + 1}, кг" ${drop.isBodyweight ? 'hidden' : 'required'} /><span class="bodyweight-value" ${drop.isBodyweight ? '' : 'hidden'}>Собственный вес</span><input type="number" inputmode="numeric" min="1" max="1000" ${field('reps')} value="${escape(drop.reps)}" required aria-label="Повторения дропа ${index + 1}" /><button type="button" class="text-button danger-text" data-remove-drop aria-label="Удалить дроп ${index + 1}">×</button></div><label class="bodyweight-check"><input type="checkbox" ${field('isBodyweight')} value="true" data-bodyweight ${drop.isBodyweight ? 'checked' : ''} />Собственный вес</label></div>`;
    };
    const refreshBodyweight = container => container.querySelectorAll('[data-bodyweight]').forEach(check => {
        const row = check.closest('[data-drop-index]') || check.closest('.set-form,.new-set-form,.record-set');
        const weight = row?.querySelector('[name="AddSet.Weight"],[data-drop-field="weight"],[data-set-field="weight"]');
        if (!weight) return;
        if (check.checked) {
            if (!weight.hidden) weight.dataset.externalWeight = weight.value;
            weight.value = '0';
        } else if (weight.hidden && weight.dataset.externalWeight != null) weight.value = weight.dataset.externalWeight;
        weight.hidden = check.checked;
        weight.required = !check.checked;
        weight.setCustomValidity('');
        const label = weight.nextElementSibling;
        if (label?.classList.contains('bodyweight-value')) label.hidden = !check.checked;
    });
    const restoreDrops = (form, pairs) => {
        const drops = [];
        for (let i = 0; i < 10; i++) {
            const value = name => pairs.find(([n]) => n === `AddSet.Drops[${i}].${name}`)?.[1];
            if (value('Reps') == null) break;
            drops.push({weight: value('Weight') ?? '', reps: value('Reps'), isBodyweight: value('IsBodyweight') === 'true'});
        }
        const host = form.querySelector('[data-set-drops]');
        if (host) host.innerHTML = drops.map((d, i) => dropMarkup(d, i)).join('');
        const add = form.querySelector('[data-add-drop]');
        if (add) add.disabled = drops.length >= 10;
        const help = form.querySelector('.drop-help');
        if (help) help.hidden = !drops.length;
        const badge = form.querySelector('[data-drop-badge]');
        if (badge) badge.hidden = !drops.length;
    };
    window.WorkoutSetFields = {dropMarkup, refreshBodyweight, restoreDrops};
    document.addEventListener('change', event => {
        if (event.target.matches('[data-bodyweight]')) refreshBodyweight(event.target.closest('form'));
    }, true);
    const root = document.querySelector('[data-active-workout]');
    if (!root) return;
    root.addEventListener('click', event => {
        const button = event.target.closest('[data-add-drop],[data-remove-drop]');
        if (!button) return;
        const form = button.closest('.set-form'), host = form.querySelector('[data-set-drops]');
        const drops = [...host.children].map(row => ({
            weight: row.querySelector('[data-drop-field="weight"]').value,
            reps: row.querySelector('[data-drop-field="reps"]').value,
            isBodyweight: row.querySelector('[data-bodyweight]').checked
        }));
        if (button.hasAttribute('data-add-drop') && drops.length < 10)
            drops.push({weight: '', reps: drops.at(-1)?.reps || form.elements.namedItem('AddSet.Reps').value, isBodyweight: false});
        else if (button.hasAttribute('data-remove-drop')) drops.splice(Number(button.closest('[data-drop-index]').dataset.dropIndex), 1);
        host.innerHTML = drops.map((d, i) => dropMarkup(d, i)).join('');
        form.querySelector('[data-add-drop]').disabled = drops.length >= 10;
        form.querySelector('.drop-help').hidden = !drops.length;
        const badge = form.querySelector('[data-drop-badge]');
        if (badge) badge.hidden = !drops.length;
        form.dispatchEvent(new Event('input', {bubbles: true}));
        if (button.hasAttribute('data-add-drop')) host.lastElementChild?.querySelector('input').focus();
    });
    refreshBodyweight(root);
})();

// Set results and effort guidance.
(() => {
    'use strict';
    const root = document.querySelector('[data-active-workout]');
    if (!root) return;
    const refresh = () => root.querySelectorAll('.set-form').forEach(form => {
        const completed = form.elements.namedItem('completed')?.checked;
        const warmup = form.elements.namedItem('AddSet.IsWarmup')?.checked;
        const skipped = !completed && form.classList.contains('is-skipped');
        const label = form.querySelector('[data-set-result-state]');
        const text = completed ? 'Выполнен · фактические значения' : skipped ? 'Пропущен' : 'Ещё не выполнен';
        if (label && label.textContent !== text) label.textContent = text;
        const badge = form.querySelector('[data-set-warmup]');
        if (badge) badge.hidden = !warmup;
        const prompt = form.querySelector('[data-effort-prompt]');
        if (prompt) {
            const reserve = form.elements.namedItem('AddSet.Rir');
            prompt.hidden = !completed || warmup || form.dataset.effortNeeded !== 'true' || reserve.value !== '';
            prompt.querySelector('[data-add-effort]').hidden = false;
        }
    });
    root.addEventListener('input', () => queueMicrotask(refresh));
    root.addEventListener('change', () => queueMicrotask(refresh));
    root.addEventListener('workout-state', refresh);
    root.addEventListener('click', event => {
        const trigger = event.target.closest('[data-add-effort]');
        if (!trigger) return;
        const form = trigger.closest('.set-form');
        form.querySelector('.set-extra').open = true;
        form.elements.namedItem('AddSet.Rir').focus();
    });
    root.addEventListener('invalid', event => {
        const details = event.target.closest('.set-extra');
        if (details) details.open = true;
    }, true);
    const list = root.querySelector('.session-exercises');
    if (list) new MutationObserver(refresh).observe(list, {childList: true, subtree: true});
    refresh();
})();

// Workout navigation and exercise ordering.
(() => {
    'use strict';
    const root = document.querySelector('[data-active-workout]');
    const initial = document.getElementById('workout-execution-state');
    if (!root || !initial || root.hasAttribute('data-training-log')) return;
    let state = JSON.parse(initial.textContent);
    if (!['InProgress', 'Paused'].includes(state.status)) return;
    const storageKey = `enix-workout-view:${root.dataset.viewer}:${root.dataset.sessionId}`;
    const reduced = matchMedia('(prefers-reduced-motion: reduce)');
    const overview = root.querySelector('[data-app-overview]');
    const sidebar = root.querySelector('[data-app-sidebar]');
    const outline = document.querySelector('[data-app-outline]');
    const orderDialog = document.getElementById('workout-outline-dialog');
    const dock = root.querySelector('[data-app-dock]');
    const action = root.querySelector('[data-app-action]');
    const feedback = root.querySelector('[data-app-feedback]');
    let selected = null, feedbackTimeout, structure = '';
    let editingOrder = false, orderBusy = false, draftOrder = [], dragged = null;
    const cards = () => [...root.querySelectorAll('.session-exercise')];
    const canReorder = () => cards().some((card, index, entries) => index > 0 && entries[index - 1].dataset.blockKey === card.dataset.blockKey);
    const forms = card => [...(card || root).querySelectorAll('.set-form')];
    const completed = form => form.elements.namedItem('completed').checked;
    const pending = form => !completed(form) && !form.classList.contains('is-skipped');
    const title = card => card.querySelector('h2').textContent;
    const current = () => cards().find(card => card.id === selected);
    const clock = seconds => `${String(Math.floor(Math.max(0, seconds) / 60)).padStart(2, '0')}:${String(Math.floor(Math.max(0, seconds) % 60)).padStart(2, '0')}`;
    try {
        selected = localStorage.getItem(storageKey);
    } catch { /* The server session remains usable without local storage. */
    }
    root.classList.add('workout-app');
    overview.hidden = false;

    function select(id, scroll = false) {
        const card = cards().find(item => item.id === id);
        if (!card) return;
        selected = card.id;
        try {
            localStorage.setItem(storageKey, selected);
        } catch { /* Selection is optional local state. */
        }
        render();
        document.getElementById('workout-outline-dialog').close();
        if (scroll) card.scrollIntoView({behavior: reduced.matches ? 'instant' : 'smooth', block: 'start'});
    }

    function buildOutline() {
        if (editingOrder) return;
        const signature = cards().map(card => `${card.id}:${title(card)}:${card.dataset.blockKey}`).join('|');
        if (signature === structure) return;
        structure = signature;
        sidebar.replaceChildren();
        outline.replaceChildren();
        for (const container of [sidebar, outline]) buildRows(container, cards());
    }

    function buildRows(container, entries) {
        container.replaceChildren();
        let previous = null;
        entries.forEach((card, index) => {
            if (card.dataset.blockKey !== previous) {
                previous = card.dataset.blockKey;
                const heading = document.createElement('div');
                heading.className = 'workout-outline-block';
                heading.dataset.kind = card.dataset.blockKind;
                const type = document.createElement('span');
                type.textContent = card.dataset.blockType;
                const name = document.createElement('strong');
                name.textContent = card.dataset.blockName;
                heading.append(type);
                if (name.textContent !== type.textContent) heading.append(name);
                if (container === outline && card.dataset.blockKind === 'superset') {
                    const hint = document.createElement('small');
                    hint.textContent = 'Связанные упражнения · подходы отмечаются отдельно';
                    heading.append(hint);
                }
                container.append(heading);
            }
            const row = document.createElement('div');
            row.className = 'workout-order-row';
            row.dataset.orderKey = card.id;
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'workout-app-exercise app-item-row';
            button.dataset.exerciseTarget = card.id;
            const number = document.createElement('span');
            number.className = 'workout-app-exercise-number';
            number.textContent = index + 1;
            const copy = document.createElement('span');
            const name = document.createElement('strong');
            name.textContent = title(card);
            const count = document.createElement('small');
            count.dataset.exerciseCount = '';
            const stepState = document.createElement('small');
            stepState.dataset.exerciseStatus = '';
            stepState.className = 'app-exercise-state';
            copy.append(name, count, stepState);
            button.append(number, copy);
            button.disabled = container === outline && editingOrder;
            button.addEventListener('click', () => select(card.id, true));
            row.append(button);
            if (container === outline && editingOrder) {
                row.draggable = true;
                row.dataset.orderEntry = card.id;
                for (const delta of [-1, 1]) {
                    const move = document.createElement('button');
                    move.type = 'button';
                    move.className = 'icon-button';
                    move.dataset.orderMove = `${card.id}:${delta}`;
                    move.textContent = delta < 0 ? '↑' : '↓';
                    move.setAttribute('aria-label', `${delta < 0 ? 'Поднять' : 'Опустить'} ${title(card)}`);
                    move.disabled = orderBusy || entries[index + delta]?.dataset.blockKey !== card.dataset.blockKey;
                    move.addEventListener('click', () => moveOrder(card.id, index + delta, `${card.id}:${-delta}`));
                    row.append(move);
                }
            }
            container.append(row);
        });
    }

    const orderMessage = message => {
        const status = orderDialog.querySelector('[data-order-status]');
        status.textContent = message;
        status.hidden = !message;
    };

    function renderOrder() {
        buildRows(outline, draftOrder.map(id => cards().find(card => card.id === id)).filter(Boolean));
        render();
    }

    function moveOrder(id, destination, focusKey) {
        const from = draftOrder.indexOf(id), target = cards().find(card => card.id === draftOrder[destination]),
            source = cards().find(card => card.id === id);
        if (!target || !source || source.dataset.blockKey !== target.dataset.blockKey || from === destination || orderBusy) return;
        const before = window.ReorderMotion?.capture(outline);
        draftOrder.splice(from, 1);
        draftOrder.splice(destination, 0, id);
        renderOrder();
        window.ReorderMotion?.play(outline, before);
        orderDialog.querySelector(`[data-order-move="${focusKey || id + ':1'}"]`)?.focus();
        orderMessage(`${title(source)} · позиция ${destination + 1}. Сохраните, чтобы применить.`);
    }

    function editOrder(value) {
        editingOrder = value;
        root.dataset.orderEditing = String(value);
        orderDialog.classList.toggle('is-ordering', value);
        for (const selector of ['[data-order-help]', '[data-order-actions]']) orderDialog.querySelector(selector).hidden = !value;
        orderDialog.querySelector('[data-order-edit]').hidden = value || !canReorder();
        orderDialog.querySelector('[data-add-exercise]').hidden = value;
        orderMessage('');
        if (value) {
            draftOrder = cards().map(card => card.id);
            renderOrder();
        } else {
            structure = '';
            render();
            if (orderDialog.open) orderDialog.querySelector('[data-order-edit]').focus();
        }
    }

    orderDialog.querySelector('[data-order-edit]').addEventListener('click', () => editOrder(true));
    orderDialog.querySelector('[data-order-cancel]').addEventListener('click', () => editOrder(false));
    orderDialog.addEventListener('close', () => {
        if (editingOrder && !orderBusy) editOrder(false);
    });
    orderDialog.addEventListener('cancel', event => {
        if (orderBusy) event.preventDefault();
    });
    orderDialog.querySelector('[data-order-save]').addEventListener('click', async () => {
        if (orderBusy || !editingOrder || !root.reorderWorkout) return;
        orderBusy = true;
        orderDialog.querySelectorAll('button').forEach(button => {
            button.disabled = true;
        });
        orderMessage('Сохраняем порядок…');
        try {
            const result = await root.reorderWorkout(draftOrder.map(id => Number(cards().find(card => card.id === id).dataset.entryId)));
            const container = root.querySelector('.session-exercises');
            const marker = document.createComment('exercise order');
            container.insertBefore(marker, container.querySelector('.session-exercise'));
            result.order.forEach(id => {
                const card = cards().find(card => Number(card.dataset.entryId) === id);
                if (card) container.insertBefore(card, marker);
            });
            marker.remove();
            cards().forEach((card, index) => {
                card.querySelector('.exercise-order').textContent = index + 1;
            });
            orderBusy = false;
            editOrder(false);
            notify('Порядок сохранён. Результаты подходов на месте.');
            orderMessage('Порядок сохранён');
        } catch (error) {
            orderMessage(error.message);
        } finally {
            orderBusy = false;
            orderDialog.querySelectorAll('button').forEach(button => {
                button.disabled = false;
            });
            if (editingOrder) renderOrder();
        }
    });
    outline.addEventListener('dragstart', event => {
        const row = event.target.closest('[data-order-entry]');
        if (!row || orderBusy) return event.preventDefault();
        dragged = row.dataset.orderEntry;
        event.dataTransfer.setData('text/plain', dragged);
        event.dataTransfer.effectAllowed = 'move';
    });
    outline.addEventListener('dragover', event => {
        const row = event.target.closest('[data-order-entry]'), source = cards().find(card => card.id === dragged),
            target = cards().find(card => card.id === row?.dataset.orderEntry);
        if (source && target && source.dataset.blockKey === target.dataset.blockKey) event.preventDefault();
    });
    outline.addEventListener('drop', event => {
        const row = event.target.closest('[data-order-entry]');
        if (row && dragged) {
            event.preventDefault();
            moveOrder(dragged, draftOrder.indexOf(row.dataset.orderEntry));
        }
        dragged = null;
    });
    outline.addEventListener('dragend', () => {
        dragged = null;
    });

    function render() {
        buildOutline();
        if (!current()) selected = cards().find(card => forms(card).some(pending))?.id || cards()[0]?.id;
        const card = current(), all = forms(), done = all.filter(completed).length,
            total = all.filter(form => completed(form) || pending(form)).length;
        const progress = root.querySelector('[data-app-progress]');
        progress.max = Math.max(1, total);
        progress.value = done;
        root.querySelector('[data-app-count]').textContent = total ? `${done} / ${total} подходов` : 'Начните с первого упражнения';
        root.querySelector('[data-app-outline-count]').textContent = `Состав · ${cards().length}`;
        sidebar.hidden = cards().length === 0;
        const hasExercises = cards().length > 0;
        overview.hidden = !hasExercises;
        dock.hidden = !hasExercises || state.status === 'Paused';
        root.querySelector('[data-exercises-heading]').hidden = !hasExercises;
        root.querySelector('[data-exercise-total]').textContent = cards().length;
        root.querySelector('.workout-empty')?.toggleAttribute('hidden', hasExercises);
        overview.querySelector('[data-dialog="workout-outline-dialog"]').hidden = !hasExercises;
        if (!editingOrder) orderDialog.querySelector('[data-order-edit]').hidden = !canReorder();
        cards().forEach(item => {
            const selectedCard = item === card;
            item.classList.toggle('is-current', selectedCard);
            item.setAttribute('aria-label', title(item));
            const next = forms(item).find(pending);
            forms(item).forEach(form => form.classList.toggle('is-next-set', form === next));
            if (selectedCard && next) next.querySelector('input[name="AddSet.Reps"]').setAttribute('min', '1');
        });
        for (const button of document.querySelectorAll('[data-exercise-target]')) {
            const item = cards().find(c => c.id === button.dataset.exerciseTarget);
            if (!item) continue;
            const sets = forms(item), count = sets.filter(completed).length;
            button.setAttribute('aria-current', item === card ? 'step' : 'false');
            const hasPending = sets.some(pending),
                skipped = sets.filter(form => form.classList.contains('is-skipped')).length;
            const isCurrent = item === card;
            let stepState = !sets.length ? 'empty' : hasPending ? count ? 'partial' : 'pending' : count === sets.length ? 'done' : count ? 'partial' : 'skipped';
            let stepLabel = !sets.length ? 'Пока без подходов' : hasPending ? count ? 'Часть выполнена' : 'Ожидает выполнения' : skipped === sets.length ? 'Все пропущены' : skipped ? 'Разобрано · есть пропуски' : 'Все выполнены';
            if (isCurrent && hasPending) {
                const ownRest = state.remainingRestSeconds > 0 && sets.some(form => Number(form.elements.namedItem('setId').value) === state.restAfterSetId);
                stepState = state.status === 'Paused' ? 'paused' : ownRest ? 'rest' : 'current';
                stepLabel = state.status === 'Paused' ? 'На паузе' : ownRest ? 'Отдых после подхода' : 'Сейчас';
            }
            button.dataset.appStep = stepState;
            button.classList.toggle('is-done', stepState === 'done');
            button.querySelector('[data-exercise-count]').textContent = `${count} / ${sets.length} подходов`;
            button.querySelector('[data-exercise-status]').textContent = stepLabel;
        }
        const paused = state.status === 'Paused', rest = state.remainingRestSeconds;
        const next = card && forms(card).find(pending);
        const another = cards().find(item => item !== card && forms(item).some(pending));
        let caption = card ? title(card) : 'Свободная тренировка', detail, label, mode;
        if (paused) {
            caption = 'Занятие на паузе';
            detail = rest != null ? `Отдых остановлен · ${clock(rest)}` : 'Продолжите, когда будете готовы';
            label = 'Тренировка на паузе';
            mode = 'paused';
        } else if (rest > 0) {
            caption = 'Отдых после подхода';
            detail = clock(rest);
            label = 'Закончить отдых';
            mode = 'rest';
        } else if (!card) {
            detail = 'Собирайте занятие по ходу';
            label = 'Выбрать упражнение';
            mode = 'add';
        } else if (next) {
            detail = `Подход ${next.elements.namedItem('AddSet.SetNumber').value} · вес и повторения выше`;
            label = 'Подход выполнен';
            mode = 'set';
        } else if (another) {
            detail = 'Подходы этого упражнения разобраны';
            label = 'Следующее упражнение';
            mode = 'next';
        } else {
            detail = 'Добавьте упражнение или завершите занятие кнопкой вверху';
            label = 'Все подходы отмечены';
            mode = 'done';
        }
        root.querySelector('[data-app-action-caption]').textContent = caption;
        root.querySelector('[data-app-action-detail]').textContent = detail;
        action.textContent = label;
        action.dataset.mode = mode;
        action.disabled = paused || mode === 'done';
        root.querySelector('[data-finish]').disabled = !all.some(form => completed(form) &&
            !form.elements.namedItem('AddSet.IsWarmup').checked && Number(form.elements.namedItem('AddSet.Reps').value) > 0);
        dock.classList.toggle('is-resting', rest > 0 && !paused);
        dock.classList.toggle('is-paused', paused);
        root.querySelector('[data-app-rest-tools]').hidden = rest == null || rest <= 0 || paused;
        if (innerWidth >= 1024) {
            const bounds = root.querySelector('.session-exercises').getBoundingClientRect();
            root.style.setProperty('--workout-dock-left', `${bounds.left}px`);
            root.style.setProperty('--workout-dock-width', `${bounds.width}px`);
        }
    }

    function notify(text, undoForm) {
        clearTimeout(feedbackTimeout);
        feedback.replaceChildren();
        const message = document.createElement('span');
        message.textContent = text;
        feedback.append(message);
        if (undoForm) {
            const undo = document.createElement('button');
            undo.type = 'button';
            undo.textContent = 'Отменить';
            undo.addEventListener('click', () => {
                if (undoForm.isConnected && state.status !== 'Paused') {
                    const setId = Number(undoForm.elements.namedItem('setId').value);
                    const input = undoForm.elements.namedItem('completed');
                    input.checked = false;
                    input.dispatchEvent(new Event('change', {bubbles: true}));
                    if (state.restAfterSetId === setId) root.querySelector('[data-control="skip-rest"]').click();
                    render();
                }
                feedback.hidden = true;
            });
            feedback.append(undo);
        }
        feedback.hidden = false;
        feedbackTimeout = setTimeout(() => {
            feedback.hidden = true;
        }, 6500);
    }

    root.addEventListener('workout-state', event => {
        state = event.detail;
        render();
    });
    root.addEventListener('workout-exercises-changed', event => {
        if (event.detail.exerciseId) selected = event.detail.exerciseId;
        render();
        if (selected) select(selected);
        notify('Упражнение добавлено в занятие');
    });
    root.addEventListener('change', event => {
        const form = event.target.closest('.set-form');
        if (!form) return;
        queueMicrotask(() => {
            render();
            if (event.target.name === 'completed' && completed(form) && form.classList.contains('is-completed')) {
                form.classList.remove('app-set-pulse');
                void form.offsetWidth;
                form.classList.add('app-set-pulse');
                notify('Подход отмечен', form);
            }
        });
    });
    root.addEventListener('input', event => {
        if (event.target.closest('.set-form')) render();
    });
    action.addEventListener('click', () => {
        switch (action.dataset.mode) {
            case 'resume':
                root.querySelector('[data-pause-button]').click();
                break;
            case 'rest':
                root.querySelector('[data-control="skip-rest"]').click();
                break;
            case 'add':
                root.querySelector('[data-add-exercise]').click();
                break;
            case 'next':
                select(cards().find(item => item !== current() && forms(item).some(pending)).id, true);
                break;
            case 'finish':
                root.querySelector('[data-finish]').click();
                break;
            case 'set': {
                const form = forms(current()).find(pending), input = form.elements.namedItem('completed');
                input.checked = true;
                const reps = form.elements.namedItem('AddSet.Reps');
                reps.setCustomValidity(Number(reps.value) < 1 ? 'Укажите хотя бы одно повторение.' : '');
                if (!form.reportValidity()) {
                    input.checked = false;
                    form.scrollIntoView({behavior: reduced.matches ? 'instant' : 'smooth', block: 'center'});
                    return;
                }
                input.dispatchEvent(new Event('change', {bubbles: true}));
                break;
            }
        }
    });
    // Removal by the execution client must update counts and navigation as well.
    new MutationObserver(() => render()).observe(root.querySelector('.session-exercises'), {
        childList: true,
        subtree: true
    });
    new ResizeObserver(() => render()).observe(root.querySelector('.session-exercises'));
    let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
    const keyboardLayout = () => {
        const viewport = window.visualViewport;
        if (!viewport) return;
        if (viewportWidth !== innerWidth) {
            viewportWidth = innerWidth;
            viewportHeight = viewport.height;
        }
        const input = document.activeElement;
        const editing = matchMedia('(max-width:1023px)').matches && root.contains(input) &&
            !input?.closest('dialog') && input?.matches('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),textarea');
        const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
        const open = editing && (inset > 120 || viewportHeight - viewport.height > 120);
        document.body.classList.toggle('workout-keyboard-open', !!open);
        root.style.setProperty('--workout-keyboard-inset', `${open ? inset : 0}px`);
        if (open) {
            const inputBounds = document.activeElement.getBoundingClientRect(),
                dockBounds = dock.getBoundingClientRect();
            if (inputBounds.bottom > dockBounds.top - 16) window.scrollBy({
                top: inputBounds.bottom - dockBounds.top + 16,
                behavior: 'instant'
            });
        }
        if (!editing) viewportHeight = viewport.height;
    };
    window.visualViewport?.addEventListener('resize', keyboardLayout);
    window.visualViewport?.addEventListener('scroll', keyboardLayout);
    document.addEventListener('focusin', keyboardLayout);
    document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
    render();
})();

// Workout execution, synchronization and timers.
(() => {
    'use strict';
    const root = document.querySelector('[data-active-workout]');
    if (!root) return;
    const initialState = document.getElementById('workout-execution-state');
    if (!initialState) return; // An older running server can still render the pre-migration page.
    const key = `enix-session-queue:${root.dataset.viewer}:${root.dataset.sessionId}`;
    const status = root.querySelector('[data-sync-status]');
    let state = JSON.parse(initialState.textContent);
    let revision = state.revision, queue = [], sending = false, refreshing = false, conflict = false,
        storageAvailable = true;
    let offset = Date.parse(state.serverNowUtc) - Date.now(), sampledAt = Date.now(), soundedRest = null;
    let sound = false, audioContext = null, wakeLock = null;
    const active = () => ['InProgress', 'Paused'].includes(state.status);
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const clock = seconds => `${String(Math.floor(Math.max(0, seconds) / 60)).padStart(2, '0')}:${String(Math.floor(Math.max(0, seconds) % 60)).padStart(2, '0')}`;
    const scratchKey = `${key}:fields`;
    let scratch = {}, rejectedOperationId = null;
    const debounce = new Map();
    const pickerRoot = document.getElementById('workout-library'), picker = pickerRoot?.exerciseLibrary;
    let picking = false, ordering = false, cancelling = false, pickAttempt = null, orderAttempt = null;

    async function flushResults() {
        for (const timeout of debounce.values()) clearTimeout(timeout);
        debounce.clear();
        if (state.status !== 'Paused') for (const id of Object.keys(scratch)) {
            const form = [...root.querySelectorAll('.set-form')].find(f => f.elements.namedItem('setId').value === id);
            if (form && !saveSet(form)) throw new Error('Заполните результаты подходов перед изменением состава.');
        }
        await drain();
        for (let n = 0; sending && n < 200; n++) await new Promise(resolve => setTimeout(resolve, 25));
        if (queue.length || sending) throw new Error('Дождитесь сохранения результатов. Ваш выбор остаётся здесь.');
    }

    root.reorderWorkout = async entries => {
        if (picking || ordering || cancelling) throw new Error('Дождитесь сохранения состава тренировки.');
        ordering = true;
        try {
            await flushResults();
            const signature = JSON.stringify(entries);
            if (orderAttempt?.signature !== signature) orderAttempt = {signature, id: crypto.randomUUID(), revision};
            const body = new URLSearchParams({
                orderJson: signature,
                operationId: orderAttempt.id,
                expectedRevision: orderAttempt.revision,
                __RequestVerificationToken: token
            });
            const response = await fetch(`${location.pathname}?handler=Reorder`, {
                method: 'POST',
                headers: {'X-Requested-With': 'XMLHttpRequest'},
                body,
                redirect: 'error'
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok) throw new Error(response.status === 409 ? 'Тренировка изменена в другом окне. Обновите страницу перед перестановкой.' : result.error || 'Не удалось сохранить порядок. Попробуйте ещё раз.');
            revision = result.state.revision;
            persist();
            update(result.state);
            orderAttempt = null;
            syncMessage();
            return result;
        } catch (error) {
            throw new Error(error instanceof TypeError ? 'Нет связи. Порядок остался в этом окне. Подключитесь и нажмите «Сохранить порядок» ещё раз.' : error.message);
        } finally {
            ordering = false;
        }
    };
    // Structural log edits use the same queue and revision as set results.
    root.saveWorkoutStructure = async (form, submitter) => {
        if (picking || ordering || cancelling) throw new Error('Дождитесь сохранения тренировки.');
        ordering = true;
        try {
            await flushResults();
            const fields = new FormData(form);
            if (submitter?.name) fields.set(submitter.name, submitter.value);
            form.dataset.operationId ||= crypto.randomUUID();
            fields.set('operationId', form.dataset.operationId);
            fields.set('expectedRevision', revision);
            const response = await fetch(form.getAttribute('action') || location.pathname, {method: 'POST', body: fields, headers: {'X-Requested-With': 'XMLHttpRequest'}});
            if (!response.ok) {
                const error = await response.json().catch(() => ({}));
                throw new Error(error.error || 'Не удалось сохранить изменение. Обновите страницу и повторите.');
            }
            const html = new DOMParser().parseFromString(await response.text(), 'text/html');
            const nextState = html.getElementById('workout-execution-state');
            const errors = html.querySelector('.validation-summary-errors');
            if (errors) throw new Error(errors.textContent.trim());
            const nextList = html.querySelector('.session-exercises');
            if (!nextState || !nextList) throw new Error('Не удалось получить тренировку. Проверьте вход в аккаунт.');
            root.dispatchEvent(new CustomEvent('workout-exercises-replacing'));
            root.querySelector('.session-exercises').replaceChildren(...nextList.children);
            const latest = JSON.parse(nextState.textContent);
            revision = latest.revision;
            persist();
            update(latest);
            picker?.setSelected([...root.querySelectorAll('[data-catalog-id]')].map(card => window.ExerciseIds.normalize(card.dataset.catalogId)));
            root.dispatchEvent(new CustomEvent('workout-exercises-changed'));
            syncMessage();
            delete form.dataset.operationId;
            return latest;
        } finally { ordering = false; }
    };
    root.pickWorkoutExercises = async (exercises, entryId = 0) => {
        if (picking || ordering || cancelling) throw new Error('Дождитесь сохранения состава тренировки.');
        picking = true;
        picker.setBusy(true);
        picker.message('');
        try {
            await flushResults();
            const entry = String(entryId);
            const signature = JSON.stringify([entry, exercises]);
            if (pickAttempt?.signature !== signature) pickAttempt = {signature, id: crypto.randomUUID()};
            const body = new URLSearchParams({
                __RequestVerificationToken: token,
                selectionJson: JSON.stringify(exercises),
                ExerciseEntryId: entry,
                expectedRevision: revision,
                operationId: pickAttempt.id
            });
            const response = await fetch(`${location.pathname}?handler=PickExercises`, {
                method: 'POST',
                headers: {'X-Requested-With': 'XMLHttpRequest'},
                body,
                redirect: 'error'
            });
            if (!response.ok) {
                const data = await response.json().catch(() => ({}));
                throw new Error(data.error || 'Не удалось добавить упражнения. Попробуйте ещё раз.');
            }
            const html = new DOMParser().parseFromString(await response.text(), 'text/html');
            const nextList = html.querySelector('.session-exercises'),
                nextState = html.getElementById('workout-execution-state');
            if (!nextList || !nextState) throw new Error('Обновите страницу, чтобы увидеть сохранённые упражнения.');
            const currentList = root.querySelector('.session-exercises');
            root.dispatchEvent(new CustomEvent('workout-exercises-replacing'));
            if (entry === '0') {
                const existing = new Set([...currentList.children].map(card => card.id));
                for (const card of [...nextList.children]) if (!existing.has(card.id)) currentList.append(card);
            } else {
                const nextCard = [...nextList.children].find(card => card.id === `exercise-${entry}`);
                if (!nextCard) throw new Error('Обновите страницу, чтобы увидеть замену.');
                document.getElementById(`exercise-${entry}`).replaceWith(nextCard);
            }
            const latest = JSON.parse(nextState.textContent);
            revision = latest.revision;
            persist();
            update(latest);
            picker.setSelected([...html.querySelectorAll('#workout-library .is-added')].map(row => window.ExerciseIds.normalize(row.dataset.exerciseId)));
            picker.clear();
            pickAttempt = null;
            document.getElementById('exercise-dialog').close();
            const empty = root.querySelector('.workout-empty');
            if (empty) empty.hidden = true;
            const heading = root.querySelector('[data-exercises-heading]');
            if (heading) heading.hidden = false;
            const total = root.querySelector('[data-exercise-total]');
            if (total) total.textContent = currentList.children.length;
            root.querySelector('[data-dialog="load-template-dialog"]')?.setAttribute('hidden', '');
            syncMessage(entry === '0' ? 'Упражнения добавлены · все изменения сохранены' : 'Упражнение заменено · все изменения сохранены');
            const ids = exercises.map(p => p.exerciseId);
            const card = [...root.querySelectorAll('.session-exercise')].find(el => ids.includes(window.ExerciseIds.normalize(el.dataset.catalogId)));
            root.dispatchEvent?.(new CustomEvent('workout-exercises-changed', {detail: {exerciseId: card?.id}}));
            card?.classList.add('exercise-just-added');
            card?.scrollIntoView({
                behavior: window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches ? 'instant' : 'smooth',
                block: 'start'
            });
            if (state.status !== 'Paused' && window.matchMedia?.('(min-width:1024px)')?.matches) card?.querySelector('input:not([type="hidden"])')?.focus({preventScroll: true});
            return latest;
        } catch (error) {
            picker.message(error instanceof TypeError ? 'Нет соединения. Выбранные упражнения остались здесь — повторите добавление после восстановления связи.' : error.message);
            throw error;
        } finally {
            picking = false;
            picker.setBusy(false);
        }
    };
    if (picker) pickerRoot.addEventListener('exercises-selected', event => {
        void root.pickWorkoutExercises(event.detail.exercises, document.querySelector('[data-exercise-entry]').value).catch(() => {});
    });

    function persist() {
        try {
            localStorage.setItem(scratchKey, JSON.stringify(scratch));
            localStorage.setItem(key, JSON.stringify({revision, queue}));
            storageAvailable = true;
        } catch {
            storageAvailable = false;
        }
    }

    try {
        scratch = JSON.parse(localStorage.getItem(scratchKey)) || {};
        const saved = JSON.parse(localStorage.getItem(key));
        if (Array.isArray(saved?.queue) && saved.queue.length) {
            queue = saved.queue;
            revision = saved.revision;
        }
        if (root.dataset.recordedRequest) {
            const draftKey = `enix-record-draft:${root.dataset.viewer}`;
            const draft = JSON.parse(localStorage.getItem(draftKey));
            if (draft?.fields?.ClientRequestId === root.dataset.recordedRequest) localStorage.removeItem(draftKey);
        }
    } catch { /* Storage may be unavailable. */
    }

    function syncMessage(message, explicitState) {
        const hasDraft = Object.keys(scratch).length > 0;
        status.textContent = message || (queue.length ? storageAvailable ? `${queue.length} изменений на устройстве · ожидают отправки` : `${queue.length} изменений ожидают отправки · локальное восстановление недоступно` : hasDraft ? storageAvailable ? 'Черновик полей сохранён на устройстве' : 'Черновик полей остаётся на странице · восстановление недоступно' : storageAvailable ? 'Все изменения сохранены' : 'Сохранено на сервере. Локальное восстановление недоступно.');
        status.classList.toggle('has-pending', queue.length > 0);
        window.AppComponents?.setNoticeState(status, explicitState || (queue.length ? 'pending' : hasDraft ? storageAvailable ? 'local' : 'error' : 'saved'));
    }

    function update(data) {
        state = data;
        offset = Date.parse(data.serverNowUtc) - Date.now();
        sampledAt = Date.now();
        queue.forEach(previewOperation);
        tick();
        maintainWakeLock();
    }

    function tick() {
        root.dataset.sessionStatus = state.status;
        const elapsed = state.elapsedSeconds + (state.status === 'InProgress' ? (Date.now() - sampledAt) / 1000 : 0);
        root.querySelectorAll('[data-session-clock]').forEach(sessionClock => {
            sessionClock.textContent = root.hasAttribute('data-quick-workout')
                ? `${String(Math.floor(elapsed / 3600)).padStart(2, '0')}:${clock(elapsed % 3600)}` : clock(elapsed);
        });
        const paused = state.status === 'Paused', notice = root.querySelector('[data-pause-notice]'),
            pauseButton = root.querySelector('[data-pause-button]');
        if (notice) notice.hidden = !paused;
        if (pauseButton) {
            pauseButton.dataset.control = paused ? 'resume' : 'pause';
            pauseButton.textContent = paused ? 'Продолжить' : 'Пауза';
        }
        root.querySelectorAll('.set-form input:not([type="hidden"]),.set-form button').forEach(input => {
            if (active()) input.disabled = paused;
        });
        const remaining = paused ? state.restRemainingSeconds : state.restEndsAtUtc ? Math.max(0, Math.ceil((Date.parse(state.restEndsAtUtc) - Date.now() - offset) / 1000)) : state.restRemainingSeconds;
        root.dispatchEvent?.(new CustomEvent('workout-state', {
            detail: {
                ...state,
                remainingRestSeconds: remaining,
                elapsedSeconds: elapsed
            }
        }));
        const panel = root.querySelector('[data-rest-panel]');
        if (panel) {
            if (!root.hasAttribute('data-quick-workout')) {
                panel.hidden = remaining == null || !active();
                root.querySelector('[data-rest-clock]').textContent = clock(remaining || 0);
                root.querySelector('[data-rest-caption]').textContent = paused ? 'Отдых на паузе' : remaining === 0 ? 'Можно продолжать' : 'Отдых после подхода';
            }
            if (remaining === 0 && !paused && state.restEndsAtUtc && soundedRest !== state.restEndsAtUtc) {
                soundedRest = state.restEndsAtUtc;
                if (sound && audioContext) {
                    const oscillator = audioContext.createOscillator(), gain = audioContext.createGain();
                    oscillator.frequency.value = 660;
                    gain.gain.value = 0.12;
                    oscillator.connect(gain);
                    gain.connect(audioContext.destination);
                    oscillator.start();
                    oscillator.stop(audioContext.currentTime + 0.3);
                }
            }
        }
    }

    async function maintainWakeLock() {
        if (!('wakeLock' in navigator)) return;
        try {
            if (state.status === 'InProgress' && document.visibilityState === 'visible' && !wakeLock) {
                wakeLock = await navigator.wakeLock.request('screen');
                wakeLock.addEventListener('release', () => wakeLock = null);
            } else if (state.status !== 'InProgress' && wakeLock) {
                await wakeLock.release();
                wakeLock = null;
            }
        } catch { /* Deadlines do not depend on screen lock. */
        }
    }

    function restoreForms() {
        for (const op of queue.filter(o => o.kind === 'set')) {
            const form = [...root.querySelectorAll('.set-form')].find(f => f.elements.namedItem('setId')?.value === op.setId);
            if (!form) continue;
            window.WorkoutSetFields.restoreDrops(form, op.fields);
            for (const input of form.querySelectorAll('input[name]')) {
                if (input.name === '__RequestVerificationToken') continue;
                const pair = op.fields.find(([n]) => n === input.name);
                if (input.type === 'checkbox') input.checked = !!pair; else if (pair) input.value = pair[1];
            }
            form.classList.toggle('is-completed', form.elements.namedItem('completed').checked);
            form.classList.toggle('is-skipped', op.fields.some(([n, v]) => n === 'skipped' && v === 'true'));
            window.WorkoutSetFields.refreshBodyweight(form);
        }
    }

    async function drain() {
        if (sending || conflict || cancelling || !queue.length) return;
        sending = true;
        try {
            while (queue.length && !cancelling) {
                const op = queue[0];
                syncMessage('Сохраняем результаты…', 'pending');
                const body = new URLSearchParams(op.fields);
                body.set('__RequestVerificationToken', token);
                body.set('operationId', op.id);
                body.set('expectedRevision', revision);
                const response = await fetch(`${location.pathname}?handler=${op.kind === 'set' ? 'Set' : 'Control'}`, {
                    method: 'POST',
                    headers: {'X-Requested-With': 'XMLHttpRequest'},
                    body,
                    redirect: 'error'
                });
                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    if (response.status === 400) {
                        rejectedOperationId = op.id;
                        root.querySelector("[data-discard-operation]").hidden = false;
                    }
                    conflict = response.status === 409;
                    if (conflict) root.querySelector('[data-reconcile]').hidden = false;
                    throw new Error(data.error || 'Не удалось сохранить. Проверьте соединение и вход в аккаунт.');
                }
                revision = data.revision;
                queue.shift();
                if (op.kind === "set" && JSON.stringify(scratch[op.setId]) === JSON.stringify(op.fields.filter(([n]) => !["performedAtUtc", "skipped", "remove"].includes(n)))) delete scratch[op.setId];
                persist();
                update(data);
                if (op.fields.some(([n, v]) => n === 'remove' && v === 'true')) [...root.querySelectorAll('.set-form')].find(f => f.elements.namedItem('setId')?.value === op.setId)?.remove();
                if (data.status === 'Cancelled') {
                    location.reload();
                    return;
                }
            }
            syncMessage();
        } catch (error) {
            syncMessage(`${error.message} Изменения ${storageAvailable ? 'сохранены на устройстве' : 'остались в открытой странице'}.`, navigator.onLine ? 'error' : 'pending');
        } finally {
            sending = false;
        }
    }

    function previewOperation(op) {
        const value = name => op.fields.find(([n]) => n === name)?.[1];
        const at = Date.parse(value('performedAtUtc') || new Date(Date.now() + offset).toISOString());
        if (op.kind === 'set' && op.startsRest && state.status === 'InProgress') {
            const rest = Number(value('AddSet.RestSeconds') || 0);
            state.restEndsAtUtc = rest > 0 ? new Date(at + rest * 1000).toISOString() : null;
            state.restAfterSetId = Number(op.setId);
        }
        if (op.kind === 'control') {
            const action = value('action'), seconds = Number(value('seconds') || 0);
            if (action === 'pause' && state.status === 'InProgress') {
                state.elapsedSeconds = Math.max(0, state.elapsedSeconds + (at - offset - sampledAt) / 1000);
                sampledAt = Date.now();
                state.status = 'Paused';
                state.restRemainingSeconds = state.restEndsAtUtc ? Math.max(0, Math.ceil((Date.parse(state.restEndsAtUtc) - at) / 1000)) : state.restRemainingSeconds;
                state.restEndsAtUtc = null;
            } else if (action === 'resume' && state.status === 'Paused') {
                state.status = 'InProgress';
                sampledAt = at - offset;
                state.restEndsAtUtc = state.restRemainingSeconds > 0 ? new Date(at + state.restRemainingSeconds * 1000).toISOString() : null;
                state.restRemainingSeconds = null;
            } else if (action === 'skip-rest') {
                state.restEndsAtUtc = null;
                state.restRemainingSeconds = null;
                state.restAfterSetId = null;
            } else if (action === 'start-rest' && state.status === 'InProgress') {
                state.restEndsAtUtc = new Date(at + seconds * 1000).toISOString();
                state.restRemainingSeconds = null;
                state.restAfterSetId = null;
            } else if (action === 'pause-rest' && state.status === 'InProgress') {
                state.restRemainingSeconds = state.restEndsAtUtc ? Math.max(0, Math.ceil((Date.parse(state.restEndsAtUtc) - at) / 1000)) : state.restRemainingSeconds;
                state.restEndsAtUtc = null;
            } else if (action === 'resume-rest' && state.status === 'InProgress') {
                state.restEndsAtUtc = state.restRemainingSeconds > 0 ? new Date(at + state.restRemainingSeconds * 1000).toISOString() : null;
                state.restRemainingSeconds = null;
            } else if (action === 'set-rest' && state.status === 'InProgress') {
                if (state.restEndsAtUtc) state.restEndsAtUtc = seconds > 0 ? new Date(at + seconds * 1000).toISOString() : null;
                else if (state.restRemainingSeconds != null) state.restRemainingSeconds = seconds > 0 ? seconds : null;
                if (seconds === 0) state.restAfterSetId = null;
            } else if (action === 'adjust-rest') {
                if (!state.restEndsAtUtc && state.restRemainingSeconds != null) state.restRemainingSeconds = Math.max(0, Math.min(1800, state.restRemainingSeconds + seconds));
                else if (state.restEndsAtUtc) state.restEndsAtUtc = new Date(at + Math.max(0, Math.min(1800, (Date.parse(state.restEndsAtUtc) - at) / 1000 + seconds)) * 1000).toISOString();
            }
        }
    }

    function enqueue(op) {
        queue.push({...op, id: crypto.randomUUID()});
        persist();
        syncMessage();
        drain();
    }

    function saveSet(form, extra = [], quiet = false) {
        if (state.status === 'Paused') return false;
        const completedInput = form.elements.namedItem("completed"), repsInput = form.elements.namedItem("AddSet.Reps");
        repsInput.setCustomValidity(completedInput.checked && Number(repsInput.value) < 1 ? "Укажите хотя бы одно повторение для выполненного подхода." : "");
        if (!(quiet ? form.checkValidity() : form.reportValidity()) && !extra.some(([n]) => n === 'remove')) return false;
        if (rejectedOperationId && queue[0]?.id === rejectedOperationId && queue[0]?.setId === form.elements.namedItem("setId").value) {
            queue.shift();
            rejectedOperationId = null;
            root.querySelector("[data-discard-operation]").hidden = true;
        }
        const fields = [...new FormData(form).entries()].filter(([n]) => n !== '__RequestVerificationToken');
        if (completedInput.checked) form.classList.toggle('is-skipped', false);
        else if (form.classList.contains('is-skipped') && !extra.some(([n]) => n === 'skipped')) extra.push(['skipped', 'true']);
        // A completion checkbox changes the submitted values too. Track this snapshot so its acknowledgement clears the draft.
        scratch[form.elements.namedItem('setId').value] = fields.map(pair => [...pair]);
        fields.push(...extra, ['performedAtUtc', new Date(Date.now() + offset).toISOString()]);
        const startsRest = form.elements.namedItem("completed").checked && !form.classList.contains("is-completed");
        form.classList.toggle("is-completed", form.elements.namedItem("completed").checked);
        if (startsRest && state.status === "InProgress") {
            const rest = Number(form.elements.namedItem("AddSet.RestSeconds").value);
            state.restEndsAtUtc = rest > 0 ? new Date(Date.now() + offset + rest * 1000).toISOString() : null;
            state.restAfterSetId = Number(form.elements.namedItem("setId").value);
            tick();
        }
        enqueue({kind: "set", setId: form.elements.namedItem("setId").value, fields, startsRest});
        return true;
    }

    root.addEventListener('click', event => {
        const button = event.target.closest('[data-copy-previous]');
        if (!button || !active() || state.status === 'Paused') return;
        const form = button.closest('.set-form');
        if (!form || form.classList.contains('is-completed') || form.classList.contains('is-skipped')) return;
        const weight = form.elements.namedItem('AddSet.Weight');
        const reps = form.elements.namedItem('AddSet.Reps');
        const bodyweight = form.elements.namedItem('AddSet.IsBodyweight');
        bodyweight.checked = button.dataset.previousBodyweight === 'true';
        weight.value = bodyweight.checked ? '0' : button.dataset.weight;
        weight.dataset.externalWeight = weight.value;
        reps.value = button.dataset.reps;
        // Use the existing draft/command queue; copying never marks a set complete or starts rest.
        bodyweight.dispatchEvent(new Event('change', {bubbles: true}));
        reps.focus();
    });

    root.addEventListener("input", event => {
        const form = event.target.closest(".set-form");
        if (!form || !active() || state.status === "Paused") return;
        const id = form.elements.namedItem("setId").value;
        scratch[id] = [...new FormData(form).entries()].filter(([n]) => n !== "__RequestVerificationToken");
        persist();
        syncMessage();
        clearTimeout(debounce.get(id));
        debounce.set(id, setTimeout(() => saveSet(form, [], true), 500));
    });
    root.addEventListener('change', event => {
        const form = event.target.closest('.set-form');
        if (form && !event.target.readOnly && active()) {
            clearTimeout(debounce.get(form.elements.namedItem('setId').value));
            if (!saveSet(form) && event.target.name === 'completed') {
                event.target.checked = form.classList.contains('is-completed');
                scratch[form.elements.namedItem('setId').value] = [...new FormData(form).entries()].filter(([n]) => n !== '__RequestVerificationToken');
                persist();
            }
        }
    });

    function completionFields(form) {
        for (const [name, value] of [['operationId', crypto.randomUUID()], ['expectedRevision', revision]]) {
            let input = form.elements.namedItem(name);
            if (!input) {
                input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                form.append(input);
            }
            input.value = value;
        }
    }

    function nativeOrderRevision(form) {
        if (form.hasAttribute('data-native-order')) form.elements.namedItem('expectedRevision').value = revision;
    }

    async function cancelWorkout(form) {
        if (cancelling) return;
        cancelling = true;
        const message = form.closest('dialog').querySelector('[data-cancel-status]');
        const submit = form.querySelector('[type="submit"]');
        message.hidden = false;
        message.textContent = 'Отменяем тренировку…';
        submit.disabled = true;
        form.setAttribute('aria-busy', 'true');
        try {
            // An invalid or offline set must not prevent cancellation. Wait only for requests already in flight.
            for (const timeout of debounce.values()) clearTimeout(timeout);
            debounce.clear();
            for (let n = 0; (sending || picking || ordering) && n < 400; n++) await new Promise(resolve => setTimeout(resolve, 25));
            if (sending || picking || ordering) throw new Error('Дождитесь ответа сервера и повторите отмену.');
            const snapshot = await fetch(`${location.pathname}?handler=State`, {cache: 'no-store', redirect: 'error'});
            if (!snapshot.ok) throw new Error('Не удалось проверить тренировку. Повторите отмену.');
            const latest = await snapshot.json();
            const body = new URLSearchParams(new FormData(form));
            body.set('expectedRevision', latest.revision);
            const response = await fetch(form.action, {
                method: 'POST', headers: {'X-Requested-With': 'XMLHttpRequest'}, body, redirect: 'error'
            });
            const result = await response.json().catch(() => ({}));
            if (!response.ok || result.status !== 'Cancelled') throw new Error(result.error || 'Не удалось отменить тренировку. Повторите попытку.');
            queue = [];
            scratch = {};
            persist();
            try { localStorage.removeItem(`enix-workout-view:${root.dataset.viewer}:${root.dataset.sessionId}`); } catch { /* Optional view state. */ }
            location.assign(root.dataset.workoutsUrl);
        } catch (error) {
            message.textContent = error instanceof TypeError ? 'Нет связи с сервером. Отмена не подтверждена. Подключитесь к сети и повторите.' : error.message;
        } finally {
            cancelling = false;
            submit.disabled = false;
            form.removeAttribute('aria-busy');
        }
    }

    document.addEventListener('submit', async event => {
        if (event.target.hasAttribute('data-cancel-form')) {
            event.preventDefault();
            await cancelWorkout(event.target);
            return;
        }
        if (event.target.hasAttribute('data-pick-form')) {
            event.preventDefault();
            picker?.commit();
            return;
        }
        const form = event.target;
        if (form.matches('.set-form')) {
            event.preventDefault();
            clearTimeout(debounce.get(form.elements.namedItem("setId").value));
            saveSet(form, event.submitter?.name === 'remove' ? [['remove', 'true']] : []);
            return;
        }
        if (!root.contains(form) && !form.hasAttribute('data-complete-form') && !form.closest('#exercise-dialog,#load-template-dialog')) return;
        if (form.dataset.flushed) return;
        event.preventDefault();
        if (form.dataset.flushing) return;
        form.dataset.flushing = 'true';
        const message = form.closest('dialog')?.querySelector('[data-finish-status]');
        try {
            await flushResults();
            form.dataset.flushed = 'true';
            nativeOrderRevision(form);
            if (form.hasAttribute('data-complete-form')) completionFields(form);
            // Let the original submit event finish before starting native submission again.
            const submitter = event.submitter;
            setTimeout(() => form.requestSubmit(submitter), 0);
        } catch (error) {
            syncMessage(error.message, 'error');
            if (message) {
                message.hidden = false;
                message.textContent = error.message;
            }
        } finally {
            delete form.dataset.flushing;
        }
    });
    root.controlWorkout = (action, seconds = 0) => {
            const op = {
                kind: "control",
                fields: [["action", action], ["seconds", String(seconds)], ["performedAtUtc", new Date(Date.now() + offset).toISOString()]]
            };
            previewOperation(op);
            tick();
            enqueue(op);
    };
    document.addEventListener('click', event => {
        const control = event.target.closest("[data-control]");
        if (control) root.controlWorkout(control.dataset.control, Number(control.dataset.seconds || 0));
        const skip = event.target.closest('[data-skip-set]');
        if (skip) {
            const form = skip.closest('.set-form');
            form.elements.namedItem('completed').checked = false;
            form.classList.add('is-skipped');
            saveSet(form, [['skipped', 'true']]);
        }
        if (event.target.closest('[data-finish]')) {
            const forms = [...root.querySelectorAll('.set-form')],
                completed = forms.filter(f => f.elements.namedItem('completed').checked),
                working = completed.filter(f => !f.elements.namedItem('AddSet.IsWarmup').checked);
            const volume = working.reduce((sum, f) => sum + (f.elements.namedItem('AddSet.IsBodyweight').checked ? 0 : Number(f.elements.namedItem('AddSet.Weight').value) * Number(f.elements.namedItem('AddSet.Reps').value)) +
                [...f.querySelectorAll('[data-drop-index]')].reduce((total, row) => total + (row.querySelector('[data-bodyweight]').checked ? 0 : Number(row.querySelector('[data-drop-field="weight"]').value) * Number(row.querySelector('[data-drop-field="reps"]').value)), 0), 0);
            document.querySelector('[data-finish-summary]').textContent = `Рабочих подходов: ${working.length} · Объём: ${volume.toLocaleString('ru-RU')} кг · Осталось: ${forms.length - completed.length}`;
        }
        const soundButton = event.target.closest('[data-sound]');
        if (soundButton) {
            sound = !sound;
            soundButton.textContent = `Звук: ${sound ? 'вкл.' : 'выкл.'}`;
            soundButton.setAttribute('aria-pressed', String(sound));
            if (sound) {
                audioContext ||= new (window.AudioContext || window.webkitAudioContext)();
                audioContext.resume();
            }
        }
        const trigger = event.target.closest('[data-add-exercise],[data-replace-exercise]');
        if (trigger) {
            document.querySelector('[data-exercise-entry]').value = trigger.dataset.replaceExercise || '0';
            document.getElementById('exercise-dialog-title').textContent = trigger.dataset.replaceExercise ? 'Заменить упражнение' : 'Добавить упражнения';
            picker?.setSingle(!!trigger.dataset.replaceExercise);
            picker?.message('');
        }
    });
    root.querySelector("[data-discard-operation]").addEventListener("click", () => {
        if (queue[0]?.id === rejectedOperationId) queue.shift();
        rejectedOperationId = null;
        root.querySelector("[data-discard-operation]").hidden = true;
        persist();
        drain();
    });
    root.querySelector('[data-reconcile]').addEventListener('click', async () => {
        try {
            const response = await fetch(`${location.pathname}?handler=State`, {cache: 'no-store'});
            if (!response.ok) throw new Error();
            const data = await response.json();
            revision = data.revision;
            conflict = false;
            persist();
            root.querySelector('[data-reconcile]').hidden = true;
            update(data);
            await drain();
        } catch {
            syncMessage('Не удалось получить текущую версию. Проверьте соединение.');
        }
    });

    async function refresh() {
        if (picking || ordering || cancelling || root.dataset.orderEditing === 'true' || pickerRoot?.closest('dialog')?.open) return;
        if (queue.length) {
            drain();
            return;
        }
        if (sending || refreshing) return;
        refreshing = true;
        const requestedRevision = revision;
        try {
            const response = await fetch(`${location.pathname}?handler=State`, {cache: 'no-store'});
            if (!response.ok) return;
            const data = await response.json();
            // A result saved while this GET was in flight owns the newer revision.
            if (queue.length || sending || revision !== requestedRevision || picking || ordering || cancelling || root.dataset.orderEditing === 'true' || pickerRoot?.closest('dialog')?.open) return;
            if (data.revision !== revision) {
                location.reload();
                return;
            }
            update(data);
        } catch {/* Offline changes remain local. */
        } finally {
            refreshing = false;
        }
    }

    document.querySelectorAll('[data-local-time]').forEach(time => time.textContent = new Date(time.dataset.localTime).toLocaleTimeString('ru-RU', {
        hour: '2-digit',
        minute: '2-digit'
    }));
    window.addEventListener('online', drain);
    window.addEventListener('beforeunload', event => {
        if ((queue.length || Object.keys(scratch).length) && !storageAvailable) {
            event.preventDefault();
            event.returnValue = '';
        }
    });
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            tick();
            refresh();
            maintainWakeLock();
        }
    });
    window.addEventListener('pageshow', () => {
        tick();
        refresh();
        maintainWakeLock();
    });
    restoreForms();
    queue.forEach(previewOperation);
    for (const [id, pairs] of Object.entries(scratch)) {
        const form = [...root.querySelectorAll(".set-form")].find(f => f.elements.namedItem("setId").value === id);
        if (!form) continue;
        window.WorkoutSetFields.restoreDrops(form, pairs);
        for (const input of form.querySelectorAll("input[name]")) {
            if (input.name === "__RequestVerificationToken") continue;
            const pair = pairs.find(([n]) => n === input.name);
            if (input.type === "checkbox") input.checked = !!pair; else if (pair) input.value = pair[1];
        }
        window.WorkoutSetFields.refreshBodyweight(form);
        if (active() && state.status !== "Paused") saveSet(form);
    }
    syncMessage();
    tick();
    maintainWakeLock();
    drain();
    setInterval(tick, 1000);
    setInterval(refresh, 15000);
})();

// Completed workout recording.
(() => {
    'use strict';
    const root = document.querySelector('[data-record-workout]');
    if (!root) return;
    const form = root.querySelector('[data-record-form]'), list = root.querySelector('[data-record-exercises]');
    const status = root.querySelector('[data-record-status]'), outline = root.querySelector('[data-record-outline]');
    const review = document.getElementById('record-review-dialog'),
        confirm = review.querySelector('[data-record-confirm]');
    const feedback = root.querySelector('[data-record-feedback]'),
        sourceHint = root.querySelector('[data-record-source-note]');
    const key = `enix-record-draft:${root.dataset.viewer}`;
    const mobile = matchMedia('(max-width:1023px)'), basics = root.querySelector('.record-basics');
    const libraryRoot = document.getElementById('record-library'), library = new window.ExerciseLibrary(libraryRoot);
    const field = name => form.elements.namedItem(`Input.${name}`);
    const escape = value => String(value ?? '').replace(/[&<>"']/g, c => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[c]));
    const number = value => Number(String(value).trim().replace(',', '.'));
    const format = value => new Intl.NumberFormat('ru-RU', {maximumFractionDigits: 2}).format(value);
    const blankSet = () => ({weight: '', reps: '', rir: '', isWarmup: false, isBodyweight: false, drops: [], notes: ''});
    const exerciseId = window.ExerciseIds.normalize;
    const normalize = value => {
        if (!Array.isArray(value) || value.length > 40) throw new Error('Invalid draft');
        return value.map(e => {
            if (!e || !exerciseId(e.exerciseId) || !Array.isArray(e.sets) || !e.sets.length || e.sets.length > 20) throw new Error('Invalid draft');
            return {
                exerciseId: exerciseId(e.exerciseId), sets: e.sets.map(s => {
                    if (!s || typeof s !== 'object') throw new Error('Invalid set');
                    return {
                        ...blankSet(),
                        weight: s.weight ?? '',
                        reps: s.reps ?? '',
                        rir: s.rir ?? '',
                        isWarmup: !!s.isWarmup,
                        isBodyweight: !!s.isBodyweight,
                        drops: Array.isArray(s.drops) ? s.drops.slice(0, 10).map(d => ({weight: d.weight ?? '', reps: d.reps ?? '', isBodyweight: !!d.isBodyweight})) : [],
                        notes: s.notes ?? ''
                    };
                })
            };
        });
    };
    const serverError = !!root.querySelector('.validation-summary-errors');
    let exercises = [], selectedId, sourceNote = '', restored = false, dirty = false, storageAvailable = true;
    let undo, submitting = false, loading = false, version = 0;
    try {
        exercises = normalize(JSON.parse(field('ResultsJson').value));
    } catch { /* Keep server errors visible. */
    }
    if (!exercises.length && !serverError) {
        try {
            const draft = JSON.parse(localStorage.getItem(key));
            if (draft?.viewer === root.dataset.viewer && draft.fields && Array.isArray(draft.exercises)) {
                exercises = normalize(draft.exercises);
                for (const name of ['Title', 'Date', 'DurationMinutes', 'Notes', 'ClientRequestId'])
                    if (draft.fields[name] != null) field(name).value = draft.fields[name];
                selectedId = exerciseId(draft.selectedId);
                sourceNote = draft.sourceNote || '';
                restored = true;
            }
        } catch { /* Storage may be unavailable or the draft unreadable. */
        }
    }
    const localDay = new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 10);
    field('UtcOffsetMinutes').value = -new Date().getTimezoneOffset();
    field('Date').max = localDay;
    if (!restored && !serverError) field('Date').value = localDay;
    root.classList.add('record-enhanced');

    function message(text, canUndo = false) {
        feedback.hidden = false;
        feedback.querySelector('[data-record-feedback-text]').textContent = text;
        feedback.querySelector('[data-record-undo]').hidden = !canUndo;
    }

    function syncResults() {
        // Autofill and browser restoration may update values without firing input.
        for (const input of list.querySelectorAll('[data-set-field]')) {
            const exercise = exercises[Number(input.closest('[data-index]').dataset.index)];
            const set = exercise.sets[Number(input.closest('[data-set-index]').dataset.setIndex)];
            const drop = input.closest('[data-drop-index]');
            const target = drop ? set.drops[Number(drop.dataset.dropIndex)] : set;
            target[input.dataset.setField] = input.type === 'checkbox' ? input.checked : input.value;
        }
    }

    function save() {
        syncResults();
        const fields = Object.fromEntries(['Title', 'Date', 'DurationMinutes', 'Notes', 'ClientRequestId'].map(name => [name, field(name).value]));
        dirty = true;
        try {
            localStorage.setItem(key, JSON.stringify({
                viewer: root.dataset.viewer,
                fields,
                exercises,
                selectedId,
                sourceNote
            }));
            storageAvailable = true;
            status.textContent = 'Черновик сохранён на этом устройстве · ещё не в истории';
            window.AppComponents?.setNoticeState(status, 'local');
        } catch {
            storageAvailable = false;
            status.textContent = 'Черновик недоступен. Оставьте страницу открытой до сохранения записи.';
            window.AppComponents?.setNoticeState(status, 'error');
        }
    }

    function summary() {
        root.querySelector('[data-record-title]').textContent = field('Title').value.trim() || 'Без названия';
        const date = new Date(field('Date').value + 'T12:00:00');
        root.querySelector('[data-record-date]').textContent = Number.isFinite(date.getTime()) ? new Intl.DateTimeFormat('ru-RU', {
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        }).format(date) : 'Выберите дату';
        root.querySelector('[data-record-optional-summary]').textContent =
            [field('DurationMinutes').value ? `${field('DurationMinutes').value} мин` : '', field('Notes').value.trim() ? 'есть заметка' : ''].filter(Boolean).join(' · ') || 'необязательно';
        const sets = exercises.flatMap(e => e.sets), working = sets.filter(s => !s.isWarmup);
        root.querySelector('[data-record-summary]').textContent = exercises.length ?
            `Упражнений: ${exercises.length} · рабочих подходов: ${working.length}${sets.length > working.length ? ` · разминка: ${sets.length - working.length}` : ''}` : 'Начните с упражнения';
    }

    function select(id) {
        selectedId = exercises.some(e => e.exerciseId === exerciseId(id)) ? exerciseId(id) : exercises[0]?.exerciseId;
        [...list.children].forEach(card => {
            card.hidden = exerciseId(card.dataset.exerciseId) !== selectedId;
        });
        outline.querySelectorAll('button').forEach(button => button.setAttribute('aria-current', String(exerciseId(button.dataset.recordSelect) === selectedId)));
        requestAnimationFrame(() => {
            const button = outline.querySelector('[aria-current="true"]');
            if (!button) return;
            const bounds = outline.getBoundingClientRect(), item = button.getBoundingClientRect();
            if (item.left < bounds.left) outline.scrollLeft += item.left - bounds.left;
            else if (item.right > bounds.right) outline.scrollLeft += item.right - bounds.right;
        });
    }

    function render() {
        root.classList.toggle('record-has-exercises', exercises.length > 0);
        basics.open = !mobile.matches || !exercises.length;
        root.querySelector('[data-record-empty]').hidden = exercises.length > 0;
        root.querySelector('[data-record-outline-host]').hidden = !exercises.length;
        root.querySelector('[data-record-count]').textContent = exercises.length;
        sourceHint.hidden = !sourceNote;
        sourceHint.textContent = sourceNote;
        library.setSelected(exercises.map(e => e.exerciseId));
        outline.innerHTML = exercises.map((e, i) => `<button class="app-item-row" type="button" data-record-select="${e.exerciseId}"><span>${i + 1}</span><strong>${escape(library.byId.get(e.exerciseId)?.dataset.name || 'Упражнение недоступно')}</strong></button>`).join('');
        list.innerHTML = exercises.map((exercise, i) => {
            const row = library.byId.get(exercise.exerciseId);
            return `<section class="panel session-exercise record-exercise" data-index="${i}" data-exercise-id="${exercise.exerciseId}">
                <header><span class="exercise-order">${i + 1}</span><div><h2>${escape(row?.dataset.name || 'Упражнение недоступно')}</h2><p>${escape(row?.dataset.group || '')} · подходов: ${exercise.sets.length}</p></div><details class="exercise-menu"><summary aria-label="Действия с упражнением">⋯</summary><div><button type="button" data-move-exercise="-1" ${i === 0 ? 'disabled' : ''}>Выше в списке</button><button type="button" data-move-exercise="1" ${i === exercises.length - 1 ? 'disabled' : ''}>Ниже в списке</button><button type="button" class="danger-text" data-remove-exercise>Удалить упражнение</button></div></details></header>
                <details class="record-batch"><summary>Одинаковый вес и повторения</summary><div><label>Вес для всех<input type="text" inputmode="decimal" data-batch-weight placeholder="кг" aria-label="Вес для всех подходов" /></label><label>Повторения<input type="number" min="1" max="1000" data-batch-reps placeholder="раз" aria-label="Повторения для всех подходов" /></label><button type="button" class="button secondary" data-apply-batch>Применить</button></div></details>
                <div class="record-set-labels"><span>№</span><span>Вес, кг</span><span>Повторения</span><span></span></div>
                ${exercise.sets.map((s, n) => `<div class="record-set app-set-row" data-set-index="${n}">
                    <div class="record-set-main"><span class="set-number">${n + 1}</span><input type="text" inputmode="decimal" data-set-field="weight" value="${escape(s.weight)}" ${s.isBodyweight ? 'hidden' : 'required'} aria-label="Вес подхода ${n + 1}, кг" placeholder="кг" /><span class="bodyweight-value" ${s.isBodyweight ? '' : 'hidden'}>Собственный вес</span><input type="number" inputmode="numeric" data-set-field="reps" value="${escape(s.reps)}" min="1" max="1000" required aria-label="Повторения подхода ${n + 1}" /><button type="button" class="text-button danger-text" data-remove-set aria-label="Удалить подход ${n + 1}">×</button></div>
                    <div class="set-kind-controls"><label class="bodyweight-check"><input type="checkbox" data-set-field="isBodyweight" data-bodyweight ${s.isBodyweight ? 'checked' : ''} />Собственный вес</label>${s.drops.length ? '<span class="drop-set-badge">Дропсет</span>' : ''}</div>
                    <div class="set-drops" data-set-drops>${s.drops.map((d, i) => window.WorkoutSetFields.dropMarkup(d, i, true)).join('')}</div>
                    <button type="button" class="text-button add-drop" data-add-drop ${s.drops.length >= 10 ? 'disabled' : ''}>+ Дроп: снизить вес</button>
                    ${s.drops.length ? '<small class="drop-help">Ступени выполняются без отдыха и входят в один подход.</small>' : ''}
                    <details class="set-extra app-disclosure" ${s.isWarmup || s.notes || s.rir !== '' ? 'open' : ''}><summary>Разминка, усилие и заметка</summary><div><label>Осталось повторов (RIR)<input type="number" min="0" max="10" inputmode="numeric" data-set-field="rir" value="${escape(s.rir)}" /></label><label class="set-note">Заметка<input maxlength="500" data-set-field="notes" value="${escape(s.notes)}" /></label><label class="warmup-check"><input type="checkbox" data-set-field="isWarmup" ${s.isWarmup ? 'checked' : ''} />Разминка</label></div></details></div>`).join('')}
                <p class="record-weight-hint">Без дополнительного отягощения отметьте «Собственный вес».</p>
                <footer class="record-exercise-footer"><button type="button" class="button secondary" data-add-set ${exercise.sets.length >= 20 ? 'disabled' : ''}>+ Подход</button><div>${i > 0 ? `<button type="button" class="button secondary" data-record-neighbor="${exercises[i - 1].exerciseId}">Назад</button>` : ''}${i < exercises.length - 1 ? `<button type="button" class="button secondary" data-record-neighbor="${exercises[i + 1].exerciseId}">Следующее →</button>` : ''}</div></footer>
            </section>`;
        }).join('');
        select(selectedId);
        summary();
    }

    function change(action, text) {
        syncResults();
        undo = {
            exercises: structuredClone(exercises),
            selectedId,
            sourceNote,
            title: field('Title').value,
            requestId: field('ClientRequestId').value
        };
        action();
        version++;
        render();
        save();
        message(text, true);
    }

    root.querySelector('[data-record-undo]').addEventListener('click', () => {
        if (!undo) return;
        ({exercises, selectedId, sourceNote} = undo);
        field('Title').value = undo.title;
        field('ClientRequestId').value = undo.requestId;
        undo = null;
        version++;
        render();
        save();
        message('Изменение отменено.');
    });
    const updateInput = event => {
        const input = event.target;
        if (input.dataset.setField) {
            input.setCustomValidity('');
        }
        undo = null;
        feedback.hidden = true;
        version++;
        save();
        summary();
    };
    form.addEventListener('input', updateInput);
    form.addEventListener('change', updateInput);
    outline.addEventListener('click', event => {
        const button = event.target.closest('[data-record-select]');
        if (button) {
            select(button.dataset.recordSelect);
            save();
            if (mobile.matches) root.querySelector('[data-record-outline-host]').scrollIntoView({
                block: 'start',
                behavior: matchMedia('(prefers-reduced-motion:reduce)').matches ? 'instant' : 'smooth'
            });
        }
    });
    mobile.addEventListener('change', () => {
        basics.open = !mobile.matches || !exercises.length;
    });
    list.addEventListener('click', event => {
        const button = event.target.closest('button'), section = button?.closest('[data-index]');
        if (!section) return;
        const i = Number(section.dataset.index), exercise = exercises[i];
        if (button.hasAttribute('data-record-neighbor')) {
            select(button.dataset.recordNeighbor);
            save();
            return;
        }
        if (button.hasAttribute('data-remove-exercise')) change(() => {
            exercises.splice(i, 1);
            selectedId = exercises[Math.min(i, exercises.length - 1)]?.exerciseId;
        }, 'Упражнение удалено.');
        else if (button.hasAttribute('data-move-exercise')) {
            const target = i + Number(button.dataset.moveExercise);
            if (target < 0 || target >= exercises.length) return;
            change(() => {
                [exercises[i], exercises[target]] = [exercises[target], exercises[i]];
            }, 'Порядок упражнений изменён.');
        } else if (button.hasAttribute('data-add-set') && exercise.sets.length < 20) {
            change(() => exercise.sets.push(structuredClone(exercise.sets.at(-1) || blankSet())), 'Подход добавлен. Проверьте вес и повторения.');
        } else if (button.hasAttribute('data-add-drop')) {
            const set = exercise.sets[Number(button.closest('[data-set-index]').dataset.setIndex)];
            if (set.drops.length < 10) change(() => set.drops.push({weight: '', reps: set.drops.at(-1)?.reps || set.reps, isBodyweight: false}), 'Дроп добавлен. Укажите сниженный вес и повторения.');
        } else if (button.hasAttribute('data-remove-drop')) {
            const set = exercise.sets[Number(button.closest('[data-set-index]').dataset.setIndex)];
            change(() => set.drops.splice(Number(button.closest('[data-drop-index]').dataset.dropIndex), 1), 'Ступень дропсета удалена.');
        } else if (button.hasAttribute('data-remove-set')) change(() => {
            exercise.sets.splice(Number(button.closest('[data-set-index]').dataset.setIndex), 1);
            if (!exercise.sets.length) exercise.sets.push(blankSet());
        }, 'Подход удалён.');
        else if (button.hasAttribute('data-apply-batch')) {
            const weight = section.querySelector('[data-batch-weight]').value.trim(),
                reps = section.querySelector('[data-batch-reps]').value;
            if (weight !== '' && (!Number.isFinite(number(weight)) || number(weight) < 0 || number(weight) > 10000)) {
                message('Введите вес от 0 до 10000 кг.');
                return;
            }
            if (reps !== '' && (!Number.isInteger(number(reps)) || number(reps) < 1 || number(reps) > 1000)) {
                message('Введите повторения от 1 до 1000.');
                return;
            }
            if (weight === '' && reps === '') {
                message('Укажите вес или повторения для заполнения подходов.');
                return;
            }
            change(() => exercise.sets.forEach(s => {
                if (weight !== '') { s.weight = weight; s.isBodyweight = false; }
                if (reps !== '') s.reps = reps;
            }), 'Значения применены ко всем подходам упражнения.');
        }
    });
    document.addEventListener('click', event => {
        if (event.target.closest('[data-dialog="record-exercise-dialog"]')) library.setContext(field('Title').value || 'Запись выполненной тренировки');
    });
    libraryRoot.addEventListener('exercises-selected', event => {
        const picks = event.detail.exercises.filter(p => !exercises.some(e => e.exerciseId === p.exerciseId));
        if (exercises.length + picks.length > 40) {
            library.message('Максимум 40 упражнений в записи.');
            return;
        }
        if (!picks.length) return;
        change(() => {
            picks.forEach(p => exercises.push({
                exerciseId: p.exerciseId,
                sets: Array.from({length: p.setsCount}, () => ({
                    ...blankSet(),
                    weight: p.weight ?? '',
                    reps: p.reps ?? ''
                }))
            }));
            selectedId = picks[0].exerciseId;
        }, 'Упражнения добавлены. Заполните фактические результаты.');
        library.clear();
        document.getElementById('record-exercise-dialog').close();
        (mobile.matches ? root.querySelector('[data-record-outline-host]') : list.querySelector(`[data-exercise-id="${selectedId}"]`)).scrollIntoView({
            block: 'start',
            behavior: matchMedia('(prefers-reduced-motion:reduce)').matches ? 'instant' : 'smooth'
        });
    });
    document.addEventListener('click', async event => {
        const button = event.target.closest('[data-record-source]');
        if (!button || loading) return;
        if (exercises.length && !window.confirm('Заменить упражнения выбранной основой? Изменение можно отменить.')) return;
        loading = true;
        button.disabled = true;
        const initialVersion = version;
        try {
            const url = new URL(location.href);
            url.search = new URLSearchParams({
                handler: 'Source',
                source: button.dataset.recordSource,
                templateId: button.dataset.templateId || ''
            });
            const response = await fetch(url, {cache: 'no-store'}), data = await response.json();
            if (!response.ok) throw new Error(data.error || 'Не удалось загрузить основу.');
            const loaded = normalize(data.exercises);
            if (version !== initialVersion) throw new Error('Черновик изменился во время загрузки. Повторите выбор основы.');
            change(() => {
                exercises = loaded;
                selectedId = loaded[0]?.exerciseId;
                field('Title').value = data.title;
                field('ClientRequestId').value = crypto.randomUUID();
                sourceNote = button.dataset.recordSource === 'template' ? 'Основа из шаблона: проверьте плановые значения и замените их фактическими результатами.' : 'Основа из последней тренировки: проверьте результаты для новой даты.';
            }, 'Основа загружена. Проверьте каждый подход.');
            document.getElementById('record-template-dialog').close();
        } catch (error) {
            message(error.message);
        } finally {
            loading = false;
            button.disabled = false;
        }
    });
    root.querySelector('[data-reset-draft]').addEventListener('click', () => {
        if ((dirty || restored || exercises.length) && !window.confirm('Удалить текущий черновик и начать пустую запись?')) return;
        form.reset();
        exercises = [];
        selectedId = undefined;
        sourceNote = '';
        undo = null;
        version++;
        field('Date').value = localDay;
        field('ClientRequestId').value = crypto.randomUUID();
        root.querySelector('.record-optional').open = false;
        feedback.hidden = true;
        render();
        save();
    });
    const reveal = input => {
        if (review.open) review.close();
        const card = input.closest('[data-exercise-id]');
        if (card) select(card.dataset.exerciseId);
        for (let parent = input.parentElement; parent && parent !== form; parent = parent.parentElement)
            if (parent instanceof HTMLDetailsElement) parent.open = true;
        input.focus();
        input.reportValidity();
    };

    function validate() {
        syncResults();
        for (const input of list.querySelectorAll('[data-set-field="weight"]'))
            input.setCustomValidity(input.hidden || input.value.trim() !== '' && Number.isFinite(number(input.value)) && number(input.value) >= 0 && number(input.value) <= 10000 ? '' : 'Введите вес от 0 до 10000 кг или отметьте «Собственный вес».');
        const invalid = [...form.querySelectorAll('input:invalid,textarea:invalid')].find(input => !input.matches('[data-batch-weight],[data-batch-reps]'));
        if (invalid) {
            message('Проверьте выделенное поле.');
            reveal(invalid);
            return null;
        }
        if (!exercises.length) {
            message('Добавьте хотя бы одно упражнение.');
            return null;
        }
        if (exercises.some(e => !library.byId.has(e.exerciseId))) {
            message('Упражнение недоступно. Удалите его и выберите другое из библиотеки.');
            return null;
        }
        const payload = exercises.map(e => ({
            exerciseId: e.exerciseId,
            sets: e.sets.map(s => ({
                weight: s.isBodyweight ? 0 : number(s.weight),
                reps: number(s.reps),
                rir: s.rir === '' || s.rir == null ? null : number(s.rir),
                isWarmup: !!s.isWarmup,
                isBodyweight: !!s.isBodyweight,
                drops: s.drops.map(d => ({weight: d.isBodyweight ? 0 : number(d.weight), reps: number(d.reps), isBodyweight: !!d.isBodyweight})),
                notes: s.notes || null
            }))
        }));
        if (!payload.some(e => e.sets.some(s => !s.isWarmup))) {
            message('Добавьте хотя бы один рабочий подход. Сейчас указана только разминка.');
            return null;
        }
        return payload;
    }

    form.addEventListener('submit', event => {
        event.preventDefault();
        if (submitting || loading) return;
        const payload = validate();
        if (!payload) return;
        if (!event.submitter?.hasAttribute('data-record-confirm')) {
            review.querySelector('[data-record-review-status]').hidden = true;
            const sets = payload.flatMap(e => e.sets), working = sets.filter(s => !s.isWarmup);
            const date = new Intl.DateTimeFormat('ru-RU', {
                day: 'numeric',
                month: 'long',
                year: 'numeric'
            }).format(new Date(field('Date').value + 'T12:00:00'));
            review.querySelector('[data-record-review-content]').innerHTML = `<h3>${escape(field('Title').value)}</h3><p class="record-review-date">${escape(date)} · ${field('DurationMinutes').value ? escape(field('DurationMinutes').value) + ' мин' : 'Длительность не указана'}</p><div class="record-review-stats"><div><span>Рабочих подходов</span><strong>${working.length}</strong></div><div><span>Объём, кг</span><strong>${format(working.reduce((sum, s) => sum + s.weight * s.reps + s.drops.reduce((v, d) => v + d.weight * d.reps, 0), 0))}</strong></div></div><ul class="record-review-list">${payload.map(e => `<li><strong>${escape(library.byId.get(e.exerciseId).dataset.name)}</strong><span>${e.sets.length} подх.${e.sets.some(s => s.drops.length) ? ' · дропсет' : ''}${e.sets.some(s => s.isBodyweight) ? ' · собственный вес' : ''}</span></li>`).join('')}</ul>${sets.some(s => s.isWarmup) ? `<p class="record-review-date">Разминочных подходов: ${sets.length - working.length}</p>` : ''}`;
            save();
            review.showModal();
            return;
        }
        if (!navigator.onLine) {
            save();
            const notice = review.querySelector('[data-record-review-status]');
            notice.hidden = false;
            notice.textContent = 'Нет соединения. Черновик сохранён на этом устройстве. Отправьте запись, когда появится сеть.';
            if (!storageAvailable) notice.textContent = 'Нет соединения, и браузер не сохранил черновик. Оставьте страницу открытой и повторите отправку после подключения.';
            window.AppComponents?.setNoticeState(notice, storageAvailable ? 'pending' : 'error');
            return;
        }
        field('ResultsJson').value = JSON.stringify(payload);
        field('UtcOffsetMinutes').value = -new Date().getTimezoneOffset();
        save();
        submitting = true;
        confirm.disabled = true;
        confirm.textContent = 'Сохраняем…';
        // Keep the existing server form, antiforgery and request identity.
        HTMLFormElement.prototype.submit.call(form);
    });
    window.addEventListener('beforeunload', event => {
        if (dirty && !storageAvailable && !submitting) {
            event.preventDefault();
            event.returnValue = '';
        }
    });
    window.addEventListener('pageshow', () => {
        submitting = false;
        confirm.disabled = false;
        confirm.textContent = 'Сохранить в историю';
    });
    let viewportHeight = window.visualViewport?.height || innerHeight, viewportWidth = innerWidth;
    const keyboardLayout = () => {
        const viewport = window.visualViewport;
        if (!viewport) return;
        if (viewportWidth !== innerWidth) {
            viewportWidth = innerWidth;
            viewportHeight = viewport.height;
        }
        const input = document.activeElement;
        const editing = matchMedia('(max-width:1023px)').matches && form.contains(input) && !input?.closest('dialog') &&
            input?.matches('input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),textarea');
        const inset = Math.max(0, innerHeight - viewport.height - viewport.offsetTop);
        const open = editing && (inset > 120 || viewportHeight - viewport.height > 120);
        document.body.classList.toggle('record-keyboard-open', !!open);
        const bounds = form.getBoundingClientRect();
        root.style.setProperty('--record-keyboard-inset', `${open ? inset : 0}px`);
        root.style.setProperty('--record-save-left', `${bounds.left}px`);
        root.style.setProperty('--record-save-width', `${bounds.width}px`);
        if (open) {
            const inputBounds = input.getBoundingClientRect(),
                bar = root.querySelector('.record-savebar').getBoundingClientRect();
            if (inputBounds.bottom > bar.top - 16) window.scrollBy({
                top: inputBounds.bottom - bar.top + 16,
                behavior: 'instant'
            });
        }
        if (!editing) viewportHeight = viewport.height;
    };
    window.visualViewport?.addEventListener('resize', keyboardLayout);
    window.visualViewport?.addEventListener('scroll', keyboardLayout);
    document.addEventListener('focusin', keyboardLayout);
    document.addEventListener('focusout', () => setTimeout(keyboardLayout, 0));
    render();
    if (restored) status.textContent = 'Черновик восстановлен. Проверьте результаты перед сохранением.';
    const invalidServerField = root.querySelector('.input-validation-error:not([type="hidden"])') ||
        (serverError ? [...form.querySelectorAll('input:invalid,textarea:invalid')].find(input => input.type !== 'hidden') : null);
    if (invalidServerField) reveal(invalidServerField);
})();

// Initial training setup.
(() => {
    'use strict';
    const root = document.querySelector('[data-training-setup]');
    if (!root) return;
    const key = `enix-training-setup:${root.dataset.viewer}`;
    if (root.dataset.saved === 'true') {
        try {
            sessionStorage.removeItem(key);
        } catch {
        }
        return;
    }
    const form = root.querySelector('[data-setup-form]');
    if (!form) return;
    const steps = [...form.querySelectorAll('[data-setup-step]')],
        tabs = [...form.querySelectorAll('[data-setup-goto]')];
    const next = form.querySelector('[data-setup-next]'), back = form.querySelector('[data-setup-back]'),
        save = form.querySelector('[data-setup-save]'), status = form.querySelector('[data-setup-status]');
    const later = form.querySelector('[name="Input.DaysLater"][type="checkbox"]'),
        dayInputs = [...form.querySelectorAll('[name="Input.Days"]')];
    const revision = form.querySelector('[name="Input.Revision"]').value;
    let step = 0, stored = false, busy = false;
    const notice = state => window.AppComponents?.setNoticeState(status, state);
    const buttons = [...form.querySelectorAll('.setup-actions button,.setup-steps button')];
    const resetBusy = () => {
        busy = false;
        buttons.forEach(button => {
            button.disabled = false;
            button.removeAttribute('aria-busy');
        });
    };
    const selected = name => [...form.querySelectorAll(`[name="${name}"]:checked`)].map(input => input.value);
    const title = input => {
        const label = input?.closest('label');
        return (label?.querySelector('strong') || label?.querySelector('span'))?.textContent.trim();
    };
    const data = () => ({
        version: 1,
        revision,
        step,
        goal: selected('Input.Goal')[0],
        level: selected('Input.Level')[0],
        location: selected('Input.Location')[0],
        equipment: selected('Input.Equipment'),
        days: selected('Input.Days'),
        later: later.checked
    });
    const persist = () => {
        try {
            sessionStorage.setItem(key, JSON.stringify(data()));
            stored = true;
        } catch {
            stored = false;
        }
    };
    const renderSummary = () => {
        const rows = [['Цель', title(form.querySelector('[name="Input.Goal"]:checked')) || 'Выберите цель'], ['Уровень', title(form.querySelector('[name="Input.Level"]:checked')) || 'Выберите уровень'], ['Место', title(form.querySelector('[name="Input.Location"]:checked')) || 'Выберите место'], ['Оборудование', [...form.querySelectorAll('[name="Input.Equipment"]:checked')].map(title).join(', ') || 'Без дополнительного оборудования'], ['Удобные дни', later.checked ? 'Дни выберу позже' : dayInputs.filter(input => input.checked).map(input => input.closest('label').querySelector('small').textContent).join(', ') || 'Выберите дни']];
        form.querySelectorAll('[data-setup-preview],[data-setup-review]').forEach(dl => {
            dl.replaceChildren(...rows.map(([label, value]) => {
                const row = document.createElement('div'), dt = document.createElement('dt'),
                    dd = document.createElement('dd');
                dt.textContent = label;
                dd.textContent = value;
                row.append(dt, dd);
                return row;
            }));
        });
        dayInputs.forEach(input => {
            input.disabled = later.checked;
        });
    };
    const show = (index, focus = false) => {
        step = Math.max(0, Math.min(3, index));
        steps.forEach((section, i) => {
            section.hidden = i !== step;
            section.classList.toggle('is-entering', focus && i === step);
        });
        tabs.forEach((tab, i) => {
            if (i === Math.min(step, 2)) tab.setAttribute('aria-current', 'step'); else tab.removeAttribute('aria-current');
        });
        back.hidden = step === 0;
        next.hidden = step === 3;
        save.hidden = step !== 3;
        next.childNodes[0].textContent = step === 2 ? 'Проверить выбор ' : 'Дальше ';
        renderSummary();
        persist();
        if (focus) {
            steps[step].querySelector('h2').focus({preventScroll: true});
            steps[step].scrollIntoView({
                block: 'start',
                behavior: matchMedia('(prefers-reduced-motion:reduce)').matches ? 'instant' : 'smooth'
            });
        }
    };
    const validate = index => {
        const invalid = [...steps[index].querySelectorAll('input')].find(input => !input.disabled && !input.checkValidity());
        if (invalid) {
            show(index);
            invalid.reportValidity();
            return false;
        }
        if (index === 2) {
            const dayError = form.querySelector('[data-setup-days-error]');
            dayError.hidden = later.checked || dayInputs.some(input => input.checked);
            if (!dayError.hidden) {
                show(2);
                dayInputs[0].focus();
                return false;
            }
        }
        return true;
    };
    if (root.dataset.invalid !== 'true') {
        try {
            const draft = JSON.parse(sessionStorage.getItem(key));
            if (draft?.version === 1 && draft.revision === revision) {
                for (const [name, values] of [['Input.Goal', [draft.goal]], ['Input.Level', [draft.level]], ['Input.Location', [draft.location]], ['Input.Equipment', draft.equipment], ['Input.Days', draft.days]]) {
                    if (!Array.isArray(values)) continue;
                    form.querySelectorAll(`[name="${name}"]`).forEach(input => input.checked = values.includes(input.value));
                }
                later.checked = draft.later === true;
                step = Number.isInteger(draft.step) ? Math.max(0, Math.min(3, draft.step)) : 0;
                status.textContent = 'Ваш выбор восстановлен в этой вкладке. Он ещё не отправлен.';
                notice('local');
            }
        } catch {
        }
    }
    form.classList.add('is-enhanced');
    if (root.dataset.invalid === 'true') {
        const invalid = steps.findIndex(section => section.querySelector('.input-validation-error,.field-validation-error'));
        step = invalid < 0 ? 0 : invalid;
    }
    if (step > 0 && !form.querySelector('[name="Input.Goal"]:checked')) step = 0;
    if (step > 1 && !form.querySelector('[name="Input.Location"]:checked')) step = 1;
    if (step > 2 && !later.checked && !dayInputs.some(input => input.checked)) step = 2;
    show(step);
    form.addEventListener('change', event => {
        renderSummary();
        persist();
        status.textContent = stored ? 'Ваш выбор сохранён в этой вкладке. Он ещё не отправлен.' : 'Выбор остался на странице. Сохраните условия, когда будете готовы.';
        notice(stored ? 'local' : 'info');
        if (['Input.Days', 'Input.DaysLater'].includes(event.target.name)) form.querySelector('[data-setup-days-error]').hidden = later.checked || dayInputs.some(input => input.checked);
    });
    next.addEventListener('click', () => {
        if (validate(step)) show(step + 1, true);
    });
    back.addEventListener('click', () => show(step - 1, true));
    tabs.forEach((tab, index) => tab.addEventListener('click', () => {
        if (index > step) {
            for (let i = step; i < index; i++) if (!validate(i)) return;
        }
        show(index, true);
    }));
    form.addEventListener('submit', event => {
        if (busy) {
            event.preventDefault();
            return;
        }
        const skip = event.submitter?.matches('[data-setup-skip]');
        if (!skip) {
            for (let i = 0; i < 3; i++) if (!validate(i)) {
                event.preventDefault();
                return;
            }
            if (step !== 3) {
                event.preventDefault();
                show(3, true);
                return;
            }
        }
        if (!navigator.onLine) {
            event.preventDefault();
            persist();
            status.textContent = stored ? 'Сейчас нет сети. Ваш выбор сохранён в этой вкладке; повторите после подключения.' : 'Сейчас нет сети. Оставьте страницу открытой и повторите после подключения.';
            notice('error');
            return;
        }
        if (skip) {
            try {
                sessionStorage.removeItem(key);
            } catch {
            }
        } else persist();
        busy = true;
        event.submitter?.setAttribute('aria-busy', 'true');
        // Preserve the native submitter and its handler until the form is serialized.
        setTimeout(() => {
            if (busy) buttons.forEach(button => button.disabled = true);
        }, 0);
        status.textContent = skip ? 'Ожидаем подтверждения переноса настройки…' : 'Ожидаем подтверждения сохранения…';
        notice('pending');
    });
    window.addEventListener('online', () => {
        if (status.dataset.appState === 'error') {
            status.textContent = 'Подключение восстановлено. Можно сохранить условия.';
            notice('local');
        }
    });
    window.addEventListener('pageshow', () => {
        const wasBusy = busy;
        resetBusy();
        if (wasBusy) {
            persist();
            status.textContent = stored ? 'Выбор сохранён в этой вкладке. Проверьте его и повторите сохранение.' : 'Выбор остался на странице. Повторите сохранение.';
            notice(stored ? 'local' : 'info');
        }
    });
})();
