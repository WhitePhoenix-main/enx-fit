// Shared notice states.
(() => {
  'use strict';
  const noticeStates = new Set(['info','saved','local','pending','error']);
  // States are supplied by the owning workflow; never infer persistence from message text.
  window.AppComponents = Object.freeze({
    setNoticeState(element, state) {
      if (!element) return;
      element.dataset.appState = noticeStates.has(state) ? state : 'info';
    }
  });
})();

// Exercise identifiers and legacy draft compatibility.
(() => {
    'use strict';
    // Match ExerciseIds.FromLegacy and the database migration for old drafts and favorites.
    const normalize = value => {
        if (typeof value === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)) return value.toLowerCase();
        const legacy = typeof value === 'number' ? value : typeof value === 'string' && /^-?\d+$/.test(value) ? Number(value) : NaN;
        if (!Number.isInteger(legacy) || legacy < -2147483648 || legacy > 2147483647 || legacy === 0) return null;
        return 'e71c15e5-0000-4000-8000-0000' + (legacy >>> 0).toString(16).padStart(8, '0');
    };
    window.ExerciseIds = { normalize };
})();

// Mobile navigation dialogs.
(() => {
  'use strict';
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-mobile-dialog]');
    if (trigger) {
      const dialog = document.getElementById(trigger.dataset.mobileDialog);
      if (dialog instanceof HTMLDialogElement) {
        document.querySelectorAll('.em-mobile-dialog[open]').forEach(item => item.close());
        dialog.showModal();
      }
    }
    const close = event.target.closest('[data-mobile-close], .em-mobile-menu-links a');
    if (close) close.closest('dialog')?.close();
    if (event.target instanceof HTMLDialogElement && event.target.matches('.em-mobile-dialog')) {
      const box = event.target.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) event.target.close();
    }
  });
  window.matchMedia('(min-width:1024px)').addEventListener('change', event => {
    if (event.matches) document.querySelectorAll('.em-mobile-dialog[open]').forEach(dialog => dialog.close());
  });
})();

// Exercise reorder animation.
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

// Workspace form feedback and submission.
(() => {
 'use strict';
 document.addEventListener('keydown',event=>{
  if(event.key!=='Escape')return;
  const menu=document.querySelector('.admin-mobile-account[open]');
  if(menu){menu.open=false;menu.querySelector('summary')?.focus();}
 });
 document.querySelectorAll('[data-admin-form]').forEach(form=>{
  let busy=false;
  const buttons=[...form.querySelectorAll('button[type="submit"]')];
  const disabled=new Map(buttons.map(button=>[button,button.disabled]));
  let feedback=form.querySelector('[data-admin-feedback]');
  if(!feedback){feedback=document.createElement('p');feedback.className='admin-form-feedback app-status app-notice';feedback.dataset.adminFeedback='';feedback.setAttribute('role','status');feedback.setAttribute('aria-live','polite');feedback.hidden=true;form.append(feedback);}
  const message=(text,state)=>{feedback.hidden=false;feedback.textContent=text;window.AppComponents?.setNoticeState(feedback,state);};
  form.addEventListener('input',()=>{if(!busy)message('Есть несохранённые изменения.','pending');});
  form.addEventListener('invalid',event=>{const details=event.target.closest('details');if(details)details.open=true;},true);
  form.addEventListener('submit',event=>{
   if(busy){event.preventDefault();return;}
   if(!form.checkValidity()){event.preventDefault();form.reportValidity();return;}
   if(!navigator.onLine){event.preventDefault();message('Нет соединения. Заполненные поля остаются здесь. Подключитесь к сети и повторите отправку.','error');return;}
  });
  // Mark sending only after the form's validation handlers have accepted the POST.
  addEventListener('submit',event=>{
   if(event.target!==form||event.defaultPrevented)return;
   busy=true;message('Ожидаем подтверждения сервера…','pending');form.setAttribute('aria-busy','true');event.submitter?.setAttribute('aria-busy','true');
   // Defer disabling so the native submitter remains part of the POST.
   setTimeout(()=>{if(busy)buttons.forEach(button=>button.disabled=true);},0);
  });
  addEventListener('online',()=>{if(feedback.dataset.appState==='error')message('Подключение восстановлено. Проверьте поля и повторите отправку.','pending');});
  addEventListener('pageshow',()=>{const wasBusy=busy;busy=false;form.removeAttribute('aria-busy');buttons.forEach(button=>{button.disabled=disabled.get(button);button.removeAttribute('aria-busy');});if(wasBusy)message('Проверьте результат перед повторной отправкой.','pending');});
 });
})();
