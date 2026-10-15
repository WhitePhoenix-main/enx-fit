// Dashboard navigation, catalog, widgets and workout history.
if (document.body.classList.contains('fitness-app')) {
    // Local timezone for server-rendered dates.
    (() => {
        'use strict';
        document.cookie = `enix-utc-offset=${-new Date().getTimezoneOffset()}; Path=/; SameSite=Lax; Max-Age=31536000`;
    })();

    // Dashboard navigation, dialogs and forms.
    (() => {
        'use strict';
        const openDialog = id => {
            const dialog = document.getElementById(id);
            if (!(dialog instanceof HTMLDialogElement)) return;
            document.querySelectorAll('dialog[open]').forEach(item => item.close());
            dialog.showModal();
        };
        document.addEventListener('click', event => {
            const trigger = event.target.closest('[data-dialog]');
            if (trigger) {
                if (trigger.tagName === 'A') event.preventDefault();
                openDialog(trigger.dataset.dialog);
            }
            const close = event.target.closest('[data-close]');
            if (close) close.closest('dialog').close();
            if (event.target instanceof HTMLDialogElement) {
                const box = event.target.getBoundingClientRect();
                if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
            }
            if (event.target.closest('.search-results a')) event.target.closest('dialog').close();
            const menu = document.querySelector('.account-menu');
            if (menu && !menu.contains(event.target)) menu.open = false;
        });
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape') {
                const dialog = document.querySelector('dialog[open]');
                if (dialog) {
                    event.preventDefault();
                    dialog.close();
                }
                document.querySelectorAll('.account-menu[open], .ef-chart-period[open], .ef-why[open]')
                    .forEach(menu => {
                        menu.open = false;
                    });
            }
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
                event.preventDefault();
                openDialog('search-dialog');
            }
        });
        document.querySelector('[data-section-search]')?.addEventListener('input', event => {
            const search = event.target.value.trim().toLocaleLowerCase('ru');
            const links = [...document.querySelectorAll('#search-dialog .search-results a')];
            links.forEach(link => {
                link.hidden = !link.textContent.toLocaleLowerCase('ru').includes(search);
            });
            document.querySelector('[data-search-empty]').hidden = links.some(link => !link.hidden);
        });
        document.querySelectorAll('[data-submit-change]').forEach(input => input.addEventListener('change', () => input.form.requestSubmit()));
        document.querySelector('.period-form')?.addEventListener('submit', event => {
            // Avoid duplicate Days values when a period button is the submitter.
            const fallback = event.target.querySelector('[data-default-period]');
            if (fallback) fallback.disabled = event.submitter?.name === 'Days';
        });
        document.querySelectorAll('.dashboard-form').forEach(form => {
            const button = form.querySelector('button[type="submit"]');
            if (!button) return;
            const feedback = document.createElement('p');
            feedback.className = 'app-status';
            feedback.setAttribute('role', 'status');
            feedback.setAttribute('aria-live', 'polite');
            feedback.dataset.dashboardFeedback = '';
            feedback.hidden = true;
            form.append(feedback);
            let busy = false;
            const notice = (state, message) => {
                window.AppComponents?.setNoticeState(feedback, state);
                feedback.textContent = message;
                feedback.hidden = false;
            };
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
                    notice('error', 'Сейчас нет сети. Данные остались в форме; повторите сохранение после подключения.');
                }
            });
            // Run after form/document validators. Disabling is deferred so the native submitter remains in the POST.
            window.addEventListener('submit', event => {
                if (event.target !== form || event.defaultPrevented || !form.checkValidity() || busy) return;
                busy = true;
                form.setAttribute('aria-busy', 'true');
                button.setAttribute('aria-busy', 'true');
                notice('pending', 'Ожидаем подтверждения сохранения…');
                setTimeout(() => {
                    if (busy) button.disabled = true;
                }, 0);
            });
            window.addEventListener('online', () => {
                if (feedback.dataset.appState === 'error') notice('pending', 'Подключение восстановлено. Проверьте данные и повторите сохранение.');
            });
            window.addEventListener('pageshow', () => {
                const wasBusy = busy;
                busy = false;
                form.removeAttribute('aria-busy');
                button.removeAttribute('aria-busy');
                button.disabled = false;
                if (wasBusy) notice('pending', 'Проверьте данные перед повторным сохранением.');
            });
        });
    })();

    // Mobile tabs and exercise catalog.
    (() => {
        'use strict';
        const tabButtons = [...document.querySelectorAll('[data-mobile-tab]')];
        const selectTab = button => {
            tabButtons.forEach(tab => {
                const selected = tab === button;
                tab.classList.toggle('active', selected);
                tab.setAttribute('aria-selected', String(selected));
                tab.tabIndex = selected ? 0 : -1;
                document.getElementById(tab.dataset.mobileTab).hidden = !selected;
            });
        };
        tabButtons.forEach((button, index) => {
            button.addEventListener('click', () => selectTab(button));
            button.addEventListener('keydown', event => {
                if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
                event.preventDefault();
                const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabButtons.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabButtons.length) % tabButtons.length;
                selectTab(tabButtons[next]);
                tabButtons[next].focus();
            });
        });
        if (tabButtons.length && new URLSearchParams(location.search).has('Month') && location.hash !== '#mobile-history') selectTab(tabButtons[1]);
        document.querySelectorAll('[data-current-timezone="false"]').forEach(label => {
            const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;
            label.textContent = zone === 'Asia/Irkutsk' ? 'Иркутск (UTC+8)' : zone;
        });

        const library = document.querySelector('.em-library[data-exercise-library]');
        if (!library) return;
        const cards = [...library.querySelectorAll('[data-library-id]')];
        const search = library.querySelector('[data-library-search]');
        const group = library.querySelector('[data-library-group]');
        const tabs = [...library.querySelectorAll('[data-library-tab]')];
        const reference = library.dataset.libraryReference === 'true';
        const key = `enix-exercise-favorites:${library.dataset.libraryOwner}:${reference ? 'reference' : 'live'}`;
        let favorites;
        try {
            const saved = JSON.parse(localStorage.getItem(key));
            favorites = new Set((Array.isArray(saved) ? saved : reference ? [-1] : []).map(window.ExerciseIds.normalize).filter(Boolean));
        } catch {
            favorites = new Set(reference ? [window.ExerciseIds.normalize(-1)] : []);
        }
        let activeTab = 'all';
        const render = () => {
            const query = search.value.trim().toLocaleLowerCase('ru');
            let count = 0;
            cards.forEach(card => {
                const favorite = favorites.has(card.dataset.libraryId);
                const button = card.querySelector('[data-library-favorite]');
                button.setAttribute('aria-pressed', String(favorite));
                button.setAttribute('aria-label', `${favorite ? 'Убрать из избранного' : 'Добавить в избранное'}: ${card.querySelector('h2').textContent}`);
                card.classList.toggle('is-favorite', favorite);
                card.hidden = !card.dataset.libraryText.toLocaleLowerCase('ru').includes(query) || (group.value && card.dataset.libraryMuscle !== group.value) || (activeTab === 'favorites' && !favorite) || (activeTab === 'recent' && card.dataset.libraryRecent !== 'true');
                if (!card.hidden) count++;
            });
            library.querySelector('[data-library-empty]').hidden = count > 0;
            const plural = count % 100 >= 11 && count % 100 <= 14 ? 'упражнений' : count % 10 === 1 ? 'упражнение' : count % 10 >= 2 && count % 10 <= 4 ? 'упражнения' : 'упражнений';
            library.querySelector('[data-library-count]').textContent = reference && !query && !group.value && activeTab === 'all' ? '24 упражнения' : `${count} ${plural}`;
        };
        search.addEventListener('input', render);
        group.addEventListener('change', render);
        tabs.forEach((button, index) => {
            const select = () => {
                activeTab = button.dataset.libraryTab;
                tabs.forEach(tab => {
                    const selected = tab === button;
                    tab.classList.toggle('active', selected);
                    tab.setAttribute('aria-selected', String(selected));
                    tab.tabIndex = selected ? 0 : -1;
                });
                render();
            };
            button.addEventListener('click', select);
            button.addEventListener('keydown', event => {
                if (!['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
                event.preventDefault();
                const next = tabs[(index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length];
                next.click();
                next.focus();
            });
        });
        cards.forEach(card => card.querySelector('[data-library-favorite]').addEventListener('click', () => {
            const id = card.dataset.libraryId;
            if (favorites.has(id)) favorites.delete(id); else favorites.add(id);
            try {
                localStorage.setItem(key, JSON.stringify([...favorites]));
            } catch { /* Filtering remains usable without storage. */
            }
            render();
        }));
        render();
    })();

    // Dashboard widget layout.
    (() => {
        'use strict';
        const grid = document.querySelector('[data-modular-dashboard]');
        if (!grid) return;
        const config = JSON.parse(document.getElementById('dashboard-layout-data').textContent);
        const pinnedId = config.pinnedWidgetId;
        const copy = value => value.map(widget => ({...widget}))
            .sort((a, b) => Number(b.id === pinnedId) - Number(a.id === pinnedId));
        let saved = copy(config.widgets);
        let draft = copy(saved);
        let editing = false;
        let saving = false;
        let drag = null;
        const widgets = new Map([...grid.querySelectorAll('[data-widget-id]')].map(node => [node.dataset.widgetId, node]));
        const editor = document.getElementById('layout-editor');
        const feedback = document.querySelector('[data-layout-feedback]');
        const empty = document.querySelector('[data-layout-empty]');
        const editButtons = [...document.querySelectorAll('[data-layout-edit]')];
        const saveButton = document.querySelector('[data-layout-save]');
        const hasChanges = () => JSON.stringify(draft) !== JSON.stringify(saved);
        const say = (message, error = false) => {
            feedback.textContent = message;
            feedback.classList.toggle('is-error', error);
            feedback.hidden = !message;
        };
        const render = () => {
            const focused = document.activeElement;
            const visible = draft.filter(widget => widget.visible);
            for (const [position, state] of draft.entries()) {
                const node = widgets.get(state.id);
                node.hidden = !state.visible;
                node.dataset.widgetWidth = state.width;
                node.querySelector('.widget-editbar').hidden = !editing;
                node.querySelector('[data-widget-width-select]').value = state.width;
                const index = visible.findIndex(widget => widget.id === state.id);
                const pinned = state.id === pinnedId;
                node.querySelector('[data-widget-drag]').disabled = pinned;
                node.querySelector('[data-widget-move="-1"]').disabled = pinned || index <= 0 || visible[index - 1]?.id === pinnedId;
                node.querySelector('[data-widget-move="1"]').disabled = pinned || index === visible.length - 1;
                // Move only the changed nodes, preserving focus and mobile scroll position.
                if (grid.children[position] !== node) grid.insertBefore(node, grid.children[position] || null);
            }
            document.querySelectorAll('[data-widget-toggle]').forEach(toggle => {
                toggle.checked = draft.find(widget => widget.id === toggle.dataset.widgetToggle).visible;
            });
            document.querySelector('[data-widget-count]').textContent = `${visible.length} из ${draft.length}`;
            empty.hidden = visible.length > 0;
            if (focused instanceof HTMLElement && focused.closest('[data-widget-id]') && !focused.closest('[data-widget-id]').hidden)
                focused.focus({preventScroll: true});
        };
        const setEditing = value => {
            editing = value;
            editor.hidden = !value;
            document.body.classList.toggle('is-layout-editing', value);
            editButtons.forEach(button => button.setAttribute('aria-expanded', String(value)));
            render();
        };
        const begin = () => {
            if (saving) return;
            if (!editing) {
                draft = copy(saved);
                setEditing(true);
                say('');
            }
            editor.scrollIntoView({block: 'start', behavior: 'auto'});
            editor.querySelector('summary').focus({preventScroll: true});
        };
        const changed = message => say(`${message} Сохраните раскладку, когда закончите.`);
        const moveRelative = (id, direction) => {
            if (!editing || saving || id === pinnedId) return;
            const visible = draft.filter(widget => widget.visible);
            const index = visible.findIndex(widget => widget.id === id);
            const target = visible[index + direction];
            if (!target || target.id === pinnedId) return;
            const from = draft.findIndex(widget => widget.id === id);
            const to = draft.findIndex(widget => widget.id === target.id);
            [draft[from], draft[to]] = [draft[to], draft[from]];
            render();
            widgets.get(id).querySelector('[data-widget-drag]').focus({preventScroll: true});
            widgets.get(id).querySelector('[data-widget-drag]').scrollIntoView({block: 'nearest', behavior: 'instant'});
            changed(`Блок «${widgets.get(id).dataset.widgetTitle}» перемещён ${direction < 0 ? 'раньше' : 'позже'}.`);
        };
        editButtons.forEach(button => button.addEventListener('click', begin));
        document.querySelector('[data-layout-cancel]').addEventListener('click', () => {
            if (saving) return;
            finishDrag(false);
            draft = copy(saved);
            setEditing(false);
            say('Изменения отменены. Восстановлена сохранённая раскладка.');
            editButtons[0].focus({preventScroll: true});
        });
        document.querySelector('[data-layout-reset]').addEventListener('click', () => {
            if (saving) return;
            draft = copy(config.defaults);
            render();
            changed('Восстановлена раскладка по умолчанию.');
        });
        editor.addEventListener('change', event => {
            if (saving || !event.target.matches('[data-widget-toggle]')) return;
            const state = draft.find(widget => widget.id === event.target.dataset.widgetToggle);
            state.visible = event.target.checked;
            render();
            changed(`Блок «${widgets.get(state.id).dataset.widgetTitle}» ${state.visible ? 'добавлен' : 'скрыт'}.`);
        });
        grid.addEventListener('change', event => {
            if (!editing || saving || !event.target.matches('[data-widget-width-select]')) return;
            const node = event.target.closest('[data-widget-id]');
            draft.find(widget => widget.id === node.dataset.widgetId).width = event.target.value;
            render();
            changed(`Ширина блока «${node.dataset.widgetTitle}» изменена.`);
        });
        grid.addEventListener('click', event => {
            if (!editing || saving) return;
            const node = event.target.closest('[data-widget-id]');
            if (!node) return;
            const move = event.target.closest('[data-widget-move]');
            if (move) moveRelative(node.dataset.widgetId, Number(move.dataset.widgetMove));
            if (event.target.closest('[data-widget-hide]')) {
                draft.find(widget => widget.id === node.dataset.widgetId).visible = false;
                render();
                changed(`Блок «${node.dataset.widgetTitle}» скрыт. Его можно вернуть из каталога.`);
                editButtons[0].focus({preventScroll: true});
            }
        });
        grid.addEventListener('keydown', event => {
            if (!event.target.matches('[data-widget-drag]')) return;
            if (['ArrowUp', 'ArrowLeft', 'ArrowDown', 'ArrowRight'].includes(event.key)) {
                event.preventDefault();
                moveRelative(event.target.closest('[data-widget-id]').dataset.widgetId,
                    ['ArrowUp', 'ArrowLeft'].includes(event.key) ? -1 : 1);
            }
        });

        saveButton.addEventListener('click', async () => {
            if (saving || !editing) return;
            finishDrag(false);
            saving = true;
            const controls = [...editor.querySelectorAll('button, input'), ...grid.querySelectorAll('.widget-editbar button, .widget-editbar select')];
            const disabled = controls.map(control => control.disabled);
            controls.forEach(control => {
                control.disabled = true;
            });
            saveButton.textContent = 'Сохраняем…';
            saveButton.setAttribute('aria-busy', 'true');
            say('Сохраняем вашу раскладку…');
            try {
                const response = await fetch(grid.dataset.layoutUrl, {
                    method: 'POST', credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('[data-layout-token] input').value
                    },
                    body: JSON.stringify({widgets: draft}),
                    signal: AbortSignal.timeout(15000)
                });
                if (response.redirected || response.status === 401)
                    throw new Error('Сессия истекла. Откройте вход в другой вкладке и повторите сохранение.');
                if (!response.headers.get('content-type')?.includes('application/json'))
                    throw new Error('Сохранение недоступно. Проверьте подключение и доступ к странице, затем повторите попытку.');
                const result = await response.json();
                if (!response.ok) throw new Error(result.error || 'Не удалось сохранить раскладку. Попробуйте ещё раз.');
                saved = copy(result.widgets);
                draft = copy(saved);
                setEditing(false);
                say('Раскладка сохранена. Она будет доступна при следующем входе и на других устройствах.');
                editButtons[0].focus({preventScroll: true});
            } catch (error) {
                say(error instanceof Error && error.name !== 'TypeError' && error.name !== 'TimeoutError'
                    ? error.message : 'Нет ответа от сервера. Ваши изменения остались на экране — повторите сохранение.', true);
            } finally {
                saving = false;
                controls.forEach((control, index) => {
                    control.disabled = disabled[index];
                });
                saveButton.textContent = 'Сохранить';
                saveButton.removeAttribute('aria-busy');
            }
        });

        const clearDropTargets = () => widgets.forEach(node => node.classList.remove('drop-before', 'drop-after'));
        const updateDropTarget = () => {
            if (!drag) return;
            clearDropTargets();
            const node = document.elementFromPoint(drag.x, drag.y)?.closest('[data-widget-id]');
            drag.target = null;
            if (!node || node === drag.node || node.hidden) return;
            const box = node.getBoundingClientRect();
            // At row edges the vertical position disambiguates full-width modules.
            const after = node.dataset.widgetId === pinnedId || drag.y > box.bottom - Math.min(70, box.height / 3) ||
                (drag.y >= box.top + Math.min(70, box.height / 3) && drag.x > box.left + box.width / 2);
            drag.target = node;
            drag.after = after;
            node.classList.add(after ? 'drop-after' : 'drop-before');
        };
        const dragFrame = () => {
            if (!drag?.active) return;
            const margin = 90;
            if (drag.y < margin) window.scrollBy(0, -Math.ceil((margin - drag.y) / 5));
            else if (drag.y > innerHeight - margin) window.scrollBy(0, Math.ceil((drag.y - innerHeight + margin) / 5));
            updateDropTarget();
            drag.frame = requestAnimationFrame(dragFrame);
        };

        function finishDrag(commit) {
            if (!drag) return;
            const current = drag;
            drag = null;
            cancelAnimationFrame(current.frame);
            if (current.handle.hasPointerCapture(current.pointerId)) current.handle.releasePointerCapture(current.pointerId);
            current.ghost?.remove();
            current.node.classList.remove('is-dragging');
            document.body.classList.remove('widget-dragging');
            clearDropTargets();
            if (commit && current.active && current.target) {
                const from = draft.findIndex(widget => widget.id === current.node.dataset.widgetId);
                const [state] = draft.splice(from, 1);
                const to = draft.findIndex(widget => widget.id === current.target.dataset.widgetId);
                draft.splice(Math.max(1, to + (current.after ? 1 : 0)), 0, state);
                render();
                current.handle.focus({preventScroll: true});
                changed(`Положение блока «${current.node.dataset.widgetTitle}» изменено.`);
            }
        }

        grid.addEventListener('pointerdown', event => {
            const handle = event.target.closest('[data-widget-drag]');
            if (!handle || handle.disabled || !editing || saving || event.button !== 0) return;
            handle.focus({preventScroll: true});
            handle.setPointerCapture(event.pointerId);
            drag = {
                handle,
                pointerId: event.pointerId,
                node: handle.closest('[data-widget-id]'),
                startX: event.clientX,
                startY: event.clientY,
                x: event.clientX,
                y: event.clientY,
                active: false,
                target: null
            };
            event.preventDefault();
        });
        grid.addEventListener('pointermove', event => {
            if (!drag || event.pointerId !== drag.pointerId) return;
            drag.x = event.clientX;
            drag.y = event.clientY;
            if (!drag.active && Math.hypot(drag.x - drag.startX, drag.y - drag.startY) > 7) {
                drag.active = true;
                drag.ghost = document.createElement('div');
                drag.ghost.className = 'widget-drag-ghost';
                drag.ghost.textContent = drag.node.dataset.widgetTitle;
                document.body.append(drag.ghost);
                drag.node.classList.add('is-dragging');
                document.body.classList.add('widget-dragging');
                drag.frame = requestAnimationFrame(dragFrame);
            }
            if (drag.active) {
                drag.ghost.style.left = `${Math.min(innerWidth - 200, Math.max(8, drag.x + 16))}px`;
                drag.ghost.style.top = `${Math.min(innerHeight - 55, Math.max(8, drag.y + 16))}px`;
                updateDropTarget();
            }
        });
        grid.addEventListener('pointerup', () => finishDrag(true));
        grid.addEventListener('pointercancel', () => finishDrag(false));
        grid.addEventListener('lostpointercapture', () => finishDrag(false));
        window.addEventListener('keydown', event => {
            if (event.key === 'Escape') finishDrag(false);
        });
        window.addEventListener('beforeunload', event => {
            if (!editing || !hasChanges()) return;
            event.preventDefault();
            event.returnValue = '';
        });

        // Navigation to a deliberately hidden module may reveal it for this visit,
        // without silently overwriting the saved visibility preference.
        const revealAnchor = () => {
            let target;
            try {
                target = document.getElementById(decodeURIComponent(location.hash.slice(1)));
            } catch {
                return;
            }
            const node = target?.closest('[data-widget-id]');
            if (!node?.hidden) return;
            if (editing) {
                draft.find(widget => widget.id === node.dataset.widgetId).visible = true;
                render();
                changed(`Блок «${node.dataset.widgetTitle}» добавлен.`);
            } else {
                node.hidden = false;
                empty.hidden = true;
                say(`Блок «${node.dataset.widgetTitle}» открыт по ссылке. Сохранённая раскладка не изменилась.`);
            }
            target.scrollIntoView({block: 'start'});
        };
        window.addEventListener('hashchange', revealAnchor);
        render();
        revealAnchor();
    })();

    // Home progress and workout choices.
    (() => {
        'use strict';
        document.querySelectorAll('[data-home-select]').forEach(select => select.addEventListener('change', () => select.form.requestSubmit()));
        if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
            const entrances = new IntersectionObserver(entries => entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('home-interactive-entered');
                    entrances.unobserve(entry.target);
                }
            }), {threshold: .12});
            document.querySelectorAll('[data-home-progress],.em-home-goal,.em-home-last').forEach(card => entrances.observe(card));
        }
        document.querySelectorAll('[data-home-progress]').forEach(card => {
            const detail = card.querySelector('[data-home-point-detail]');
            if (!detail) return;
            const points = [...card.querySelectorAll('[data-chart-point]')];
            const show = point => {
                points.forEach(item => item.classList.toggle('is-selected', item === point));
                detail.querySelector('span').textContent = `${point.dataset.date} · ${point.dataset.value} кг${point.dataset.reps ? ` × ${point.dataset.reps}` : ''}`;
                const link = detail.querySelector('a');
                link.hidden = !point.dataset.workoutUrl;
                if (point.dataset.workoutUrl) link.href = point.dataset.workoutUrl;
            };
            points.forEach(point => {
                point.addEventListener('click', () => show(point));
                point.addEventListener('focus', () => show(point));
                point.addEventListener('keydown', event => {
                    if (['Enter', ' '].includes(event.key)) {
                        event.preventDefault();
                        show(point);
                    }
                });
            });
            const svg = card.querySelector('.epr-chart');
            const viewBox = svg.getAttribute('viewBox').split(/\s+/).map(Number);
            const geometry = [...svg.querySelectorAll('[x],[x1],[x2],[cx],[y],[y1],[y2],[cy],path[d]')].map(element => ({
                element,
                coordinates: ['x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy'].filter(name => element.hasAttribute(name)).map(name => [name, Number(element.getAttribute(name))]),
                path: element.getAttribute('d')
            }));
            const resize = () => {
                const width = svg.clientWidth, height = svg.clientHeight;
                if (!width || !height) return;
                const ratioX = width / viewBox[2], ratioY = height / viewBox[3];
                svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
                geometry.forEach(({element, coordinates, path}) => {
                    coordinates.forEach(([name, value]) => element.setAttribute(name, value * (['x', 'x1', 'x2', 'cx'].includes(name) ? ratioX : ratioY)));
                    if (path) element.setAttribute('d', path.replace(/([ML])([\d.]+),([\d.]+)/g, (_, command, x, y) => `${command}${Number(x) * ratioX},${Number(y) * ratioY}`));
                });
            };
            new ResizeObserver(resize).observe(svg);
            resize();
            svg.addEventListener('click', event => {
                const closest = points.map(point => {
                    const box = point.getBoundingClientRect();
                    return {
                        point,
                        distance: Math.hypot(event.clientX - box.left - box.width / 2, event.clientY - box.top - box.height / 2)
                    };
                }).sort((a, b) => a.distance - b.distance)[0];
                if (closest?.distance <= 24) show(closest.point);
            });
        });
        document.querySelectorAll('.home-workout-choices form').forEach(form => form.addEventListener('submit', () => {
            const button = form.querySelector('button');
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
        }));
        window.addEventListener('pageshow', () => document.querySelectorAll('.home-workout-choices [aria-busy]').forEach(button => {
            button.disabled = false;
            button.removeAttribute('aria-busy');
        }));
    })();

    // Workout history filters.
    (() => {
        const form = document.querySelector('[data-workout-filters]');
        if (!form) return;
        const search = form.querySelector('input[type="search"]');
        const program = form.querySelector('select');
        let timer;
        const submit = () => {
            clearTimeout(timer);
            form.requestSubmit();
        };
        program.addEventListener('change', submit);
        // Preserve typing focus and the cursor through the server-rendered filter update.
        form.addEventListener('submit', () => {
            if (document.activeElement === search) {
                sessionStorage.setItem('enix-workout-search-focus', 'true');
            }
        });
        search.addEventListener('input', () => {
            clearTimeout(timer);
            timer = setTimeout(submit, 650);
        });
        if (sessionStorage.getItem('enix-workout-search-focus')) {
            sessionStorage.removeItem('enix-workout-search-focus');
            search.focus({preventScroll: true});
            const length = search.value.length;
            search.setSelectionRange(length, length);
        }
    })();
}

