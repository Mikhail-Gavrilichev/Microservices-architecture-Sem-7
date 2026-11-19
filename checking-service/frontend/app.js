'use strict';

/* =====================================================================
   Вердикт — рабочее место жюри.
   Одна работа = один экран: ответ участника, эталон, две кнопки.
   Никакой настоящей авторизации: имя и роль хранятся только в браузере.
   ===================================================================== */

const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => Array.from(root.querySelectorAll(selector));
const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (ch) => (
  { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));

const LS = {
  user: 'verdict.user',
  lastName: 'verdict.lastName',
  decisions: 'verdict.decisions',
  position: 'verdict.position',
  demo: 'verdict.demoItems',
};

const ROLE_LABEL = {
  participant: 'Участник',
  judge: 'Жюри',
  senior: 'Старшее жюри',
  organizer: 'Организатор',
};

const STATUS_LABEL = {
  Assigned: 'назначена', Accepted: 'принята', Escalated: 'передана старшему',
  Queued: 'в очереди', Completed: 'проверена', Declined: 'отказ',
  Created: 'создано', PhotoAttached: 'фото загружено', PhotoInvalid: 'фото отклонено',
  AcceptedForChecking: 'на проверке',
};

const state = {
  user: null,
  judgeId: null,        // соответствующий член жюри на сервере (для реальных работ)
  judges: [],
  catalog: [],
  submissions: [],
  queue: [],
  results: [],
  myChecks: [],
  demo: [],             // 100 тестовых работ (только localStorage)
  decisions: {},        // key работы -> 'ok' | 'bad'
  pool: [],             // работы текущей проверки
  idx: 0,
  finished: false,
  previews: {},         // submissionId -> data URL только что загруженного файла
  pendingPhoto: null,   // data URL выбранного, но ещё не загруженного файла
  serverOk: true,       // доступен ли backend (иначе работают только тестовые работы)
};

/* --------------------------------------------------------------- утилиты */

let toastTimer;
function toast(text, error = false) {
  const node = $('#toast');
  node.textContent = text;
  node.className = 'show' + (error ? ' error' : '');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { node.className = ''; }, 4000);
}

const readLS = (key, fallback) => {
  try {
    const raw = localStorage.getItem(key);
    return raw === null ? fallback : JSON.parse(raw);
  } catch { return fallback; }
};
const writeLS = (key, value) => {
  try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* приватный режим */ }
};

async function api(path, method = 'GET', body) {
  const isForm = body instanceof FormData;
  const response = await fetch('/api' + path, {
    method,
    headers: isForm || body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: isForm ? body : body === undefined ? undefined : JSON.stringify(body),
  });
  let data = null;
  try { data = await response.json(); } catch { data = null; }
  if (!response.ok) {
    throw new Error(data?.error
      || (data?.errors ? Object.values(data.errors).flat().join(' ') : '')
      || data?.title || `HTTP ${response.status}`);
  }
  return data;
}

/* --------------------------------------------------------------- вход */

function startSession(user) {
  state.user = user;
  writeLS(LS.user, user);
  writeLS(LS.lastName, user.name);
  enterApp();
}

function logout() {
  const previousName = state.user?.name || '';
  state.user = null;
  localStorage.removeItem(LS.user);
  state.pool = [];
  state.idx = 0;
  state.finished = false;
  state.judgeId = null;
  $('#appView').hidden = true;
  $('#loginView').hidden = false;
  const form = $('#loginForm');
  form.elements.name.value = readLS(LS.lastName, '') || previousName;
  form.elements.name.focus();
}

function enterApp() {
  const user = state.user;
  $('#loginView').hidden = true;
  $('#appView').hidden = false;
  $('#whoText').textContent = `${user.name} · ${ROLE_LABEL[user.role] || user.role}`;
  if (user.role === 'participant') loadParticipant();
  else if (user.role === 'organizer') loadOrganizer();
  else loadCheck();
}

const VIEWS = { check: '#checkView', participant: '#participantView', organizer: '#organizerView' };
function showView(name) {
  Object.entries(VIEWS).forEach(([key, selector]) => { $(selector).hidden = key !== name; });
  $('#progressBox').hidden = name !== 'check';
}

/* =====================================================================
   ЭКРАН ПРОВЕРКИ
   ===================================================================== */

