// מטרה: מציג נתוני כדורגל וממחיש מערכים בלי לקרוא לספק מהדפדפן.
// מי משתמש: עמוד football.html.
// פעולות: טעינת תחרויות, סינון עונה, הצגת סגלים והרכבים שפורסמו.
// תלויות: נתיבי /api/football בשרת ופקדי העמוד.
(() => {
  const $ = id => document.getElementById(id);
  let matches = [];
  let version = 0;

  /** קוראת JSON מהשרת של FootballManager ומציגה הודעה אם הבקשה נדחתה. */
  async function read(path) {
    const response = await fetch('/api/football/' + path);
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || 'הבקשה לשרת נכשלה.');
    return data;
  }

  /** מעדכנת רשימת בחירה באמצעות טקסט, כדי ששמות מהספק לא יתפרשו כ־HTML. */
  function options(id, rows, key, text) {
    $(id).replaceChildren(...rows.map(row => new Option(text(row), key(row))));
    $(id).disabled = rows.length === 0;
  }

  /** מציגה את התוצאה הקיימת. למשחק עתידי אין תוצאה להצגה. */
  function selectMatch() {
    const match = matches.find(item => item.fixture.id === Number($('match').value));
    $('match-score').textContent = !match ? '' : match.goals?.home == null || match.goals?.away == null
      ? 'התוצאה עדיין אינה זמינה.' : `${match.teams.home.name} ${match.goals.home} : ${match.goals.away} ${match.teams.away.name}`;
    $('lineups').disabled = !match;
    $('lineup-message').textContent = '';
    $('match-lineups').replaceChildren();
    document.dispatchEvent(new CustomEvent('football-match-selected', { detail: match }));
  }

  /** טוענת את בחירת הליגה והעונה ומציגה משחקים הקרובים לתאריך הנוכחי בראש הרשימה. */
  async function loadLeague(event) {
    event.preventDefault();
    const request = ++version;
    const query = new URLSearchParams({ league: $('league').value, season: $('season').value });
    $('load').disabled = true;
    $('message').textContent = 'טוען נתונים מהשרת…';
    matches = [];
    $('match').replaceChildren();
    $('team').replaceChildren();
    $('players').replaceChildren();
    $('team').disabled = $('match').disabled = $('squad').disabled = true;
    $('squad-message').textContent = '';
    selectMatch();
    const [fixtures, clubs] = await Promise.allSettled([read('matches?' + query), read('teams?' + query)]);
    if (request !== version) return;
    const errors = [...new Set([fixtures, clubs].filter(item => item.status === 'rejected').map(item => item.reason.message))];
    if (fixtures.status === 'fulfilled') {
      const now = Date.now();
      matches = fixtures.value.sort((a,b) => Math.abs(a.fixture.timestamp * 1000 - now) - Math.abs(b.fixture.timestamp * 1000 - now));
      options('match', matches, item => item.fixture.id,
        item => `${new Date(item.fixture.date).toLocaleDateString('he-IL')} · ${item.teams.home.name} מול ${item.teams.away.name}`);
      selectMatch();
    }
    if (clubs.status === 'fulfilled') {
      options('team', clubs.value, item => item.team.id, item => item.team.name);
      $('squad').disabled = clubs.value.length === 0;
    }
    document.dispatchEvent(new CustomEvent('football-league-loaded', { detail: {
      matches, league: Number($('league').value), season: Number($('season').value)
    } }));
    $('message').textContent = errors.join(' ') || (matches.length || $('team').value ? 'בחר משחק או קבוצה.' : 'אין נתונים לליגה ולעונה שנבחרו.');
    $('load').disabled = false;
  }

  /** יוצרת רשימה לקריאה מסגל או מהרכב שפורסם. */
  function playerList(players) {
    const list = document.createElement('ul');
    for (const player of players) {
      const row = document.createElement('li');
      row.textContent = [player.number, player.name, player.position || player.pos].filter(value => value != null).join(' · ');
      list.append(row);
    }
    return list;
  }

  /** טוענת את הסגל של הקבוצה שנבחרה ומסננת תוצאות של בחירה קודמת. */
  async function loadSquad() {
    const selected = $('team').value;
    const request = version;
    $('players').replaceChildren();
    $('squad-message').textContent = 'טוען סגל…';
    try {
      const groups = await read('squad/' + selected);
      if (request !== version || selected !== $('team').value) return;
      const players = groups[0]?.players ?? [];
      document.dispatchEvent(new CustomEvent('football-squad-loaded', { detail: { team: Number(selected), players } }));
      $('players').replaceChildren(...playerList(players).children);
      $('squad-message').textContent = players.length ? 'הסגל הנוכחי של הקבוצה לפי הספק.' : 'לא פורסם סגל לקבוצה שנבחרה.';
    } catch (error) {
      if (request === version && selected === $('team').value) $('squad-message').textContent = error.message;
    }
  }

  /** טוענת פותחים ומחליפים כפי שפורסמו עבור המשחק המסוים. */
  async function loadLineups() {
    const selected = $('match').value;
    const request = version;
    $('match-lineups').replaceChildren();
    $('lineup-message').textContent = 'טוען הרכבים…';
    try {
      const teams = await read('lineups/' + selected);
      if (request !== version || selected !== $('match').value) return;
      for (const team of teams) {
        const section = document.createElement('section');
        section.className = 'lineup-team';
        const title = document.createElement('h3');
        title.textContent = [team.team.name, team.formation].filter(Boolean).join(' · ');
        section.append(title);
        for (const [label, group] of [['פותחים', team.startXI], ['מחליפים', team.substitutes]]) {
          const heading = document.createElement('strong');
          heading.textContent = label;
          section.append(heading, playerList((group ?? []).map(row => row.player)));
        }
        $('match-lineups').append(section);
      }
      $('lineup-message').textContent = teams.length ? '' : 'עדיין אין הרכבים שפורסמו למשחק הזה אצל הספק.';
    } catch (error) {
      if (request === version && selected === $('match').value) $('lineup-message').textContent = error.message;
    }
  }

  /** מציירת 11 משבצות לפי המערך. הסדר על המגרש הוא מההתקפה אל השוער. */
  function paintFormation() {
    const lines = $('formation').value.split('-').map(Number).reverse();
    lines.push(1);
    let number = 11;
    $('pitch').replaceChildren(...lines.map(count => {
      const row = document.createElement('div');
      row.className = 'pitch-row';
      for (let i = 0; i < count; i++) {
        const position = document.createElement('span');
        position.className = 'position';
        position.textContent = number === 1 ? 'שוער' : number;
        number--;
        row.append(position);
      }
      return row;
    }));
  }

  /** טוענת את הקטלוג המקומי בלי לצרוך מכסה אצל הספק. */
  async function initialize() {
    try {
      const catalog = await read('catalog');
      options('league', catalog.leagues, item => item.id, item => item.name);
      options('formation', catalog.formations, item => item, item => item);
      $('season').value = catalog.season;
      $('season').max = new Date().getFullYear();
      $('load').disabled = false;
      paintFormation();
    } catch (error) { $('message').textContent = error.message; }
  }

  $('filters').addEventListener('submit', loadLeague);
  $('match').addEventListener('change', selectMatch);
  $('team').addEventListener('change', () => {
    $('players').replaceChildren();
    $('squad-message').textContent = '';
  });
  $('lineups').addEventListener('click', loadLineups);
  $('squad').addEventListener('click', loadSquad);
  $('formation').addEventListener('change', paintFormation);
  initialize();
})();
