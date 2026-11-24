// מטרה: מחבר את דף הבית, החשבון והדשבורדים באתר לשירות.
// מי משתמש: index.html ודפדפן המשתמש.
// פעולות: בדיקת קלט, התחברות, טעינת תוכן וניהול משתמשים.
// תלויות: FootballManagerWebApi ו־DOM.
(() => {
  const $ = (selector) => document.querySelector(selector);
  const state = { user: null, players: [], teams: [], adminUsers: [], adminSection: 'users' };
  const roleLabels = { Guest: 'אורח', Player: 'שחקן', Manager: 'מאמן', Admin: 'מנהל מערכת' };
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);

  /** שולח בקשה לנתיב שהאתר מארח ומחזיר גוף JSON, אם השרת החזיר אותו. */
  async function api(url, options = {}) {
    const response = await fetch(url, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }, ...options });
    const payload = response.status === 204 ? {} : await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(payload.message || `שגיאת שירות (${response.status})`);
    return payload;
  }

  /** מציג הודעת מצב בטופס או בדשבורד ומסמן שגיאה כשנדרש. */
  function setMessage(selector, message, error = false) {
    const el = $(selector); if (!el) return;
    el.textContent = message || ''; el.classList.toggle('error', error);
  }

  /** מחליף את התצוגה הפעילה וטוען נתונים למסך שדורש אותם. */
  function showView(name) {
    document.querySelectorAll('.view').forEach((view) => view.classList.toggle('active', view.id === `view-${name}`));
    document.querySelectorAll('.nav-link').forEach((button) => button.classList.toggle('active', button.dataset.view === name));
    if (name === 'players') loadPlayers();
    if (name === 'teams') loadTeams();
    if (name === 'dashboard') loadDashboard();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /** מציג את מצב החשבון והתפקיד שקיבל הלקוח מן השרת. */
  function updateAccountBar() {
    const loggedIn = Boolean(state.user);
    $('#account-label').textContent = loggedIn ? `${state.user.username} · ${roleLabels[state.user.role] || state.user.role}` : 'אורח';
    $('#login-open').classList.toggle('hidden', loggedIn);
    $('#register-open').classList.toggle('hidden', loggedIn);
    $('#logout').classList.toggle('hidden', !loggedIn);
    $('#dashboard-open').classList.toggle('hidden', !loggedIn);
  }

  /** בונה כרטיס משדות שחקן. פעולת המועדפים מוצגת לפי המסך. */
  function playerCard(player, allowFavorite = false) {
    const favorite = allowFavorite ? `<button class="mini-button favorite-button" data-favorite="${player.playerId}">＋ הוסף למועדפים</button>` : '';
    return `<article class="player-card"><span class="rating">${esc(player.rating)}</span><h3>${esc(player.firstName)} ${esc(player.lastName)}</h3><div class="player-meta">${esc(player.position)} · גיל ${esc(player.age)}</div><div class="player-stats"><span>מהירות ${esc(player.speed)}</span><span>מסירה ${esc(player.passing)}</span><span>הגנה ${esc(player.defense)}</span></div>${favorite}</article>`;
  }

  /** קורא שחקנים מהשרת לפי טקסט ועמדה ומרענן את רשימת הכרטיסים. */
  async function loadPlayers() {
    const box = $('#players-list'); box.innerHTML = '<p class="muted">טוען נתונים…</p>';
    const params = new URLSearchParams();
    if ($('#player-search').value.trim()) params.set('searchText', $('#player-search').value.trim());
    if ($('#position-filter').value) params.set('position', $('#position-filter').value);
    try {
      const result = await api(`/api/players?${params}`); state.players = result.players || [];
      box.innerHTML = state.players.length ? state.players.map((player) => playerCard(player, state.user?.role === 'Player')).join('') : '<p class="muted">לא נמצאו שחקנים. בדוק שנתוני ההדגמה נוספו למסד.</p>';
      setMessage('#players-message', result.message);
    } catch (error) { box.innerHTML = ''; setMessage('#players-message', error.message, true); }
  }

  /** טוען את הקבוצות ומציג אותן לבחירה. */
  async function loadTeams() {
    const box = $('#teams-list'); box.innerHTML = '<p class="muted">טוען נתונים…</p>';
    try {
      const result = await api('/api/teams'); state.teams = result.teams || [];
      box.innerHTML = state.teams.length ? state.teams.map((team) => `<article class="team-card" data-team="${team.teamId}"><span class="eyebrow">${esc(team.country)}</span><h3>${esc(team.name)}</h3><p>${esc(team.league)} · תקציב ${Number(team.budget).toLocaleString('he-IL')}</p></article>`).join('') : '<p class="muted">אין קבוצות במסד. הפעל הכנת נתוני ההדגמה.</p>';
      setMessage('#teams-message', result.message);
    } catch (error) { box.innerHTML = ''; setMessage('#teams-message', error.message, true); }
  }

  /** מקבל מזהה קבוצה ומציג את הסגל שהשירות מחזיר. */
  async function loadRoster(teamId) {
    const team = state.teams.find((item) => item.teamId === Number(teamId));
    if (!team) return;
    $('#roster-title').textContent = team.name;
    $('#roster-panel').classList.remove('hidden');
    const result = await api(`/api/teams/${teamId}/roster`);
    $('#roster-list').innerHTML = (result.players || []).length ? result.players.map((player) => playerCard(player, false)).join('') : '<p class="muted">אין שחקנים בסגל הפעיל של הקבוצה.</p>';
  }

  /** טוען את החלק האישי לפי סוג החשבון הפעיל. */
  async function loadDashboard() {
    if (!state.user) return showView('home');
    $('#dashboard-title').textContent = `שלום, ${state.user.username}`;
    $('#dashboard-subtitle').textContent = `מחובר בתפקיד ${roleLabels[state.user.role] || state.user.role}. הפעולות נבדקות גם בשירות.`;
    ['player', 'manager', 'admin'].forEach((role) => $(`#${role}-dashboard`).classList.toggle('hidden', state.user.role.toLowerCase() !== role));
    if (state.user.role === 'Player') await loadFavorites();
    if (state.user.role === 'Manager') await loadManagerTools();
    if (state.user.role === 'Admin') await loadAdminUsers();
  }

  /** טוען מהשרת רק מועדפים של החשבון בעוגיית ההתחברות. */
  async function loadFavorites() {
    try {
      const result = await api('/api/favorites');
      $('#favorites-list').innerHTML = (result.players || []).length ? result.players.map((player) => `<div class="compact-row"><span>${esc(player.firstName)} ${esc(player.lastName)}<small>${esc(player.position)} · דירוג ${esc(player.rating)}</small></span><button class="mini-button" data-remove-favorite="${player.playerId}">הסר</button></div>`).join('') : '<p class="muted">עדיין לא שמרת שחקנים.</p>';
    } catch (error) { $('#favorites-list').innerHTML = `<p class="muted">${esc(error.message)}</p>`; }
  }

  /** טוען שחקנים וקבוצות לפקדי המאמן. */
  async function loadManagerTools() {
    try {
      const [playerResult, teamResult] = await Promise.all([api('/api/players'), api('/api/teams')]);
      state.players = playerResult.players || []; state.teams = teamResult.teams || [];
      $('#manager-players-list').innerHTML = state.players.map((player) => `<div class="compact-row"><span>${esc(player.firstName)} ${esc(player.lastName)}<small>${esc(player.position)} · ${esc(player.rating)}</small></span><button class="mini-button danger" data-delete-player="${player.playerId}">מחק</button></div>`).join('') || '<p class="muted">אין שחקנים.</p>';
      $('#manager-player-select').innerHTML = state.players.map((player) => `<option value="${player.playerId}">${esc(player.firstName)} ${esc(player.lastName)}</option>`).join('');
      $('#manager-team-select').innerHTML = state.teams.map((team) => `<option value="${team.teamId}">${esc(team.name)}</option>`).join('');
    } catch (error) { setMessage('#manager-message', error.message, true); }
  }

  /** מסדר את תצוגת ניהול המשתמשים לפי הכרטיס שבחר האדמין. */
  function renderUsers() {
    const users = state.adminUsers;
    $('#admin-users-count').textContent = `${users.length} חשבונות במערכת`;
    $('#admin-blocked-count').textContent = `${users.filter((user) => user.isBlocked).length} חסומים`;
    $('#admin-stat-summary').textContent = `${users.filter((user) => !user.isBlocked).length} פעילים`;
    const counts = { Player: 0, Manager: 0, Admin: 0 };
    users.forEach((user) => { if (Object.hasOwn(counts, user.role)) counts[user.role]++; });
    const title = { users: 'משתמשי המערכת', control: 'חשבונות בבקרה', stats: 'סטטיסטיקת חשבונות', communication: 'ספר המשתמשים' };
    const subtitle = { users: 'פעולות ניהול נשמרות במסד דרך WCF', control: 'רשימת החשבונות החסומים והפעילים', stats: 'ספירה חיה מטבלת Users', communication: 'שמות משתמש ותפקידים מתוך Users' };
    $('#admin-section-title').textContent = title[state.adminSection];
    $('#admin-section-eyebrow').textContent = subtitle[state.adminSection];
    $('#admin-create-area').classList.toggle('hidden', state.adminSection !== 'users');
    if (state.adminSection === 'stats') {
      $('#admin-section-body').innerHTML = `<div class="stats-grid">${[['כל החשבונות', users.length], ['פעילים', users.filter((user) => !user.isBlocked).length], ['שחקנים', counts.Player], ['מאמנים', counts.Manager], ['מנהלי מערכת', counts.Admin], ['חסומים', users.filter((user) => user.isBlocked).length]].map(([label, value]) => `<div class="stat-box"><b>${value}</b><span>${label}</span></div>`).join('')}</div>`;
      return;
    }
    const visible = state.adminSection === 'control' ? users.filter((user) => user.isBlocked) : users;
    if (state.adminSection === 'communication') {
      $('#admin-section-body').innerHTML = `<p class="muted">ספר המשתמשים מציג את המידע הקיים בטבלת Users. אין בטבלה כתובות דוא״ל או הודעות, ולכן אין כאן שליחת הודעות.</p>${userTable(visible, false, true)}`;
      return;
    }
    $('#admin-section-body').innerHTML = userTable(visible, state.adminSection === 'users', false);
  }

  /** בונה טבלה מרשימת משתמשים וכולל פעולות לפי מצב התצוגה. */
  function userTable(users, actions, copy) {
    if (!users.length) return '<p class="muted">אין חשבונות להצגה.</p>';
    return `<div class="table-scroll"><table class="data-table"><thead><tr><th>שם משתמש</th><th>תפקיד</th><th>מצב</th><th>${actions ? 'פעולות' : (copy ? 'כלי עזר' : '')}</th></tr></thead><tbody>${users.map((user) => `<tr><td>${esc(user.username)}</td><td>${esc(roleLabels[user.role] || user.role)}</td><td><span class="badge ${user.isBlocked ? 'blocked' : ''}">${user.isBlocked ? 'חסום' : 'פעיל'}</span></td><td>${actions ? `<div class="table-actions"><select class="mini-role" data-user-role="${user.userId}" aria-label="תפקיד חדש"><option value="Player" ${user.role === 'Player' ? 'selected' : ''}>Player</option><option value="Manager" ${user.role === 'Manager' ? 'selected' : ''}>Manager</option><option value="Admin" ${user.role === 'Admin' ? 'selected' : ''}>Admin</option></select><button class="mini-button" data-save-role="${user.userId}">עדכן</button><button class="mini-button" data-toggle-block="${user.userId}" data-blocked="${user.isBlocked}">${user.isBlocked ? 'שחרר' : 'חסום'}</button><button class="mini-button danger" data-delete-user="${user.userId}">מחק</button></div>` : copy ? `<button class="mini-button" data-copy-user="${esc(user.username)}">העתק שם</button>` : ''}</td></tr>`).join('')}</tbody></table></div>`;
  }

  /** מבקש רשימת חשבונות מהשרת. הפעולה מוגבלת לאדמין. */
  async function loadAdminUsers() {
    try {
      const result = await api('/api/admin/users');
      if (!result.succeeded) { $('#admin-section-body').innerHTML = `<p class="muted">${esc(result.message)}</p>`; return; }
      state.adminUsers = result.users || []; renderUsers();
    } catch (error) { setMessage('#dashboard-message', error.message, true); }
  }

  /** מפעיל את כפתור הכניסה רק כששם החשבון והסיסמה תקינים. */
  function validateLogin() {
    $('#login-submit').disabled = !($('#login-username').value.trim().length >= 3 && $('#login-password').value.length >= 8);
  }

  /** בודק את שדות ההרשמה ואת התאמת אימות הסיסמה לפני השליחה. */
  function validateRegistration() {
    const username = $('#register-username').value.trim(); const password = $('#register-password').value; const confirm = $('#register-confirm').value;
    const usernameValid = /^[A-Za-z0-9_]{3,30}$/.test(username);
    const passwordValid = password.length >= 8 && password.length <= 128;
    const confirmValid = confirm.length > 0 && confirm === password;
    $('#register-user-hint').classList.toggle('error', username.length > 0 && !usernameValid);
    $('#register-password-hint').classList.toggle('error', password.length > 0 && !passwordValid);
    $('#register-confirm-hint').textContent = confirm.length && !confirmValid ? 'הסיסמאות אינן תואמות.' : '';
    $('#register-confirm-hint').classList.toggle('error', confirm.length > 0 && !confirmValid);
    $('#register-submit').disabled = !(usernameValid && passwordValid && confirmValid);
  }

  document.addEventListener('click', async (event) => {
    const viewButton = event.target.closest('[data-view]');
    if (viewButton) { showView(viewButton.dataset.view); return; }
    if (event.target.closest('#login-open')) { showView('login'); return; }
    if (event.target.closest('#register-open,#hero-register,#switch-register')) { showView('register'); return; }
    if (event.target.closest('#switch-login')) { showView('login'); return; }
    const eye = event.target.closest('[data-toggle-password]');
    if (eye) { const input = $(`#${eye.dataset.togglePassword}`); input.type = input.type === 'password' ? 'text' : 'password'; return; }
    if (event.target.closest('#player-search-button')) { loadPlayers(); return; }
    const teamCard = event.target.closest('[data-team]');
    if (teamCard) { try { await loadRoster(teamCard.dataset.team); } catch (error) { setMessage('#teams-message', error.message, true); } return; }
    const favorite = event.target.closest('[data-favorite]');
    if (favorite) {
      if (!state.user) return showView('login');
      const result = await api(`/api/favorites/${favorite.dataset.favorite}`, { method: 'POST', body: '{}' });
      setMessage('#players-message', result.message, !result.succeeded); if (result.succeeded) favorite.textContent = '✓ נוסף'; return;
    }
    const removeFavorite = event.target.closest('[data-remove-favorite]');
    if (removeFavorite) { const result = await api(`/api/favorites/${removeFavorite.dataset.removeFavorite}`, { method: 'DELETE' }); setMessage('#dashboard-message', result.message, !result.succeeded); await loadFavorites(); return; }
    const adminTab = event.target.closest('[data-admin-section]');
    if (adminTab) { state.adminSection = adminTab.dataset.adminSection; document.querySelectorAll('.admin-card').forEach((card) => card.classList.toggle('selected', card === adminTab)); renderUsers(); return; }
    if (event.target.closest('#admin-refresh')) { await loadAdminUsers(); return; }
    const block = event.target.closest('[data-toggle-block]');
    if (block) { const result = await api(`/api/admin/users/${block.dataset.toggleBlock}/block`, { method: 'POST', body: JSON.stringify({ isBlocked: block.dataset.blocked !== 'true' }) }); setMessage('#admin-message', result.message, !result.succeeded); await loadAdminUsers(); return; }
    const saveRole = event.target.closest('[data-save-role]');
    if (saveRole) { const role = document.querySelector(`[data-user-role="${saveRole.dataset.saveRole}"]`).value; const result = await api(`/api/admin/users/${saveRole.dataset.saveRole}/role`, { method: 'POST', body: JSON.stringify({ role }) }); setMessage('#admin-message', result.message, !result.succeeded); await loadAdminUsers(); return; }
    const deleteUser = event.target.closest('[data-delete-user]');
    if (deleteUser) { const user = state.adminUsers.find((item) => item.userId === Number(deleteUser.dataset.deleteUser)); if (!confirm(`למחוק את החשבון ${user?.username || ''}?`)) return; const result = await api(`/api/admin/users/${deleteUser.dataset.deleteUser}`, { method: 'DELETE' }); setMessage('#admin-message', result.message, !result.succeeded); await loadAdminUsers(); return; }
    const deletePlayer = event.target.closest('[data-delete-player]');
    if (deletePlayer) { if (!confirm('למחוק את רשומת השחקן?')) return; const result = await api(`/api/manager/players/${deletePlayer.dataset.deletePlayer}`, { method: 'DELETE' }); setMessage('#manager-message', result.message, !result.succeeded); await loadManagerTools(); return; }
    const copyUser = event.target.closest('[data-copy-user]');
    if (copyUser) { await navigator.clipboard?.writeText(copyUser.dataset.copyUser); copyUser.textContent = 'הועתק'; return; }
    if (event.target.closest('#manager-assign')) {
      const team = $('#manager-team-select').value, player = $('#manager-player-select').value;
      if (!team || !player) return setMessage('#manager-message', 'בחר קבוצה ושחקן.', true);
      const result = await api(`/api/manager/teams/${team}/players/${player}`, { method: 'POST', body: '{}' }); setMessage('#manager-message', result.message, !result.succeeded); return;
    }
    if (event.target.closest('#logout')) {
      await api('/api/auth/logout', { method: 'POST', body: '{}' }); state.user = null; updateAccountBar(); showView('home');
    }
  });

  $('#login-username').addEventListener('input', validateLogin); $('#login-password').addEventListener('input', validateLogin);
  $('#register-username').addEventListener('input', validateRegistration); $('#register-password').addEventListener('input', validateRegistration); $('#register-confirm').addEventListener('input', validateRegistration);
  /** מעדכן את לחצני טופסי הניהול לפי התווים, האורך והטווחים המותרים. */
  function validateManagementForms() {
    const admin = $('#admin-create-form');
    const username = $('#admin-new-username').value.trim();
    const password = $('#admin-new-password').value;
    admin.querySelector('[type=submit]').disabled = !/^[a-zA-Z0-9_]{3,30}$/.test(username) ||
      password.length < 8 || password.length > 128;
    const manager = $('#manager-player-form');
    manager.querySelector('[type=submit]').disabled = !manager.checkValidity() ||
      !['#manager-first', '#manager-last', '#manager-age', '#manager-rating'].every(selector => $(selector).value.trim());
  }

  $('#admin-create-form').addEventListener('input', validateManagementForms);
  $('#manager-player-form').addEventListener('input', validateManagementForms);
  validateManagementForms();
  $('#player-search').addEventListener('keydown', (event) => { if (event.key === 'Enter') loadPlayers(); });
  $('#position-filter').addEventListener('change', loadPlayers);

  $('#login-form').addEventListener('submit', async (event) => {
    event.preventDefault(); $('#login-submit').disabled = true;
    try {
      const result = await api('/api/auth/login', { method: 'POST', body: JSON.stringify({ username: $('#login-username').value.trim(), password: $('#login-password').value }) });
      if (!result.succeeded) { setMessage('#login-message', result.message, true); return; }
      state.user = { username: result.username, role: result.role }; updateAccountBar(); showView('dashboard');
    } catch (error) { setMessage('#login-message', error.message, true); }
    finally { validateLogin(); }
  });

  $('#register-form').addEventListener('submit', async (event) => {
    event.preventDefault(); $('#register-submit').disabled = true;
    try {
      const result = await api('/api/auth/register', { method: 'POST', body: JSON.stringify({ username: $('#register-username').value.trim(), password: $('#register-password').value }) });
      setMessage('#register-message', result.message, !result.succeeded);
      if (result.succeeded) { $('#login-username').value = $('#register-username').value.trim(); $('#login-password').value = ''; showView('login'); setMessage('#login-message', 'החשבון נוצר. אפשר להתחבר כעת.'); validateLogin(); }
    } catch (error) { setMessage('#register-message', error.message, true); }
    finally { validateRegistration(); }
  });

  $('#admin-create-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    try {
      const result = await api('/api/admin/users', { method: 'POST', body: JSON.stringify({ username: $('#admin-new-username').value.trim(), password: $('#admin-new-password').value, role: $('#admin-new-role').value }) });
      setMessage('#admin-message', result.message, !result.succeeded); if (result.succeeded) { event.target.reset(); await loadAdminUsers(); }
    } catch (error) { setMessage('#admin-message', error.message, true); }
    finally { validateManagementForms(); }
  });

  $('#manager-player-form').addEventListener('submit', async (event) => {
    event.preventDefault();
    try {
      const result = await api('/api/manager/players', { method: 'POST', body: JSON.stringify({ playerId: 0, firstName: $('#manager-first').value.trim(), lastName: $('#manager-last').value.trim(), age: Number($('#manager-age').value), position: $('#manager-position').value, rating: Number($('#manager-rating').value), speed: 70, shooting: 65, passing: 70, defense: 65 }) });
      setMessage('#manager-message', result.message, !result.succeeded); if (result.succeeded) { event.target.reset(); await loadManagerTools(); }
    } catch (error) { setMessage('#manager-message', error.message, true); }
    finally { validateManagementForms(); }
  });

  /** בודק אם קיימת התחברות מהדפדפן ומעדכן את מצב המסך. */
  async function initialize() {
    try {
      const status = await api('/api/status'); $('#service-status').innerHTML = `<i class="live-dot"></i> ${status.databaseReady ? 'WCF ו־Access מחוברים' : 'המסד אינו זמין'}`;
      if (!status.databaseReady) $('#service-status').classList.add('offline');
    } catch { $('#service-status').textContent = 'השירות אינו זמין'; $('#service-status').classList.add('offline'); }
    try {
      const session = await api('/api/session');
      if (session.succeeded) { state.user = { username: session.username, role: session.role }; updateAccountBar(); }
    } catch { /* אורח ללא חשבון פעיל */ }
  }
  initialize();
})();