async function loadCheck() {
  showView('check');
  state.myChecks = [];
  state.queue = [];
  state.finished = false;
  rebuildPool();
  restorePosition();
  renderCheck();

  try {
    state.judges = await api('/judges');
    const role = state.user.role === 'senior' ? 'SeniorJudge' : 'Judge';
    const name = state.user.name.trim().toLowerCase();
    const judge = state.judges.find((j) => j.role === role && j.name.trim().toLowerCase() === name);
    state.judgeId = judge ? judge.id : null;
    state.serverOk = true;
    if (state.judgeId) await fetchServerWork();
  } catch (error) {
    state.serverOk = false;
    toast('Сервер недоступен: ' + error.message, true);
  }
  renderJudgeNotice();
  renderCheck();
}

/* Обе роли получают только собственные назначения. */
async function fetchServerWork() {
  if (!state.judgeId) return 0;
  const checks = await api(`/judges/${state.judgeId}/checks`);
  state.myChecks = checks.filter((c) => c.status !== 'Completed' && c.status !== 'Declined');
  state.queue = [];
  // Назначение выполняет backend; свободная очередь доступна организатору.
  return mergeServerItems();
}

function checkToItem(check) {
  return {
    key: 'check-' + check.id,
    kind: 'server',
    id: check.id,
    check,
    taskId: check.taskId,
    formId: check.formId,
    gameId: check.gameId,
    taskCondition: check.taskCondition,
    answer: check.answer,
    correctAnswer: check.correctAnswer,
    photoUrl: check.photoUrl,
    status: check.status,
  };
}

function serverItems() {
  return [...state.myChecks, ...state.queue].map(checkToItem);
}

/* Добавляет новые серверные работы в пул, сохраняя уже показанные (и проверенные). */
function mergeServerItems() {
  const index = new Map(state.pool.map((item, at) => [item.key, at]));
  let added = 0;
  for (const item of serverItems()) {
    const at = index.get(item.key);
    if (at === undefined) {
      state.pool.push(item);
      index.set(item.key, state.pool.length - 1);
      added += 1;
    } else {
      Object.assign(state.pool[at], item);
    }
  }
  return added;
}

function rebuildPool() {
  state.pool = state.demo.map((demo) => ({
    key: 'demo-' + demo.id,
    kind: 'demo',
    id: demo.id,
    taskId: demo.taskId,
    formId: demo.formId,
    gameId: demo.gameId,
    taskCondition: demo.taskCondition,
    answer: demo.answer,
    correctAnswer: demo.correctAnswer,
    photoUrl: demo.photoUrl || null,
    status: 'demo',
  }));
  mergeServerItems();
}

function restorePosition() {
  const saved = readLS(LS.position, null);
  if (saved && typeof saved.key === 'string') {
    const at = state.pool.findIndex((item) => item.key === saved.key);
    if (at >= 0) { state.idx = at; state.finished = false; return; }
  }
  const firstOpen = state.pool.findIndex((item) => !state.decisions[item.key]);
  state.idx = firstOpen >= 0 ? firstOpen : 0;
  state.finished = false;
}

function go(index) {
  if (!state.pool.length) { state.idx = 0; state.finished = true; renderCheck(); return; }
  state.idx = Math.max(0, Math.min(index, state.pool.length - 1));
  state.finished = false;
  const item = state.pool[state.idx];
  writeLS(LS.position, { key: item.key });
  renderCheck();
}

function renderCheck() {
  const total = state.pool.length;
  const decided = state.pool.filter((item) => state.decisions[item.key]).length;
  $('#progressBox').hidden = total === 0;
  $('#counter').textContent = `${decided} / ${total}`;
  $('#progressFill').style.width = total ? Math.round((decided / total) * 100) + '%' : '0%';

  const item = state.pool[state.idx];
  const showDone = state.finished || !item;
  $('#pair').hidden = showDone;
  $('#decideBar').hidden = showDone;
  $('#doneView').hidden = !showDone;
  if (showDone) { renderDone(total, decided); return; }

  const decision = state.decisions[item.key];
  $('#workMeta').textContent = metaText(item);
  $('#answerText').textContent = item.answer || '(пусто)';
  $('#refText').textContent = item.correctAnswer || '—';
  $('#conditionText').textContent = item.taskCondition ? 'Условие: ' + item.taskCondition : '';
  $('#okBtn').disabled = Boolean(decision);
  $('#noBtn').disabled = Boolean(decision);
  $('#escalateBtn').hidden = !(item.kind === 'server' && item.status === 'Assigned' && !decision && state.judgeId);
  renderPhoto(item);
}