// Progress workspace and training analytics.
// Progress page charts have their own interactions, separate from home charts.
if (document.body.classList.contains('ef-progress-page')) {
    // Progress workspace and interactive SVG charts.
    (() => {
        'use strict';
        document.addEventListener('click', event => {
            const link = event.target.closest('a[data-dialog^="progress-"]');
            if (link) event.preventDefault();
            document.querySelectorAll('.epr-period[open]').forEach(menu => {
                if (!menu.contains(event.target)) menu.open = false;
            });
        });
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape') document.querySelectorAll('.epr-period[open]').forEach(menu => {
                menu.open = false;
            });
        });
        const workspace = document.querySelector('[data-progress-workspace]');
        if (workspace) {
            const mobile = matchMedia('(max-width:767px)');
            const tabs = [...workspace.querySelectorAll('[data-progress-metric]')];
            const panels = [...workspace.querySelectorAll('[data-progress-panel]')];
            const selectMetric = (metric, animate = false) => {
                if (!tabs.some(tab => tab.dataset.progressMetric === metric)) return;
                workspace.dataset.metric = metric;
                tabs.forEach(tab => {
                    const selected = tab.dataset.progressMetric === metric;
                    tab.setAttribute('aria-selected', String(selected));
                    tab.tabIndex = selected ? 0 : -1;
                });
                panels.forEach(panel => {
                    const selected = panel.dataset.progressPanel === metric;
                    panel.classList.remove('is-entering');
                    if (mobile.matches) {
                        panel.setAttribute('role', 'tabpanel');
                        panel.setAttribute('aria-labelledby', `pw-tab-${panel.dataset.progressPanel}`);
                        panel.setAttribute('aria-hidden', String(!selected));
                        if (selected && animate) panel.classList.add('is-entering');
                    } else {
                        panel.removeAttribute('role');
                        panel.removeAttribute('aria-hidden');
                        panel.setAttribute('aria-labelledby', `pw-${panel.dataset.progressPanel}-title`);
                    }
                });
            };
            tabs.forEach((tab, index) => {
                tab.addEventListener('click', event => {
                    if (!mobile.matches || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
                    event.preventDefault();
                    selectMetric(tab.dataset.progressMetric, true);
                    const url = new URL(location.href);
                    url.searchParams.set('ProgressMetric', tab.dataset.progressMetric);
                    history.replaceState(null, '', url);
                });
                tab.addEventListener('keydown', event => {
                    if (!mobile.matches || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
                    event.preventDefault();
                    const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
                    tabs[next].click();
                    tabs[next].focus();
                });
            });
            mobile.addEventListener('change', () => selectMetric(workspace.dataset.metric));
            selectMetric(workspace.dataset.metric);
        }
        document.querySelectorAll('[data-progress-chart]').forEach(chart => {
            const svg = chart.querySelector('svg');
            const originalViewBox = svg.getAttribute('viewBox').split(/\s+/).map(Number);
            const geometry = [...svg.querySelectorAll('[x],[x1],[x2],[cx],[y],[y1],[y2],[cy],path[d]')].map(element => ({
                element,
                coordinates: ['x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy'].filter(name => element.hasAttribute(name)).map(name => [name, Number(element.getAttribute(name))]),
                path: element.getAttribute('d')
            }));
            const resize = () => {
                const responsive = window.innerWidth < 768 || chart.closest('[data-progress-workspace]');
                const width = responsive ? svg.clientWidth : originalViewBox[2];
                const height = responsive ? svg.clientHeight : originalViewBox[3];
                if (width <= 0 || height <= 0) return;
                const ratioX = width / originalViewBox[2];
                const ratioY = height / originalViewBox[3];
                svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
                geometry.forEach(({element, coordinates, path}) => {
                    coordinates.forEach(([name, value]) => element.setAttribute(name, value * (['x', 'x1', 'x2', 'cx'].includes(name) ? ratioX : ratioY)));
                    if (path) element.setAttribute('d', path.replace(/([ML])([\d.]+),([\d.]+)/g, (_, command, x, y) => `${command}${Number(x) * ratioX},${Number(y) * ratioY}`));
                });
            };
            new ResizeObserver(resize).observe(svg);
            resize();
            const tooltip = chart.querySelector('output');
            svg.addEventListener('click', event => {
                if (event.target.matches('[data-chart-point]')) return;
                const closest = [...chart.querySelectorAll('[data-chart-point]')].map(point => {
                    const box = point.getBoundingClientRect();
                    return {
                        point,
                        distance: Math.hypot(event.clientX - box.left - box.width / 2, event.clientY - box.top - box.height / 2)
                    };
                }).sort((a, b) => a.distance - b.distance)[0];
                if (closest?.distance <= 24) closest.point.dispatchEvent(new MouseEvent('click', {bubbles: false}));
            });
            const hide = () => {
                tooltip.hidden = true;
            };
            const points = [...chart.querySelectorAll('[data-chart-point]')];
            points.forEach((point, index) => {
                const select = () => {
                    const result = chart.querySelector('[data-progress-result]');
                    if (!result) return;
                    points.forEach(other => other.setAttribute('aria-pressed', String(other === point)));
                    result.hidden = false;
                    result.querySelector(':scope > span').textContent = point.dataset.detail || `${point.dataset.date} · ${point.dataset.value} кг${point.dataset.reps ? ` × ${point.dataset.reps}` : ''}`;
                    const link = result.querySelector('a');
                    link.hidden = !point.dataset.workoutUrl;
                    if (point.dataset.workoutUrl) link.href = point.dataset.workoutUrl;
                    result.querySelector('[data-point-link-label]').textContent = point.dataset.linkLabel || 'Открыть тренировку';
                };
                point.addEventListener('click', select);
                point.addEventListener('keydown', event => {
                    if (['Enter', ' '].includes(event.key)) {
                        event.preventDefault();
                        select();
                    } else if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) {
                        event.preventDefault();
                        const next = event.key === 'Home' ? 0 : event.key === 'End' ? points.length - 1 : Math.max(0, Math.min(points.length - 1, index + (event.key === 'ArrowRight' ? 1 : -1)));
                        points[next].focus();
                        points[next].dispatchEvent(new MouseEvent('click'));
                    }
                });
                const show = () => {
                    tooltip.textContent = point.dataset.detail || `${point.dataset.date} · ${point.dataset.value}`;
                    tooltip.hidden = false;
                    const bounds = chart.getBoundingClientRect();
                    const position = point.getBoundingClientRect();
                    tooltip.style.left = `${Math.max(0, Math.min(bounds.width - tooltip.offsetWidth, position.left - bounds.left - tooltip.offsetWidth / 2))}px`;
                    tooltip.style.top = `${Math.max(0, position.top - bounds.top - tooltip.offsetHeight - 10)}px`;
                };
                point.addEventListener('mouseenter', show);
                point.addEventListener('mouseleave', hide);
                point.addEventListener('focus', show);
                point.addEventListener('blur', hide);
            });
            if (chart.dataset.autoSelect === 'true') points.at(-1)?.dispatchEvent(new MouseEvent('click'));
        });
    })();
}

