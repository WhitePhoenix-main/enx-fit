(() => {
    'use strict';
    const menu = document.querySelector('[data-home-menu]');
    const menuToggle = menu?.querySelector('summary');
    const closeMenu = (restoreFocus = false) => {
        if (!menu?.open) return;
        menu.open = false;
        if (restoreFocus) menuToggle.focus();
    };
    menu?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => closeMenu()));
    document.addEventListener('click', event => {
        if (!menu?.contains(event.target)) closeMenu();
    });
    document.addEventListener('focusin', event => {
        if (!menu?.contains(event.target)) closeMenu();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && menu?.open) {
            event.preventDefault();
            closeMenu(true);
        }
    });
    const desktop = matchMedia('(min-width: 851px)');
    desktop.addEventListener('change', event => { if (event.matches) closeMenu(); });

    const carousel = document.querySelector('[data-workflow-carousel]');
    if (!carousel) return;
    const tablist = carousel.querySelector('[data-workflow-tabs]');
    const tabs = [...tablist.querySelectorAll('[role="tab"]')];
    const panels = tabs.map(tab => document.getElementById(tab.getAttribute('aria-controls')));
    const frame = carousel.querySelector('[data-workflow-frame]');
    const rings = tabs.map(tab => tab.querySelector('[data-workflow-ring]'));
    const accents = panels.map(panel => panel.querySelector('[data-workflow-accent]'));
    const status = carousel.querySelector('[data-workflow-status]');
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    const horizontalTabs = matchMedia('(min-width: 601px) and (max-width: 850px)');
    const duration = () => Number(panels[current].dataset.duration) || Number(carousel.dataset.interval) || 6000;
    let current = 0;
    let autoplay = !reducedMotion.matches;
    let visible = false;
    let focused = false;
    let ready = false;
    let preload;
    let timer;
    let animation;
    let elapsed = 0;
    let startedAt = 0;
    let request = 0;
    let gesture;
    let emphasized = -1;
    let emphasis;

    const emphasize = () => {
        if (!visible || !ready || reducedMotion.matches || emphasized === current) return;
        emphasized = current;
        emphasis?.cancel();
        emphasis = accents[current]?.animate([
            { opacity: 0, transform: 'scale(1.04)', offset: 0 },
            { opacity: .9, transform: 'scale(1)', offset: .25 },
            { opacity: .65, transform: 'scale(1)', offset: .65 },
            { opacity: 0, transform: 'scale(1.02)', offset: 1 }
        ], { duration: 1800, delay: 450, easing: 'ease-out' });
    };

    const pause = () => {
        if (timer !== undefined) elapsed = Math.min(duration(), elapsed + performance.now() - startedAt);
        clearTimeout(timer);
        timer = undefined;
        animation?.pause();
        if (animation) animation.currentTime = elapsed;
        carousel.dataset.playing = 'false';
    };
    const reset = () => {
        pause();
        animation?.cancel();
        animation = undefined;
        elapsed = 0;
        carousel.dataset.timing = 'false';
    };
    const schedule = () => {
        emphasize();
        if (!autoplay) { reset(); return; }
        if (!visible || focused || document.hidden || !ready) { pause(); return; }
        if (timer !== undefined) return;
        carousel.dataset.playing = 'true';
        carousel.dataset.timing = 'true';
        if (!animation && !reducedMotion.matches) animation = rings[current].animate(
            [{ strokeDashoffset: 100 }, { strokeDashoffset: 0 }],
            { duration: duration(), fill: 'forwards' }
        );
        if (animation) { animation.currentTime = elapsed; animation.play(); }
        startedAt = performance.now();
        timer = setTimeout(() => {
            if (current < tabs.length - 1) activate(current + 1);
            else { autoplay = false; schedule(); }
        }, Math.max(0, duration() - elapsed));
    };
    const activate = async (next, manual = false) => {
        pause();
        const selectedRequest = ++request;
        if (manual) { autoplay = false; reset(); }
        const image = panels[next].querySelector('img');
        image.loading = 'eager';
        try { await image.decode(); } catch {
            if (selectedRequest === request) { autoplay = false; reset(); }
            return;
        }
        if (selectedRequest !== request) return;
        if (!manual && (!autoplay || !visible || focused || document.hidden)) return;
        reset();
        current = next;
        tabs.forEach((tab, index) => {
            const selected = index === current;
            tab.setAttribute('aria-selected', String(selected));
            tab.tabIndex = selected ? 0 : -1;
            panels[index].dataset.active = String(selected);
            panels[index].setAttribute('aria-hidden', String(!selected));
            panels[index].inert = !selected;
            panels[index].tabIndex = selected ? 0 : -1;
        });
        if (manual) status.textContent = 'Шаг ' + (current + 1) + ' из ' + tabs.length + ': ' + tabs[current].querySelector('strong').textContent;
        schedule();
    };
    const updateOrientation = () => tablist.setAttribute('aria-orientation', horizontalTabs.matches ? 'horizontal' : 'vertical');
    updateOrientation();
    horizontalTabs.addEventListener('change', updateOrientation);
    panels.forEach((panel, index) => {
        panel.hidden = false;
        panel.inert = index !== current;
    });
    tabs.forEach((tab, index) => {
        tab.disabled = false;
        tab.addEventListener('click', () => activate(index, true));
        tab.addEventListener('keydown', event => {
            let next;
            if (event.key === 'ArrowDown' || event.key === 'ArrowRight') next = (index + 1) % tabs.length;
            if (event.key === 'ArrowUp' || event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
            if (event.key === 'Home') next = 0;
            if (event.key === 'End') next = tabs.length - 1;
            if (next === undefined) return;
            event.preventDefault();
            tabs[next].focus();
            activate(next, true);
        });
    });
    frame.addEventListener('pointerdown', event => {
        if (event.pointerType !== 'touch' || !event.isPrimary) return;
        gesture = { x: event.clientX, y: event.clientY, time: performance.now() };
        frame.setPointerCapture(event.pointerId);
    });
    frame.addEventListener('pointerup', event => {
        if (!gesture) return;
        const dx = event.clientX - gesture.x;
        const dy = event.clientY - gesture.y;
        const quick = performance.now() - gesture.time < 800;
        gesture = null;
        if (quick && Math.abs(dx) >= 45 && Math.abs(dx) > Math.abs(dy) * 1.4)
            activate(Math.max(0, Math.min(tabs.length - 1, current + (dx < 0 ? 1 : -1))), true);
    });
    frame.addEventListener('pointercancel', () => { gesture = null; });
    carousel.addEventListener('focusin', event => { focused = event.target.matches(':focus-visible'); schedule(); });
    carousel.addEventListener('focusout', () => queueMicrotask(() => {
        focused = carousel.contains(document.activeElement) && document.activeElement.matches(':focus-visible');
        schedule();
    }));
    document.addEventListener('visibilitychange', schedule);
    reducedMotion.addEventListener('change', event => {
        if (event.matches) { autoplay = false; emphasis?.cancel(); }
        schedule();
    });
    new IntersectionObserver(entries => {
        visible = entries[0].isIntersecting && entries[0].intersectionRatio >= .5;
        if (visible && !preload) {
            preload = Promise.allSettled(panels.map(panel => {
                const image = panel.querySelector('img');
                image.loading = 'eager';
                return image.decode();
            })).then(() => { ready = true; schedule(); });
        }
        schedule();
    }, { threshold: [0, .5] }).observe(frame);
})();
