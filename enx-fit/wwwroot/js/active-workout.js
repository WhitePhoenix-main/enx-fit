(() => {
'use strict';
const root = document.querySelector('[data-active-workout]');
if (!root) return;
const initialState = document.getElementById('workout-execution-state');
if (!initialState) return; // An older running server can still render the pre-migration page.
const key = `enix-session-queue:${root.dataset.viewer}:${root.dataset.sessionId}`;
const status = root.querySelector('[data-sync-status]');
let state = JSON.parse(initialState.textContent);
let revision = state.revision, queue = [], sending = false, conflict = false, storageAvailable = true;
let offset = Date.parse(state.serverNowUtc) - Date.now(), sampledAt = Date.now(), soundedRest = null;
let sound = false, audioContext = null, wakeLock = null;
const active = () => ['InProgress','Paused'].includes(state.status);
const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
const clock = seconds => `${String(Math.floor(Math.max(0,seconds)/60)).padStart(2,'0')}:${String(Math.floor(Math.max(0,seconds)%60)).padStart(2,'0')}`;
const scratchKey = `${key}:fields`; let scratch = {}, rejectedOperationId = null; const debounce = new Map();
const pickerRoot = document.getElementById('workout-library'), picker = pickerRoot?.exerciseLibrary;
let picking = false, pickAttempt = null;
if (picker) pickerRoot.addEventListener('exercises-selected', async event => {
 if (picking) return; picking = true; picker.setBusy(true); picker.message('');
 try {
  for (const timeout of debounce.values()) clearTimeout(timeout); debounce.clear();
  if (state.status !== 'Paused') for (const id of Object.keys(scratch)) {
   const form = [...root.querySelectorAll('.set-form')].find(f => f.elements.namedItem('setId').value === id);
   if (form && !saveSet(form)) throw new Error('Заполните результаты подходов перед добавлением упражнений.');
  }
  await drain();
  for (let n = 0; sending && n < 200; n++) await new Promise(resolve => setTimeout(resolve, 25));
  if (queue.length || sending) throw new Error('Дождитесь сохранения результатов. Выбор упражнений остаётся здесь.');
  const entry = document.querySelector('[data-exercise-entry]').value;
  const signature = JSON.stringify([entry, event.detail.exercises]);
  if (pickAttempt?.signature !== signature) pickAttempt = {signature, id:crypto.randomUUID()};
  const body = new URLSearchParams({__RequestVerificationToken:token, selectionJson:JSON.stringify(event.detail.exercises), ExerciseEntryId:entry, expectedRevision:revision, operationId:pickAttempt.id});
  const response = await fetch(`${location.pathname}?handler=PickExercises`, {method:'POST',headers:{'X-Requested-With':'XMLHttpRequest'},body,redirect:'error'});
  if (!response.ok) { const data = await response.json().catch(() => ({})); throw new Error(data.error || 'Не удалось добавить упражнения. Попробуйте ещё раз.'); }
  const html = new DOMParser().parseFromString(await response.text(),'text/html');
  const nextList = html.querySelector('.session-exercises'), nextState = html.getElementById('workout-execution-state');
  if (!nextList || !nextState) throw new Error('Обновите страницу, чтобы увидеть сохранённые упражнения.');
  const currentList = root.querySelector('.session-exercises');
  if (entry === '0') {
   const existing = new Set([...currentList.children].map(card => card.id));
   for (const card of [...nextList.children]) if (!existing.has(card.id)) currentList.append(card);
  } else {
   const nextCard = [...nextList.children].find(card => card.id === `exercise-${entry}`);
   if (!nextCard) throw new Error('Обновите страницу, чтобы увидеть замену.');
   document.getElementById(`exercise-${entry}`).replaceWith(nextCard);
  }
  const latest = JSON.parse(nextState.textContent); revision = latest.revision; persist(); update(latest);
  picker.setSelected([...html.querySelectorAll('#workout-library .is-added')].map(row => Number(row.dataset.exerciseId)));
  picker.clear(); pickAttempt = null; document.getElementById('exercise-dialog').close();
  const empty = root.querySelector('.workout-empty'); if (empty) empty.hidden = true;
  root.querySelector('[data-dialog="load-template-dialog"]')?.setAttribute('hidden','');
  syncMessage(entry === '0' ? 'Упражнения добавлены · все изменения сохранены' : 'Упражнение заменено · все изменения сохранены');
  const ids = event.detail.exercises.map(p => p.exerciseId);
  const card = [...root.querySelectorAll('.session-exercise')].find(el => ids.includes(Number(el.dataset.catalogId)));
  root.dispatchEvent?.(new CustomEvent('workout-exercises-changed', {detail:{exerciseId:card?.id}}));
  card?.classList.add('exercise-just-added'); card?.scrollIntoView({behavior:window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches?'instant':'smooth',block:'start'});
  if (state.status !== 'Paused' && window.matchMedia?.('(min-width:1024px)')?.matches) card?.querySelector('input:not([type="hidden"])')?.focus({preventScroll:true});
 } catch (error) { picker.message(error.message); }
 finally { picking = false; picker.setBusy(false); }
});
function persist() { try { localStorage.setItem(scratchKey,JSON.stringify(scratch)); localStorage.setItem(key,JSON.stringify({revision,queue})); storageAvailable=true; } catch { storageAvailable=false; } }
try {
 scratch=JSON.parse(localStorage.getItem(scratchKey))||{};
 const saved=JSON.parse(localStorage.getItem(key)); if(Array.isArray(saved?.queue)&&saved.queue.length) { queue=saved.queue; revision=saved.revision; }
 if(root.dataset.recordedRequest) { const draftKey=`enix-record-draft:${root.dataset.viewer}`; const draft=JSON.parse(localStorage.getItem(draftKey)); if(draft?.fields?.ClientRequestId===root.dataset.recordedRequest) localStorage.removeItem(draftKey); }
} catch { /* Storage may be unavailable. */ }
function syncMessage(message) { status.textContent=message||(queue.length?`${queue.length} изменений на устройстве · ожидают отправки`:Object.keys(scratch).length?'Незаполненные поля сохранены на устройстве':storageAvailable?'Все изменения сохранены':'Сохранено на сервере. Локальное восстановление недоступно.'); status.classList.toggle('has-pending',queue.length>0); }
function update(data) { state=data; offset=Date.parse(data.serverNowUtc)-Date.now(); sampledAt=Date.now(); queue.forEach(previewOperation); tick(); maintainWakeLock(); }
function tick() {
 const elapsed=state.elapsedSeconds+(state.status==='InProgress'?(Date.now()-sampledAt)/1000:0);
 const sessionClock=root.querySelector('[data-session-clock]'); if(sessionClock) sessionClock.textContent=clock(elapsed);
 const paused=state.status==='Paused', notice=root.querySelector('[data-pause-notice]'), pauseButton=root.querySelector('[data-pause-button]');
 if(notice) notice.hidden=!paused;
 if(pauseButton) { pauseButton.dataset.control=paused?'resume':'pause'; pauseButton.textContent=paused?'Продолжить':'Пауза'; }
 root.querySelectorAll('.set-form input:not([type="hidden"]),.set-form button').forEach(input=>{ if(active()) input.disabled=paused; });
 const remaining=paused?state.restRemainingSeconds:state.restEndsAtUtc?Math.max(0,Math.ceil((Date.parse(state.restEndsAtUtc)-Date.now()-offset)/1000)):null;
 root.dispatchEvent?.(new CustomEvent('workout-state', {detail:{...state,remainingRestSeconds:remaining,elapsedSeconds:elapsed}}));
 const panel=root.querySelector('[data-rest-panel]');
 if(panel) { panel.hidden=remaining==null||!active(); root.querySelector('[data-rest-clock]').textContent=clock(remaining||0); root.querySelector('[data-rest-caption]').textContent=paused?'Отдых на паузе':remaining===0?'Отдых закончен · можно начинать следующий подход':'Восстановитесь перед следующим подходом';
 if(remaining===0&&!paused&&state.restEndsAtUtc&&soundedRest!==state.restEndsAtUtc) { soundedRest=state.restEndsAtUtc; if(sound&&audioContext) { const oscillator=audioContext.createOscillator(),gain=audioContext.createGain(); oscillator.frequency.value=660; gain.gain.value=0.12; oscillator.connect(gain); gain.connect(audioContext.destination); oscillator.start(); oscillator.stop(audioContext.currentTime+0.3); } } }
}
async function maintainWakeLock() { if(!('wakeLock' in navigator))return; try { if(state.status==='InProgress'&&document.visibilityState==='visible'&&!wakeLock) { wakeLock=await navigator.wakeLock.request('screen'); wakeLock.addEventListener('release',()=>wakeLock=null); } else if(state.status!=='InProgress'&&wakeLock) { await wakeLock.release(); wakeLock=null; } } catch { /* Deadlines do not depend on screen lock. */ } }
function restoreForms() { for(const op of queue.filter(o=>o.kind==='set')) { const form=[...root.querySelectorAll('.set-form')].find(f=>f.elements.namedItem('setId')?.value===op.setId); if(!form)continue; for(const input of form.querySelectorAll('input[name]')) { if(input.name==='__RequestVerificationToken')continue; const pair=op.fields.find(([n])=>n===input.name); if(input.type==='checkbox')input.checked=!!pair; else if(pair)input.value=pair[1]; } form.classList.toggle('is-completed',form.elements.namedItem('completed').checked); form.classList.toggle('is-skipped',op.fields.some(([n,v])=>n==='skipped'&&v==='true')); } }
async function drain() {
 if(sending||conflict||!queue.length)return; sending=true;
 try { while(queue.length) {
  const op=queue[0]; syncMessage('Сохраняем результаты…'); const body=new URLSearchParams(op.fields); body.set('__RequestVerificationToken',token); body.set('operationId',op.id); body.set('expectedRevision',revision);
  const response=await fetch(`${location.pathname}?handler=${op.kind==='set'?'Set':'Control'}`,{method:'POST',headers:{'X-Requested-With':'XMLHttpRequest'},body,redirect:'error'}); const data=await response.json().catch(()=>({}));
  if(!response.ok) { if(response.status===400) { rejectedOperationId=op.id; root.querySelector("[data-discard-operation]").hidden=false; } conflict=response.status===409; if(conflict)root.querySelector('[data-reconcile]').hidden=false; throw new Error(data.error||'Не удалось сохранить. Проверьте соединение и вход в аккаунт.'); }
  revision=data.revision; queue.shift(); if(op.kind === "set" && JSON.stringify(scratch[op.setId]) === JSON.stringify(op.fields.filter(([n]) => !["performedAtUtc","skipped","remove"].includes(n)))) delete scratch[op.setId]; persist(); update(data);
  if(op.fields.some(([n,v])=>n==='remove'&&v==='true')) [...root.querySelectorAll('.set-form')].find(f=>f.elements.namedItem('setId')?.value===op.setId)?.remove();
  if(data.status==='Cancelled') { location.reload(); return; }
 } syncMessage(); } catch(error) { syncMessage(`${error.message} Изменения ${storageAvailable?'сохранены на устройстве':'остались в открытой странице'}.`); } finally { sending=false; }
}
function previewOperation(op) {
 const value = name => op.fields.find(([n])=>n===name)?.[1];
 const at = Date.parse(value('performedAtUtc') || new Date(Date.now()+offset).toISOString());
 if(op.kind==='set' && op.startsRest && state.status==='InProgress') {
  const rest=Number(value('AddSet.RestSeconds')||0); state.restEndsAtUtc=rest>0?new Date(at+rest*1000).toISOString():null; state.restAfterSetId=Number(op.setId);
 }
 if(op.kind==='control') {
  const action=value('action'), seconds=Number(value('seconds')||0);
  if(action==='pause' && state.status==='InProgress') {
   state.elapsedSeconds=Math.max(0,state.elapsedSeconds+(at-offset-sampledAt)/1000); sampledAt=Date.now(); state.status='Paused';
   state.restRemainingSeconds=state.restEndsAtUtc?Math.max(0,Math.ceil((Date.parse(state.restEndsAtUtc)-at)/1000)):null;state.restEndsAtUtc=null;
  } else if(action==='resume' && state.status==='Paused') {
   state.status='InProgress';sampledAt=at-offset;state.restEndsAtUtc=state.restRemainingSeconds>0?new Date(at+state.restRemainingSeconds*1000).toISOString():null;state.restRemainingSeconds=null;
  } else if(action==='skip-rest') { state.restEndsAtUtc=null;state.restRemainingSeconds=null;state.restAfterSetId=null; }
  else if(action==='adjust-rest') {
   if(state.status==='Paused' && state.restRemainingSeconds!=null)state.restRemainingSeconds=Math.max(0,Math.min(1800,state.restRemainingSeconds+seconds));
   else if(state.restEndsAtUtc)state.restEndsAtUtc=new Date(at+Math.max(0,Math.min(1800,(Date.parse(state.restEndsAtUtc)-at)/1000+seconds))*1000).toISOString();
  }
 }
}
function enqueue(op) { queue.push({...op,id:crypto.randomUUID()}); persist(); syncMessage(); drain(); }
function saveSet(form,extra=[],quiet=false) {
 if(state.status==='Paused')return false;
 const completedInput=form.elements.namedItem("completed"), repsInput=form.elements.namedItem("AddSet.Reps"); repsInput.setCustomValidity(completedInput.checked && Number(repsInput.value)<1 ? "Укажите хотя бы одно повторение для выполненного подхода." : "");
 if(!(quiet?form.checkValidity():form.reportValidity())&&!extra.some(([n])=>n==='remove'))return false;
 if(rejectedOperationId && queue[0]?.id === rejectedOperationId && queue[0]?.setId === form.elements.namedItem("setId").value) { queue.shift(); rejectedOperationId=null; root.querySelector("[data-discard-operation]").hidden=true; }
 const fields=[...new FormData(form).entries()].filter(([n])=>n!=='__RequestVerificationToken'); fields.push(...extra,['performedAtUtc',new Date(Date.now()+offset).toISOString()]);
 const startsRest = form.elements.namedItem("completed").checked && !form.classList.contains("is-completed");
 form.classList.toggle("is-completed",form.elements.namedItem("completed").checked); if(startsRest && state.status === "InProgress") { const rest=Number(form.elements.namedItem("AddSet.RestSeconds").value); state.restEndsAtUtc=rest>0?new Date(Date.now()+offset+rest*1000).toISOString():null; state.restAfterSetId=Number(form.elements.namedItem("setId").value); tick(); } enqueue({kind:"set",setId:form.elements.namedItem("setId").value,fields,startsRest}); return true;
}
root.addEventListener("input", event => { const form=event.target.closest(".set-form"); if(!form || !active() || state.status==="Paused") return; const id=form.elements.namedItem("setId").value; scratch[id]=[...new FormData(form).entries()].filter(([n])=>n!=="__RequestVerificationToken"); persist(); clearTimeout(debounce.get(id)); debounce.set(id,setTimeout(()=>saveSet(form,[],true),500)); });
root.addEventListener('change',event=>{const form=event.target.closest('.set-form'); if(form&&!event.target.readOnly&&active()){clearTimeout(debounce.get(form.elements.namedItem('setId').value));if(!saveSet(form)&&event.target.name==='completed'){event.target.checked=form.classList.contains('is-completed');scratch[form.elements.namedItem('setId').value]=[...new FormData(form).entries()].filter(([n])=>n!=='__RequestVerificationToken');persist();}}});
function completionFields(form) { for(const [name,value] of [['operationId',crypto.randomUUID()],['expectedRevision',revision]]) { let input=form.elements.namedItem(name); if(!input) {input=document.createElement('input');input.type='hidden';input.name=name;form.append(input);}input.value=value; } }
document.addEventListener('submit',async event=>{
 if (event.target.hasAttribute('data-pick-form')) { event.preventDefault(); picker?.commit(); return; }
 const form=event.target; if(form.matches('.set-form')) { event.preventDefault(); clearTimeout(debounce.get(form.elements.namedItem("setId").value)); saveSet(form,event.submitter?.name==='remove'?[['remove','true']]:[]); return; }
 if(!root.contains(form)&&!form.hasAttribute('data-complete-form')&&!form.closest('#exercise-dialog,#load-template-dialog'))return;
 if(form.dataset.flushed)return; for(const [id,pairs] of Object.entries(scratch)) { const pendingForm=[...root.querySelectorAll(".set-form")].find(f=>f.elements.namedItem("setId").value===id); if(pendingForm && state.status!=="Paused" && !saveSet(pendingForm)) { event.preventDefault();return; } }
 for(const setForm of root.querySelectorAll('.set-form:focus-within')) { if(!saveSet(setForm)){event.preventDefault();return;} }
 if(!queue.length&&!sending) {if(form.hasAttribute('data-complete-form'))completionFields(form);return;}
 event.preventDefault();await drain();if(queue.length||sending){syncMessage('Дождитесь отправки всех изменений перед переходом.');return;}
 form.dataset.flushed='true';if(form.hasAttribute('data-complete-form'))completionFields(form);form.requestSubmit(event.submitter);
});
document.addEventListener('click',event=>{
 const control=event.target.closest("[data-control]");if(control) { const op={kind:"control",fields:[["action",control.dataset.control],["seconds",control.dataset.seconds||"0"],["performedAtUtc",new Date(Date.now()+offset).toISOString()]]}; previewOperation(op);tick();enqueue(op); }
 const skip=event.target.closest('[data-skip-set]');if(skip){const form=skip.closest('.set-form');form.elements.namedItem('completed').checked=false;form.classList.add('is-skipped');saveSet(form,[['skipped','true']]);}
 if(event.target.closest('[data-finish]')) { const forms=[...root.querySelectorAll('.set-form')], completed=forms.filter(f=>f.elements.namedItem('completed').checked),working=completed.filter(f=>!f.elements.namedItem('AddSet.IsWarmup').checked);const volume=working.reduce((sum,f)=>sum+Number(f.elements.namedItem('AddSet.Weight').value)*Number(f.elements.namedItem('AddSet.Reps').value),0);document.querySelector('[data-finish-summary]').textContent=`Рабочих подходов: ${working.length} · Объём: ${volume.toLocaleString('ru-RU')} кг · Осталось: ${forms.length-completed.length}`;}
 const soundButton=event.target.closest('[data-sound]');if(soundButton){sound=!sound;soundButton.textContent=`Звук: ${sound?'вкл.':'выкл.'}`;soundButton.setAttribute('aria-pressed',String(sound));if(sound){audioContext ||= new(window.AudioContext||window.webkitAudioContext)();audioContext.resume();}}
 const trigger=event.target.closest('[data-add-exercise],[data-replace-exercise]');if(trigger){document.querySelector('[data-exercise-entry]').value=trigger.dataset.replaceExercise||'0';document.getElementById('exercise-dialog-title').textContent=trigger.dataset.replaceExercise?'Заменить упражнение':'Добавить упражнения';picker?.setSingle(!!trigger.dataset.replaceExercise);picker?.message('');}
});
root.querySelector("[data-discard-operation]").addEventListener("click",()=>{if(queue[0]?.id===rejectedOperationId)queue.shift();rejectedOperationId=null;root.querySelector("[data-discard-operation]").hidden=true;persist();drain();});
root.querySelector('[data-reconcile]').addEventListener('click',async()=>{try{const response=await fetch(`${location.pathname}?handler=State`,{cache:'no-store'});if(!response.ok)throw new Error();const data=await response.json();revision=data.revision;conflict=false;persist();root.querySelector('[data-reconcile]').hidden=true;update(data);await drain();}catch{syncMessage('Не удалось получить текущую версию. Проверьте соединение.');}});
async function refresh(){if(picking||pickerRoot?.closest('dialog')?.open)return;if(queue.length){drain();return;}if(sending)return;try{const response=await fetch(`${location.pathname}?handler=State`,{cache:'no-store'});if(!response.ok)return;const data=await response.json();if(picking||pickerRoot?.closest('dialog')?.open)return;if(data.revision!==revision){location.reload();return;}update(data);}catch{/* Offline changes remain local. */}}
document.querySelectorAll('[data-local-time]').forEach(time=>time.textContent=new Date(time.dataset.localTime).toLocaleTimeString('ru-RU',{hour:'2-digit',minute:'2-digit'}));
window.addEventListener('online',drain);window.addEventListener('beforeunload',event=>{if((queue.length||Object.keys(scratch).length)&&!storageAvailable){event.preventDefault();event.returnValue='';}});
document.addEventListener('visibilitychange',()=>{if(document.visibilityState==='visible'){tick();refresh();maintainWakeLock();}});window.addEventListener('pageshow',refresh);
restoreForms(); queue.forEach(previewOperation); for(const [id,pairs] of Object.entries(scratch)) { const form=[...root.querySelectorAll(".set-form")].find(f=>f.elements.namedItem("setId").value===id); if(!form) continue; for(const input of form.querySelectorAll("input[name]")) { if(input.name==="__RequestVerificationToken")continue; const pair=pairs.find(([n])=>n===input.name); if(input.type==="checkbox")input.checked=!!pair;else if(pair)input.value=pair[1]; } if(active()&&state.status!=="Paused")saveSet(form); } syncMessage();tick();maintainWakeLock();drain();setInterval(tick,1000);setInterval(refresh,15000);
})();
