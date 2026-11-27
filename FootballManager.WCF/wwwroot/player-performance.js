// מטרה: מציג סטטיסטיקה ועובדות על שחקן מהסגל הציבורי.
// מי משתמש: גולשי עמוד ליגות והרכבים.
// פעולות: דירוג עונתי, ביצועי משחקים אחרונים, אירועים ומצב פרסום ההרכב הבא.
// תלויות: נתיבי הכדורגל בשרת והאירועים שמפרסם football.js.
(() => {
  const $ = id => document.getElementById(id);
  let catalog;
  let squad;
  let match;
  let version = 0;
  let eventsVersion = 0;
  const finished = new Set(['FT', 'AET', 'PEN']);
  const upcoming = new Set(['NS', 'TBD']);

  /** קוראת נתונים מהשרת של הפרויקט, עם הודעת שגיאה בטוחה להצגה. */
  async function read(path) {
    const response = await fetch('/api/football/' + path);
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || 'הבקשה לא הצליחה.');
    return data;
  }

  function clearPerformance() {
    version++;
    for (const id of ['performance-status', 'performance-season', 'performance-games', 'performance-next']) $(id).replaceChildren();
    $('performance-load').disabled = !squad?.players.length;
  }

  function setSquad(value) {
    squad = value;
    const options = (squad?.players || []).map(player => new Option(player.name, player.id));
    $('performance-player').replaceChildren(...options);
    $('performance-player').disabled = !options.length;
    clearPerformance();
  }

  function item(list, text) {
    const row = document.createElement('li');
    row.textContent = text;
    $(list).append(row);
  }

  function fixtureLabel(fixture) {
    return `${new Date(fixture.fixture.date).toLocaleDateString('he-IL')} · ${fixture.teams.home.name} מול ${fixture.teams.away.name}`;
  }

  /** מציגה רק נתוני שחקן שהספק פרסם. הופעות עבר משמשות להקשר, בלי להציג אותן כתחזית. */
  async function loadPerformance() {
    if (!squad || !catalog) return;
    clearPerformance();
    const request = version;
    const player = Number($('performance-player').value);
    const team = squad.team;
    const fixtures = catalog.matches.filter(row => [row.teams.home.id, row.teams.away.id].includes(team));
    const recent = fixtures.filter(row => finished.has(row.fixture.status.short))
      .sort((a, b) => b.fixture.timestamp - a.fixture.timestamp).slice(0, 5);
    const next = fixtures.filter(row => upcoming.has(row.fixture.status.short) && row.fixture.timestamp * 1000 > Date.now())
      .sort((a, b) => a.fixture.timestamp - b.fixture.timestamp)[0];
    $('performance-load').disabled = true;
    $('performance-status').textContent = 'טוען נתוני שחקן מהשרת…';
    const query = new URLSearchParams({ player, league: catalog.league, season: catalog.season });
    const [season, ...games] = await Promise.allSettled([
      read('player-season?' + query), ...recent.map(row => read('match-players/' + row.fixture.id)),
      next ? read('lineups/' + next.fixture.id) : Promise.resolve([])
    ]);
    if (request !== version) return;
    const nextLineup = games.pop();
    const stats = season.value?.[0]?.statistics?.find(row => row.team?.id === team);
    $('performance-season').textContent = stats?.games?.rating != null
      ? `דירוג אישי ממוצע בעונה בקבוצה: ${Number(stats.games.rating).toFixed(2)} מתוך 10.`
      : 'אין דירוג עונתי זמין לשחקן בקבוצה שנבחרה.';
    let logged = 0;
    let withMinutes = 0;
    games.forEach((result, index) => {
      const roster = result.value?.find(row => row.team?.id === team)?.players;
      const line = roster?.find(row => row.player?.id === player)?.statistics?.[0];
      if (line?.games?.minutes != null) {
        withMinutes++;
        if (line.games.minutes > 0) logged++;
      }
      const label = fixtureLabel(recent[index]);
      const details = line ? `דקות: ${line.games?.minutes ?? 'לא דווחו'} · דירוג: ${line.games?.rating ?? 'לא דווח'} · שערים: ${line.goals?.total ?? 'לא דווחו'} · בישולים: ${line.goals?.assists ?? 'לא דווחו'}`
        : result.status === 'rejected' ? 'נתוני המשחק לא נטענו.' : 'לא נמצאה רשומת ביצועים לשחקן במשחק זה.';
      item('performance-games', `${label} — ${details}`);
    });
    const listing = nextLineup.value?.find(row => row.team?.id === team);
    let status = next ? 'טרם פורסם הרכב לקבוצה למשחק הבא.' : 'אין משחק עתידי זמין בעונה שנבחרה.';
    if (nextLineup.status === 'rejected') status = 'בדיקת פרסום ההרכב נכשלה.';
    if (listing?.startXI?.length) {
      status = listing.startXI.some(row => row.player?.id === player) ? 'השחקן מופיע ב־11 הפותחים שפורסמו.'
        : listing.substitutes?.some(row => row.player?.id === player) ? 'השחקן מופיע ברשימת המחליפים שפורסמה.'
        : 'השחקן אינו מופיע בהרכב שפורסם.';
    }
    $('performance-next').textContent = `${next ? fixtureLabel(next) + ' · ' : ''}${status} ${withMinutes ? `השחקן שיחק ב־${logged} מתוך ${withMinutes} משחקים עם נתוני דקות. הנתון מתאר משחקי עבר.` : ''}`;
    $('performance-status').textContent = recent.length ? 'עד חמישה משחקים שהסתיימו בעונה שנבחרה.' : 'לא נמצאו משחקים שהסתיימו לקבוצה ולעונה שנבחרו.';
    $('performance-load').disabled = false;
  }

  /** טוענת אירועים שפורסמו למשחק הנבחר בלבד. */
  async function loadEvents() {
    const request = ++eventsVersion;
    $('events-list').replaceChildren();
    $('events-status').textContent = 'טוען אירועים…';
    try {
      const events = await read('events/' + match.fixture.id);
      if (request !== eventsVersion) return;
      for (const row of events) {
        const type = { Goal: 'שער', Card: 'כרטיס', subst: 'חילוף', Var: 'בדיקת וידאו' }[row.type] || row.type;
        const minute = row.time?.elapsed == null ? '' : `${row.time.elapsed}${row.time.extra ? '+' + row.time.extra : ''}׳`;
        item('events-list', [minute, type, row.detail, row.team?.name, row.player?.name, row.assist?.name].filter(Boolean).join(' · '));
      }
      $('events-status').textContent = events.length ? '' : 'הספק לא פרסם אירועים למשחק הזה.';
    } catch (error) {
      if (request === eventsVersion) $('events-status').textContent = error.message;
    }
  }

  $('filters').addEventListener('submit', () => { catalog = null; setSquad(null); });
  $('team').addEventListener('change', () => setSquad(null));
  $('performance-player').addEventListener('change', clearPerformance);
  document.addEventListener('football-league-loaded', event => { catalog = event.detail; });
  document.addEventListener('football-squad-loaded', event => setSquad(event.detail));
  document.addEventListener('football-match-selected', event => {
    match = event.detail;
    eventsVersion++;
    $('events-list').replaceChildren();
    $('events-status').textContent = '';
    $('match-events').disabled = !match;
  });
  $('match-events').addEventListener('click', loadEvents);
  $('performance-load').addEventListener('click', loadPerformance);
})();