function metaText(item) {
  const parts = ['Работа #' + (item.check?.gameName ? item.formId : item.id)];
  if (item.check?.gameName) parts.push(item.check.gameName, 'Команда ' + item.check.teamId);
  if (item.taskId) parts.push('Задача #' + item.taskId);
  if (item.kind === 'demo') parts.push('тест');
  else if (item.status) parts.push(STATUS_LABEL[item.status] || item.status);
  const decision = state.decisions[item.key];
  if (decision) parts.push(decision === 'ok' ? '✓ уже проверено: зачёт' : '✕ уже проверено: незачёт');
  return parts.join(' · ');
}

function renderDone(total, decided) {
  const empty = total === 0;
  $('#doneTitle').textContent = empty ? 'Нет работ для проверки' : 'Все доступные работы проверены';
  $('#doneText').textContent = empty
    ? 'Работы появятся, когда участники подтвердят фотографии. Для проверки интерфейса можно создать тестовые работы.'
    : `Проверено ${decided} из ${total}. После обновления очереди могут появиться новые работы.`;
  $('#doneBack').hidden = empty;
  $('#doneDemo').hidden = !empty && decided < total;
}

/* --------------------------------------------------------- фотографии */

/* Приводим путь фото к виду, который отдаёт nginx: /api/... */
function photoSrc(url) {
  if (!url) return null;
  const value = String(url).trim();
  if (!value) return null;
  if (/^(data:|blob:)/i.test(value)) return value;
  if (/^https?:\/\//i.test(value)) {
    try {
      const parsed = new URL(value);
      // Абсолютная внутренняя ссылка (http://backend:8080/api/...) идёт через тот же nginx.
      return parsed.pathname + parsed.search;
    } catch { return value; }
  }
  return value.startsWith('/') ? value : '/' + value;
}

function renderPhoto(item) {
  const wrap = $('#photoWrap');
  wrap.textContent = '';
  const src = photoSrc(item.photoUrl);
  if (!src) {
    wrap.innerHTML = '<div class="photo-empty">Фотография не приложена</div>';
    return;
  }

  const button = document.createElement('button');
  button.type = 'button';
  button.className = 'photo-btn';
  button.title = 'Нажмите, чтобы увеличить';
  const img = document.createElement('img');
  img.className = 'photo';
  img.alt = 'Фотография работы';
  button.append(img);

  const hint = document.createElement('div');
  hint.className = 'photo-hint';
  hint.textContent = 'Нажмите на фото, чтобы увеличить';
  wrap.append(button, hint);

  // Обработчик ставим до src, иначе мгновенная ошибка загрузки потеряется.
  img.addEventListener('error', () => {
    wrap.innerHTML = '<div class="photo-error">Не удалось загрузить фотографию.'
      + '<div><button type="button" class="btn ghost small" id="photoRetry">Повторить</button> '
      + `<a class="btn ghost small" href="${esc(src)}" target="_blank" rel="noopener">Открыть отдельно</a></div></div>`;
    $('#photoRetry').addEventListener('click', () => renderPhoto(item));
  });
  button.addEventListener('click', () => openZoom(src));
  img.src = src;
}

function openZoom(src) {
  const dialog = $('#zoomDialog');
  $('#zoomImg').src = src;
  if (typeof dialog.showModal === 'function') dialog.showModal();
  else dialog.setAttribute('open', '');
}

/* ------------------------------------------------------------ решения */

function setDecision(key, isCorrect) {
  state.decisions[key] = isCorrect ? 'ok' : 'bad';
  writeLS(LS.decisions, state.decisions);
}

async function decide(isCorrect) {
  const item = state.pool[state.idx];
  if (!item || state.finished || state.decisions[item.key]) return;

  if (item.kind === 'demo') {
    setDecision(item.key, isCorrect);
    advance();
    return;
  }

  $('#okBtn').disabled = true;
  $('#noBtn').disabled = true;
  try {
    await completeOnServer(item, isCorrect);
    setDecision(item.key, isCorrect);
    toast(isCorrect ? 'Зачёт — следующая работа' : 'Незачёт — следующая работа');
    advance();
  } catch (error) {
    toast(error.message, true);
    renderCheck();
  }
}

/* Зачёт/незачёт одной работой: принять (если нужно) и сразу завершить проверку. */
async function completeOnServer(item, isCorrect) {
  if (!state.judgeId) throw new Error('Нет члена жюри на сервере. Создайте его кнопкой ниже.');
  const check = item.check;
  const status = check.status;
  if (status === 'Assigned' || status === 'Escalated') {
    await api(`/checks/${check.id}/accept`, 'POST', { judgeId: state.judgeId });
    check.status = item.status = 'Accepted';
  }
  await api(`/checks/${check.id}/complete`, 'POST', {
    judgeId: state.judgeId,
    isCorrect,
    pointsAwarded: isCorrect ? 1 : 0,
    comment: '',
  });
  check.status = 'Completed';
  item.status = 'Completed';
}

