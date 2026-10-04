(() => {
 'use strict';
 const root=document.querySelector('[data-training-setup]'); if(!root)return;
 const key=`enix-training-setup:${root.dataset.viewer}`;
 if(root.dataset.saved==='true'){try{sessionStorage.removeItem(key);}catch{}return;}
 const form=root.querySelector('[data-setup-form]');if(!form)return;
 const steps=[...form.querySelectorAll('[data-setup-step]')],tabs=[...form.querySelectorAll('[data-setup-goto]')];
 const next=form.querySelector('[data-setup-next]'),back=form.querySelector('[data-setup-back]'),save=form.querySelector('[data-setup-save]'),status=form.querySelector('[data-setup-status]');
 const later=form.querySelector('[name="Input.DaysLater"][type="checkbox"]'),dayInputs=[...form.querySelectorAll('[name="Input.Days"]')];
 const revision=form.querySelector('[name="Input.Revision"]').value;let step=0,stored=false;
 const selected=name=>[...form.querySelectorAll(`[name="${name}"]:checked`)].map(input=>input.value);
 const title=input=>{const label=input?.closest('label');return (label?.querySelector('strong')||label?.querySelector('span'))?.textContent.trim();};
 const data=()=>({version:1,revision,step,goal:selected('Input.Goal')[0],location:selected('Input.Location')[0],equipment:selected('Input.Equipment'),days:selected('Input.Days'),later:later.checked});
 const persist=()=>{try{sessionStorage.setItem(key,JSON.stringify(data()));stored=true;}catch{stored=false;}};
 const renderSummary=()=>{
  const rows=[['Цель',title(form.querySelector('[name="Input.Goal"]:checked'))||'Выберите цель'],['Место',title(form.querySelector('[name="Input.Location"]:checked'))||'Выберите место'],['Оборудование',[...form.querySelectorAll('[name="Input.Equipment"]:checked')].map(title).join(', ')||'Без дополнительного оборудования'],['Удобные дни',later.checked?'Дни выберу позже':dayInputs.filter(input=>input.checked).map(input=>input.closest('label').querySelector('small').textContent).join(', ')||'Выберите дни']];
  form.querySelectorAll('[data-setup-preview],[data-setup-review]').forEach(dl=>{dl.replaceChildren(...rows.map(([label,value])=>{const row=document.createElement('div'),dt=document.createElement('dt'),dd=document.createElement('dd');dt.textContent=label;dd.textContent=value;row.append(dt,dd);return row;}));});
  dayInputs.forEach(input=>{input.disabled=later.checked;});
 };
 const show=(index,focus=false)=>{
  step=Math.max(0,Math.min(3,index));
  steps.forEach((section,i)=>{section.hidden=i!==step;section.classList.toggle('is-entering',focus&&i===step);});
  tabs.forEach((tab,i)=>{if(i===Math.min(step,2))tab.setAttribute('aria-current','step');else tab.removeAttribute('aria-current');});
  back.hidden=step===0;next.hidden=step===3;save.hidden=step!==3;
  next.childNodes[0].textContent=step===2?'Проверить выбор ':'Дальше ';
  renderSummary();persist();
  if(focus){steps[step].querySelector('h2').focus({preventScroll:true});steps[step].scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion:reduce)').matches?'instant':'smooth'});}
 };
 const validate=index=>{
  const invalid=[...steps[index].querySelectorAll('input')].find(input=>!input.disabled&&!input.checkValidity());
  if(invalid){show(index);invalid.reportValidity();return false;}
  if(index===2){const dayError=form.querySelector('[data-setup-days-error]');dayError.hidden=later.checked||dayInputs.some(input=>input.checked);if(!dayError.hidden){show(2);dayInputs[0].focus();return false;}}
  return true;
 };
 if(root.dataset.invalid!=='true'){
  try{
   const draft=JSON.parse(sessionStorage.getItem(key));
   if(draft?.version===1&&draft.revision===revision){
    for(const [name,values] of [['Input.Goal',[draft.goal]],['Input.Location',[draft.location]],['Input.Equipment',draft.equipment],['Input.Days',draft.days]]){
     if(!Array.isArray(values))continue;form.querySelectorAll(`[name="${name}"]`).forEach(input=>input.checked=values.includes(input.value));
    }
    later.checked=draft.later===true;step=Number.isInteger(draft.step)?Math.max(0,Math.min(3,draft.step)):0;
    status.textContent='Ваш выбор восстановлен в этой вкладке.';
   }
  }catch{}
 }
 form.classList.add('is-enhanced');
 if(root.dataset.invalid==='true'){
  const invalid=steps.findIndex(section=>section.querySelector('.input-validation-error,.field-validation-error'));
  step=invalid<0?0:invalid;
 }
 if(step>0&&!form.querySelector('[name="Input.Goal"]:checked'))step=0;
 if(step>1&&!form.querySelector('[name="Input.Location"]:checked'))step=1;
 if(step>2&&!later.checked&&!dayInputs.some(input=>input.checked))step=2;
 show(step);
 form.addEventListener('change',event=>{renderSummary();persist();if(['Input.Days','Input.DaysLater'].includes(event.target.name))form.querySelector('[data-setup-days-error]').hidden=later.checked||dayInputs.some(input=>input.checked);});
 next.addEventListener('click',()=>{if(validate(step))show(step+1,true);});back.addEventListener('click',()=>show(step-1,true));
 tabs.forEach((tab,index)=>tab.addEventListener('click',()=>{if(index>step){for(let i=step;i<index;i++)if(!validate(i))return;}show(index,true);}));
 form.addEventListener('submit',event=>{
  const skip=event.submitter?.matches('[data-setup-skip]');
  if(!skip){for(let i=0;i<3;i++)if(!validate(i)){event.preventDefault();return;}if(step!==3){event.preventDefault();show(3,true);return;}}
  if(!navigator.onLine){event.preventDefault();persist();status.textContent=stored?'Сейчас нет сети. Ваш выбор сохранён в этой вкладке; повторите после подключения.':'Сейчас нет сети. Оставьте страницу открытой и повторите после подключения.';return;}
  if(skip){try{sessionStorage.removeItem(key);}catch{}}else persist();
  status.textContent=skip?'Откладываем настройку…':'Сохраняем условия…';
 });
 window.addEventListener('online',()=>{status.textContent='Подключение восстановлено. Можно сохранить условия.';});
})();
