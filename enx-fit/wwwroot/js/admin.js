(() => {
 'use strict';
 const toggle=document.getElementById('admin-menu-toggle'),sidebar=document.getElementById('admin-sidebar'),desktop=matchMedia('(min-width:1024px)');
 function updateMenu(open){
  const visible=desktop.matches&&open;
  document.body.classList.toggle('admin-menu-collapsed',desktop.matches&&!open);
  if(sidebar)sidebar.inert=!visible;
  toggle?.setAttribute('aria-expanded',String(visible));
 }
 if(toggle&&sidebar){updateMenu(true);toggle.addEventListener('click',()=>updateMenu(toggle.getAttribute('aria-expanded')!=='true'));desktop.addEventListener('change',()=>updateMenu(true));}
 document.querySelectorAll('[data-auto-submit]').forEach(select=>select.addEventListener('change',()=>select.form.requestSubmit()));
 document.addEventListener('keydown',event=>{
  if(event.key==='/'&&!event.ctrlKey&&!event.metaKey&&!['INPUT','TEXTAREA','SELECT'].includes(document.activeElement.tagName)&&!document.activeElement.isContentEditable){
   const target=[...document.querySelectorAll('.admin-global-search input,.admin-filter input[type="search"]')].find(input=>input.offsetParent!==null);
   if(target){event.preventDefault();target.focus();}
  }
 });
})();