async function advance(from = state.idx) {
  for (let at = from + 1; at < state.pool.length; at += 1) {
    if (!state.decisions[state.pool[at].key]) { go(at); return; }
  }
  // Дошли до конца списка — один раз спрашиваем сервер о новых работах.
  if (state.judgeId) {
    try {
      const added = await fetchServerWork();
      if (added) {
        for (let at = from + 1; at < state.pool.length; at += 1) {
          if (!state.decisions[state.pool[at].key]) { go(at); return; }
        }
      }
    } catch { /* остаёмся на месте, жюри увидит текущую работу */ }
  }
  const firstOpen = state.pool.findIndex((item) => !state.decisions[item.key]);
  if (firstOpen >= 0) { go(firstOpen); return; }
  state.finished = true;
  renderCheck();
}

async function passToSenior() {
  const item = state.pool[state.idx];
  if (!item || item.kind !== 'server' || item.status !== 'Assigned' || !state.judgeId) return;
  try {
    await api(`/checks/${item.id}/decline`, 'POST', { judgeId: state.judgeId });
    const at = state.pool.findIndex((it) => it.key === item.key);
    if (at >= 0) state.pool.splice(at, 1);
    state.finished = false;
    state.idx = Math.max(0, at - 1);
    toast('Работа передана старшему жюри.');
    await advance(state.idx);
  } catch (error) {
    toast(error.message, true);
  }
}

/* Плашка «нет члена жюри на сервере» — только когда реальные работы недоступны. */
function renderJudgeNotice() {
  const box = $('#judgeNotice');
  if (state.judgeId) { box.hidden = true; box.textContent = ''; return; }
  if (!state.serverOk) {
    box.hidden = false;
    box.textContent = 'Сервер недоступен — доступны только тестовые работы (создаются в браузере).';
    return;
  }
  const role = state.user.role === 'senior' ? 'SeniorJudge' : 'Judge';
  const candidates = state.judges.filter((j) => j.role === role);
  box.hidden = false;
  box.innerHTML = `<span>На сервере нет члена жюри «${esc(state.user.name)}»: тестовые работы доступны всегда, реальные — после создания члена жюри.</span>
    <button type="button" class="btn small" id="createJudgeBtn">Создать члена жюри «${esc(state.user.name)}»</button>
    ${candidates.length ? `<select id="pickJudge"><option value="">Выбрать существующего</option>${candidates.map((j) => `<option value="${j.id}">${esc(j.name)}</option>`).join('')}</select>` : ''}`;
  $('#createJudgeBtn').addEventListener('click', createJudgeForUser);
  const pick = $('#pickJudge');
  if (pick) {
    pick.addEventListener('change', async () => {
      if (!pick.value) return;
      state.judgeId = Number(pick.value);
      try { await fetchServerWork(); } catch (error) { toast(error.message, true); }
      restorePosition();
      renderJudgeNotice();
      renderCheck();
    });
  }
}

async function createJudgeForUser() {
  const role = state.user.role === 'senior' ? 'SeniorJudge' : 'Judge';
  try {
    const judge = await api('/judges', 'POST', { name: state.user.name.trim(), role, isAvailable: true });
    state.judges = await api('/judges');
    state.judgeId = judge.id;
    await fetchServerWork();
    restorePosition();
    renderJudgeNotice();
    renderCheck();
    toast('Член жюри создан — доступны реальные работы.');
  } catch (error) {
    toast(error.message, true);
  }
}

/* =====================================================================
   ТЕСТОВЫЕ РАБОТЫ (только фронтенд, localStorage)
   ===================================================================== */

const DEMO_TASKS = [
  { gameId: 1, formId: 101, taskId: 1, condition: 'Назовите столицу Франции.', correct: 'Париж' },
  { gameId: 1, formId: 102, taskId: 2, condition: 'Сколько будет 7 × 8?', correct: '56' },
  { gameId: 1, formId: 103, taskId: 3, condition: 'Какой химический символ у воды?', correct: 'H₂O' },
];

const DEMO_ANSWERS = {
  1: {
    correct: ['Париж', 'париж', ' Париж ', 'Париж, Франция'],
    wrong: ['Лондон', 'Прага', 'Парижж', 'Парижск', 'Нью-Йорк', 'Париж?'],
  },
  2: {
    correct: ['56', '56.', '56 (семью восемь)'],
    wrong: ['48', '54', '5 6', 'пятьдесят шесть', '15', '58'],
  },
  3: {
    correct: ['H₂O', 'H2O', 'h2o', 'Вода — H2O'],
    wrong: ['CO2', 'H2O2', 'O2', 'Н2О2', 'HO'],
  },
};

