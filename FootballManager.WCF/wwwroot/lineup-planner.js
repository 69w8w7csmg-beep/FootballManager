// מטרה: מציג עורך לתוכנית הרכב של מאמן עבור משחק מהקטלוג.
// מי משתמש: חשבון Manager שמחובר לאתר.
// פעולות: פתיחת תוכנית, בחירת פותחים וספסל, חילוף, שמירה ובקשת המלצה.
// תלויות: football.js ונתיבי plans בשרת.
(() => {
  const $ = id => document.getElementById(id);
  let match = null;
  let squad = [];
  let validFormations = [];
  let path = '';
  let version = 0;
  let busy = false;

  /** קוראת לשרת הפרויקט ומשתמשת בהודעת ה־HTTP שהשרת החזיר. */
  async function request(url, method = 'GET', data) {
    const options = { method };
    if (data) {
      options.headers = { 'Content-Type': 'application/json' };
      options.body = JSON.stringify(data);
    }
    const response = await fetch(url, options);
    const body = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(body.message || `שגיאה מהשרת (${response.status}).`);
    return body;
  }

  /** מחליפה את קבוצות הבחירה למשחק הנבחר ומסירה תצוגה של משחק קודם. */
  function setMatch(next) {
    match = next ?? null;
    version++;
    path = '';
    $('plan-editor').hidden = true;
    $('plan-status').textContent = '';
    $('plan-answer').textContent = '';
    const teams = match ? [match.teams.home, match.teams.away] : [];
    $('plan-team').replaceChildren(...teams.map(team => new Option(team.name, team.id)));
    $('plan-team').disabled = !match;
    $('plan-open').disabled = !match || busy;
  }

  /** יוצרת שדה בחירה מתוך הסגל. השרת בודק שוב שחקנים כפולים ושוער. */
  function playerSelect(title, value, isKeeper) {
    const label = document.createElement('label');
    label.textContent = title;
    const select = document.createElement('select');
    const choices = isKeeper ? squad.filter(isGoalkeeper) : squad;
    select.append(new Option('בחר שחקן', ''), ...choices.map(player =>
      new Option(`${player.name} · ${player.position ?? ''}`, player.id)));
    select.value = value || '';
    select.addEventListener('change', updateActions);
    label.append(select);
    return label;
  }

  /** מזהה שוער מתוך שם העמדה שהספק מחזיר. */
  function isGoalkeeper(player) {
    return String(player.position).toLowerCase() === 'goalkeeper';
  }

  /** מציגה את שמות העמדות לפי החלוקה של המערך. */
  function renderStarters(values) {
    const lines = $('plan-formation').value.split('-').map(Number);
    const names = ['שוער'];
    lines.forEach((count, line) => {
      for (let i = 1; i <= count; i++) {
        const title = line === 0 ? 'הגנה' : line === lines.length - 1 ? 'התקפה' : `קישור ${line}`;
        names.push(`${title} · ${i}`);
      }
    });
    $('plan-starters').replaceChildren(...names.map((name, i) => playerSelect(name, values[i], i === 0)));
  }

  /** מחזירה את הנתונים כפי שהם נראים בעורך. */
  function values(id) {
    return Array.from($(id).querySelectorAll('select'), field => Number(field.value));
  }

  /** מאפשרת פעולה רק כשיש הרכב מלא וללא כפילויות. */
  function updateActions() {
    const starters = values('plan-starters');
    const bench = values('plan-bench');
    const selected = [...starters, ...bench].filter(Boolean);
    const valid = starters.length === 11 && starters.every(Boolean) && new Set(selected).size === selected.length;
    $('plan-save').disabled = $('plan-advice').disabled = busy || !valid;
    const addSwapOptions = (id, group) => {
      const selectedIndex = $(id).value;
      $(id).replaceChildren(new Option('בחר', ''), ...group.flatMap((playerId, index) => {
        const player = squad.find(item => item.id === playerId);
        return player ? [new Option(player.name, index)] : [];
      }));
      $(id).value = selectedIndex;
      if ($(id).selectedIndex < 0) $(id).selectedIndex = 0;
    };
    addSwapOptions('swap-out', starters);
    addSwapOptions('swap-in', bench);
    $('plan-swap').disabled = busy || !bench.some(Boolean);
  }

  /** פותחת תוכנית מהשרת וטוענת גם סגל עדכני לקבוצה שבמשחק. */
  async function openPlan() {
    if (busy || !match || !$('plan-team').value) return;
    const team = $('plan-team').value;
    const currentVersion = ++version;
    const resource = `/api/football/plans/${match.fixture.id}/${team}`;
    path = '';
    $('plan-editor').hidden = true;
    $('plan-answer').textContent = '';
    $('plan-status').textContent = 'טוען תוכנית וסגל…';
    busy = true;
    $('plan-open').disabled = true;
    try {
      const [{ plan }, groups] = await Promise.all([request(resource), request('/api/football/squad/' + team)]);
      if (currentVersion !== version) return;
      squad = groups[0]?.players ?? [];
      if (!squad.length) {
        $('plan-status').textContent = 'לספק אין סגל זמין לקבוצה.';
        return;
      }
      path = resource;
      $('plan-formation').value = validFormations.includes(plan?.formation) ? plan.formation : validFormations[0];
      renderStarters(plan?.starters ?? []);
      $('plan-bench').replaceChildren(...Array.from({ length: 12 }, (_, i) =>
        playerSelect(`מחליף ${i + 1}`, plan?.bench?.[i], false)));
      $('plan-editor').hidden = false;
      $('plan-status').textContent = plan ? 'התוכנית השמורה נטענה.' : 'בחר 11 פותחים ומחליפים.';
    } catch (error) {
      if (currentVersion === version) $('plan-status').textContent = error.message;
    } finally {
      busy = false;
      $('plan-open').disabled = !match;
      updateActions();
    }
  }

  /** מעבירה פותח לספסל ומחליף לעמדה. השינוי נשמר רק בלחיצה על שמירה. */
  function swapPlayers() {
    const out = $('swap-out').value;
    const incoming = $('swap-in').value;
    if (out === '' || incoming === '' || busy) {
      $('plan-status').textContent = 'בחר שחקן שיוצא ושחקן שנכנס.';
      return;
    }
    const starter = $('plan-starters').querySelectorAll('select')[Number(out)];
    const bench = $('plan-bench').querySelectorAll('select')[Number(incoming)];
    if (Number(out) === 0 && !squad.some(p => p.id === Number(bench.value) && isGoalkeeper(p))) {
      $('plan-status').textContent = 'אפשר להחליף שוער רק בשוער מהספסל.';
      return;
    }
    const previous = starter.value;
    starter.value = bench.value;
    bench.value = previous;
    updateActions();
    $('plan-status').textContent = 'החילוף מוכן. שמור את התוכנית כדי לעדכן את החשבון.';
  }

  /** שולחת את אותה תוכנית לשמירה או לבינה. המלצה מוצגת כטקסט בלבד. */
  async function submit(advice) {
    if (!path || busy || $('plan-save').disabled) return;
    const currentVersion = version;
    const plan = { formation: $('plan-formation').value, starters: values('plan-starters'), bench: values('plan-bench').filter(Boolean) };
    busy = true;
    $('plan-open').disabled = true;
    updateActions();
    $('plan-status').textContent = advice ? 'מכין המלצה מנתוני המשחק והסגל…' : 'שומר תוכנית…';
    $('plan-answer').textContent = '';
    try {
      const result = await request(path + (advice ? '/advice' : ''), advice ? 'POST' : 'PUT', plan);
      if (currentVersion !== version) return;
      $('plan-status').textContent = result.message || '';
      $('plan-answer').textContent = result.advice || '';
    } catch (error) {
      if (currentVersion === version) $('plan-status').textContent = error.message;
    } finally {
      busy = false;
      $('plan-open').disabled = !match;
      updateActions();
    }
  }

  /** בודקת איזו תצוגה לפתוח לפי התפקיד שהשרת מחזיר. */
  async function initialize() {
    try {
      const session = await request('/api/session');
      if (session.role !== 'Manager') return;
      const catalog = await request('/api/football/catalog');
      validFormations = catalog.formations;
      $('plan-formation').replaceChildren(...validFormations.map(value => new Option(value, value)));
      $('plan-account').textContent = 'התוכנית פרטית לחשבון המאמן. אפשר לערוך אותה ולבקש ניתוח מהשרת.';
      $('plan-controls').hidden = false;
    } catch { /* התצוגה הציבורית נשארת זמינה גם ללא התחברות. */ }
  }

  document.addEventListener('football-match-selected', event => setMatch(event.detail));
  $('plan-team').addEventListener('change', () => {
    version++; path = '';
    $('plan-editor').hidden = true;
    $('plan-status').textContent = '';
    $('plan-answer').textContent = '';
  });
  $('plan-open').addEventListener('click', openPlan);
  $('plan-formation').addEventListener('change', () => {
    renderStarters(values('plan-starters')); updateActions();
  });
  $('plan-swap').addEventListener('click', swapPlayers);
  $('plan-save').addEventListener('click', () => submit(false));
  $('plan-advice').addEventListener('click', () => submit(true));
  initialize();
})();
