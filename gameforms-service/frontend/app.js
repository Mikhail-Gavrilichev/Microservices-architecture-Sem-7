const state = { games: [], tasks: [], sets: [], details: {}, tab: 'games', selectedGame: null, selectedTask: null, textEdit: null };
const $ = (selector) => document.querySelector(selector);
const escapeHtml = (value) => String(value ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const statusNames = { Draft: 'Черновик', Published: 'Опубликовано', Created: 'Создан', Prepared: 'Подготовлен', Printed: 'Напечатан', Issued: 'Выдан' };
const badge = (status) => `<span class="badge ${escapeHtml(status)}">${statusNames[status] || escapeHtml(status)}</span>`;
let toastTimer;
function toast(message, error = false) {
  const node = $('#toast'); node.textContent = message; node.className = (error ? 'error ' : '') + 'visible';
  clearTimeout(toastTimer); toastTimer = setTimeout(() => node.className = '', 4000);
}
async function api(path, method = 'GET', body) {
  const response = await fetch('/api' + path, { method, headers: body === undefined ? {} : { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) });
  let data; try { data = await response.json(); } catch { data = null; }
  if (!response.ok) {
    const validation = data?.errors ? Object.values(data.errors).flat().join(' ') : '';
    throw new Error(data?.error || validation || data?.title || `Ошибка HTTP ${response.status}`);
  }
  return data;
}
async function refresh() {
  const [games, tasks, sets] = await Promise.all([api('/games'), api('/tasks'), api('/formsets')]);
  state.games = games; state.tasks = tasks; state.sets = sets;
  const detailEntries = await Promise.all(sets.map(async (set) => [set.id, await api(`/formsets/${set.id}`)]));
  state.details = Object.fromEntries(detailEntries);
  render();
}
function optionGames(games) { return '<option value="">Выберите игру</option>' + games.map(g => `<option value="${g.id}">${escapeHtml(g.name)}</option>`).join(''); }
function render() {
  $('#gamesCount').textContent = state.games.length; $('#tasksCount').textContent = state.tasks.length; $('#setsCount').textContent = state.sets.length;
  $('#statGames').textContent = state.games.length; $('#statPublished').textContent = state.games.filter(g => g.status === 'Published').length; $('#statTasks').textContent = state.tasks.filter(t => t.status === 'Published').length;
  $('#gamesTotal').textContent = `${state.games.length} записей`;
  $('#gamesList').innerHTML = state.games.length ? state.games.map(gameCard).join('') : empty('Пока нет игр', 'Создайте первую игру, чтобы начать подготовку.');
  const filter = $('#taskFilter'); const current = filter.value;
  filter.innerHTML = '<option value="">Все игры</option>' + state.games.map(g => `<option value="${g.id}">${escapeHtml(g.name)}</option>`).join(''); filter.value = current;
  const filtered = state.tasks.filter(t => !filter.value || String(t.gameId) === filter.value);
  $('#tasksTotal').textContent = `${filtered.length} задач`;
  $('#tasksList').innerHTML = filtered.length ? filtered.map(taskCard).join('') : empty('Задач пока нет', 'Создайте задачу и заполните условие и ответ.');
  $('#setsList').innerHTML = state.sets.length ? state.sets.map(setCard).join('') : empty('Комплектов пока нет', 'Опубликуйте игру и создайте комплекты для команд.');
  $('#taskForm').elements.gameId.innerHTML = optionGames(state.games);
  $('#setsForm').elements.gameId.innerHTML = optionGames(state.games.filter(g => g.status === 'Published' && !state.sets.some(s => s.gameId === g.id)));
}
function empty(title, subtitle) { return `<div class="empty"><div style="font-size:27px">▦</div><strong>${title}</strong><span>${subtitle}</span></div>`; }
function gameCard(g) {
  const open = state.selectedGame === g.id;
  const count = state.tasks.filter(t => t.gameId === g.id).length;
  return `<article class="card"><div class="card-top"><div><h3 class="card-title">${escapeHtml(g.name)}</h3><p class="card-sub">Игра #${g.id} · ${count} задач · ${new Date(g.createdAt).toLocaleDateString('ru-RU')}</p></div>${badge(g.status)}</div>
    ${g.description ? `<p class="card-body">${escapeHtml(g.description)}</p>` : ''}
    ${open ? `<div class="card-body"><strong>Правила игры</strong><br>${escapeHtml(g.rules || 'Не заполнены')}<br><br><strong>Правила формы</strong><br>${escapeHtml(g.formRules || 'Не заполнены')}</div>` : ''}
    <div class="card-actions"><button class="mini" data-action="show-game" data-id="${g.id}">${open ? 'Скрыть детали' : 'Правила и детали'}</button>${g.status === 'Draft' ? `<button class="mini" data-action="rules" data-id="${g.id}">Правила игры</button><button class="mini" data-action="form-rules" data-id="${g.id}">Правила формы</button><button class="mini accent" data-action="publish-game" data-id="${g.id}">Опубликовать →</button>` : ''}<button class="mini" data-action="tasks-game" data-id="${g.id}">Задачи игры</button></div></article>`;
}
function taskCard(t) {
  const game = state.games.find(g => g.id === t.gameId);
  const open = state.selectedTask === t.id;
  return `<article class="card"><div class="card-top"><div><h3 class="card-title">${escapeHtml(t.title)}</h3><p class="card-sub">${escapeHtml(game?.name || `Игра #${t.gameId}`)} · Задача #${t.id} · ${t.points} балл.</p></div>${badge(t.status)}</div>
    <p class="card-body">${escapeHtml(t.condition || 'Условие пока не заполнено.')}</p>
    ${open ? `<div class="hint" style="margin:16px 0 0"><strong>Правильный ответ:</strong> ${escapeHtml(t.correctAnswer || 'Не указан')}</div>` : ''}
    <div class="card-actions"><button class="mini" data-action="show-task" data-id="${t.id}">${open ? 'Скрыть ответ' : 'Просмотр и ответ'}</button>${t.status === 'Draft' ? `<button class="mini" data-action="edit-task" data-id="${t.id}">Редактировать</button><button class="mini accent" data-action="publish-task" data-id="${t.id}">Опубликовать →</button>` : `<button class="mini warn" data-action="unpublish-task" data-id="${t.id}">Снять публикацию</button>`}</div></article>`;
}
function setCard(s) {
  const detail = state.details[s.id] || { forms: [] }; const forms = detail.forms || [];
  const game = state.games.find(g => g.id === s.gameId);
  const assigned = new Set(forms.map(f => f.taskId));
  const available = state.tasks.filter(t => t.gameId === s.gameId && t.status === 'Published' && !assigned.has(t.id));
  return `<article class="card"><div class="card-top"><div><h3 class="card-title">Команда ${escapeHtml(s.teamId)}</h3><p class="card-sub">${escapeHtml(game?.name || `Игра #${s.gameId}`)} · Комплект #${s.id} · ${forms.length} форм</p></div>${badge(s.status)}</div>
    ${forms.length ? forms.map(f => `<div class="form-row"><div class="form-left"><strong>Форма #${f.id}</strong><span class="muted">${escapeHtml(f.taskTitle)} · задача #${f.taskId}</span>${badge(f.status)}</div><div class="form-controls">${f.status === 'Created' && s.status === 'Created' ? `<button class="mini" data-action="prepare-form" data-id="${f.id}">Подготовить</button>` : ''}${f.status === 'Prepared' && s.status === 'Prepared' ? `<button class="mini" data-action="print-form" data-id="${f.id}">Печать</button>` : ''}</div></div>`).join('') : '<p class="card-body">Формы не добавлены. Назначьте опубликованную задачу ниже.</p>'}
    ${s.status === 'Created' ? `<div class="add-form"><select class="form-task-select" data-set="${s.id}" aria-label="Задача для набора ${s.id}"><option value="">Выберите задачу</option>${available.map(t => `<option value="${t.id}">${escapeHtml(t.title)}</option>`).join('')}</select><button class="mini" data-action="add-form" data-id="${s.id}" ${available.length ? '' : 'disabled'}>＋ Добавить форму</button></div>` : ''}
    <div class="card-actions">${s.status === 'Created' ? `<button class="mini accent" data-action="prepare-set" data-id="${s.id}">Подготовить комплект</button>` : ''}${s.status === 'Prepared' ? `<button class="mini accent" data-action="print-set" data-id="${s.id}">Напечатать комплект</button>` : ''}${s.status === 'Printed' ? `<button class="mini accent" data-action="issue-set" data-id="${s.id}">Выдать команде →</button>` : ''}${['Printed', 'Issued'].includes(s.status) ? `<button class="mini" data-action="print-view" data-id="${s.id}">Лист для печати ↗</button>` : ''}</div></article>`;
}
function switchTab(tab) {
  state.tab = tab;
  document.querySelectorAll('.page').forEach(p => p.classList.toggle('active', p.id === `${tab}Page`));
  document.querySelectorAll('.nav-item').forEach(n => n.classList.toggle('active', n.dataset.tab === tab));
  $('#breadcrumb').textContent = ({ games: 'Игры', tasks: 'Задачи', sets: 'Комплекты форм' })[tab];
}
function printView(id) {
  const detail = state.details[id]; const game = state.games.find(g => g.id === detail.gameId);
  const rows = detail.forms.map((f, index) => {
    const task = state.tasks.find(t => t.id === f.taskId);
    return `<section style="padding:24px 0;border-bottom:1px solid #bbb;break-inside:avoid"><h2>${index + 1}. ${escapeHtml(task?.title || f.taskTitle)} <small style="font-weight:normal">(${task?.points ?? 1} балл.)</small></h2><p style="white-space:pre-wrap">${escapeHtml(task?.condition || '')}</p><div style="margin-top:42px">Ответ: _________________________________________________________</div></section>`;
  }).join('');
  const popup = window.open('', '_blank'); if (!popup) { toast('Разрешите всплывающие окна для печати.', true); return; }
  popup.document.write(`<!doctype html><html lang="ru"><head><meta charset="utf-8"><title>Формы — ${escapeHtml(detail.teamId)}</title></head><body style="font-family:Arial,sans-serif;max-width:800px;margin:40px auto;padding:20px;color:#222"><h1>${escapeHtml(game?.name || 'Игра')}</h1><p>Команда: <b>${escapeHtml(detail.teamId)}</b> · Комплект #${id}</p><p>${escapeHtml(game?.formRules || '')}</p>${rows}</body></html>`);
  popup.document.close();
  popup.focus(); popup.print();
}
async function run(action, id, button) {
  if (action === 'show-game') { state.selectedGame = state.selectedGame === id ? null : id; render(); return; }
  if (action === 'show-task') { state.selectedTask = state.selectedTask === id ? null : id; render(); return; }
  if (action === 'tasks-game') { switchTab('tasks'); $('#taskFilter').value = id; render(); return; }
  if (action === 'print-view') { printView(id); return; }
  if (action === 'rules' || action === 'form-rules') {
    const game = state.games.find(g => g.id === id);
    state.textEdit = { id, kind: action }; $('#textDialogTitle').textContent = action === 'rules' ? 'Правила игры' : 'Правила формы';
    $('#textForm').elements.value.value = action === 'rules' ? game.rules : game.formRules;
    $('#textDialog').showModal(); return;
  }
  if (action === 'edit-task') {
    const task = state.tasks.find(t => t.id === id); const form = $('#taskForm'); form.reset();
    form.elements.id.value = id; form.elements.gameId.value = task.gameId; form.elements.gameId.disabled = true;
    form.elements.title.value = task.title; form.elements.condition.value = task.condition; form.elements.correctAnswer.value = task.correctAnswer; form.elements.points.value = task.points;
    $('#taskDialogTitle').textContent = 'Редактировать задачу'; $('#taskDialogEyebrow').textContent = `ЗАДАЧА #${id}`; $('#taskDialog').showModal(); return;
  }
  const routes = {
    'publish-game': `/games/${id}/publish`, 'publish-task': `/tasks/${id}/publish`, 'unpublish-task': `/tasks/${id}/unpublish`,
    'prepare-form': `/forms/${id}/prepare`, 'print-form': `/forms/${id}/print`,
    'prepare-set': `/formsets/${id}/prepare`, 'print-set': `/formsets/${id}/print`, 'issue-set': `/formsets/${id}/issue`
  };
  if (action === 'add-form') {
    const select = document.querySelector(`.form-task-select[data-set="${id}"]`);
    if (!select?.value) throw new Error('Выберите задачу для формы.');
    await api('/forms', 'POST', { formSetId: id, taskId: Number(select.value) });
  } else if (routes[action]) {
    await api(routes[action], 'POST');
  } else return;
  toast('Готово! Изменения сохранены.'); await refresh();
}
document.addEventListener('click', async (event) => {
  const nav = event.target.closest('[data-tab]'); if (nav) { switchTab(nav.dataset.tab); return; }
  const open = event.target.closest('[data-open]'); if (open) {
    if (open.dataset.open === 'taskDialog') { const form = $('#taskForm'); form.reset(); form.elements.id.value = ''; form.elements.gameId.disabled = false; $('#taskDialogTitle').textContent = 'Новая задача'; $('#taskDialogEyebrow').textContent = 'НОВАЯ ЗАПИСЬ'; }
    document.getElementById(open.dataset.open).showModal(); return;
  }
  const close = event.target.closest('[data-close]'); if (close) { close.closest('dialog').close(); return; }
  const action = event.target.closest('[data-action]'); if (!action) return;
  action.disabled = true;
  try { await run(action.dataset.action, Number(action.dataset.id), action); } catch (error) { toast(error.message, true); } finally { action.disabled = false; }
});
$('#taskFilter').addEventListener('change', render);
$('#gameForm').addEventListener('submit', async (event) => {
  event.preventDefault(); const form = event.currentTarget;
  try { await api('/games', 'POST', { name: form.elements.name.value, description: form.elements.description.value }); form.closest('dialog').close(); form.reset(); toast('Игра создана.'); await refresh(); } catch (error) { toast(error.message, true); }
});
$('#taskForm').addEventListener('submit', async (event) => {
  event.preventDefault(); const form = event.currentTarget; const id = form.elements.id.value;
  try {
    if (id) {
      await api(`/tasks/${id}`, 'PUT', { title: form.elements.title.value, condition: form.elements.condition.value, points: Number(form.elements.points.value) });
      await api(`/tasks/${id}/answer`, 'PUT', { value: form.elements.correctAnswer.value });
    } else {
      await api('/tasks', 'POST', { gameId: Number(form.elements.gameId.value), title: form.elements.title.value, condition: form.elements.condition.value, correctAnswer: form.elements.correctAnswer.value, points: Number(form.elements.points.value) });
    }
    form.closest('dialog').close(); form.reset(); toast(id ? 'Задача обновлена.' : 'Задача создана.'); await refresh();
  } catch (error) { toast(error.message, true); }
});
$('#setsForm').addEventListener('submit', async (event) => {
  event.preventDefault(); const form = event.currentTarget;
  const teamIds = form.elements.teams.value.split(/[,\n;]+/).map(x => x.trim()).filter(Boolean);
  try { await api('/formsets/generate', 'POST', { gameId: Number(form.elements.gameId.value), teamIds }); form.closest('dialog').close(); form.reset(); switchTab('sets'); toast('Комплекты созданы для всех команд.'); await refresh(); } catch (error) { toast(error.message, true); }
});
$('#textForm').addEventListener('submit', async (event) => {
  event.preventDefault(); const form = event.currentTarget; const { id, kind } = state.textEdit;
  try { await api(`/games/${id}/${kind === 'rules' ? 'rules' : 'form-rules'}`, 'PUT', { value: form.elements.value.value }); form.closest('dialog').close(); toast('Правила сохранены.'); await refresh(); } catch (error) { toast(error.message, true); }
});
refresh().catch(error => toast('Не удалось подключиться к API: ' + error.message, true));
