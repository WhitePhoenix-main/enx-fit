// Public navigation, previews and animations.
if (document.body.classList.contains('enix-landing')) {
    // Public navigation and FAQ links.
    (() => {
        'use strict';
        document.documentElement.classList.add('landing-ready');
        const menu = document.querySelector('[data-public-menu]');
        const summary = menu?.querySelector('summary');
        const close = restore => {
            if (!menu?.open) return;
            menu.open = false;
            if (restore) summary.focus();
        };
        menu?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => close(false)));
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && menu?.open) {
                event.preventDefault();
                close(true);
            }
        });
        document.addEventListener('click', event => {
            if (!menu?.contains(event.target)) close(false);
        });
        document.addEventListener('focusin', event => {
            if (!menu?.contains(event.target)) close(false);
        });
        const openLinkedAnswer = () => {
            const target = document.getElementById(location.hash.slice(1));
            if (target?.matches('.faq-items details')) target.open = true;
        };
        window.addEventListener('hashchange', openLinkedAnswer);
        openLinkedAnswer();
        const header = document.querySelector('.landing-header');
        let frame = 0;
        const update = () => {
            header?.classList.toggle('is-scrolled', scrollY > 40);
            frame = 0;
        };
        window.addEventListener('scroll', () => {
            if (!frame) frame = requestAnimationFrame(update);
        }, {passive: true});
        update();
    })();

    // Landing animations.
    (() => {
        'use strict';
        const root = document.documentElement;
        const hero = document.querySelector('.hero');
        if (!hero) return;
        const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
        const desktopPointer = matchMedia('(min-width: 901px) and (hover: hover) and (pointer: fine)');
        const animations = new Set();
        root.classList.add('motion-ready');

        const animate = (element, frames, options = {}) => {
            if (!element || reducedMotion.matches || document.hidden) return;
            const animation = element.animate(frames, {
                duration: 650, easing: 'cubic-bezier(.22,1,.36,1)', fill: 'backwards', ...options
            });
            animations.add(animation);
            animation.finished.then(() => animations.delete(animation), () => animations.delete(animation));
        };
        const reveal = (element, delay = 0) => animate(element, [
            {opacity: 0, translate: '0 16px'}, {opacity: 1, translate: '0 0'}
        ], {delay});
        const revealImage = async (element, delay = 0) => {
            const images = Array.from(element.querySelectorAll('img'));
            await Promise.all(images.map(img => img.decode().catch(() => {
            })));
            const bounds = element.getBoundingClientRect();
            if (bounds.bottom > 0 && bounds.top < innerHeight) reveal(element, delay);
        };

        const entrances = new Map();
        entrances.set(hero, () => {
            hero.querySelectorAll('.hero-copy > *').forEach((element, index) => reveal(element, index * 65));
            revealImage(hero.querySelector('.dashboard-device'), 100);
            revealImage(hero.querySelector('.phone-device'), 250);
            reveal(hero.querySelector('.athlete-quote'), 350);
            reveal(hero.querySelector('.hero-preview-caption'), 400);
        });
        document.querySelectorAll('.section-copy').forEach(element => entrances.set(element, () => reveal(element)));
        document.querySelectorAll('[data-reveal]').forEach(element => entrances.set(element, () => revealImage(element)));
        document.querySelectorAll('.integration-card, .price-card, .faq-items details').forEach(element => {
            const index = Array.from(element.parentElement.children).indexOf(element);
            entrances.set(element, () => reveal(element, index * 70));
        });
        const steps = document.querySelector('.steps');
        steps?.querySelectorAll('li > div').forEach(content => {
            const line = document.createElement('span');
            line.className = 'step-progress';
            line.setAttribute('aria-hidden', 'true');
            content.prepend(line);
        });
        if (steps) entrances.set(steps, () => {
            steps.querySelectorAll('li').forEach((step, index) => {
                reveal(step, index * 100);
                animate(step.querySelector('.step-progress'), [
                    {transform: 'scaleX(0)'}, {transform: 'scaleX(1)'}
                ], {delay: index * 130 + 100, duration: 500});
            });
        });
        const closing = document.querySelector('.closing-cta');
        if (closing) entrances.set(closing, () => reveal(closing));
        const entering = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                const run = entrances.get(entry.target);
                entrances.delete(entry.target);
                entering.unobserve(entry.target);
                run?.();
            });
            if (!entrances.size) entering.disconnect();
        }, {threshold: .12});
        entrances.forEach((run, element) => entering.observe(element));

        let pointerFrame = 0;
        let heroBounds;
        const resetParallax = () => {
            cancelAnimationFrame(pointerFrame);
            pointerFrame = 0;
            heroBounds = undefined;
            root.style.removeProperty('--hero-x');
            root.style.removeProperty('--hero-y');
        };
        hero.addEventListener('pointermove', event => {
            if (!desktopPointer.matches || reducedMotion.matches || event.pointerType === 'touch') return;
            heroBounds ||= hero.getBoundingClientRect();
            const x = Math.max(-1, Math.min(1, (event.clientX - heroBounds.left) / heroBounds.width * 2 - 1));
            const y = Math.max(-1, Math.min(1, (event.clientY - heroBounds.top) / heroBounds.height * 2 - 1));
            cancelAnimationFrame(pointerFrame);
            pointerFrame = requestAnimationFrame(() => {
                root.style.setProperty('--hero-x', x.toFixed(3));
                root.style.setProperty('--hero-y', y.toFixed(3));
                pointerFrame = 0;
            });
        });
        hero.addEventListener('pointerleave', resetParallax);
        hero.addEventListener('focusin', resetParallax);
        desktopPointer.addEventListener('change', resetParallax);
        document.querySelectorAll('.price-card, .integration-card').forEach(card => {
            let frame = 0;
            card.addEventListener('pointermove', event => {
                if (!desktopPointer.matches || reducedMotion.matches || event.pointerType === 'touch') return;
                cancelAnimationFrame(frame);
                frame = requestAnimationFrame(() => {
                    const bounds = card.getBoundingClientRect();
                    card.style.setProperty('--pointer-x', (event.clientX - bounds.left) + 'px');
                    card.style.setProperty('--pointer-y', (event.clientY - bounds.top) + 'px');
                    frame = 0;
                });
            });
            card.addEventListener('pointerleave', () => {
                cancelAnimationFrame(frame);
                frame = 0;
            });
        });
        window.addEventListener('scroll', () => {
            heroBounds = undefined;
        }, {passive: true});
        const navigationLinks = Array.from(document.querySelectorAll(".landing-nav a")).filter(link => link.pathname === location.pathname && link.hash);
        const sections = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                navigationLinks.forEach(link => {
                    if (link.hash === '#' + entry.target.id) link.setAttribute('aria-current', 'location');
                    else link.removeAttribute('aria-current');
                });
            });
        }, {rootMargin: '-15% 0px -60% 0px'});
        document.querySelectorAll('main > section[id]').forEach(section => sections.observe(section));
        const heroVisibility = new IntersectionObserver(entries => {
            hero.classList.toggle('is-motion-visible', entries[0].isIntersecting);
        }, {threshold: .05});
        heroVisibility.observe(hero);

        const finishMotion = () => {
            animations.forEach(animation => animation.cancel());
            resetParallax();
        };
        document.addEventListener('visibilitychange', () => {
            root.classList.toggle('motion-paused', document.hidden);
            if (document.hidden) finishMotion();
        });
        window.addEventListener('pagehide', () => {
            finishMotion();
            root.classList.add('motion-paused');
        });
        window.addEventListener('pageshow', () => root.classList.toggle('motion-paused', document.hidden));
        reducedMotion.addEventListener('change', () => {
            if (reducedMotion.matches) finishMotion();
        });
    })();

    // Product previews and support contact.
    (() => {
        'use strict';
        document.querySelectorAll('[data-public-preview]').forEach(preview => {
            const tablist = preview.querySelector('[role="tablist"]');
            const tabs = [...preview.querySelectorAll('[data-preview-tab]')];
            const panels = [...preview.querySelectorAll('[data-preview-panel]')];
            if (!tablist || tabs.length !== panels.length) return;
            const select = (tab, focus = false) => {
                tabs.forEach(item => {
                    const active = item === tab;
                    item.setAttribute('aria-selected', String(active));
                    item.tabIndex = active ? 0 : -1;
                });
                panels.forEach(panel => {
                    const active = panel.dataset.previewPanel === tab.dataset.previewTab;
                    panel.open = active;
                    panel.hidden = !active;
                });
                if (focus) tab.focus();
            };
            panels.forEach(panel => {
                panel.setAttribute('role', 'tabpanel');
                panel.setAttribute('aria-labelledby', 'preview-tab-' + panel.dataset.previewPanel);
                panel.tabIndex = 0;
            });
            tabs.forEach((tab, index) => {
                tab.addEventListener('click', () => select(tab));
                tab.addEventListener('keydown', event => {
                    let next;
                    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
                    else if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
                    else if (event.key === 'Home') next = 0;
                    else if (event.key === 'End') next = tabs.length - 1;
                    else return;
                    event.preventDefault();
                    select(tabs[next], true);
                });
            });
            select(tabs[0]);
            preview.classList.add('is-interactive');
            tablist.hidden = false;
        });
        const copy = document.querySelector('[data-copy-support]');
        const address = document.querySelector('#support-address');
        const feedback = document.querySelector('[data-copy-feedback]');
        if (copy && address && feedback) {
            copy.hidden = false;
            copy.addEventListener('click', async () => {
                copy.disabled = true;
                feedback.textContent = 'Копируем адрес…';
                window.AppComponents?.setNoticeState(feedback, 'pending');
                try {
                    if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable');
                    await navigator.clipboard.writeText(address.value);
                    feedback.textContent = 'Адрес скопирован.';
                    window.AppComponents?.setNoticeState(feedback, 'saved');
                } catch {
                    address.focus();
                    address.select();
                    feedback.textContent = 'Адрес выделен. Скопируй его вручную.';
                    window.AppComponents?.setNoticeState(feedback, 'error');
                } finally {
                    copy.disabled = false;
                }
            });
        }
    })();
}

