// Временная имитация WebAPI для разработки фронтенда.

const STORAGE_KEYS = {
  users: 'poll-service-users',
  polls: 'poll-service-polls',
  votes: 'poll-service-votes',
  session: 'poll-service-session'
};

function read(key, fallback) {
  try {
    const value = localStorage.getItem(key);
    return value ? JSON.parse(value) : fallback;
  } catch {
    return fallback;
  }
}

function write(key, value) {
  localStorage.setItem(key, JSON.stringify(value));
}

function makeId() {
  if (crypto.randomUUID) return crypto.randomUUID();
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function getUsers() {
  return read(STORAGE_KEYS.users, []);
}

function getVotes() {
  return read(STORAGE_KEYS.votes, []);
}

function getCurrentSession() {
  return read(STORAGE_KEYS.session, null);
}

function getPollsFromStorage() {
  return read(STORAGE_KEYS.polls, []);
}


export const api = {
  async getCurrentUser() {
    return getCurrentSession();
  },

  async register({ name, email }) {
    const users = getUsers();
    const normalizedEmail = email.trim().toLowerCase();

    if (users.some((user) => user.email === normalizedEmail)) {
      throw new Error('Пользователь с такой почтой уже зарегистрирован.');
    }

    const user = {
      id: makeId(),
      name: name.trim(),
      email: normalizedEmail
    };

    users.push(user);
    write(STORAGE_KEYS.users, users);
    write(STORAGE_KEYS.session, user);
    return user;
  },

  async login({ email }) {
    const normalizedEmail = email.trim().toLowerCase();
    const user = getUsers().find((item) => item.email === normalizedEmail);

    if (!user) {
      throw new Error('Пользователь не найден. Сначала зарегистрируйтесь.');
    }

    // В деморежиме пароль не сохраняется и не проверяется.
    write(STORAGE_KEYS.session, user);
    return user;
  },

  async logout() {
    localStorage.removeItem(STORAGE_KEYS.session);
  },

  async getPolls() {
    return getPollsFromStorage();
  },

  async getPoll(pollId) {
    const poll = getPollsFromStorage().find((item) => item.id === pollId);
    if (!poll) throw new Error('Опрос не найден.');

    const user = getCurrentSession();
    const hasVoted = getVotes().some((vote) => vote.pollId === pollId && vote.userId === user?.id);
    return { ...poll, hasVoted };
  },

  async createPoll({ title, questions }) {
    const user = getCurrentSession();
    if (!user) throw new Error('Для создания опроса нужно войти в аккаунт.');

    const poll = {
      id: makeId(),
      title: title.trim(),
      creatorId: user.id,
      questions: questions.map((question) => ({
        id: makeId(),
        title: question.title.trim(),
        type: question.type,
        options: question.options.map((text) => ({ id: makeId(), text: text.trim() }))
      }))
    };

    const polls = getPollsFromStorage();
    polls.unshift(poll);
    write(STORAGE_KEYS.polls, polls);
    return poll;
  },

  async vote(pollId, answers) {
    const user = getCurrentSession();
    if (!user) throw new Error('Для голосования нужно войти в аккаунт.');

    const poll = getPollsFromStorage().find((item) => item.id === pollId);
    if (!poll) throw new Error('Опрос не найден.');

    const votes = getVotes();
    if (votes.some((vote) => vote.pollId === pollId && vote.userId === user.id)) {
      throw new Error('Вы уже голосовали в этом опросе.');
    }

    for (const question of poll.questions) {
      const selected = answers[question.id] || [];
      if (selected.length === 0) throw new Error('Ответьте на каждый вопрос.');
      if (question.type === 'single' && selected.length !== 1) {
        throw new Error('Для вопроса с одним вариантом выберите только один ответ.');
      }
      if (selected.some((optionId) => !question.options.some((option) => option.id === optionId))) {
        throw new Error('В ответе обнаружен неизвестный вариант.');
      }
    }

    votes.push({
      id: makeId(),
      pollId,
      userId: user.id,
      answers
    });
    write(STORAGE_KEYS.votes, votes);
  },

  async getPollStats(pollId) {
    const poll = getPollsFromStorage().find((item) => item.id === pollId);
    if (!poll) throw new Error('Опрос не найден.');

    const votes = getVotes().filter((vote) => vote.pollId === pollId);
    return {
      totalVotes: votes.length,
      questions: poll.questions.map((question) => {
        const options = question.options.map((option) => {
          const count = votes.filter((vote) => (vote.answers[question.id] || []).includes(option.id)).length;
          return {
            id: option.id,
            count,
            percent: votes.length ? Math.round((count / votes.length) * 100) : 0
          };
        });
        return { id: question.id, options };
      })
    };
  }
};
