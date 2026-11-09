import { api } from './api.js';

const app = document.querySelector('#app');
const toast = document.querySelector('#toast');

let currentUser = null;
let currentView = 'all';
let selectedPollId = null;
let authMode = 'login';
let toastTimer = null;

function escapeHtml(value = '') {
  return String(value).replace(/[&<>"']/g, (character) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#039;'
  })[character]);
}

function showToast(message, isError = false) {
  toast.textContent = message;
  toast.classList.toggle('toast-error', isError);
  toast.classList.add('toast-visible');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove('toast-visible'), 3200);
}

function renderAuth() {
  const isRegister = authMode === 'register';

  app.innerHTML = `
    <main class="auth-page">
      <section class="auth-card">
        <h1>${isRegister ? 'Регистрация' : 'Вход'}</h1>
        <p class="auth-intro">${isRegister ? 'Создайте аккаунт, чтобы создавать опросы и голосовать.' : 'Войдите, чтобы создавать опросы и голосовать.'}</p>
        <form id="auth-form" class="form-stack">
          ${isRegister ? `
            <label class="field-label" for="auth-name">Имя</label>
            <input id="auth-name" name="name" type="text" placeholder="Ваше имя" maxlength="60" required>
          ` : ''}
          <label class="field-label" for="auth-email">Электронная почта</label>
          <input id="auth-email" name="email" type="email" placeholder="name@example.com" autocomplete="email" required>
          <label class="field-label" for="auth-password">Пароль</label>
          <input id="auth-password" name="password" type="password" minlength="6" autocomplete="${isRegister ? 'new-password' : 'current-password'}" required>
          <button class="button button-primary button-wide" type="submit">${isRegister ? 'Зарегистрироваться' : 'Войти'}</button>
        </form>
        <p class="auth-switch">${isRegister ? 'Уже есть аккаунт?' : 'Нет аккаунта?'}
          <button class="text-button" data-action="switch-auth">${isRegister ? 'Войти' : 'Зарегистрироваться'}</button>
        </p>
        <p class="demo-note">Данные сохраняются только в этом браузере.</p>
      </section>
    </main>`;
}

function navButton(view, label) {
  return `<button class="nav-button ${currentView === view ? 'nav-active' : ''}" data-view="${view}">${label}</button>`;
}

function renderShell(content) {
  app.innerHTML = `
    <div class="app-layout">
      <header class="topbar">
        <div class="topbar-right">
          <span class="user-name">${escapeHtml(currentUser.name)}</span>
          <button class="logout-button" data-action="logout">Выход</button>
        </div>
      </header>
      <div class="workspace">
        <nav class="sidebar">
          ${navButton('all', 'Все опросы')}
          ${navButton('mine', 'Мои опросы')}
          ${navButton('create', 'Создать опрос')}
        </nav>
        <main class="main-content">${content}</main>
      </div>
    </div>`;
}

function pollCard(poll) {
  return `
    <article class="poll-card">
      <h2>${escapeHtml(poll.title)}</h2>
      <button class="button button-secondary" data-action="open-poll" data-id="${escapeHtml(poll.id)}">Открыть</button>
    </article>`;
}

async function renderPollList() {
  const allPolls = await api.getPolls();
  const polls = currentView === 'mine'
    ? allPolls.filter((poll) => poll.creatorId === currentUser.id)
    : allPolls;
  const title = currentView === 'mine' ? 'Мои опросы' : 'Все опросы';

  renderShell(`
    <div class="page-heading"><h1>${title}</h1></div>
    ${polls.length ? `<section class="poll-grid">${polls.map(pollCard).join('')}</section>` : ''}
  `);
}

function questionEditor(index) {
  return `
    <section class="question-editor">
      <div class="question-editor-heading">
        <span class="question-number">Вопрос ${index + 1}</span>
        <button type="button" class="text-button remove-question" data-action="remove-question">Удалить</button>
      </div>
      <label class="field-label">Текст вопроса</label>
      <input class="question-title-input" type="text" maxlength="180" placeholder="Введите вопрос" required>
      <div class="question-type-row">
        <label class="field-label" for="question-type-${index}">Тип ответа</label>
        <select id="question-type-${index}" class="question-type">
          <option value="single">Один вариант</option>
          <option value="multiple">Несколько вариантов</option>
        </select>
      </div>
      <div class="options-list">
        <div class="option-input-row">
          <input class="option-text-input" type="text" maxlength="100" placeholder="Вариант ответа 1" required>
          <button type="button" class="remove-option" data-action="remove-option" aria-label="Удалить вариант">×</button>
        </div>
        <div class="option-input-row">
          <input class="option-text-input" type="text" maxlength="100" placeholder="Вариант ответа 2" required>
          <button type="button" class="remove-option" data-action="remove-option" aria-label="Удалить вариант">×</button>
        </div>
      </div>
      <button type="button" class="text-button add-option" data-action="add-option">Добавить вариант</button>
    </section>`;
}

