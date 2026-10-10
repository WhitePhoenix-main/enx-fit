(() => {
 'use strict';
 const source=document.getElementById('training-analytics-chart-data');
 if(!source||typeof Chart==='undefined')return;
 let data;try{data=JSON.parse(source.textContent);}catch{return;}
 const charts=[];
 function create(id,label,series,color){
  const canvas=document.getElementById(id);if(!canvas||!series)return;
  charts.push(new Chart(canvas,{type:'line',data:{labels:series.labels,datasets:[{label,data:series.values,borderColor:color,backgroundColor:color+'18',fill:true,tension:.2,pointRadius:3,pointHoverRadius:6}]},
   options:{responsive:true,maintainAspectRatio:false,animation:matchMedia('(prefers-reduced-motion:reduce)').matches?false:{duration:250},interaction:{mode:'index',intersect:false},
    onClick:(_,points)=>{const id=series.workoutIds?.[points[0]?.index];if(Number.isInteger(id)&&id>0)location.assign('/Workouts/Details/'+id);},
    onHover:(event,points)=>{if(event.native?.target)event.native.target.style.cursor=points.length?'pointer':'default';},
    plugins:{legend:{display:false},tooltip:{backgroundColor:'#213b5e',borderColor:'#6385b7',borderWidth:1,padding:12,displayColors:false}},
    scales:{x:{border:{display:false},grid:{display:false},ticks:{maxTicksLimit:6,maxRotation:0,color:'#aec5e6'}},y:{border:{display:false},grid:{color:'#30496b'},ticks:{maxTicksLimit:5,color:'#aec5e6'}}}
   }}));
 }
 create('working-weight-chart','Рабочий вес, кг',data.workingWeight,'#8bb6ff');
 create('estimated-one-rep-max-chart','Расчётный максимум, кг',data.estimatedOneRepMax,'#b4a5ff');
 create('exercise-volume-chart','Объём подходов, кг',data.volume,'#7fc9e8');
 document.querySelectorAll('[name="analytics-metric"]').forEach(input=>input.addEventListener('change',()=>requestAnimationFrame(()=>charts.forEach(chart=>chart.resize()))));
})();
