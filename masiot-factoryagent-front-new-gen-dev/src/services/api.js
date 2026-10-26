export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:3001/api';

// Получение заголовков аутентификации из localStorage
const getAuthHeaders = (extraHeaders = {}) => {
  const storedUser = localStorage.getItem('currentUser');
  const authHeaders = {};
  if (storedUser) {
    try {
      const user = JSON.parse(storedUser);
      if (user.id) authHeaders['X-User-Id'] = String(user.id);
      if (user.role) authHeaders['X-User-Role'] = user.role;
    } catch {
      // ignore parse error
    }
  }
  return { ...authHeaders, ...extraHeaders };
};

// Tree API
export const treeAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/tree`);
    if (!response.ok) throw new Error('Ошибка загрузки дерева');
    return response.json();
  },

  create: async (nodeData) => {
    const response = await fetch(`${API_URL}/tree`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(nodeData)
    });
    if (!response.ok) throw new Error('Ошибка создания узла');
    return response.json();
  },

  update: async (id, nodeData) => {
    const response = await fetch(`${API_URL}/tree/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(nodeData)
    });
    if (!response.ok) throw new Error('Ошибка обновления узла');
    return response.json();
  },

  // Allows setting parent_id to null explicitly
  updateParent: async (id, parentId) => {
    const response = await fetch(`${API_URL}/tree/${id}/parent`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ parent_id: parentId === undefined ? null : parentId })
    });
    if (!response.ok) throw new Error('Ошибка обновления parent_id');
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/tree/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления узла');
    return response.json();
  }
};

// Host API
export const hostAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/hosts`, { headers: getAuthHeaders() });
    if (!response.ok) throw new Error('Ошибка загрузки Hosts');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/hosts/${id}`, { headers: getAuthHeaders() });
    if (!response.ok) throw new Error('Ошибка загрузки Host');
    return response.json();
  },

  create: async (data) => {
    const response = await fetch(`${API_URL}/hosts`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      let errorData = null;
      try { errorData = await response.json(); } catch { errorData = null; }
      throw new Error(errorData?.message || `Ошибка создания Host (HTTP ${response.status})`);
    }
    return response.json();
  },

  update: async (id, data) => {
    const response = await fetch(`${API_URL}/hosts/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(data)
    });
    if (!response.ok) {
      let errorData = null;
      try { errorData = await response.json(); } catch { errorData = null; }
      throw new Error(errorData?.message || 'Ошибка обновления Host');
    }
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/hosts/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders()
    });
    if (!response.ok) {
      let errorData = null;
      try { errorData = await response.json(); } catch { errorData = null; }
      throw new Error(errorData?.message || 'Ошибка удаления Host');
    }
    return response.json();
  },

  sendCommand: async (id, command, body = {}) => {
    const response = await fetch(`${API_URL}/hosts/${id}/commands/${command}`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(body)
    });
    if (!response.ok) throw new Error('Ошибка отправки команды Host');
    return response.json();
  }
};

// Agent API
export const agentAPI = {
  getByNodeId: async (nodeId) => {
    const response = await fetch(`${API_URL}/agents/${nodeId}`);
    if (!response.ok) throw new Error('Ошибка загрузки агента');
    return response.json();
  },

  create: async (agentData) => {
    const response = await fetch(`${API_URL}/agents`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(agentData)
    });
    if (!response.ok) {
      let errorData = null;
      try { errorData = await response.json(); } catch { errorData = null; }
      console.error('Ошибка создания агента:', errorData);
      console.error('Данные агента:', agentData);
      const details = errorData?.message ||
        (errorData?.errors && Object.values(errorData.errors).flat().join('; '));
      throw new Error(details ? `Ошибка создания агента: ${details}` : `Ошибка создания агента (HTTP ${response.status})`);
    }
    return response.json();
  },

  update: async (nodeId, agentData) => {
    const response = await fetch(`${API_URL}/agents/${nodeId}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(agentData)
    });
    if (!response.ok) throw new Error('Ошибка обновления агента');
    return response.json();
  },

  delete: async (nodeId) => {
    const response = await fetch(`${API_URL}/agents/${nodeId}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления агента');
    return response.json();
  }
};

// File Upload API
export const fileUploadAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/file-uploads`);
    if (!response.ok) throw new Error('Ошибка загрузки истории файлов');
    return response.json();
  },

  create: async (uploadData) => {
    const response = await fetch(`${API_URL}/file-uploads`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(uploadData)
    });
    if (!response.ok) throw new Error('Ошибка сохранения записи о загрузке');
    return response.json();
  }
};

// AI / Gemini settings (stored encrypted on server)
export const aiSettingsAPI = {
  get: async () => {
    const response = await fetch(`${API_URL}/settings/ai`, {
      headers: getAuthHeaders()
    });
    if (!response.ok) throw new Error('Ошибка загрузки настроек нейросети');
    return response.json();
  },

  update: async (payload) => {
    const response = await fetch(`${API_URL}/settings/ai`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(payload)
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(data.message || data.detail || 'Ошибка сохранения настроек нейросети');
    }
    return data;
  }
};

