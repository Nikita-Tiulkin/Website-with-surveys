const API_URL = '/api';
const TOKEN_KEY = 'poll-service-token';

function saveToken(token) {
  localStorage.setItem(TOKEN_KEY, token);
}

function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers || {});
  const token = localStorage.getItem(TOKEN_KEY);

  if (options.body) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers
  });

  const responseText = await response.text();
  let data = null;

  if (responseText) {
    try {
      data = JSON.parse(responseText);
    } catch {
      data = { error: responseText };
    }
  }

  if (!response.ok) {
    const validationMessage = data?.errors
      ? Object.values(data.errors).flat()[0]
      : null;
    const error = new Error(
      data?.error || validationMessage || data?.title || 'Ошибка запроса к серверу.'
    );
    error.status = response.status;

    if (response.status === 401) {
      clearToken();
    }

    throw error;
  }

  return data;
}

function saveAuth(authResponse) {
  if (!authResponse?.token || !authResponse?.user) {
    throw new Error('Сервер вернул некорректный ответ авторизации.');
  }

  saveToken(authResponse.token);
  return authResponse.user;
}

export const api = {
  async getCurrentUser() {
    if (!localStorage.getItem(TOKEN_KEY)) {
      return null;
    }

    try {
      return await request('/auth/me');
    } catch (error) {
      if (error.status === 401) {
        return null;
      }
      throw error;
    }
  },

  async register({ name, email, password }) {
    const result = await request('/auth/register', {
      method: 'POST',
      body: JSON.stringify({ name, email, password })
    });

    return saveAuth(result);
  },

  async login({ email, password }) {
    const result = await request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });

    return saveAuth(result);
  },

  async logout() {
    clearToken();
  },

  async getPolls(mine = false) {
    const query = mine ? '?mine=true' : '';
    return request(`/surveys${query}`);
  },

  async getPoll(pollId) {
    return request(`/surveys/${encodeURIComponent(pollId)}`);
  },

  async createPoll({ title, questions, endsAt }) {
    const requestBody = {
      title,
      endsAt: endsAt || null,
      questions: questions.map((question) => ({
        title: question.title,
        type: question.type,
        isRequired: true,
        options: question.options.map((text) => ({ text }))
      }))
    };

    return request('/surveys', {
      method: 'POST',
      body: JSON.stringify(requestBody)
    });
  },

  async vote(pollId, answers) {
    const requestBody = {
      answers: Object.entries(answers).map(([questionId, selectedOptionIds]) => ({
        questionId,
        selectedOptionIds,
        textValue: null
      }))
    };

    return request(`/surveys/${encodeURIComponent(pollId)}/votes`, {
      method: 'POST',
      body: JSON.stringify(requestBody)
    });
  },

  async getPollStats(pollId) {
    return request(`/surveys/${encodeURIComponent(pollId)}/stats`);
  }
};
