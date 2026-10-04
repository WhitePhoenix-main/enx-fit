(() => {
  'use strict';
  document.querySelectorAll('[data-home-select]').forEach(select => select.addEventListener('change', () => select.form.requestSubmit()));
  if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
    const entrances = new IntersectionObserver(entries => entries.forEach(entry => {
      if (entry.isIntersecting) { entry.target.classList.add('home-interactive-entered'); entrances.unobserve(entry.target); }
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
      const link = detail.querySelector('a'); link.hidden = !point.dataset.workoutUrl;
      if (point.dataset.workoutUrl) link.href = point.dataset.workoutUrl;
    };
    points.forEach(point => {
      point.addEventListener('click', () => show(point)); point.addEventListener('focus', () => show(point));
      point.addEventListener('keydown', event => { if (['Enter', ' '].includes(event.key)) { event.preventDefault(); show(point); } });
    });
    const svg = card.querySelector('.epr-chart');
    const viewBox = svg.getAttribute('viewBox').split(/\s+/).map(Number);
    const geometry = [...svg.querySelectorAll('[x],[x1],[x2],[cx],[y],[y1],[y2],[cy],path[d]')].map(element => ({element,
      coordinates: ['x','x1','x2','cx','y','y1','y2','cy'].filter(name => element.hasAttribute(name)).map(name => [name, Number(element.getAttribute(name))]),
      path: element.getAttribute('d')
    }));
    const resize = () => {
      const width = svg.clientWidth, height = svg.clientHeight;
      if (!width || !height) return;
      const ratioX = width / viewBox[2], ratioY = height / viewBox[3];
      svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
      geometry.forEach(({element, coordinates, path}) => {
        coordinates.forEach(([name, value]) => element.setAttribute(name, value * (['x','x1','x2','cx'].includes(name) ? ratioX : ratioY)));
        if (path) element.setAttribute('d', path.replace(/([ML])([\d.]+),([\d.]+)/g, (_, command, x, y) => `${command}${Number(x) * ratioX},${Number(y) * ratioY}`));
      });
    };
    new ResizeObserver(resize).observe(svg); resize();
    svg.addEventListener('click', event => {
      const closest = points.map(point => {
        const box = point.getBoundingClientRect(); return {point, distance: Math.hypot(event.clientX - box.left - box.width / 2, event.clientY - box.top - box.height / 2)};
      }).sort((a,b) => a.distance - b.distance)[0];
      if (closest?.distance <= 24) show(closest.point);
    });
  });
  document.querySelectorAll('.home-workout-choices form').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('button'); button.disabled = true; button.setAttribute('aria-busy', 'true');
  }));
  window.addEventListener('pageshow', () => document.querySelectorAll('.home-workout-choices [aria-busy]').forEach(button => { button.disabled = false; button.removeAttribute('aria-busy'); }));
})();
