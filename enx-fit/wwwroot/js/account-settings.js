(() => {
 'use strict';
 const root=document.querySelector('[data-account-settings]');if(!root)return;
 const notice=(element,state)=>window.AppComponents?.setNoticeState(element,state);
 root.classList.add('is-enhanced');
 const sections=root.querySelector('[data-settings-sections]'),desktop=matchMedia('(min-width:1024px)');
 const arrange=()=>{sections.open=desktop.matches;};arrange();desktop.addEventListener('change',arrange);
 const copy=root.querySelector('[data-copy-authenticator]');
 if(copy&&navigator.clipboard){copy.hidden=false;copy.addEventListener('click',async()=>{const feedback=root.querySelector('[data-copy-feedback]');try{await navigator.clipboard.writeText(root.querySelector('#authenticator-key').textContent.trim());feedback.textContent='Ключ скопирован.';notice(feedback,'info');}catch{feedback.textContent='Не удалось скопировать. Выделите ключ вручную.';notice(feedback,'error');}});}
 root.querySelectorAll('input[type="password"][data-password-reveal]').forEach(input=>{
  const wrapper=input.parentElement,button=document.createElement('button');button.type='button';button.className='account-password-toggle app-button secondary';button.textContent='Показать';button.setAttribute('aria-controls',input.id);button.setAttribute('aria-pressed','false');
  const label=root.querySelector(`label[for="${input.id}"]`).textContent;button.setAttribute('aria-label','Показать: '+label);
  button.addEventListener('click',()=>{const reveal=input.type==='password';input.type=reveal?'text':'password';button.textContent=reveal?'Скрыть':'Показать';button.setAttribute('aria-pressed',String(reveal));button.setAttribute('aria-label',(reveal?'Скрыть: ':'Показать: ')+label);});wrapper.append(button);
 });
 root.querySelectorAll('form[data-account-form]').forEach(form=>{
  const status=form.querySelector('[data-account-feedback]'),save=form.querySelector('[data-account-save]'),fields=[...form.querySelectorAll('input:not([type="hidden"]):not(:disabled)')];
  const original=fields.map(input=>input.value),saveText=save?.textContent;
  const dirty=()=>fields.some((input,index)=>input.value!==original[index]);
  const update=()=>{const changed=dirty();if(save)save.disabled=!changed;if(status){status.textContent=changed?'Есть несохранённые изменения.':'';notice(status,changed?'pending':'info');}const confirmation=root.querySelector('.account-status:not(.is-error)');if(confirmation)confirmation.hidden=changed;};
  fields.forEach(input=>input.addEventListener('input',update));update();
  form.addEventListener('submit',event=>{
   if(!navigator.onLine){event.preventDefault();status.textContent='Сейчас нет сети. Данные остаются в форме; повторите после подключения.';notice(status,'error');return;}
   if(event.defaultPrevented)return;
   if(!form.checkValidity()){event.preventDefault();form.reportValidity();return;}
   if(typeof window.jQuery?.fn?.valid==='function'&&!window.jQuery(form).valid()){event.preventDefault();return;}
   if(save){save.disabled=true;save.setAttribute('aria-busy','true');save.textContent='Сохраняем…';}status.textContent='Ожидаем подтверждения…';notice(status,'pending');
  });
  window.addEventListener('pageshow',()=>{if(save){save.removeAttribute('aria-busy');save.textContent=saveText;}update();});
  window.addEventListener('online',()=>{if(status&&dirty()){status.textContent='Подключение восстановлено. Можно повторить сохранение.';notice(status,'pending');}});
 });
})();
