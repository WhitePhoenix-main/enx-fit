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