// Drawing analysis API (Gemini vision → labor intensity)
export const drawingsAPI = {
  analyze: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await fetch(`${API_URL}/drawings/analyze`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: formData
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(data.message || data.detail || 'Ошибка анализа чертежа');
    }
    return data;
  },

  getHistory: async (limit = 50) => {
    const response = await fetch(`${API_URL}/drawings?limit=${limit}`, {
      headers: getAuthHeaders()
    });
    if (!response.ok) throw new Error('Ошибка загрузки истории чертежей');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/drawings/${id}`, {
      headers: getAuthHeaders()
    });
    if (!response.ok) throw new Error('Ошибка загрузки результата анализа');
    return response.json();
  }
};

// Statistics API
export const statisticsAPI = {
  get: async () => {
    const response = await fetch(`${API_URL}/statistics`);
    if (!response.ok) throw new Error('Ошибка загрузки статистики');
    return response.json();
  },

  recalculate: async () => {
    const response = await fetch(`${API_URL}/statistics/recalculate`, {
      method: 'POST'
    });
    if (!response.ok) throw new Error('Ошибка пересчета статистики');
    return response.json();
  }
};

// Parameter Dictionary API
export const parameterDictionaryAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/parameter-dictionary`);
    if (!response.ok) throw new Error('Ошибка загрузки словаря параметров');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/parameter-dictionary/${id}`);
    if (!response.ok) throw new Error('Ошибка загрузки параметра');
    return response.json();
  },

  create: async (parameterData) => {
    const response = await fetch(`${API_URL}/parameter-dictionary`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(parameterData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка создания параметра');
    }
    return response.json();
  },

  update: async (id, parameterData) => {
    const response = await fetch(`${API_URL}/parameter-dictionary/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(parameterData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка обновления параметра');
    }
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/parameter-dictionary/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления параметра');
    return response.json();
  }
};

// Workflow Template API
export const workflowTemplateAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/workflow-templates`);
    if (!response.ok) throw new Error('Ошибка загрузки шаблонов workflow');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/workflow-templates/${id}`);
    if (!response.ok) throw new Error('Ошибка загрузки шаблона workflow');
    return response.json();
  },

  create: async (workflowData) => {
    const response = await fetch(`${API_URL}/workflow-templates`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(workflowData)
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || error.error || 'Ошибка создания шаблона workflow');
    }
    return response.json();
  },

  update: async (id, workflowData) => {
    const response = await fetch(`${API_URL}/workflow-templates/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(workflowData)
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || error.error || 'Ошибка обновления шаблона workflow');
    }
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/workflow-templates/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления шаблона workflow');
    return response.json();
  }
};

// Messages API
export const messagesAPI = {
  sendWork: async (messageData) => {
    const response = await fetch(`${API_URL}/messages/work`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(messageData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка отправки рабочего сообщения');
    }
    return response.json();
  },

  // Все листы ТП-* файла (или только sheetName) → папка "Технологические процессы",
  // каждый ТП сохраняется со всеми операциями
  attachTechProcess: async (file, sheetName) => {
    const formData = new FormData();
    formData.append('file', file);
    if (sheetName) {
      formData.append('sheetName', sheetName);
    }

    const response = await fetch(`${API_URL}/tech-process/attach`, {
      method: 'POST',
      body: formData
    });

    if (!response.ok) {
      let err;
      try { err = await response.json(); } catch { err = null; }
      throw new Error(err?.message || 'Ошибка прикрепления техпроцесса');
    }

    return response.json();
  },

  startTechProcess: async (agentId) => {
    const response = await fetch(`${API_URL}/tech-process/start/${agentId}`, {
      method: 'POST'
    });

    if (!response.ok) {
      let err;
      try { err = await response.json(); } catch { err = null; }
      throw new Error(err?.message || 'Ошибка запуска техпроцесса');
    }

    return response.json();
  },

  getTechProcessForAgent: async (agentId) => {
    const response = await fetch(`${API_URL}/tech-process/agent/${agentId}`);
    if (!response.ok) {
      let err;
      try { err = await response.json(); } catch { err = null; }
      throw new Error(err?.message || 'Ошибка загрузки техпроцесса агента');
    }
    return response.json();
  },

  getTechProcessOperations: async (agentId) => {
    const response = await fetch(`${API_URL}/tech-process/agent/${agentId}/operations`);
    if (!response.ok) {
      let err;
      try { err = await response.json(); } catch { err = null; }
      throw new Error(err?.message || 'Ошибка загрузки операций техпроцесса');
    }
    return response.json();
  },

  sendAgentInfo: async (messageData) => {
    const response = await fetch(`${API_URL}/messages/agent-info`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(messageData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка отправки информации об агенте');
    }
    return response.json();
  },

  sendNewWorkflow: async (messageData) => {
    const response = await fetch(`${API_URL}/messages/new-workflow`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(messageData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка отправки нового workflow');
    }
    return response.json();
  }
};

// Workflow Execution API
export const workflowAPI = {
  start: async (workflowData) => {
    const response = await fetch(`${API_URL}/workflow/start`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(workflowData)
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка запуска workflow');
    }
    return response.json();
  },

  continue: async (id, fromStep) => {
    const response = await fetch(`${API_URL}/workflow/continue/${id}`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ from_step: fromStep })
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Ошибка продолжения workflow');
    }
    return response.json();
  },

  stop: async (id) => {
    const response = await fetch(`${API_URL}/workflow/stop/${id}`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' })
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.error || 'Ошибка остановки workflow');
    }
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/workflow/${id}`);
    if (!response.ok) throw new Error('Ошибка загрузки workflow');
    return response.json();
  },

  getByAgent: async (agentId) => {
    const response = await fetch(`${API_URL}/workflow/agent/${agentId}`);
    if (!response.ok) throw new Error('Ошибка загрузки workflows агента');
    return response.json();
  },

  getByStatus: async (status) => {
    const response = await fetch(`${API_URL}/workflow/status/${status}`);
    if (!response.ok) throw new Error('Ошибка загрузки workflows по статусу');
    return response.json();
  },

  getAll: async () => {
    const response = await fetch(`${API_URL}/workflow`);
    if (!response.ok) throw new Error('Ошибка загрузки всех workflows');
    return response.json();
  },

  updateStep: async (id, step) => {
    const response = await fetch(`${API_URL}/workflow/${id}/step`, {
      method: 'PATCH',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ step })
    });
    if (!response.ok) throw new Error('Ошибка обновления шага workflow');
    return response.json();
  },

  updateStatus: async (id, status) => {
    const response = await fetch(`${API_URL}/workflow/${id}/status`, {
      method: 'PATCH',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ status })
    });
    if (!response.ok) throw new Error('Ошибка обновления статуса workflow');
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/workflow/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления workflow');
    return response.json();
  }
};

