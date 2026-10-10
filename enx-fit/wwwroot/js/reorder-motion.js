(() => {
  'use strict';
  const active = new WeakMap();
  window.ReorderMotion = {
    capture(root) {
      return new Map([...root.querySelectorAll('[data-order-key]')].filter(e => e.getClientRects().length)
        .map(e => [e.dataset.orderKey, e.getBoundingClientRect()]));
    },
    play(root, before) {
      if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;
      root.querySelectorAll('[data-order-key]').forEach(element => {
        const old = before.get(element.dataset.orderKey), next = element.getBoundingClientRect();
        if (!old || !next.height || !element.animate) return;
        const x = old.left - next.left, y = old.top - next.top;
        if (Math.abs(x) + Math.abs(y) < 1) return;
        active.get(element)?.cancel();
        active.set(element, element.animate([{transform:`translate(${x}px, ${y}px)`}, {transform:'none'}], {duration:220,easing:'cubic-bezier(.2,.7,.2,1)'}));
      });
    }
  };
})();
