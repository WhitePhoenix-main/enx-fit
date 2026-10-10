(() => {
 'use strict';
 const canvas=document.getElementById('admin-activity-chart'),source=document.getElementById('admin-chart-data'),select=document.getElementById('admin-activity-date');
 if(!canvas||!source||typeof Chart==='undefined')return;
 let data;try{data=JSON.parse(source.textContent);}catch{return;}
 new Chart(canvas,{
  type:'line',
  data:{labels:data.labels,datasets:[{label:'Записи',data:data.values,borderColor:'#80adff',backgroundColor:'#6596ff18',borderWidth:2,fill:true,tension:.2,pointRadius:data.labels.length<=7?3:0,pointHoverRadius:5,pointBackgroundColor:'#a3c7ff'}]},
  options:{responsive:true,maintainAspectRatio:false,animation:matchMedia('(prefers-reduced-motion:reduce)').matches?false:{duration:300},interaction:{mode:'index',intersect:false},
   onClick:(_,points)=>{const index=points[0]?.index;if(index!==undefined&&select&&data.dates?.[index]){select.value=data.dates[index];select.dispatchEvent(new Event('change',{bubbles:true}));select.focus({preventScroll:true});}},
   onHover:(event,points)=>{if(event.native?.target)event.native.target.style.cursor=points.length?'pointer':'default';},
   plugins:{legend:{display:false},tooltip:{backgroundColor:'#213b5e',borderColor:'#5574a1',borderWidth:1,padding:12,displayColors:false}},
   scales:{x:{grid:{display:false},border:{display:false},ticks:{maxTicksLimit:7,maxRotation:0,color:'#b0c5e2',font:{size:11}}},y:{beginAtZero:true,suggestedMax:4,border:{display:false},grid:{color:'#2c4362'},ticks:{precision:0,maxTicksLimit:5,padding:10,color:'#b0c5e2',font:{size:11}}}}
  }
 });
})();
