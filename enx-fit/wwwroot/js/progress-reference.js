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
    if (event.key === 'Escape') document.querySelectorAll('.epr-period[open]').forEach(menu => { menu.open = false; });
  });
  document.querySelectorAll('[data-progress-chart]').forEach(chart => {
    const svg = chart.querySelector('svg');
    const originalViewBox = svg.getAttribute('viewBox').split(/\s+/).map(Number);
    const geometry = [...svg.querySelectorAll('[x],[x1],[x2],[cx],[y],[y1],[y2],[cy],path[d]')].map(element => ({ element,
      coordinates: ['x', 'x1', 'x2', 'cx', 'y', 'y1', 'y2', 'cy'].filter(name => element.hasAttribute(name)).map(name => [name, Number(element.getAttribute(name))]),
      path: element.getAttribute('d')
    }));
    const resize = () => {
      const width = window.innerWidth < 768 ? svg.clientWidth : originalViewBox[2];
      const height = window.innerWidth < 768 ? svg.clientHeight : originalViewBox[3];
      if (width <= 0 || height <= 0) return;
      const ratioX = width / originalViewBox[2];
      const ratioY = height / originalViewBox[3];
      svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
      geometry.forEach(({ element, coordinates, path }) => {
        coordinates.forEach(([name, value]) => element.setAttribute(name, value * (['x', 'x1', 'x2', 'cx'].includes(name) ? ratioX : ratioY)));
        if (path) element.setAttribute('d', path.replace(/([ML])([\d.]+),([\d.]+)/g, (_, command, x, y) => `${command}${Number(x) * ratioX},${Number(y) * ratioY}`));
      });
    };
    new ResizeObserver(resize).observe(svg);
    resize();
    const tooltip = chart.querySelector('output');
    const hide = () => { tooltip.hidden = true; };
    chart.querySelectorAll('[data-chart-point]').forEach(point => {
      const show = () => {
        tooltip.textContent = `${point.dataset.date} · ${point.dataset.value}`;
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
  });
})();