const DEMO_EXPLANATION = 'Сравнил с эталоном, ответ совпадает полностью.';

function wrapText(text, width) {
  const words = String(text).split(/\s+/);
  const lines = [];
  let line = '';
  for (const word of words) {
    if ((line + ' ' + word).trim().length > width) { lines.push(line.trim()); line = word; }
    else line += ' ' + word;
  }
  if (line.trim()) lines.push(line.trim());
  return lines;
}

/* Простая «фотография» работы в виде SVG data URL — без обращений на сервер. */
function demoPhoto(task, answer, index) {
  const lines = wrapText(answer.replace(/\n/g, ' '), 24).slice(0, 3);
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="720" height="420" viewBox="0 0 720 420">
<rect width="720" height="420" fill="#fffdf6"/>
<rect x="18" y="18" width="684" height="384" fill="none" stroke="#e2dccb" stroke-width="2"/>
<text x="44" y="62" font-family="Arial, sans-serif" font-size="18" fill="#8a8578">Форма № ${task.formId} · тестовая работа ${index}</text>
<line x1="44" y1="80" x2="676" y2="80" stroke="#e2dccb" stroke-width="2"/>
<text x="44" y="124" font-family="Arial, sans-serif" font-size="22" fill="#4c5a66">${esc(task.condition)}</text>
${lines.map((line, n) => `<text x="44" y="${196 + n * 64}" font-family="Georgia, serif" font-size="44" fill="#1b3f6b">${esc(line)}</text>`).join('')}
<text x="44" y="368" font-family="Arial, sans-serif" font-size="16" fill="#b0a898">Решение участника</text>
</svg>`;
  return 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
}

function buildDemoItems(count = 100) {
  const items = [];
  for (let n = 1; n <= count; n += 1) {
    const task = DEMO_TASKS[(n - 1) % DEMO_TASKS.length];
    const variants = DEMO_ANSWERS[task.taskId];
    const isRight = n % 2 === 0 || n % 5 === 0;          // ~60 % совпадает с эталоном
    const list = isRight ? variants.correct : variants.wrong;
    let answer = list[(n + task.taskId) % list.length];
    if (n % 17 === 0) answer = answer + '\nПояснение: ' + DEMO_EXPLANATION;
    items.push({
      id: n,
      teamId: 'demo-' + String(n).padStart(3, '0'),
      gameId: task.gameId,
      formId: task.formId,
      taskId: task.taskId,
      taskCondition: task.condition,
      correctAnswer: task.correct,
      answer,
      photoUrl: n % 12 === 0 ? demoPhoto(task, answer, n) : null,
    });
  }
  return items;
}

function createDemo(count = 100) {
  state.demo = buildDemoItems(count);
  writeLS(LS.demo, state.demo);
  rebuildPool();
  updateDemoInfo();
  const firstOpen = state.pool.findIndex((item) => !state.decisions[item.key]);
  go(firstOpen >= 0 ? firstOpen : 0);
  toast(`Создано ${count} тестовых работ (только в браузере).`);
}

function clearDemo() {
  for (const demo of state.demo) delete state.decisions['demo-' + demo.id];
  state.demo = [];
  localStorage.removeItem(LS.demo);
  writeLS(LS.decisions, state.decisions);
  rebuildPool();
  restorePosition();
  renderCheck();
  updateDemoInfo();
  toast('Тестовые работы удалены.');
}

function resetProgress() {
  state.decisions = {};
  writeLS(LS.decisions, state.decisions);
  localStorage.removeItem(LS.position);
  state.finished = false;
  state.idx = 0;
  renderCheck();
  toast('Локальный прогресс проверки сброшен.');
}

function updateDemoInfo() {
  const node = $('#demoInfo');
  if (node) node.textContent = state.demo.length ? `создано: ${state.demo.length}` : 'не созданы';
}

/* =====================================================================
   УЧАСТНИК
   ===================================================================== */

async function loadParticipant() {
  showView('participant');
  try {
    state.submissions = await api('/submissions');
    state.catalog = [];
    state.catalog = await api('/catalog/forms');
  } catch (error) {
    toast('Сервер недоступен: ' + error.message, true);
  }
  renderCatalog();
  renderSubmissions();
}

function renderCatalog() {
  const select = $('#submitForm').elements.catalog;
  const previous = select.value;
  select.innerHTML = '<option value="">Выберите форму</option>'
    + state.catalog.map((c) => `<option value="${c.formId}">${esc(c.gameName)} · ${esc(c.teamId)} · Форма #${c.formId} · Задача #${c.taskId}</option>`).join('');
  select.value = previous;
  describeCatalog();
}

