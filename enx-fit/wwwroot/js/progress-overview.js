(() => {
    'use strict';
    const root=document.querySelector('[data-progress-overview]');
    if(!root)return;
    document.cookie=`enix-utc-offset=${-new Date().getTimezoneOffset()}; Path=/; SameSite=Lax; Max-Age=31536000`;
    document.addEventListener('click',event=>{
        const trigger=event.target.closest('[data-progress-dialog]');
        if(trigger){const dialog=document.getElementById(trigger.dataset.progressDialog);if(dialog instanceof HTMLDialogElement)dialog.showModal();}
        const close=event.target.closest('[data-progress-close]');
        if(close)close.closest('dialog')?.close();
    });
    document.querySelectorAll('dialog.rd-dialog').forEach(dialog=>dialog.addEventListener('click',event=>{
        if(event.target!==dialog)return;
        const rect=dialog.getBoundingClientRect();
        if(event.clientX<rect.left||event.clientX>rect.right||event.clientY<rect.top||event.clientY>rect.bottom)dialog.close();
    }));
    root.querySelectorAll('[data-progress-submit]').forEach(select=>select.addEventListener('change',()=>{
        const form=select.form;if(!form)return;
        if(['ClientId','ProgramId','SelectedClient'].includes(select.name)){
            const exercise=form.elements.namedItem('ChartExercise');if(exercise)exercise.value='';
            if(select.name==='ClientId'){
                const program=form.elements.namedItem('ProgramId');if(program)program.value='';
                const client=form.elements.namedItem('SelectedClient');if(client)client.value='';
            }
            if(select.name==='ProgramId'){const client=form.elements.namedItem('SelectedClient');if(client)client.value='';}
        }
        form.requestSubmit();
    }));
})();
