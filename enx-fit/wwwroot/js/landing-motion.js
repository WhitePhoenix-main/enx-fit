(() => {
  'use strict';

  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  const desktopPointer = matchMedia('(min-width: 901px) and (hover: hover) and (pointer: fine)');
  const compact = matchMedia('(max-width: 600px)');
  const entrancePace = 1.35;
  const runningAnimations = new Set();
  const channels = new WeakMap();
  const tweens = new Map();
  const ease = 'cubic-bezier(.22,1,.36,1)';
  const root = document.documentElement;
  root.classList.add('motion-ready');

  // Replacement animations cancel their predecessor, including rapid repeated clicks.
  // Backwards fill hides delayed elements immediately; no final inline state is retained.
  const animate = (element, frames, { channel = 'entrance', ...options } = {}) => {
    if (!element) return;
    const active = channels.get(element) || new Map();
    active.get(channel)?.cancel();
    if (reducedMotion.matches || document.hidden || !element.animate) return;
    const timing = { duration: compact.matches ? 420 : 560, easing: ease, fill: 'backwards', ...options };
    if (channel === 'entrance') {
      timing.duration *= entrancePace;
      timing.delay = (timing.delay || 0) * entrancePace;
    }
    const animation = element.animate(frames, timing);
    active.set(channel, animation);
    channels.set(element, active);
    runningAnimations.add(animation);
    const forget = () => {
      runningAnimations.delete(animation);
      if (active.get(channel) === animation) active.delete(channel);
    };
    animation.addEventListener('finish', forget, { once: true });
    animation.addEventListener('cancel', forget, { once: true });
    return animation;
  };
  const tween = (element, draw, { duration = 800, delay = 0, finish = () => {} } = {}) => {
    tweens.get(element)?.stop();
    if (reducedMotion.matches || document.hidden) { draw(1); finish(); return; }
    duration *= entrancePace;
    const start = performance.now() + delay * entrancePace;
    const job = { frame: 0, stop: () => {
      cancelAnimationFrame(job.frame);
      draw(1);
      finish();
      if (tweens.get(element) === job) tweens.delete(element);
    } };
    tweens.set(element, job);
    draw(0);
    const tick = now => {
      const progress = Math.max(0, Math.min(1, (now - start) / duration));
      draw(1 - (1 - progress) ** 3);
      if (progress < 1) job.frame = requestAnimationFrame(tick);
      else job.stop();
    };
    job.frame = requestAnimationFrame(tick);
  };
  const countUp = (element, delay = 0) => {
    tweens.get(element)?.stop();
    const finalText = element.textContent;
    element.setAttribute('aria-label', finalText);
    if (reducedMotion.matches || document.hidden) return;
    const target = Number(element.dataset.count);
    const from = Number(element.dataset.countFrom || 0);
    const decimals = Number(element.dataset.decimals || 0);
    const formatter = new Intl.NumberFormat('ru-RU', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
    // The final text stays in flow so counting never changes widths or moves neighbours.
    const overlay = document.createElement('span');
    overlay.className = 'counter-animated';
    overlay.setAttribute('aria-hidden', 'true');
    overlay.style.color = getComputedStyle(element).color;
    element.classList.add('is-counting');
    element.append(overlay);
    tween(element, progress => {
      overlay.textContent = formatter.format(from + (target - from) * progress).replace(',', '.') + (element.dataset.suffix || '');
    }, { delay, duration: compact.matches ? 600 : 850, finish: () => {
      overlay.remove();
      element.classList.remove('is-counting');
    } });
  };
  const fillRing = (element, delay = 0) => {
    tween(element, progress => element.style.setProperty('--ring-angle', `${progress * 360}deg`), {
      duration: compact.matches ? 650 : 950, delay,
      finish: () => element.style.removeProperty('--ring-angle')
    });
  };
  const reveal = (element, delay = 0, distance = 10) => animate(element, [
    { opacity: 0, translate: `0 ${distance}px` }, { opacity: 1, translate: '0 0' }
  ], { delay });

  const hero = document.querySelector('.hero');
  const dashboard = hero.querySelector('.dashboard-device');
  const phone = hero.querySelector('.phone-device');
  const heroData = (delay = 0) => {
    dashboard.querySelectorAll('[data-count]').forEach((element, index) => countUp(element, delay + index * 45));
    dashboard.querySelectorAll('.bar-column > div').forEach((bar, index) => {
      animate(bar, [{ transform: 'scaleY(0)' }, { transform: 'scaleY(1)' }], { delay: delay + index * 55, duration: compact.matches ? 500 : 650 });
    });
    fillRing(dashboard.querySelector('.mini-donut'), delay + 60);
    animate(dashboard.querySelector('.bar-tooltip'), [{ opacity: 0, scale: '.96' }, { opacity: 1, scale: '1' }], { delay: delay + 450, duration: 240 });
  };
  const heroIntro = () => {
    hero.querySelectorAll('.hero-copy > *').forEach((element, index) => reveal(element, index * 85, 12));
    // Translate is independent of existing mobile scale transforms.
    animate(dashboard, [{ opacity: 0, translate: '0 18px' }, { opacity: 1, translate: '0 0' }], { delay: 140, duration: 650 });
    animate(phone, [{ opacity: 0, translate: '24px 0' }, { opacity: 1, translate: '0 0' }], { delay: 340, duration: 600 });
    heroData(220);
  };
  document.querySelectorAll('[data-replay-hero]').forEach(button => button.addEventListener('click', () => heroData()));

  const chart = document.getElementById('progress-chart');
  const chartElements = [document.getElementById('chart-line'), document.getElementById('chart-area'), ...chart.querySelectorAll('.chart-point-ring, .chart-point, .chart-guide'), document.querySelector('.chart-tooltip')];
  const finishChart = () => chartElements.forEach(element => channels.get(element)?.get('chart')?.cancel());
  const drawChart = (initial = false) => {
    const line = chartElements[0];
    const length = line.getTotalLength();
    const duration = initial ? (compact.matches ? 600 : 850) * entrancePace : 480;
    animate(line, [{ strokeDasharray: `${length} ${length}`, strokeDashoffset: length }, { strokeDasharray: `${length} ${length}`, strokeDashoffset: 0 }], { duration, channel: 'chart' });
    animate(chartElements[1], [{ opacity: 0 }, { opacity: 1 }], { duration, channel: 'chart' });
    chartElements.slice(2, -1).forEach(element => animate(element, [{ opacity: 0 }, { opacity: 1 }], { delay: duration * .65, duration: 220, channel: 'chart' }));
    animate(chartElements.at(-1), [{ opacity: 0, scale: '.95' }, { opacity: 1, scale: '1' }], { delay: duration * .75, duration: 240, channel: 'chart' });
  };
  chart.addEventListener('enix:chart-change', () => drawChart());
  ['pointerdown', 'pointermove', 'keydown', 'focus'].forEach(event => chart.addEventListener(event, finishChart));

  // FLIP uses translation/scale for the active tab; resize immediately restores geometry.
  const tabs = document.querySelector('.chart-tabs');
  const indicator = document.createElement('span');
  indicator.className = 'chart-tab-indicator';
  indicator.setAttribute('aria-hidden', 'true');
  tabs.prepend(indicator);
  tabs.classList.add('has-indicator');
  let tabGeometry;
  const moveTabIndicator = (animated = true) => {
    const button = tabs.querySelector('button.active');
    const next = { x: button.offsetLeft, width: button.offsetWidth, height: button.offsetHeight };
    indicator.style.width = `${next.width}px`;
    indicator.style.height = `${next.height}px`;
    indicator.style.transform = `translateX(${next.x}px)`;
    if (animated && tabGeometry) animate(indicator, [
      { transform: `translateX(${tabGeometry.x}px) scaleX(${tabGeometry.width / next.width})` },
      { transform: `translateX(${next.x}px) scaleX(1)` }
    ], { duration: 240, channel: 'selection' });
    tabGeometry = next;
  };
  tabs.querySelectorAll('button').forEach(button => button.addEventListener('click', () => moveTabIndicator()));
  new ResizeObserver(() => moveTabIndicator(false)).observe(tabs);
  moveTabIndicator(false);

  const animatePlan = () => {
    document.querySelectorAll('.day-done').forEach((check, index) => {
      animate(check, [{ opacity: 0, scale: '.6' }, { opacity: 1, scale: '1' }], { delay: index * 85, duration: 320 });
    });
  };
  document.querySelectorAll('[data-day]').forEach(day => day.addEventListener('click', () => {
    animate(day.querySelector('.day-done, .day-pending'), [{ scale: '.85' }, { scale: '1' }], { duration: 220, channel: 'selection' });
    animate(document.getElementById('plan-selection'), [{ opacity: .35 }, { opacity: 1 }], { duration: 200, channel: 'selection' });
  }));
  const steps = document.querySelector('.steps');
  steps.querySelectorAll('li > div').forEach(content => {
    const line = document.createElement('span');
    line.className = 'step-progress';
    line.setAttribute('aria-hidden', 'true');
    content.prepend(line);
  });
  const animateSteps = () => {
    steps.querySelectorAll('li').forEach((step, index) => {
      const delay = index * (compact.matches ? 220 : 300);
      animate(step.querySelector('.step-number'), [{ opacity: .35, scale: '.92' }, { opacity: 1, scale: '1' }], { delay, duration: 240 });
      animate(step.querySelector('.step-progress'), [{ transform: 'scaleX(0)' }, { transform: 'scaleX(1)' }], { delay: delay + 120, duration: 320 });
    });
  };

  // One observer, one entry per section. It disconnects once everything has appeared.
  const entrances = new Map();
  entrances.set(hero, heroIntro);
  document.querySelectorAll('.social-proof [data-count]').forEach(element => entrances.set(element, () => countUp(element)));
  document.querySelectorAll('.section-copy').forEach(element => entrances.set(element, () => reveal(element)));
  document.querySelectorAll('.feature-card, .integration-card, .price-card').forEach(element => {
    const index = [...element.parentElement.children].indexOf(element);
    entrances.set(element, () => reveal(element, index * 65, element.matches('.integration-card') ? 6 : 10));
  });
  entrances.set(document.querySelector('.line-chart-panel'), () => drawChart(true));
  entrances.set(document.querySelector('.muscles-panel'), () => {
    fillRing(document.querySelector('.muscles-donut'));
    countUp(document.querySelector('.muscles-donut [data-count]'));
  });
  entrances.set(document.querySelector('.weekly-plan'), animatePlan);
  entrances.set(steps, animateSteps);
  const entering = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      const run = entrances.get(entry.target);
      entrances.delete(entry.target);
      entering.unobserve(entry.target);
      run?.();
    });
    if (!entrances.size) entering.disconnect();
  }, { threshold: .18 });
  entrances.forEach((run, element) => entering.observe(element));

  // Pointer movement schedules one update per frame and stops entirely when idle.
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

  document.querySelectorAll('.feature-card, .price-card, .integration-card').forEach(card => {
    let frame = 0;
    card.addEventListener('pointermove', event => {
      if (!desktopPointer.matches || reducedMotion.matches) return;
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        const bounds = card.getBoundingClientRect();
        card.style.setProperty('--pointer-x', `${event.clientX - bounds.left}px`);
        card.style.setProperty('--pointer-y', `${event.clientY - bounds.top}px`);
      });
    });
    card.addEventListener('pointerleave', () => cancelAnimationFrame(frame));
  });

  const header = document.querySelector('.landing-header');
  let scrollFrame = 0;
  const updateHeader = () => {
    header.classList.toggle('is-scrolled', scrollY > 40);
    heroBounds = undefined;
    scrollFrame = 0;
  };
  window.addEventListener('scroll', () => {
    if (!scrollFrame) scrollFrame = requestAnimationFrame(updateHeader);
  }, { passive: true });
  updateHeader();
  const navigationLinks = [...document.querySelectorAll('.landing-nav a[href^="#"]')];
  const sections = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      navigationLinks.forEach(link => {
        if (link.hash === `#${entry.target.id}`) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
      });
    });
  }, { rootMargin: '-15% 0px -60% 0px' });
  document.querySelectorAll('main > section').forEach(section => sections.observe(section));

  document.getElementById('billing-switch').addEventListener('click', () => {
    document.querySelectorAll('[data-monthly]').forEach(price => animate(price, [
      { opacity: .2, translate: '0 4px' }, { opacity: 1, translate: '0 0' }
    ], { duration: 220, channel: 'selection' }));
  });
  document.getElementById('add-demo-set').addEventListener('click', () => {
    animate(document.querySelector('#demo-sets > div:last-child'), [{ opacity: 0, translate: '0 -6px' }, { opacity: 1, translate: '0 0' }], { duration: 300 });
  });

  // Timer and status light only run while the phone is visible in an active tab.
  const timer = phone.querySelector('.phone-timer strong');
  const pause = document.getElementById('preview-pause');
  let seconds = 45 * 60 + 32;
  let paused = reducedMotion.matches;
  let visible = false;
  let interval;
  const updateTimer = () => {
    clearInterval(interval);
    interval = undefined;
    pause.setAttribute('aria-pressed', String(paused));
    pause.setAttribute('aria-label', paused ? 'Продолжить таймер' : 'Приостановить таймер');
    pause.textContent = paused ? '▶' : 'Ⅱ';
    phone.classList.toggle('is-paused', paused);
    phone.querySelector('.phone-timer > span').textContent = paused ? 'Тренировка на паузе' : 'Время тренировки';
    if (paused || !visible || document.hidden) return;
    interval = setInterval(() => {
      seconds++;
      timer.textContent = [Math.floor(seconds / 3600), Math.floor(seconds % 3600 / 60), seconds % 60].map(value => String(value).padStart(2, '0')).join(':');
      // A brief status blink, never a looping animation of the device itself.
      animate(phone.querySelector('.phone-timer > span'), [{ opacity: .65 }, { opacity: 1 }], { duration: 320, channel: 'timer' });
    }, 1000);
  };
  pause.addEventListener('click', () => { paused = !paused; updateTimer(); });
  const phoneVisibility = new IntersectionObserver(entries => { visible = entries[0].isIntersecting; updateTimer(); }, { threshold: .2 });
  phoneVisibility.observe(phone);
  const finishMotion = () => {
    runningAnimations.forEach(animation => animation.cancel());
    [...tweens.values()].forEach(job => job.stop());
    resetParallax();
  };
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) finishMotion();
    updateTimer();
  });
  window.addEventListener('pagehide', () => { clearInterval(interval); finishMotion(); });
  window.addEventListener('pageshow', updateTimer);
  reducedMotion.addEventListener('change', () => {
    if (!reducedMotion.matches) return;
    finishMotion();
    paused = true;
    updateTimer();
  });
})();
