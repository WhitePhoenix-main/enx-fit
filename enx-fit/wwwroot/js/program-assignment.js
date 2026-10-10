(() => {
 'use strict';
 const form=document.querySelector('[data-assignment-form]');if(!form)return;
 const save=form.querySelector('[data-assignment-save]'),feedback=form.querySelector('[data-assignment-feedback]');
 let busy=false;
 const notice=state=>window.AppComponents?.setNoticeState(feedback,state);
 const recover=()=>{busy=false;save.disabled=false;save.removeAttribute('aria-busy');};
 form.addEventListener('input',()=>{feedback.textContent='Есть несохранённые изменения назначения.';notice('pending');});
 form.addEventListener('submit',event=>{
  if(busy){event.preventDefault();return;}
  if(!form.checkValidity()){event.preventDefault();form.reportValidity();return;}
  if(!navigator.onLine){event.preventDefault();feedback.textContent='Сейчас нет сети. Получатель, дата и дни остались в форме; повторите после подключения.';notice('error');return;}
  busy=true;save.setAttribute('aria-busy','true');
  setTimeout(()=>{if(busy)save.disabled=true;},0);
  feedback.textContent='Ожидаем подтверждения назначения…';notice('pending');
 });
 addEventListener('online',()=>{if(feedback.dataset.appState==='error'){feedback.textContent='Подключение восстановлено. Проверьте назначение и повторите сохранение.';notice('pending');}});
 addEventListener('pageshow',()=>{const wasBusy=busy;recover();if(wasBusy){feedback.textContent='Проверьте назначение перед повторным сохранением.';notice('pending');}});
})();
