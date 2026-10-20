(() => {
    'use strict';
    const root=document.querySelector('[data-template-catalog]'); if(!root)return;
    const rows=[...root.querySelectorAll('[data-template-card]')],details=[...root.querySelectorAll('[data-template-detail]')];
    const key=`enix-workout-templates:${root.dataset.viewer}`,search=root.querySelector('[data-template-search]');
    const fields={goal:root.querySelector('[data-template-goal]'),level:root.querySelector('[data-template-level]'),place:root.querySelector('[data-template-place]'),duration:root.querySelector('[data-template-duration]')};
    let collection=null;let favorites=new Set(),tab='all',category=root.dataset.initialCategory||'all',limit=4,selected=root.dataset.selected;
    const normalize=s=>String(s||'').toLocaleLowerCase('ru').replace(/ё/g,'е');
    function readFavorites(){try{const saved=JSON.parse(localStorage.getItem(key));favorites=new Set(Array.isArray(saved)?saved.map(String):[]);}catch{favorites=new Set();}}
    function renderFavorites(){const host=root.querySelector('[data-template-favorite-list]');if(host){host.replaceChildren(...rows.filter(row=>favorites.has(row.dataset.templateCard)).slice(0,3).map(row=>{const button=document.createElement('button');button.type='button';button.dataset.templateSelect=row.dataset.templateCard;button.className='tc-favorite-row';const photo=row.querySelector('.tc-art').cloneNode(true),copy=document.createElement('span'),title=document.createElement('strong'),meta=document.createElement('small');title.textContent=row.querySelector('.tc-card-copy>strong').textContent;meta.textContent=row.dataset.duration+' мин';copy.append(title,meta);button.append(photo,copy);return button;}));if(!host.children.length){const p=document.createElement('p');p.textContent='Отметьте понравившиеся шаблоны сердечком.';host.append(p);}}root.querySelectorAll('[data-template-favorite]').forEach(b=>b.setAttribute('aria-pressed',String(favorites.has(b.dataset.templateFavorite))));}
    function render(maintainSelection=true){
        const words=normalize(search.value).trim().split(/\s+/).filter(Boolean);let counts={mine:0,catalog:0},total=0,matched=[];
        for(const row of rows){const d=row.dataset,home=d.home==='true',mine=d.mine==='true';
            const categoryMatch=category==='all'||category==='home'&&home||category==='gym'&&!home||category===d.focus||category===d.level||category==='strength'&&normalize(d.goal).includes('сил')||category==='weight'&&normalize(d.goal).includes('вес');
            const match=(!collection||collection.has(d.templateCard))&&(tab!=='mine'||mine)&&(tab!=='used'||Number(d.uses)>0)&&(tab!=='favorites'||favorites.has(d.templateCard))&&categoryMatch&&words.every(w=>normalize(d.search).includes(w))&&(!fields.goal.value||normalize(d.goal).includes(fields.goal.value))&&(!fields.level.value||d.level===fields.level.value)&&(!fields.place.value||home===(fields.place.value==='home'))&&(!fields.duration.value||Number(d.duration)<=Number(fields.duration.value));
            const group=mine?'mine':'catalog';if(match){counts[group]++;total++;matched.push(d.templateCard);}row.hidden=!match||counts[group]>limit;
        }
        for(const group of ['mine','catalog']){root.querySelector(`[data-template-group="${group}"]`).hidden=counts[group]===0&&group!=='mine';root.querySelector(`[data-template-count="${group}"]`).textContent=counts[group]?`(${counts[group]})`:'';}
        root.querySelector('[data-template-empty]').hidden=total>0||!rows.length;root.querySelector('[data-template-more]').hidden=counts.mine<=limit&&counts.catalog<=limit;
        root.querySelectorAll('[data-template-tab]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.templateTab===tab)));
        root.querySelectorAll('[data-template-category]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.templateCategory===category)));
        if(maintainSelection&&!matched.includes(selected)){selected=matched[0]||'';details.forEach(d=>d.hidden=d.dataset.templateDetail!==selected);if(!selected)root.classList.remove('has-detail');}
        root.querySelector('[data-template-preview-empty]').hidden=!!selected;
        for(const row of rows){const active=row.dataset.templateCard===selected;row.classList.toggle('is-selected',active);row.querySelector('[data-template-select]').setAttribute('aria-pressed',String(active));}
        renderFavorites();
    }
    function select(id,open=true){if(!details.some(d=>d.dataset.templateDetail===id))return;selected=id;details.forEach(d=>d.hidden=d.dataset.templateDetail!==id);root.classList.toggle('has-detail',open);render(false);
        const url=new URL(location.href);if(open)url.searchParams.set('Selected',id);else url.searchParams.delete('Selected');history.replaceState(null,'',url);root.querySelector('[data-template-detail-host]').scrollTop=0;
    }
    function closeDetail(){root.classList.remove('has-detail');const url=new URL(location.href);url.searchParams.delete('Selected');history.replaceState(null,'',url);}
    root.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;
        if(b.dataset.templateSelect)select(b.dataset.templateSelect);
        if(b.hasAttribute('data-template-close'))closeDetail();
        if(b.hasAttribute('data-template-favorite')){const id=b.dataset.templateFavorite;favorites.has(id)?favorites.delete(id):favorites.add(id);try{localStorage.setItem(key,JSON.stringify([...favorites]));}catch{}render();}
        if(b.dataset.templateTab){collection=null;tab=b.dataset.templateTab;limit=4;render();}
        if(b.dataset.templateCategory){collection=null;category=b.dataset.templateCategory;limit=4;render();}
        if(b.hasAttribute('data-template-more')){limit+=4;render();}
        if(b.dataset.templateAll){limit=rows.length;render();}
        if(b.hasAttribute('data-template-collection')){collection=new Set(b.dataset.templateCollection.split(','));tab='all';render();}
        if(b.hasAttribute('data-template-reset')){collection=null;tab='all';category='all';limit=4;search.value='';Object.values(fields).forEach(f=>f.value='');render();}
        if(b.dataset.templatePanel){const host=b.closest('[data-template-detail]');host.querySelectorAll('[data-template-panel]').forEach(x=>x.setAttribute('aria-pressed',String(x===b)));host.querySelectorAll('[data-template-panel-content]').forEach(x=>x.hidden=x.dataset.templatePanelContent!==b.dataset.templatePanel);}
    });
    root.addEventListener('keydown',e=>{if(e.key==='Escape')closeDetail();});
    search.addEventListener('input',()=>{limit=4;render();});Object.values(fields).forEach(f=>f.addEventListener('change',()=>{limit=4;render();}));
    root.addEventListener('submit',e=>{const form=e.target;const button=e.submitter;if(!form.matches('form[method="post"]')||!button)return;if(form.dataset.submitting){e.preventDefault();return;}form.dataset.submitting='true';button.setAttribute('aria-busy','true');});
    window.addEventListener('pageshow',()=>root.querySelectorAll('form').forEach(f=>delete f.dataset.submitting));
    window.addEventListener('storage',e=>{if(e.key===key){readFavorites();render();}});
    readFavorites();render();select(selected,root.dataset.openDetail==='true');
})();