function renderCreate() {
  renderShell(`
    <div class="page-heading"><h1>Создать опрос</h1></div>
    <form id="create-poll-form" class="create-form">
      <section class="form-panel">
        <label class="field-label" for="poll-title">Название опроса</label>
        <input id="poll-title" name="title" type="text" maxlength="100" placeholder="Введите название опроса" required>
      </section>
      <div class="section-heading">
        <h2>Вопросы</h2>
        <p>В каждом вопросе должно быть минимум два варианта ответа.</p>
      </div>
      <div id="questions-container">${questionEditor(0)}</div>
      <button type="button" class="button button-outline add-question-button" data-action="add-question">Добавить вопрос</button>
      <div class="form-actions">
        <button type="submit" class="button button-primary">Создать опрос</button>
      </div>
    </form>
  `);
}

async function renderPollDetails(pollId) {
  const poll = await api.getPoll(pollId);
  const stats = await api.getPollStats(pollId);
  const canVote = !poll.hasVoted;

  const questionsHtml = poll.questions.map((question, index) => {
    const questionStats = stats.questions.find((item) => item.id === question.id);
    const options = question.options.map((option) => {
      if (!canVote) {
        const result = questionStats.options.find((item) => item.id === option.id);
        return `
          <div class="result-option">
            <div class="result-option-line">
              <span>${escapeHtml(option.text)}</span>
              <strong>${result.percent}%</strong>
            </div>
            <div class="result-track"><span style="width: ${result.percent}%"></span></div>
            <p class="result-count">Голосов: ${result.count}</p>
          </div>`;
      }

      const inputType = question.type === 'multiple' ? 'checkbox' : 'radio';
      return `
        <label class="answer-option">
          <input type="${inputType}" name="question-${escapeHtml(question.id)}" value="${escapeHtml(option.id)}">
          <span>${escapeHtml(option.text)}</span>
        </label>`;
    }).join('');

    return `
      <section class="detail-question">
        <span class="question-number">Вопрос ${index + 1}</span>
        <h3>${escapeHtml(question.title)}</h3>
        ${canVote ? `<div class="answer-options">${options}</div>` : `<div class="results-list">${options}</div>`}
      </section>`;
  }).join('');

  renderShell(`
    <button class="back-button" data-action="back">К опросам</button>
    <div class="detail-heading">
      <h1>${escapeHtml(poll.title)}</h1>
      <div class="detail-total"><strong>${stats.totalVotes}</strong><span>голосов</span></div>
    </div>
    ${canVote
      ? `<form id="vote-form" data-id="${escapeHtml(poll.id)}">
           <div class="detail-panel">${questionsHtml}</div>
           <div class="form-actions"><button type="submit" class="button button-primary">Отправить ответы</button></div>
         </form>`
      : `<div class="detail-panel">${questionsHtml}</div>`}
  `);
}

async function renderCurrentView() {
  try {
    if (selectedPollId) {
      await renderPollDetails(selectedPollId);
    } else if (currentView === 'create') {
      renderCreate();
    } else {
      await renderPollList();
    }
  } catch (error) {
    showToast(error.message || 'Не удалось загрузить данные.', true);
    selectedPollId = null;
    currentView = 'all';
    await renderPollList();
  }
}

function getQuestionData(form) {
  const cards = [...form.querySelectorAll('.question-editor')];
  if (!cards.length) throw new Error('Добавьте хотя бы один вопрос.');

  return cards.map((card) => {
    const title = card.querySelector('.question-title-input').value.trim();
    const type = card.querySelector('.question-type').value;
    const options = [...card.querySelectorAll('.option-text-input')]
      .map((input) => input.value.trim())
      .filter(Boolean);

    if (!title) throw new Error('Заполните текст каждого вопроса.');
    if (options.length < 2) throw new Error('В каждом вопросе должно быть минимум два варианта ответа.');
    if (new Set(options.map((option) => option.toLowerCase())).size !== options.length) {
      throw new Error('Варианты ответа в одном вопросе не должны повторяться.');
    }

    return { title, type, options };
  });
}

