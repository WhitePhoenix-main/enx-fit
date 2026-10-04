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
    { opacity: 0, translate: '0 16px' }, { opacity: 1, translate: '0 0' }
  ], { delay });
  const revealImage = async (element, delay = 0) => {
    const images = Array.from(element.querySelectorAll('img'));
    await Promise.all(images.map(img => img.decode().catch(() => {})));
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
        { transform: 'scaleX(0)' }, { transform: 'scaleX(1)' }
      ], { delay: index * 130 + 100, duration: 500 });
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
  }, { threshold: .12 });
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
    card.addEventListener('pointerleave', () => { cancelAnimationFrame(frame); frame = 0; });
  });
  window.addEventListener('scroll', () => { heroBounds = undefined; }, { passive: true });
  const navigationLinks = Array.from(document.querySelectorAll(".landing-nav a")).filter(link => link.pathname === location.pathname && link.hash);
  const sections = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      navigationLinks.forEach(link => {
        if (link.hash === '#' + entry.target.id) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
      });
    });
  }, { rootMargin: '-15% 0px -60% 0px' });
  document.querySelectorAll('main > section[id]').forEach(section => sections.observe(section));
  const heroVisibility = new IntersectionObserver(entries => {
    hero.classList.toggle('is-motion-visible', entries[0].isIntersecting);
  }, { threshold: .05 });
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
