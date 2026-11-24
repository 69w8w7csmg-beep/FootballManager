// מטרה: מנהל תפריטי תוכן וטפסים באתר.
// מי משתמש: content.html ודפדפן המשתמש.
// פעולות: api, load, editor וטיפול בשליחה ובבחירה.
// תלויות: מתאם FootballManagerWebApi ועוגיית Session.
(() => {
  const $ = (selector) => document.querySelector(selector);
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const schemas = {
    players: {title:'שחקנים',url:'/api/players',items:'players',id:'playerId',write:'/api/manager/players',fields:[['firstName','שם פרטי','text'],['lastName','שם משפחה','text'],['age','גיל','number'],['position','עמדה','text'],['rating','דירוג','number'],['speed','מהירות','number'],['shooting','בעיטה','number'],['passing','מסירה','number'],['defense','הגנה','number']]},
    teams: {title:'קבוצות',url:'/api/teams',items:'teams',id:'teamId',write:'/api/manager/teams',fields:[['name','שם','text'],['country','מדינה','text'],['league','ליגה','text'],['budget','תקציב','number']]},
    matches: {title:'משחקים',url:'/api/matches',items:'matches',id:'matchId',write:'/api/matches',fields:[['homeTeamId','קבוצת בית','number'],['awayTeamId','קבוצת חוץ','number'],['matchDate','תאריך','date'],['homeScore','שערי בית','number'],['awayScore','שערי חוץ','number']]},
    transfers: {title:'העברות',url:'/api/transfers',items:'transfers',id:'transferId',write:'/api/transfers',fields:[['playerId','שחקן','number'],['fromTeamId','מקבוצת','number'],['toTeamId','לקבוצת','number'],['transferDate','תאריך','date'],['price','מחיר','number']]},
    statistics: {title:'סטטיסטיקות',url:'/api/statistics',items:'statistics',id:'statisticId',write:'/api/statistics',fields:[['playerId','שחקן','number'],['matchId','משחק','number'],['goals','שערים','number'],['assists','בישולים','number'],['minutesPlayed','דקות','number'],['rating','דירוג','number']]},
    favorites: {title:'המועדפים שלי',url:'/api/favorites',items:'players',id:'playerId',fields:[]}
  };
  let role = 'Guest', current = 'players', rows = [], loadVersion = 0;
  /** שולח בקשת HTTP לאתר מאותו מקור. מציג 401 או 403 לפי תשובת השרת במקום להסיק הרשאה מהתפריט. */
  async function api(url, options) {
    const response = await fetch(url,{credentials:'same-origin',...options});
    const data = await response.json().catch(()=>({}));
    if (response.status === 401) throw new Error('401 Unauthorized · יש להתחבר כדי לבצע את הפעולה.');
    if (response.status === 403) throw new Error('403 Forbidden · לחשבון הזה אין הרשאה לבצע את הפעולה.');
    if (!response.ok) throw new Error(`${response.status} · ${data.message || 'הפעולה נחסמה בשרת'}`);
    return data;
  }
  /** מחזיר למשתמש טקסט מצב ומסמן כשזו הודעת שגיאה. */
  function message(text, failed = false) {
    const status = $('#message');
    status.textContent = text;
    status.classList.toggle('error', failed);
  }
  /** מציג את מדורי התוכן המתאימים לתפקיד שקיבל מהשרת. */
  function renderTabs() {
    const tabs = ['players', 'teams', 'matches', 'transfers'];
    if (role === 'Manager') tabs.push('statistics');
    if (role === 'Player') tabs.push('favorites');
    $('#tabs').innerHTML = tabs.map(key=>`<button data-tab="${key}" class="${current===key?'active':''}" aria-pressed="${current===key}">${schemas[key].title}</button>`).join('');
  }
  /** טוען מדור אחד. מספר הבקשה מונע מתשובה איטית למחוק את הודעת המדור שנבחר אחריה. */
  async function load(key) {
    const version = ++loadVersion;
    const schema = schemas[key];
    current = key;
    rows = [];
    renderTabs();
    $('#section-title').textContent = schema.title;
    $('#records').innerHTML = '';
    $('#editor').hidden = true;
    message('טוען…');
    try {
      const data = await api(schema.url);
      if (version !== loadVersion) return;
      rows = data[schema.items] || [];
      $('#records').innerHTML=rows.length?rows.map((row,index)=>`<article class="record"><h2>${esc(row.name || [row.firstName,row.lastName].filter(Boolean).join(' ') || row.playerName || row.homeTeamName || schema.title)}</h2><p>${schema.fields.map(([field,label])=>`${label}: ${esc(row[field])}`).join(' · ')}</p><div class="actions">${role==='Manager'&&schema.write?`<button data-edit="${index}">ערוך</button><button data-delete="${index}">מחק</button>`:''}${key==='favorites'?`<button data-remove="${row.playerId}">הסר מהמועדפים</button>`:''}${key==='players'&&role==='Player'?`<button data-favorite="${row.playerId}">הוסף למועדפים</button>`:''}</div></article>`).join(''):'<p>אין רשומות להצגה.</p>';
      if (role==='Manager'&&schema.write) $('#records').insertAdjacentHTML('beforeend','<button class="primary" id="new-record">הוסף רשומה</button>');
      message(data.message||'הרשומות נטענו.');
    } catch(error) {
      if (version === loadVersion) message(error.message, true);
    }
  }
  /** מציג טופס ריק ליצירה או שדות מרשומה קיימת לעריכה. */
  function editor(index=-1) {
    const schema=schemas[current], row=rows[index]||{};
    $('#editor').hidden=false;$('#editor-title').textContent=index<0?'הוספת רשומה':'עדכון רשומה';
    const form = $('#record-form');
    form.dataset.index = index;
    form.innerHTML = schema.fields.map(([field,label,type]) => {
      const decimal = field === 'price' || field === 'budget' || (field === 'rating' && current === 'statistics');
      const step = decimal ? ' step="any"' : '';
      const value = type === 'date' && row[field] ? String(row[field]).slice(0, 10) : row[field] ?? '';
      return `<label>${label}<input name="${field}" type="${type}" value="${esc(value)}"${step} required></label>`;
    }).join('') + '<button class="primary" type="submit" disabled>שמור דרך השירות</button>';
    updateEditorButton();
    $('#editor').scrollIntoView({behavior:'smooth'});
  }
  /** מעדכן את כפתור השמירה לפי תקינות הקלט הנוכחי. */
  function updateEditorButton() {
    const form = $('#record-form');
    const submit = form.querySelector('[type=submit]');
    if (!submit) return;
    // תוקף בסיסי בדפדפן חוסך שליחה של טופס חסר. השירות בודק ערכים, הרשאות וקשרים במסד.
    submit.disabled = !form.checkValidity() || Array.from(form.querySelectorAll('input')).some(input => !input.value.trim());
  }
  $('#record-form').addEventListener('input', updateEditorButton);
  document.addEventListener('click',async event=>{
    const tab=event.target.closest('[data-tab]');if(tab){await load(tab.dataset.tab);return;}
    if(event.target.id==='new-record'){editor();return;}
    const edit=event.target.closest('[data-edit]');if(edit){editor(Number(edit.dataset.edit));return;}
    const action=event.target.closest('[data-delete],[data-favorite],[data-remove]');if(!action)return;
    try{const schema=schemas[current];if(action.dataset.delete!==undefined){const row=rows[Number(action.dataset.delete)];await api(`${schema.write}/${row[schema.id]}`,{method:'DELETE'});}else{const id=action.dataset.favorite||action.dataset.remove;await api(`/api/favorites/${id}`,{method:action.dataset.favorite?'POST':'DELETE'});}await load(current);}catch(error){message(error.message);}
  });
  $('#record-form').addEventListener('submit',async event=>{
    event.preventDefault();const schema=schemas[current], index=Number(event.target.dataset.index), body=index>=0?{...rows[index]}:{};
    for(const [field,,type] of schema.fields){const value=event.target.elements.namedItem(field).value;body[field]=type==='number'?Number(value):type==='date'?new Date(value).toISOString():value;}
    try{const result=await api(schema.write,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});message(result.message);if(result.succeeded)await load(current);}catch(error){message(error.message);}
  });
  api('/api/session').then(session=>{role=session.role;$('#account').textContent=`${session.username} · ${role}`;load('players');}).catch(()=>{$('#account').textContent='אורח';load('players');});
})();
