(() => {
  'use strict';
  const form = document.querySelector('[data-auth-form]');
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
    const confirmation = input.name.endsWith('ConfirmPassword');
    button.addEventListener('click', () => {
      const reveal = input.type === 'password';
      input.type = reveal ? 'text' : 'password';
      button.setAttribute('aria-pressed', String(reveal));
      button.setAttribute('aria-label', (reveal ? 'Скрыть ' : 'Показать ') + (confirmation ? 'повторный пароль' : 'пароль'));
      button.querySelector('use').setAttribute('href', reveal ? '#auth-eye' : '#auth-eye-off');
    });
  });
  const confirmation = form.querySelector('[name="Input.ConfirmPassword"]');
  if (confirmation) {
    const password = form.querySelector('[name="Input.Password"]');
    const validate = () => confirmation.setCustomValidity(confirmation.value && confirmation.value !== password.value ? 'Пароли не совпадают.' : '');
    confirmation.addEventListener('input', validate);
    password.addEventListener('input', validate);
  }
  form.addEventListener('submit', () => {
    const submit = form.querySelector('[type="submit"]');
    submit.disabled = true;
    submit.setAttribute('aria-busy', 'true');
    submit.querySelector('span').textContent = 'Подождите…';
  });
  window.addEventListener('pageshow', event => {
    // A back/forward cache restoration must not leave the form disabled.
    if (event.persisted) window.location.reload();
  });

  // Encode the public page URL only: no password, session or return URL is shared.
  const appLink = document.querySelector('.auth-app');
  if (typeof qrcode === 'function') {
    const code = qrcode(0, 'M');
    code.addData(appLink.href);
    code.make();
    const size = code.getModuleCount();
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', '0 0 ' + (size + 8) + ' ' + (size + 8));
    svg.setAttribute('shape-rendering', 'crispEdges');
    const background = document.createElementNS(svg.namespaceURI, 'rect');
    background.setAttribute('width', '100%'); background.setAttribute('height', '100%');
    background.setAttribute('fill', '#eef3fa');
    const modules = document.createElementNS(svg.namespaceURI, 'path');
    let path = '';
    for (let row = 0; row < size; row++) {
      for (let col = 0; col < size; col++) {
        if (code.isDark(row, col)) path += 'M' + (col + 4) + ' ' + (row + 4) + 'h1v1h-1z';
      }
    }
    modules.setAttribute('d', path); modules.setAttribute('fill', '#0b111a');
    svg.append(background, modules);
    document.getElementById('auth-qr').replaceChildren(svg);
  }

  const canvas = document.getElementById('auth-particles');
  const context = canvas.getContext('2d');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  const finePointer = window.matchMedia('(pointer: fine)');
  const space = document.querySelector('.auth-space');
  let width = 0, height = 0, frame = 0, previousTime = 0, elapsed = 0;
  let particles = [];
  function resize() {
    const bounds = canvas.getBoundingClientRect();
    width = bounds.width; height = bounds.height;
    const ratio = Math.min(window.devicePixelRatio || 1, 1.5);
    canvas.width = Math.round(width * ratio);
    canvas.height = Math.round(height * ratio);
    context?.setTransform(ratio, 0, 0, ratio, 0, 0);
    particles = Array.from({ length: Math.min(140, Math.max(45, Math.round(width / 10))) }, (_, index) => ({
      x: (Math.sin(index * 127.1 + 3) * 43758.5453 % 1 + 1) % 1 * width,
      y: (Math.sin(index * 311.7 + 7) * 15731.743 % 1 + 1) % 1 * height,
      radius: .8 + index % 5 * .35, phase: index * 1.7,
      speed: 10 + index % 8 * 3, drift: 3 + index % 4 * 2
    }));
  }
  function draw(time) {
    if (document.hidden || reducedMotion.matches || !context) { frame = 0; return; }
    if (time - previousTime < 33) { frame = requestAnimationFrame(draw); return; }
    const delta = previousTime ? Math.min((time - previousTime) / 1000, .1) : .033;
    previousTime = time; elapsed += delta;
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
      context.shadowColor = '#378aff'; context.shadowBlur = 10;
      context.arc(x, particle.y, particle.radius, 0, Math.PI * 2);
      context.fill();
    }
    frame = requestAnimationFrame(draw);
  }
  function updateMotion() {
    cancelAnimationFrame(frame); frame = 0; previousTime = 0;
    document.body.classList.toggle('is-background-paused', document.hidden);
    if (!document.hidden && !reducedMotion.matches && context) frame = requestAnimationFrame(draw);
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
    document.addEventListener('pointermove', event => {
      if (!finePointer.matches || reducedMotion.matches) return;
      space.style.setProperty('--space-x', ((event.clientX / window.innerWidth - .5) * -14) + 'px');
      space.style.setProperty('--space-y', ((event.clientY / window.innerHeight - .5) * -10) + 'px');
    }, { passive: true });
    updateMotion();
  }
})();