function describeCatalog() {
  const value = $('#submitForm').elements.catalog.value;
  const entry = state.catalog.find((c) => String(c.formId) === value);
  $('#submitForm').elements.teamId.value = entry?.teamId || '';
  $('#catalogHint').textContent = entry ? 'Условие: ' + entry.condition : 'Выберите форму из каталога сервера.';
}

function renderSubmissions() {
  const list = state.submissions;
  $('#subCount').textContent = list.length ? `всего: ${list.length}` : '';
  const shown = list.slice(0, 50);
  $('#subList').innerHTML = shown.length
    ? shown.map(submissionItem).join('')
    : '<p class="muted">Решений пока нет.</p>';
}

function submissionItem(sub) {
  const status = STATUS_LABEL[sub.status] || sub.status;
  const preview = state.previews[sub.id];
  const actions = [];
  if (['Created', 'PhotoInvalid', 'PhotoAttached'].includes(sub.status)) {
    actions.push(`<button class="btn ghost small" data-sub="photo" data-id="${sub.id}">${sub.status === 'Created' ? 'Загрузить фото' : 'Заменить фото'}</button>`);
  }
  if (sub.status === 'PhotoAttached') {
    actions.push(`<button class="btn small" data-sub="validate" data-id="${sub.id}">Проверить фото</button>`);
  }
  if (sub.status === 'PhotoInvalid') {
    actions.push(`<button class="btn small" data-sub="resubmit" data-id="${sub.id}">Отправить повторно</button>`);
  }
  return `<div class="item">
    <div class="item-head"><span class="item-title">Решение #${sub.id} · ${esc(sub.teamId)}</span><span class="badge">${esc(status)}</span></div>
    <div class="muted">Форма ${sub.formId} · задача ${sub.taskId}</div>
    <div class="item-answer">${esc(sub.answer)}</div>
    ${preview ? `<img class="thumb" src="${esc(preview)}" alt="Фотография решения">` : ''}
    ${actions.length ? `<div class="item-actions">${actions.join('')}</div>` : ''}
  </div>`;
}

function openPhotoDialog(id) {
  const sub = state.submissions.find((s) => s.id === id);
  const form = $('#photoForm');
  form.reset();
  form.elements.submissionId.value = id;
  form.elements.photoFormId.value = sub ? sub.formId : '';
  state.pendingPhoto = null;
  $('#photoPreview').innerHTML = '';
  $('#photoDialog').showModal();
}

function openValidateDialog(id) {
  const form = $('#validateForm');
  form.reset();
  form.elements.submissionId.value = id;
  const preview = state.previews[id];
  $('#validatePreview').innerHTML = preview
    ? `<img class="thumb" src="${esc(preview)}" alt="Фотография решения">`
    : '<div class="photo-empty">Предпросмотр доступен сразу после загрузки фото в этом браузере.</div>';
  $('#validateDialog').showModal();
}

function openResubmitDialog(id) {
  const sub = state.submissions.find((s) => s.id === id);
  const form = $('#resubmitForm');
  form.elements.submissionId.value = id;
  form.elements.answer.value = sub ? sub.answer : '';
  $('#resubmitDialog').showModal();
}

/* =====================================================================
   ОРГАНИЗАТОР
   ===================================================================== */

async function loadOrganizer() {
  showView('organizer');
  updateDemoInfo();
  try {
    const [judges, queue, results] = await Promise.all([api('/judges'), api('/checks/queue'), api('/results')]);
    state.judges = judges;
    state.queue = queue;
    state.results = results;
  } catch (error) {
    toast('Сервер недоступен: ' + error.message, true);
  }
  renderOrganizer();
}