export const workflowFunctionsAPI = {
  getAll: async () => {
    const response = await fetch(`${API_URL}/workflowfunctions`);
    if (!response.ok) throw new Error('Ошибка загрузки функций');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/workflowfunctions/${id}`);
    if (!response.ok) throw new Error('Ошибка загрузки функции');
    return response.json();
  },

  create: async (functionData) => {
    const response = await fetch(`${API_URL}/workflowfunctions`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(functionData)
    });
    if (!response.ok) throw new Error('Ошибка создания функции');
    return response.json();
  },

  update: async (id, functionData) => {
    const response = await fetch(`${API_URL}/workflowfunctions/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(functionData)
    });
    if (!response.ok) throw new Error('Ошибка обновления функции');
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/workflowfunctions/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления функции');
  }
};

// Blank OKPD-2 Mapping API (справочник ОКПД-2 для заготовок)
export const blankOkpd2API = {
  findByBlankName: async (blankName) => {
    const response = await fetch(`${API_URL}/blank-okpd2/find-by-name?name=${encodeURIComponent(blankName)}`);
    if (!response.ok) throw new Error('Ошибка поиска ОКПД-2 для заготовки');
    return response.json();
  },

  getAll: async () => {
    const response = await fetch(`${API_URL}/blank-okpd2/all`);
    if (!response.ok) throw new Error('Ошибка загрузки справочника заготовок');
    return response.json();
  },

  getById: async (id) => {
    const response = await fetch(`${API_URL}/blank-okpd2/${id}`);
    if (!response.ok) throw new Error('Ошибка получения записи');
    return response.json();
  },

  getByCode: async (code) => {
    const response = await fetch(`${API_URL}/blank-okpd2/by-code/${encodeURIComponent(code)}`);
    if (!response.ok) throw new Error('Ошибка получения записей по коду');
    return response.json();
  },

  create: async (mappingData) => {
    const response = await fetch(`${API_URL}/blank-okpd2`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(mappingData)
    });
    if (!response.ok) throw new Error('Ошибка создания записи');
    return response.json();
  },

  bulkUpsert: async (items) => {
    const response = await fetch(`${API_URL}/blank-okpd2/bulk-upsert`, {
      method: 'POST',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify({ items })
    });
    if (!response.ok) throw new Error('Ошибка массового сохранения ОКПД-2');
    return response.json();
  },

  update: async (id, mappingData) => {
    const response = await fetch(`${API_URL}/blank-okpd2/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders({ 'Content-Type': 'application/json' }),
      body: JSON.stringify(mappingData)
    });
    if (!response.ok) throw new Error('Ошибка обновления записи');
    return response.json();
  },

  delete: async (id) => {
    const response = await fetch(`${API_URL}/blank-okpd2/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error('Ошибка удаления записи');
    return response.json();
  }
};
