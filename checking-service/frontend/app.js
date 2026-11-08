const $ = (selector) => document.querySelector(selector);
const state = { catalog: [], submissions: [], judges: [], queue: [], results: [], juryChecks: [], seniorChecks: [], juryId: '', seniorId: '' };
const esc = (v) => String(v ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
const names = { Created: 'Создано', PhotoAttached: 'Фото загружено', PhotoInvalid: 'Фото отклонено', AcceptedForChecking: 'На проверке', Completed: 'Завершено', Queued: 'В очереди', Assigned: 'Назначено', Accepted: 'Принято', Escalated: 'Передано старшему', Declined: 'Отказ' };
const badge = (status) => `<span class="badge ${esc(status)}">${names[status] || esc(status)}</span>`;
let timer;
function notify(text, error = false) { const node = $('#toast'); node.textContent = text; node.className = 'show' + (error ? ' error' : ''); clearTimeout(timer); timer = setTimeout(() => node.className = '', 4500); }
async function api(path, method = 'GET', body) {
  const response = await fetch('/api' + path, { method, headers: body === undefined || body instanceof FormData ? {} : { 'Content-Type': 'application/json' }, body: body instanceof FormData ? body : body === undefined ? undefined : JSON.stringify(body) });
  let data; try { data = await response.json(); } catch { data = null; }
  if (!response.ok) throw new Error(data?.error || (data?.errors ? Object.values(data.errors).flat().join(' ') : '') || data?.title || `HTTP ${response.status}`);
  return data;
}
async function refresh() {
  const [catalog, submissions, judges, queue, results] = await Promise.all([api('/catalog/forms'), api('/submissions'), api('/judges'), api('/checks/queue'), api('/results')]);
  Object.assign(state, { catalog, submissions, judges, queue, results });
  const jury = judges.filter(j => j.role === 'Judge'); const seniors = judges.filter(j => j.role === 'SeniorJudge');
  if (!jury.some(j => String(j.id) === String(state.juryId))) state.juryId = jury[0]?.id || '';
  if (!seniors.some(j => String(j.id) === String(state.seniorId))) state.seniorId = seniors[0]?.id || '';
  const [juryChecks, seniorChecks] = await Promise.all([state.juryId ? api(`/judges/${state.juryId}/checks`) : [], state.seniorId ? api(`/judges/${state.seniorId}/checks`) : []]);
  state.juryChecks = juryChecks; state.seniorChecks = seniorChecks;
  render();
}
const empty = (title, text) => `<div class="empty"><span>◇</span><b>${title}</b>${text}</div>`;
function render() {
  $('#totalSub').textContent = state.submissions.length;
  $('#pendingSub').textContent = state.submissions.filter(s => s.status === 'AcceptedForChecking').length;
  $('#doneSub').textContent = state.submissions.filter(s => s.status === 'Completed').length;
  $('#subCount').textContent = `${state.submissions.length} работ`;
  $('#submissions').innerHTML = state.submissions.length ? state.submissions.map(subCard).join('') : empty('Решений ещё нет', 'Отправьте первую работу, чтобы начать.');
  const jury = state.judges.filter(j => j.role === 'Judge'); const seniors = state.judges.filter(j => j.role === 'SeniorJudge');
  $('#jurySelect').innerHTML = '<option value="">Выберите члена жюри</option>' + jury.map(j => `<option value="${j.id}">${esc(j.name)} · #${j.id}</option>`).join(''); $('#jurySelect').value = state.juryId;
  $('#seniorSelect').innerHTML = '<option value="">Выберите старшего</option>' + seniors.map(j => `<option value="${j.id}">${esc(j.name)} · #${j.id}</option>`).join(''); $('#seniorSelect').value = state.seniorId;
  const chosenJury = jury.find(j => j.id == state.juryId); const chosenSenior = seniors.find(j => j.id == state.seniorId);
  $('#juryState').textContent = chosenJury ? (chosenJury.isAvailable ? 'Доступен' : 'Недоступен') : 'Добавьте члена жюри';
  $('#seniorState').textContent = chosenSenior ? (chosenSenior.isAvailable ? 'Доступен' : 'Недоступен') : 'Добавьте старшего';
  $('#juryCount').textContent = `${state.juryChecks.length} работ`;
  $('#juryChecks').innerHTML = state.juryChecks.length ? state.juryChecks.map(c => checkCard(c, state.juryId)).join('') : empty('Нет назначенных работ', 'Добавьте жюри и подтвердите фотографию решения.');
  $('#queueCount').textContent = `${state.queue.length} в очереди`;
  $('#juryQueue').innerHTML = state.queue.length ? state.queue.map(c => queueCard(c, false)).join('') : empty('Очередь пуста', 'Свободное жюри получает новую работу автоматически.');
  const seniorWaiting = state.queue.filter(c => c.status === 'Escalated');
  $('#seniorCount').textContent = `${state.seniorChecks.length + seniorWaiting.length} работ`;
  $('#seniorChecks').innerHTML = state.seniorChecks.length || seniorWaiting.length ? state.seniorChecks.map(c => checkCard(c, state.seniorId)).join('') + seniorWaiting.map(c => queueCard(c, true)).join('') : empty('Переданных работ нет', 'После отказа жюри работа появится здесь.');
  $('#resultList').innerHTML = state.results.length ? state.results.map(resultCard).join('') : empty('Результатов пока нет', 'После завершения проверки оценки появятся здесь.');
  const form = $('#submitForm'); const value = form.elements.catalog.value;
  form.elements.catalog.innerHTML = '<option value="">Выберите форму</option>' + state.catalog.map(c => `<option value="${c.formId}">Игра #${c.gameId} · Форма #${c.formId} · Задача #${c.taskId}</option>`).join('');
  form.elements.catalog.value = value; describeCatalog();
}
function describeCatalog() { const c = state.catalog.find(x => String(x.formId) === $('#submitForm').elements.catalog.value); $('#catalogDescription').textContent = c ? `Условие: ${c.condition}` : 'Выберите демонстрационную форму из серверного каталога.'; }
function subCard(s) {
  return `<article class="card"><div class="card-head"><div><h3>Решение #${s.id}</h3><p class="subline">Команда ${esc(s.teamId)} · Игра #${s.gameId} · Форма #${s.formId} · Задача #${s.taskId}</p></div>${badge(s.status)}</div>
    <p class="question"><strong>Задача:</strong> ${esc(s.taskCondition)}</p><p class="answer"><strong>Ответ:</strong> ${esc(s.answer)}</p>
    <div class="buttons">${['Created','PhotoInvalid','PhotoAttached'].includes(s.status) ? `<button class="small-btn" data-action="photo" data-id="${s.id}">${s.status === 'PhotoAttached' ? 'Заменить фото' : 'Загрузить фото'}</button>` : ''}${s.status === 'PhotoAttached' ? `<button class="small-btn blue" data-action="validate" data-id="${s.id}">Проверить фото →</button>` : ''}${s.status === 'PhotoInvalid' ? `<button class="small-btn orange" data-action="resubmit" data-id="${s.id}">Отправить повторно</button>` : ''}</div></article>`;
}
function checkCard(c, judgeId) {
  const canAccept = c.status === 'Assigned' || c.status === 'Escalated';
  return `<article class="card"><div class="card-head"><div><h3>Работа #${c.id}</h3><p class="subline">Игра #${c.gameId} · Форма #${c.formId} · Задача #${c.taskId} · Назначена жюри #${c.judgeId}</p></div>${badge(c.status)}</div>
  <p class="question"><strong>Условие:</strong> ${esc(c.taskCondition)}</p><p class="answer"><strong>Ответ участника:</strong> ${esc(c.answer)}</p><p class="answer answer-key"><strong>Правильный ответ:</strong> ${esc(c.correctAnswer)}</p>
  ${c.photoUrl ? `<a href="${esc(c.photoUrl)}" target="_blank" rel="noopener"><img class="photo-preview" src="${esc(c.photoUrl)}" alt="Фотография решения"></a>` : ''}
  <div class="buttons">${canAccept ? `<button class="small-btn" data-action="accept" data-id="${c.id}" data-judge="${judgeId}">Принять</button>` : ''}${c.status === 'Assigned' ? `<button class="small-btn orange" data-action="decline" data-id="${c.id}" data-judge="${judgeId}">Отказаться → старшему</button>` : ''}${c.status === 'Accepted' ? `<button class="small-btn blue" data-action="complete" data-id="${c.id}" data-judge="${judgeId}">Завершить проверку</button>` : ''}</div></article>`;
}
function queueCard(c, senior) {
  const judgeId = senior ? state.seniorId : c.status === 'Queued' ? state.juryId : state.seniorId;
  return `<article class="card"><div class="card-head"><div><h3>${c.status === 'Escalated' ? 'Переданная работа' : 'Ожидает назначения'} #${c.id}</h3><p class="subline">Игра #${c.gameId} · Форма #${c.formId} · Задача #${c.taskId}</p></div>${badge(c.status)}</div><p class="question">${esc(c.taskCondition)}</p><div class="buttons">${judgeId ? `<button class="small-btn" data-action="assign" data-id="${c.id}" data-judge="${judgeId}">Назначить выбранному жюри</button>` : ''}${c.status === 'Escalated' ? `<button class="small-btn blue" data-action="escalate" data-id="${c.id}">Найти свободного старшего</button>` : ''}</div></article>`;
}
function resultCard(r) { return `<article class="card result-row"><div><h3>Результат #${r.id}</h3><p class="subline">Решение #${r.submissionId} · Проверка #${r.checkRequestId} · ${new Date(r.createdAt).toLocaleString('ru-RU')}</p><p class="answer">${r.isCorrect ? '✓ Ответ верный' : '✕ Ответ неверный'}${r.comment ? ' · ' + esc(r.comment) : ''}</p></div><div class="result-points">${r.pointsAwarded}<small style="font-size:12px"> балл.</small></div></article>`; }
function tab(name) { document.querySelectorAll('.panel').forEach(p => p.classList.toggle('active', p.id === name)); document.querySelectorAll('.nav').forEach(n => n.classList.toggle('active', n.dataset.tab === name)); $('#crumb').textContent = ({ participant:'Участник', jury:'Жюри', senior:'Старшее жюри', results:'Результаты' })[name]; }
async function run(action, id, judgeId) {
  if (action === 'refresh') { await refresh(); notify('Данные обновлены.'); return; }
  if (action === 'photo') { const s = state.submissions.find(x => x.id === id); const form = $('#photoForm'); form.reset(); form.elements.submissionId.value = id; form.elements.photoFormId.value = s.formId; $('#photoModal').showModal(); return; }
  if (action === 'validate') { const form = $('#validateForm'); form.reset(); form.elements.submissionId.value = id; $('#validateModal').showModal(); return; }
  if (action === 'resubmit') { const s = state.submissions.find(x => x.id === id); const form = $('#resubmitForm'); form.elements.submissionId.value = id; form.elements.answer.value = s.answer; $('#resubmitModal').showModal(); return; }
  if (action === 'complete') { const form = $('#completeForm'); form.reset(); form.elements.checkId.value = id; form.elements.judgeId.value = judgeId; $('#completeModal').showModal(); return; }
  if (action === 'assign') { if (!judgeId) throw new Error('Сначала выберите жюри.'); await api(`/checks/${id}/assign`, 'POST', { judgeId: Number(judgeId) }); }
  if (action === 'accept' || action === 'decline') await api(`/checks/${id}/${action}`, 'POST', { judgeId: Number(judgeId) });
  if (action === 'escalate') await api(`/checks/${id}/escalate`, 'POST');
  notify('Изменения сохранены.'); await refresh();
}
document.addEventListener('click', async e => {
  const nav = e.target.closest('[data-tab]'); if (nav) { tab(nav.dataset.tab); return; }
  const open = e.target.closest('[data-open]'); if (open) { if (open.dataset.open === 'judgeModal') $('#judgeForm').elements.role.value = document.querySelector('#senior.panel.active') ? 'SeniorJudge' : 'Judge'; document.getElementById(open.dataset.open).showModal(); return; }
  const close = e.target.closest('[data-close]'); if (close) { close.closest('dialog').close(); return; }
  const button = e.target.closest('[data-action]'); if (!button) return;
  button.disabled = true;
  try { await run(button.dataset.action, Number(button.dataset.id), button.dataset.judge); } catch (err) { notify(err.message, true); await refresh().catch(() => {}); } finally { button.disabled = false; }
});
$('#submitForm').elements.catalog.addEventListener('change', describeCatalog);
$('#jurySelect').addEventListener('change', async e => { state.juryId = e.target.value; try { await refresh(); } catch (err) { notify(err.message, true); } });
$('#seniorSelect').addEventListener('change', async e => { state.seniorId = e.target.value; try { await refresh(); } catch (err) { notify(err.message, true); } });
$('#submitForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; const c = state.catalog.find(x => String(x.formId) === f.elements.catalog.value); if (!c) { notify('Выберите форму.', true); return; } try { await api('/submissions','POST',{teamId:f.elements.teamId.value,gameId:c.gameId,formId:c.formId,taskId:c.taskId,answer:f.elements.answer.value}); f.closest('dialog').close(); f.reset(); notify('Решение отправлено. Теперь загрузите фото.'); await refresh(); } catch (err) { notify(err.message,true); } });
$('#photoForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; const data = new FormData(); data.append('file',f.elements.file.files[0]); data.append('photoFormId',f.elements.photoFormId.value); try { await api(`/submissions/${f.elements.submissionId.value}/photo`,'POST',data); f.closest('dialog').close(); notify('Фото загружено. Подтвердите читаемость.'); await refresh(); } catch (err) { notify(err.message,true); await refresh().catch(()=>{}); } });
$('#validateForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; try { await api(`/submissions/${f.elements.submissionId.value}/validate-photo`,'POST',{isPhotoReadable:f.elements.readable.checked,isAnswerVisible:f.elements.visible.checked}); f.closest('dialog').close(); notify('Фото подтверждено, запрос добавлен в очередь.'); await refresh(); } catch (err) { f.closest('dialog').close(); notify(err.message,true); await refresh().catch(()=>{}); } });
$('#resubmitForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; try { await api(`/submissions/${f.elements.submissionId.value}/resubmit`,'POST',{answer:f.elements.answer.value}); f.closest('dialog').close(); notify('Ответ обновлён. Загрузите новое фото.'); await refresh(); } catch (err) { notify(err.message,true); } });
$('#judgeForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; try { const judge = await api('/judges','POST',{name:f.elements.name.value,role:f.elements.role.value,isAvailable:f.elements.available.checked}); if (judge.role === 'Judge') state.juryId = judge.id; else state.seniorId = judge.id; f.closest('dialog').close(); f.reset(); notify('Член жюри добавлен.'); await refresh(); } catch (err) { notify(err.message,true); } });
$('#completeForm').addEventListener('submit', async e => { e.preventDefault(); const f = e.currentTarget; try { await api(`/checks/${f.elements.checkId.value}/complete`,'POST',{judgeId:Number(f.elements.judgeId.value),isCorrect:f.elements.isCorrect.checked,pointsAwarded:Number(f.elements.pointsAwarded.value),comment:f.elements.comment.value}); f.closest('dialog').close(); notify('Результат сохранён и доступен для подсчёта.'); await refresh(); } catch (err) { notify(err.message,true); } });
refresh().catch(err => notify('Не удалось подключиться к API: ' + err.message,true));