function renderOrganizer() {
  $('#judgeInfo').textContent = state.judges.length ? `всего: ${state.judges.length}` : '';
  $('#judgeList').innerHTML = state.judges.length
    ? state.judges.map((j) => `<div class="item">
        <div class="item-head"><span class="item-title">#${j.id} · ${esc(j.name)}</span>
        <span class="badge">${j.role === 'SeniorJudge' ? 'старшее жюри' : 'жюри'}${j.isAvailable ? '' : ' · недоступен'}</span></div>
      </div>`).join('')
    : '<p class="muted">Членов жюри пока нет.</p>';

  $('#assignJudge').innerHTML = '<option value="">Выберите жюри</option>'
    + state.judges.map((j) => `<option value="${j.id}">${esc(j.name)} · ${j.role === 'SeniorJudge' ? 'старшее' : 'жюри'}</option>`).join('');

  $('#queueInfo').textContent = state.queue.length ? `в очереди: ${state.queue.length}` : '';
  $('#queueList').innerHTML = state.queue.length
    ? state.queue.map(queueItem).join('')
    : '<p class="muted">Очередь пуста.</p>';

  $('#resultInfo').textContent = state.results.length ? `всего: ${state.results.length}` : '';
  $('#resultList').innerHTML = state.results.length
    ? state.results.slice(0, 100).map(resultItem).join('')
    : '<p class="muted">Результатов пока нет.</p>';
}

function queueItem(check) {
  const src = photoSrc(check.photoUrl);
  return `<div class="item">
    <div class="item-head"><span class="item-title">Работа #${check.id} · задача ${check.taskId}</span>
      <span class="badge">${esc(STATUS_LABEL[check.status] || check.status)}</span></div>
    <div class="item-answer">${esc(check.answer)}</div>
    <div class="muted">Эталон: ${esc(check.correctAnswer)}</div>
    ${src ? `<img class="thumb" src="${esc(src)}" alt="Фотография работы">` : ''}
    <div class="item-actions">
      <button class="btn small" data-queue="assign" data-id="${check.id}">Назначить выбранному</button>
      ${check.status === 'Escalated' ? `<button class="btn ghost small" data-queue="escalate" data-id="${check.id}">Найти свободного старшего</button>` : ''}
    </div>
  </div>`;
}

function resultItem(result) {
  return `<div class="item">
    <div class="item-head"><span class="item-title">Результат #${result.id} · решение #${result.submissionId}</span>
      <span class="badge ${result.isCorrect ? 'done' : 'bad'}">${result.isCorrect ? 'зачёт' : 'незачёт'}</span></div>
    <div class="muted">${result.pointsAwarded} балл. · ${new Date(result.createdAt).toLocaleString('ru-RU')}</div>
  </div>`;
}

/* =====================================================================
   СОБЫТИЯ
   ===================================================================== */

$('#loginForm').addEventListener('submit', (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const name = form.elements.name.value.trim();
  if (!name) { form.elements.name.focus(); return; }
  startSession({ name, role: form.elements.role.value });
});

$('#logoutBtn').addEventListener('click', logout);
$('#okBtn').addEventListener('click', () => decide(true));
$('#noBtn').addEventListener('click', () => decide(false));
$('#prevBtn').addEventListener('click', () => go(state.idx - 1));
$('#nextBtn').addEventListener('click', () => go(state.idx + 1));
$('#escalateBtn').addEventListener('click', passToSenior);

$('#doneRefresh').addEventListener('click', async () => {
  try {
    const added = await fetchServerWork();
    if (added) { restorePosition(); renderCheck(); toast('Получены новые работы.'); }
    else { renderCheck(); toast('Новых работ нет.'); }
  } catch (error) { toast(error.message, true); }
});
$('#doneBack').addEventListener('click', () => go(state.pool.length - 1));
$('#doneDemo').addEventListener('click', () => createDemo(100));

$('#demoCreateBtn').addEventListener('click', () => createDemo(100));
$('#demoClearBtn').addEventListener('click', clearDemo);
$('#demoResetBtn').addEventListener('click', resetProgress);

$('#submitForm').elements.catalog.addEventListener('change', describeCatalog);
$('#submitForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const entry = state.catalog.find((c) => String(c.formId) === form.elements.catalog.value);
  if (!entry) { toast('Выберите форму.', true); return; }
  try {
    await api('/submissions', 'POST', {
      teamId: form.elements.teamId.value,
      gameId: entry.gameId,
      formId: entry.formId,
      taskId: entry.taskId,
      answer: form.elements.answer.value,
    });
    form.reset();
    await loadParticipant();
    toast('Решение отправлено. Теперь загрузите фотографию.');
  } catch (error) { toast(error.message, true); }
});

$('#photoForm').elements.file.addEventListener('change', (event) => {
  const file = event.target.files[0];
  state.pendingPhoto = null;
  $('#photoPreview').innerHTML = '';
  if (!file) return;
  const reader = new FileReader();
  reader.onload = () => {
    state.pendingPhoto = reader.result;
    $('#photoPreview').innerHTML = `<img class="thumb" src="${esc(reader.result)}" alt="Предпросмотр фотографии">`;
  };
  reader.readAsDataURL(file);
});