// Training analytics charts.
(() => {
    const initTrainingCharts = () => {
        'use strict';
        const source = document.getElementById('training-analytics-chart-data');
        if (!source || typeof Chart === 'undefined') return;
        let data;
        try {
            data = JSON.parse(source.textContent);
        } catch {
            return;
        }
        const charts = [];

        function create(id, label, series, color) {
            const canvas = document.getElementById(id);
            if (!canvas || !series) return;
            charts.push(new Chart(canvas, {
                type: 'line',
                data: {
                    labels: series.labels,
                    datasets: [{
                        label,
                        data: series.values,
                        borderColor: color,
                        backgroundColor: color + '18',
                        fill: true,
                        tension: .2,
                        pointRadius: 3,
                        pointHoverRadius: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: matchMedia('(prefers-reduced-motion:reduce)').matches ? false : {duration: 250},
                    interaction: {mode: 'index', intersect: false},
                    onClick: (_, points) => {
                        const id = series.workoutIds?.[points[0]?.index];
                        if (Number.isInteger(id) && id > 0) location.assign('/Workouts/Details/' + id);
                    },
                    onHover: (event, points) => {
                        if (event.native?.target) event.native.target.style.cursor = points.length ? 'pointer' : 'default';
                    },
                    plugins: {
                        legend: {display: false},
                        tooltip: {
                            backgroundColor: '#213b5e',
                            borderColor: '#6385b7',
                            borderWidth: 1,
                            padding: 12,
                            displayColors: false
                        }
                    },
                    scales: {
                        x: {
                            border: {display: false},
                            grid: {display: false},
                            ticks: {maxTicksLimit: 6, maxRotation: 0, color: '#aec5e6'}
                        },
                        y: {
                            border: {display: false},
                            grid: {color: '#30496b'},
                            ticks: {maxTicksLimit: 5, color: '#aec5e6'}
                        }
                    }
                }
            }));
        }

        create('working-weight-chart', 'Рабочий вес, кг', data.workingWeight, '#8bb6ff');
        create('estimated-one-rep-max-chart', 'Расчётный максимум, кг', data.estimatedOneRepMax, '#b4a5ff');
        create('exercise-volume-chart', 'Объём подходов, кг', data.volume, '#7fc9e8');
        document.querySelectorAll('[name="analytics-metric"]').forEach(input => input.addEventListener('change', () => requestAnimationFrame(() => charts.forEach(chart => chart.resize()))));
    };
    // Chart.js is loaded later by the analytics page's Scripts section.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initTrainingCharts, {once: true});
    } else {
        initTrainingCharts();
    }
})();

// Body measurements and subscription.
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
            links.forEach(link => {
                link.classList.toggle('is-selected', link === point);
                link.setAttribute('aria-pressed', String(link === point));
            });
            picker.value = point.dataset.bodyPoint;
            chart.querySelector('[data-body-selected-date]').textContent = point.dataset.bodyDate;
            chart.querySelector('[data-body-selected-value]').textContent = point.dataset.bodyValue;
            chart.querySelector('[data-body-selected-edit]').href = point.getAttribute('href');
            const address = new URL(location.href);
            address.searchParams.set('Point', point.dataset.bodyPoint);
            history.replaceState(history.state, '', address);
        };
        links.forEach(link => {
            link.setAttribute('role', 'button');
            link.setAttribute('aria-pressed', String(link.classList.contains('is-selected')));
            link.setAttribute('aria-label', link.getAttribute('aria-label').replace('Изменить замер', 'Выбрать замер'));
            link.addEventListener('keydown', event => {
                if (event.key === ' ') {
                    event.preventDefault();
                    link.dispatchEvent(new MouseEvent('click', {bubbles: true, cancelable: true}));
                }
            });
            link.addEventListener('click', event => {
                if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
                event.preventDefault();
                select(link.dataset.bodyPoint);
            });
        });
        picker.addEventListener('change', () => select(picker.value));
        chart.querySelector('[data-body-point-picker]').addEventListener('submit', event => {
            event.preventDefault();
            select(picker.value);
        });
    }
    root.querySelectorAll('[data-body-filters] select').forEach(select => select.addEventListener('change', () => select.form.requestSubmit()));
    const form = root.querySelector('[data-body-form]');
    if (form) {
        const feedback = form.querySelector('[data-body-feedback]');
        const save = form.querySelector('[data-body-save]');
        const caption = save.textContent;
        let awaiting = false;
        const notice = state => window.AppComponents?.setNoticeState(feedback, state);
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
        numbers.forEach(field => field.addEventListener('input', () => {
            validate(field);
            feedback.textContent = '';
            notice('info');
        }));
        const notes = form.querySelector('[data-body-notes]');
        notes?.addEventListener('input', () => {
            form.querySelector('[data-body-note-count]').textContent = notes.value.length + ' / 1000';
        });
        const recover = () => {
            save.disabled = false;
            save.removeAttribute('aria-busy');
            save.textContent = caption;
            if (awaiting) {
                feedback.textContent = '';
                notice('info');
                awaiting = false;
            }
        };
        form.addEventListener('submit', event => {
            numbers.forEach(validate);
            const invalid = [...form.querySelectorAll('input,select,textarea')].find(field => !field.checkValidity());
            if (invalid) {
                event.preventDefault();
                const details = invalid.closest('details');
                if (details) details.open = true;
                invalid.focus();
                invalid.reportValidity();
                return;
            }
            if (!navigator.onLine) {
                event.preventDefault();
                recover();
                feedback.textContent = 'Сейчас нет сети. Данные остались в форме; повторите сохранение после подключения.';
                notice('error');
                return;
            }
            awaiting = true;
            feedback.textContent = 'Ожидаем подтверждения…';
            notice('pending');
            save.disabled = true;
            save.setAttribute('aria-busy', 'true');
            save.textContent = save.classList.contains('danger') ? 'Удаляем…' : 'Сохраняем…';
        });
        // Validate hidden optional inputs before native constraint validation tries to focus them.
        form.addEventListener('invalid', event => {
            const details = event.target.closest('details');
            if (details) details.open = true;
        }, true);
        window.addEventListener('online', () => {
            if (feedback.textContent) {
                feedback.textContent = 'Соединение восстановлено. Повторите сохранение.';
                notice('pending');
            }
        });
        window.addEventListener('pageshow', recover);
    }
    const plans = [...root.querySelectorAll('[data-plan]')];
    if (plans.length) {
        const desktop = matchMedia('(min-width: 900px)');
        const layout = () => plans.forEach(plan => {
            plan.open = desktop.matches || plan.dataset.plan === root.dataset.currentPlan;
        });
        desktop.addEventListener('change', layout);
        layout();
    }
})();