// Sign-in, registration and recovery.
if (document.getElementById('auth-dialog')) {
    (() => {
        'use strict';
        const dialog = document.getElementById('auth-dialog');
        const info = {
            privacy: ['Конфиденциальность', 'Политика конфиденциальности Enix Fit готовится к публикации.'],
            terms: ['Условия использования', 'Полные условия использования Enix Fit готовятся к публикации.'],
            support: ['Поддержка Enix Fit', 'Если не получается войти, проверьте email и пароль или воспользуйтесь ссылкой «Забыли пароль?» в форме входа.']
        };

        function showInfo(title, description) {
            document.getElementById('auth-dialog-title').textContent = title;
            document.getElementById('auth-dialog-description').textContent = description;
            dialog.showModal();
        }

        document.querySelectorAll('[data-auth-info]').forEach(button => {
            button.hidden = false;
            button.addEventListener('click', () => showInfo(...info[button.dataset.authInfo]));
        });
        document.querySelectorAll('[data-close-auth-dialog]').forEach(button => button.addEventListener('click', () => dialog.close()));
        dialog.addEventListener('click', event => {
            const bounds = dialog.getBoundingClientRect();
            if (event.target === dialog && (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)) dialog.close();
        });
        document.querySelectorAll('[data-unavailable-provider]').forEach(button => {
            button.addEventListener('click', event => {
                event.preventDefault();
                showInfo('Вход через ' + button.dataset.unavailableProvider, 'Этот способ входа пока недоступен. Вы можете войти или создать аккаунт с помощью email и пароля.');
            });
        });
        document.querySelectorAll('[data-reveal]').forEach(button => {
            button.hidden = false;
            const input = document.getElementById(button.dataset.reveal);
            const fieldLabel = document.querySelector(`label[for="${input.id}"]`)?.textContent.trim().toLowerCase() || 'пароль';
            button.addEventListener('click', () => {
                const reveal = input.type === 'password';
                input.type = reveal ? 'text' : 'password';
                button.setAttribute('aria-pressed', String(reveal));
                button.setAttribute('aria-label', (reveal ? 'Скрыть ' : 'Показать ') + fieldLabel);
                button.setAttribute('aria-controls', input.id);
                button.querySelector('use').setAttribute('href', reveal ? '#auth-eye' : '#auth-eye-off');
            });
        });
        document.querySelectorAll('[data-auth-form]').forEach(form => {
            const confirmation = form.querySelector('[name="Input.ConfirmPassword"]');
            if (confirmation) {
                const password = form.querySelector('[name="Input.Password"]');
                const validate = () => {
                    const message = confirmation.value && confirmation.value !== password.value ? 'Пароли не совпадают.' : '';
                    confirmation.setCustomValidity(message);
                    const error = form.querySelector('[data-valmsg-for="Input.ConfirmPassword"]');
                    if (error) error.textContent = message;
                };
                confirmation.addEventListener('input', validate);
                password.addEventListener('input', validate);
            }
            const submit = form.querySelector('[type="submit"]');
            const feedback = form.querySelector('[data-auth-feedback]');
            let busy = false;
            const message = (text, state) => {
                if (!feedback) return;
                feedback.hidden = false;
                feedback.textContent = text;
                window.AppComponents?.setNoticeState(feedback, state);
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
                    message('Сейчас нет сети. Поля остаются в форме; повторите после подключения.', 'error');
                }
            });
            // All validation handlers on the form must accept the native POST first.
            window.addEventListener('submit', event => {
                if (event.target !== form || event.defaultPrevented) return;
                busy = true;
                form.setAttribute('aria-busy', 'true');
                submit.setAttribute('aria-busy', 'true');
                message('Ожидаем ответа сервера…', 'pending');
                setTimeout(() => {
                    if (busy) submit.disabled = true;
                }, 0);
            });
            window.addEventListener('pageshow', () => {
                const wasBusy = busy;
                busy = false;
                form.removeAttribute('aria-busy');
                submit.disabled = false;
                submit.removeAttribute('aria-busy');
                if (wasBusy) message('Проверьте результат перед повторной отправкой.', 'pending');
                else if (feedback) {
                    feedback.hidden = true;
                    feedback.textContent = '';
                    window.AppComponents?.setNoticeState(feedback, 'info');
                }
            });
            window.addEventListener('online', () => {
                if (feedback?.dataset.appState === 'error') message('Подключение восстановлено. Повторите отправку.', 'pending');
            });
        });

        // Encode the public page URL only: no password, session or return URL is shared.
        const appLink = document.querySelector('.auth-app');
        if (appLink && typeof qrcode === 'function') {
            const code = qrcode(0, 'M');
            code.addData(appLink.href);
            code.make();
            const size = code.getModuleCount();
            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.setAttribute('viewBox', '0 0 ' + (size + 8) + ' ' + (size + 8));
            svg.setAttribute('shape-rendering', 'crispEdges');
            const background = document.createElementNS(svg.namespaceURI, 'rect');
            background.setAttribute('width', '100%');
            background.setAttribute('height', '100%');
            background.setAttribute('fill', '#eef3fa');
            const modules = document.createElementNS(svg.namespaceURI, 'path');
            let path = '';
            for (let row = 0; row < size; row++) {
                for (let col = 0; col < size; col++) {
                    if (code.isDark(row, col)) path += 'M' + (col + 4) + ' ' + (row + 4) + 'h1v1h-1z';
                }
            }
            modules.setAttribute('d', path);
            modules.setAttribute('fill', '#0b111a');
            svg.append(background, modules);
            document.getElementById('auth-qr').replaceChildren(svg);
        }

        const canvas = document.getElementById('auth-particles');
        const context = canvas.getContext('2d');
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        const finePointer = window.matchMedia('(pointer: fine)');
        const desktopMotion = window.matchMedia('(min-width:1024px)');
        const space = document.querySelector('.auth-space');
        let width = 0, height = 0, frame = 0, previousTime = 0, elapsed = 0;
        let particles = [];

        function resize() {
            const bounds = canvas.getBoundingClientRect();
            width = bounds.width;
            height = bounds.height;
            const ratio = Math.min(window.devicePixelRatio || 1, 1.5);
            canvas.width = Math.round(width * ratio);
            canvas.height = Math.round(height * ratio);
            context?.setTransform(ratio, 0, 0, ratio, 0, 0);
            particles = Array.from({length: Math.min(140, Math.max(45, Math.round(width / 10)))}, (_, index) => ({
                x: (Math.sin(index * 127.1 + 3) * 43758.5453 % 1 + 1) % 1 * width,
                y: (Math.sin(index * 311.7 + 7) * 15731.743 % 1 + 1) % 1 * height,
                radius: .8 + index % 5 * .35, phase: index * 1.7,
                speed: 10 + index % 8 * 3, drift: 3 + index % 4 * 2
            }));
        }

        function draw(time) {
            if (document.hidden || reducedMotion.matches || !desktopMotion.matches || !context) {
                frame = 0;
                return;
            }
            if (time - previousTime < 33) {
                frame = requestAnimationFrame(draw);
                return;
            }
            const delta = previousTime ? Math.min((time - previousTime) / 1000, .1) : .033;
            previousTime = time;
            elapsed += delta;
            context.clearRect(0, 0, width, height);
            for (const particle of particles) {
                particle.y -= delta * particle.speed;
                particle.x += delta * particle.drift;
                if (particle.y < -10) particle.y = height + 10;
                if (particle.x > width + 30) particle.x = -30;
                const x = particle.x + Math.sin(elapsed * .3 + particle.phase) * 24;
                const central = x > width * .27 && x < width * .73;
                const opacity = (.26 + (Math.sin(elapsed * .7 + particle.phase) + 1) * .23) * (central ? .18 : 1);
                context.beginPath();
                context.fillStyle = 'rgba(70, 141, 255, ' + opacity + ')';
                context.shadowColor = '#378aff';
                context.shadowBlur = 10;
                context.arc(x, particle.y, particle.radius, 0, Math.PI * 2);
                context.fill();
            }
            frame = requestAnimationFrame(draw);
        }

        function updateMotion() {
            cancelAnimationFrame(frame);
            frame = 0;
            previousTime = 0;
            document.body.classList.toggle('is-background-paused', document.hidden);
            if (!document.hidden && !reducedMotion.matches && desktopMotion.matches && context) frame = requestAnimationFrame(draw);
            else {
                space.style.removeProperty('--space-x');
                space.style.removeProperty('--space-y');
            }
        }

        if (context) {
            resize();
            const observer = new ResizeObserver(resize);
            observer.observe(document.querySelector('.auth-universe'));
            document.addEventListener('visibilitychange', updateMotion);
            reducedMotion.addEventListener('change', updateMotion);
            desktopMotion.addEventListener('change', updateMotion);
            document.addEventListener('pointermove', event => {
                if (!finePointer.matches || reducedMotion.matches || !desktopMotion.matches) return;
                space.style.setProperty('--space-x', ((event.clientX / window.innerWidth - .5) * -14) + 'px');
                space.style.setProperty('--space-y', ((event.clientY / window.innerHeight - .5) * -10) + 'px');
            }, {passive: true});
            updateMotion();
        }
    })();
}