$('#photoForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const file = form.elements.file.files[0];
  if (!file) { toast('Выберите файл.', true); return; }
  const id = Number(form.elements.submissionId.value);
  const data = new FormData();
  data.append('file', file);
  data.append('photoFormId', form.elements.photoFormId.value);
  try {
    await api(`/submissions/${id}/photo`, 'POST', data);
    if (state.pendingPhoto) state.previews[id] = state.pendingPhoto;
    $('#photoDialog').close();
    await loadParticipant();
    toast('Фото загружено. Подтвердите читаемость.');
  } catch (error) {
    $('#photoDialog').close();
    await loadParticipant();
    toast(error.message, true);
  }
});

$('#validateForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const id = Number(form.elements.submissionId.value);
  try {
    await api(`/submissions/${id}/validate-photo`, 'POST', {
      isPhotoReadable: form.elements.readable.checked,
      isAnswerVisible: form.elements.visible.checked,
    });
    $('#validateDialog').close();
    await loadParticipant();
    toast('Фото подтверждено, работа попала в очередь жюри.');
  } catch (error) {
    $('#validateDialog').close();
    toast(error.message, true);
    await loadParticipant();
  }
});

$('#resubmitForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const id = Number(form.elements.submissionId.value);
  try {
    await api(`/submissions/${id}/resubmit`, 'POST', { answer: form.elements.answer.value });
    delete state.previews[id];
    $('#resubmitDialog').close();
    await loadParticipant();
    toast('Ответ обновлён. Загрузите новое фото.');
  } catch (error) { toast(error.message, true); }
});

$('#judgeForm').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  try {
    await api('/judges', 'POST', {
      name: form.elements.name.value,
      role: form.elements.role.value,
      isAvailable: form.elements.available.checked,
    });
    form.reset();
    await loadOrganizer();
    toast('Член жюри добавлен.');
  } catch (error) { toast(error.message, true); }
});

/* делегирование кликов по динамическим спискам */
document.addEventListener('click', async (event) => {
  const closer = event.target.closest('[data-close]');
  if (closer) { closer.closest('dialog').close(); return; }

  const subButton = event.target.closest('[data-sub]');
  if (subButton) {
    const id = Number(subButton.dataset.id);
    if (subButton.dataset.sub === 'photo') openPhotoDialog(id);
    if (subButton.dataset.sub === 'validate') openValidateDialog(id);
    if (subButton.dataset.sub === 'resubmit') openResubmitDialog(id);
    return;
  }

  const queueButton = event.target.closest('[data-queue]');
  if (queueButton) {
    const id = Number(queueButton.dataset.id);
    queueButton.disabled = true;
    try {
      if (queueButton.dataset.queue === 'assign') {
        const judgeId = Number($('#assignJudge').value);
        if (!judgeId) { toast('Сначала выберите жюри.', true); return; }
        await api(`/checks/${id}/assign`, 'POST', { judgeId });
      } else {
        await api(`/checks/${id}/escalate`, 'POST');
      }
      await loadOrganizer();
      toast('Готово.');
    } catch (error) {
      toast(error.message, true);
      queueButton.disabled = false;
    }
    return;
  }

  const thumb = event.target.closest('.thumb');
  if (thumb && thumb.src) openZoom(thumb.src);
});

$('#zoomDialog').addEventListener('click', () => $('#zoomDialog').close());

/* клавиатура: 1 — зачёт, 2 — незачёт, ←/→ — переход между работами */
document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape') return;
  const tag = event.target.tagName;
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
  if (document.querySelector('dialog[open]')) return;
  if ($('#checkView').hidden) return;
  if (event.key === '1') { event.preventDefault(); decide(true); }
  else if (event.key === '2') { event.preventDefault(); decide(false); }
  else if (event.key === 'ArrowRight') { event.preventDefault(); go(state.idx + 1); }
  else if (event.key === 'ArrowLeft') { event.preventDefault(); go(state.idx - 1); }
});

/* --------------------------------------------------------------- запуск */

function boot() {
  state.decisions = readLS(LS.decisions, {});
  state.demo = readLS(LS.demo, []);
  state.catalog = [];
  $('#loginForm').elements.name.value = readLS(LS.lastName, '');
  const user = readLS(LS.user, null);
  if (user && user.name && ROLE_LABEL[user.role]) { state.user = user; enterApp(); }
  else { $('#loginView').hidden = false; $('#appView').hidden = true; }
}

boot();