// Account and security settings.
(() => {
    'use strict';
    const root = document.querySelector('[data-account-settings]');
    if (!root) return;
    const notice = (element, state) => window.AppComponents?.setNoticeState(element, state);
    root.classList.add('is-enhanced');
    const sections = root.querySelector('[data-settings-sections]'), desktop = matchMedia('(min-width:1024px)');
    const arrange = () => {
        sections.open = desktop.matches;
    };
    arrange();
    desktop.addEventListener('change', arrange);
    const copy = root.querySelector('[data-copy-authenticator]');
    if (copy && navigator.clipboard) {
        copy.hidden = false;
        copy.addEventListener('click', async () => {
            const feedback = root.querySelector('[data-copy-feedback]');
            try {
                await navigator.clipboard.writeText(root.querySelector('#authenticator-key').textContent.trim());
                feedback.textContent = 'Ключ скопирован.';
                notice(feedback, 'info');
            } catch {
                feedback.textContent = 'Не удалось скопировать. Выделите ключ вручную.';
                notice(feedback, 'error');
            }
        });
    }
    root.querySelectorAll('input[type="password"][data-password-reveal]').forEach(input => {
        const wrapper = input.parentElement, button = document.createElement('button');
        button.type = 'button';
        button.className = 'account-password-toggle app-button secondary';
        button.textContent = 'Показать';
        button.setAttribute('aria-controls', input.id);
        button.setAttribute('aria-pressed', 'false');
        const label = root.querySelector(`label[for="${input.id}"]`).textContent;
        button.setAttribute('aria-label', 'Показать: ' + label);
        button.addEventListener('click', () => {
            const reveal = input.type === 'password';
            input.type = reveal ? 'text' : 'password';
            button.textContent = reveal ? 'Скрыть' : 'Показать';
            button.setAttribute('aria-pressed', String(reveal));
            button.setAttribute('aria-label', (reveal ? 'Скрыть: ' : 'Показать: ') + label);
        });
        wrapper.append(button);
    });
    root.querySelectorAll('form[data-account-form]').forEach(form => {
        const status = form.querySelector('[data-account-feedback]'), save = form.querySelector('[data-account-save]'),
            fields = [...form.querySelectorAll('input:not([type="hidden"]):not(:disabled)')];
        const original = fields.map(input => input.value), saveText = save?.textContent;
        const dirty = () => fields.some((input, index) => input.value !== original[index]);
        const update = () => {
            const changed = dirty();
            if (save) save.disabled = !changed;
            if (status) {
                status.textContent = changed ? 'Есть несохранённые изменения.' : '';
                notice(status, changed ? 'pending' : 'info');
            }
            const confirmation = root.querySelector('.account-status:not(.is-error)');
            if (confirmation) confirmation.hidden = changed;
        };
        fields.forEach(input => input.addEventListener('input', update));
        update();
        form.addEventListener('submit', event => {
            if (!navigator.onLine) {
                event.preventDefault();
                status.textContent = 'Сейчас нет сети. Данные остаются в форме; повторите после подключения.';
                notice(status, 'error');
                return;
            }
            if (event.defaultPrevented) return;
            if (!form.checkValidity()) {
                event.preventDefault();
                form.reportValidity();
                return;
            }
            if (typeof window.jQuery?.fn?.valid === 'function' && !window.jQuery(form).valid()) {
                event.preventDefault();
                return;
            }
            if (save) {
                save.disabled = true;
                save.setAttribute('aria-busy', 'true');
                save.textContent = 'Сохраняем…';
            }
            status.textContent = 'Ожидаем подтверждения…';
            notice(status, 'pending');
        });
        window.addEventListener('pageshow', () => {
            if (save) {
                save.removeAttribute('aria-busy');
                save.textContent = saveText;
            }
            update();
        });
        window.addEventListener('online', () => {
            if (status && dirty()) {
                status.textContent = 'Подключение восстановлено. Можно повторить сохранение.';
                notice(status, 'pending');
            }
        });
    });
})();