function renumberQuestions() {
  document.querySelectorAll('.question-editor').forEach((card, index) => {
    card.querySelector('.question-number').textContent = `Вопрос ${index + 1}`;
    const select = card.querySelector('.question-type');
    select.id = `question-type-${index}`;
    card.querySelector('.question-type-row label').htmlFor = select.id;
  });
}

app.addEventListener('click', async (event) => {
  const target = event.target.closest('[data-action], [data-view]');
  if (!target) return;

  if (target.dataset.view) {
    selectedPollId = null;
    currentView = target.dataset.view;
    await renderCurrentView();
    return;
  }

  const action = target.dataset.action;

  try {
    if (action === 'switch-auth') {
      authMode = authMode === 'login' ? 'register' : 'login';
      renderAuth();
    } else if (action === 'logout') {
      await api.logout();
      currentUser = null;
      selectedPollId = null;
      authMode = 'login';
      renderAuth();
    } else if (action === 'back') {
      selectedPollId = null;
      currentView = 'all';
      await renderCurrentView();
    } else if (action === 'open-poll') {
      selectedPollId = target.dataset.id;
      await renderCurrentView();
    } else if (action === 'add-question') {
      const container = document.querySelector('#questions-container');
      container.insertAdjacentHTML('beforeend', questionEditor(container.querySelectorAll('.question-editor').length));
      renumberQuestions();
    } else if (action === 'remove-question') {
      const cards = document.querySelectorAll('.question-editor');
      if (cards.length === 1) return showToast('В опросе должен остаться хотя бы один вопрос.', true);
      target.closest('.question-editor').remove();
      renumberQuestions();
    } else if (action === 'add-option') {
      const list = target.closest('.question-editor').querySelector('.options-list');
      const count = list.querySelectorAll('.option-input-row').length + 1;
      const row = document.createElement('div');
      row.className = 'option-input-row';
      row.innerHTML = `
        <input class="option-text-input" type="text" maxlength="100" placeholder="Вариант ответа ${count}" required>
        <button type="button" class="remove-option" data-action="remove-option" aria-label="Удалить вариант">×</button>`;
      list.append(row);
      row.querySelector('input').focus();
    } else if (action === 'remove-option') {
      const list = target.closest('.options-list');
      if (list.querySelectorAll('.option-input-row').length <= 2) {
        return showToast('Нужно оставить минимум два варианта.', true);
      }
      target.closest('.option-input-row').remove();
    }
  } catch (error) {
    showToast(error.message || 'Произошла ошибка.', true);
  }
});

app.addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = event.target;

  try {
    if (form.id === 'auth-form') {
      const formData = new FormData(form);
      const email = String(formData.get('email') || '').trim();
      const password = String(formData.get('password') || '');

      if (password.length < 6) throw new Error('Пароль должен содержать не менее 6 символов.');

      if (authMode === 'register') {
        const name = String(formData.get('name') || '').trim();
        if (!name) throw new Error('Укажите имя.');
        currentUser = await api.register({ name, email, password });
      } else {
        currentUser = await api.login({ email, password });
      }

      currentView = 'all';
      selectedPollId = null;
      await renderCurrentView();
      showToast(authMode === 'register' ? 'Аккаунт создан.' : 'Вы вошли в систему.');
    } else if (form.id === 'create-poll-form') {
      const title = form.elements.title.value.trim();
      if (!title) throw new Error('Укажите название опроса.');

      const poll = await api.createPoll({
        title,
        questions: getQuestionData(form)
      });

      selectedPollId = poll.id;
      await renderCurrentView();
      showToast('Опрос создан.');
    } else if (form.id === 'vote-form') {
      const pollId = form.dataset.id;
      const poll = await api.getPoll(pollId);
      const answers = {};

      for (const question of poll.questions) {
        const selected = [...form.querySelectorAll(`[name="question-${CSS.escape(question.id)}"]:checked`)]
          .map((input) => input.value);
        if (selected.length === 0) throw new Error('Ответьте на каждый вопрос.');
        answers[question.id] = selected;
      }

      await api.vote(pollId, answers);
      await renderCurrentView();
      showToast('Ваш голос учтён.');
    }
  } catch (error) {
    showToast(error.message || 'Не удалось выполнить действие.', true);
  }
});

async function start() {
  currentUser = await api.getCurrentUser();
  if (currentUser) {
    await renderCurrentView();
  } else {
    renderAuth();
  }
}

start();
