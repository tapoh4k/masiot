import React, { useState, useEffect } from 'react';
import { useLocation, useParams, useNavigate } from 'react-router-dom';
import AddParametersModal from './AddParametersModal.jsx';
import { API_URL, blankOkpd2API, parameterDictionaryAPI, hostAPI } from '../services/api.js';
import './BranchPage.css';

function BranchPage({ onToggleEditMode, onAddAgent, onDeleteNode, treeNodes }) {
  const location = useLocation();
  const navigate = useNavigate();
  const { branchId } = useParams();
  const branchName = location.state?.branchName || 'Токарный цех';
  const nodeId = location.state?.nodeId || branchId;

  // Load active tab from localStorage or default to 'parametry'
  const [activeTab, setActiveTab] = useState(() => {
    const savedTab = localStorage.getItem('branchPageActiveTab');
    return savedTab || 'parametry';
  });

  const [agentName, setAgentName] = useState('');

  // Save active tab to localStorage when it changes
  useEffect(() => {
    localStorage.setItem('branchPageActiveTab', activeTab);
  }, [activeTab]);
  const [quantity, setQuantity] = useState(1);
  const [selectedOKPD2, setSelectedOKPD2] = useState('');
  const [manualOkpd2, setManualOkpd2] = useState(false);
  const [selectedParams, setSelectedParams] = useState({});
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [okpList, setOkpList] = useState([]);
  const [loadingOkp, setLoadingOkp] = useState(false);
  const [searchingOkp, setSearchingOkp] = useState(false);
  const [parameters, setParameters] = useState([]);
  const [loadingParameters, setLoadingParameters] = useState(false);
  const [agents, setAgents] = useState([]);
  const [loadingAgents, setLoadingAgents] = useState(false);
  const [hosts, setHosts] = useState([]);
  const [selectedHostId, setSelectedHostId] = useState('');

  // Загрузка параметров и агентов при монтировании компонента
  useEffect(() => {
    loadParameters();
    loadAgents();
    loadHosts();
  }, [nodeId]);

  const loadHosts = async () => {
    try {
      const data = await hostAPI.getAll();
      setHosts(data);
      if (data.length > 0) {
        setSelectedHostId(String(data[0].id));
      }
    } catch (e) {
      console.error('Ошибка загрузки Hosts:', e);
    }
  };

  // Автоматический поиск ОКПД-2 при изменении имени агента
  useEffect(() => {
    const searchTimer = setTimeout(() => {
      if (agentName.trim().length > 2) {
        searchOkpd2ByName(agentName.trim());
      } else {
        setOkpList([]);
        setSelectedOKPD2('');
      }
    }, 500); // Задержка 500мс после окончания ввода

    return () => clearTimeout(searchTimer);
  }, [agentName]);

  const loadAgents = async () => {
    if (!nodeId) return;

    try {
      setLoadingAgents(true);
      const response = await fetch(`${API_URL}/agents/${nodeId}`);
      if (response.ok) {
        const agentsData = await response.json();
        setAgents(agentsData);
      }
    } catch (error) {
      console.error('Ошибка загрузки агентов:', error);
    } finally {
      setLoadingAgents(false);
    }
  };

  const searchOkpd2ByName = async (name) => {
    if (manualOkpd2) return; // Не ищем автоматически если пользователь вводит вручную
    try {
      setSearchingOkp(true);
      const result = await blankOkpd2API.findByBlankName(name);
      if (result && result.okpd2Code) {
        setOkpList([{ keywords: result.keywords, okpd2_code: result.okpd2Code, description: result.description, source: result.source }]);
        setSelectedOKPD2(result.okpd2Code);
      } else {
        setOkpList([]);
        setSelectedOKPD2('');
      }
    } catch (error) {
      console.error('Ошибка поиска ОКПД-2:', error);
      setOkpList([]);
      setSelectedOKPD2('');
    } finally {
      setSearchingOkp(false);
    }
  };

  const loadParameters = async () => {
    try {
      setLoadingParameters(true);
      const parametersData = await parameterDictionaryAPI.getAll();
      setParameters(parametersData);
    } catch (error) {
      console.error('Ошибка загрузки параметров:', error);
      alert('Ошибка загрузки параметров: ' + error.message);
    } finally {
      setLoadingParameters(false);
    }
  };

  const handleParamToggle = (paramId) => {
    setSelectedParams(prev => ({
      ...prev,
      [paramId]: !prev[paramId]
    }));
  };

  const handleTreeSettings = () => {
    onToggleEditMode && onToggleEditMode();
  };

  const handleDeleteBranch = async () => {
    if (window.confirm('Вы уверены, что хотите удалить эту ветку? Все вложенные агенты будут удалены.')) {
      if (onDeleteNode && nodeId) {
        try {
          await onDeleteNode(nodeId);
          alert('Ветка удалена');
          navigate('/');
        } catch (error) {
          console.error('Ошибка удаления ветки:', error);
          alert('Ошибка при удалении ветки');
        }
      } else {
        alert('Ошибка: не удалось удалить ветку');
      }
    }
  };

  const handleCreateAgent = async () => {
    if (!agentName.trim()) {
      alert('Пожалуйста, введите имя агента');
      return;
    }

    if (!selectedHostId) {
      alert('Пожалуйста, выберите Host');
      return;
    }

    if (!selectedOKPD2) {
      alert('Пожалуйста, выберите ОКПД-2');
      return;
    }

    const selectedParamIds = Object.keys(selectedParams).filter(key => selectedParams[key]);

    if (selectedParamIds.length === 0) {
      alert('Пожалуйста, выберите хотя бы один параметр');
      return;
    }

    const selectedParameters = parameters
      .filter(p => selectedParamIds.includes(String(p.id)))
      .map(p => ({
        id: p.id,
        name: p.name,
        unit: p.unit || '',
        value: '',
        description: p.description || ''
      }));

    // Создаем указанное количество агентов
    try {
      // Если код введён вручную — сохраняем в справочник
      if (manualOkpd2 && selectedOKPD2.trim()) {
        try {
          await blankOkpd2API.create({
            keywords: agentName.trim(),
            okpd2Code: selectedOKPD2.trim(),
            description: `Добавлено вручную: ${agentName.trim()}`
          });
          console.log(`[OKPD-2] Новый код сохранён в справочник: ${selectedOKPD2} для "${agentName}"`);
        } catch (e) {
          console.warn('[OKPD-2] Не удалось сохранить в справочник:', e);
        }
      }

      let createdCount = 0;
      for (let i = 0; i < quantity; i++) {
        const agentData = {
          name: agentName,
          host_id: Number(selectedHostId),
          type: 'equipment',
          okpd2_code: selectedOKPD2,
          quantity: 1, // Каждый агент имеет количество 1
          queueNumber: Date.now() + i,
          parameters: selectedParameters
        };

        if (onAddAgent && nodeId) {
          await onAddAgent(nodeId, agentData);
          createdCount++;
        }
      }

      if (createdCount > 0) {
        alert(`Создано агентов: ${createdCount}`);
        setAgentName('');
        setQuantity(1);
        setSelectedOKPD2('');
        setManualOkpd2(false);
        setSelectedParams({});
      } else {
        alert('Ошибка: не удалось добавить агентов');
      }
    } catch (error) {
      console.error('Ошибка создания агентов:', error);
      alert('Ошибка при создании агентов: ' + error.message);
    }
  };

  const handleSaveParameters = async (selectedParamIds) => {
    const selectedParameters = parameters
      .filter(p => selectedParamIds.includes(String(p.id)))
      .map(p => ({
        id: p.id,
        name: p.name,
        unit: p.unit || '',
        value: '',
        description: p.description || ''
      }));

    const agentData = {
      name: agentName,
      host_id: Number(selectedHostId),
      type: 'equipment',
      okpd2_code: selectedOKPD2,
      quantity: 1,
      queueNumber: Date.now(),
      parameters: selectedParameters
    };

    if (onAddAgent && nodeId) {
      try {
        await onAddAgent(nodeId, agentData);
      } catch (error) {
        alert('Ошибка при создании агента: ' + error.message);
        return;
      }
      alert(`Агент "${agentName}" создан с ${selectedParameters.length} параметрами`);

      setAgentName('');
      setQuantity(1);
      setSelectedOKPD2('');
      setSelectedParams({});
    } else {
      alert('Ошибка: не удалось добавить агента');
    }
  };

  return (
    <div className="branch-page">
      <div className="branch-header">
        <h1 className="branch-title">{branchName}</h1>
        <button className="btn-tree-settings" onClick={handleTreeSettings}>
          Настройка дерева
        </button>
      </div>
      <div className="branch-divider"></div>

      <div className="branch-actions">
        <button className="btn-delete-branch" onClick={handleDeleteBranch}>
          Удалить ветку
        </button>
      </div>

      <div className="branch-tabs">
        <button
          className={`tab-button ${activeTab === 'info' ? 'active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          Инфо
        </button>
        <button
          className={`tab-button ${activeTab === 'process' ? 'active' : ''}`}
          onClick={() => setActiveTab('process')}
        >
          Рабочий процесс
        </button>
        <button
          className={`tab-button ${activeTab === 'parametry' ? 'active' : ''}`}
          onClick={() => setActiveTab('parametry')}
        >
          Параметры
        </button>
      </div>
      <div className="tabs-divider"></div>

      <div className="tab-content">
        {activeTab === 'info' && (
          <div className="tab-panel">
            <div className="info-card">
              <h2 className="card-title">Информация о ветке</h2>
              {loadingAgents ? (
                <p style={{ textAlign: 'center', padding: '20px' }}>Загрузка...</p>
              ) : (
                <div className="info-stats">
                  <div className="stat-item">
                    <span className="stat-label">Всего агентов:</span>
                    <span className="stat-value">{agents.length}</span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Агентов с workflow:</span>
                    <span className="stat-value">
                      {agents.filter(a => a.workflow_applied).length}
                    </span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Готовых агентов:</span>
                    <span className="stat-value">
                      {agents.filter(a => a.workflow_status === 'ready').length}
                    </span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Всего параметров:</span>
                    <span className="stat-value">
                      {agents.reduce((sum, a) => sum + (a.parameters?.length || 0), 0)}
                    </span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Уникальных ОКПД-2:</span>
                    <span className="stat-value">
                      {new Set(agents.map(a => a.okpd2_code).filter(Boolean)).size}
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'process' && (
          <div className="tab-panel">
            <div className="workflow-status-card">
              <h2 className="card-title">Статус workflow агентов</h2>
              {loadingAgents ? (
                <p style={{ textAlign: 'center', padding: '20px' }}>Загрузка...</p>
              ) : agents.length === 0 ? (
                <p style={{ textAlign: 'center', padding: '20px', color: '#999' }}>
                  В этой ветке нет агентов
                </p>
              ) : (
                <div className="workflow-status-table-container">
                  <table className="workflow-status-table">
                    <thead>
                      <tr>
                        <th>Имя агента</th>
                        <th>ОКПД-2</th>
                        <th>Применённые workflow</th>
                        <th>Статус</th>
                      </tr>
                    </thead>
                    <tbody>
                      {agents.map((agent) => (
                        <tr key={agent.id}>
                          <td>{agent.name || 'Без имени'}</td>
                          <td>{agent.okpd2_code || '-'}</td>
                          <td>
                            {agent.applied_workflows && agent.applied_workflows.length > 0 ? (
                              <span className="workflows-list">
                                {agent.applied_workflows.join(', ')}
                              </span>
                            ) : (
                              <span style={{ color: '#999' }}>Не применены</span>
                            )}
                          </td>
                          <td>
                            <span className={`status-badge-small status-${agent.workflow_status || 'not_ready'}`}>
                              {agent.workflow_status === 'ready' ? '✓ Готов' :
                               agent.workflow_status === 'error' ? '✗ Ошибка' :
                               '○ Не готов'}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'parametry' && (
          <div className="tab-panel">
            <div className="create-agent-card">
              <h2 className="card-title">Создать нового агента</h2>
              <div className="agent-form">
                <div className="form-field">
                  <label className="field-label">Имя агента</label>
                  <input
                    type="text"
                    className="field-input"
                    placeholder="Введите имя агента"
                    value={agentName}
                    onChange={(e) => setAgentName(e.target.value)}
                  />
                </div>
                <div className="form-field">
                  <label className="field-label">Количество:</label>
                  <input
                    type="number"
                    className="field-input"
                    value={quantity}
                    onChange={(e) => setQuantity(parseInt(e.target.value) || 1)}
                    min="1"
                  />
                </div>
                <div className="form-field">
                  <label className="field-label">Host:</label>
                  <select
                    className="field-input"
                    value={selectedHostId}
                    onChange={(e) => setSelectedHostId(e.target.value)}
                  >
                    <option value="">Выберите Host</option>
                    {hosts.map((h) => (
                      <option key={h.id} value={h.id}>
                        {h.name} ({h.type}) — {h.agents_count}/{h.max_agents}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-field">
                  <label className="field-label">ОКПД-2:</label>
                  {searchingOkp ? (
                    <div className="okp-loading" style={{ padding: '10px', color: '#666' }}>
                      Поиск ОКПД-2...
                    </div>
                  ) : manualOkpd2 ? (
                    <div>
                      <input
                        type="text"
                        className="field-input"
                        value={selectedOKPD2}
                        onChange={(e) => setSelectedOKPD2(e.target.value)}
                        placeholder="Введите код ОКПД-2 (например: 28.41.21)"
                        style={{ borderColor: '#2196F3' }}
                      />
                      <button
                        type="button"
                        className="btn-toggle-manual"
                        onClick={() => {
                          setManualOkpd2(false);
                          if (agentName.trim().length > 2) {
                            searchOkpd2ByName(agentName.trim());
                          }
                        }}
                        style={{ marginTop: '6px', fontSize: '12px', color: '#2196F3', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
                      >
                        ← Вернуть автопоиск
                      </button>
                    </div>
                  ) : okpList.length === 0 && agentName.trim().length > 2 ? (
                    <div>
                      <div className="okp-not-found" style={{ padding: '10px', color: '#d32f2f', background: '#ffebee', borderRadius: '4px', marginBottom: '6px' }}>
                        ОКПД-2 не найден для "{agentName}".
                      </div>
                      <button
                        type="button"
                        className="btn-toggle-manual"
                        onClick={() => setManualOkpd2(true)}
                        style={{ fontSize: '12px', color: '#2196F3', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
                      >
                        Ввести код ОКПД-2 вручную →
                      </button>
                    </div>
                  ) : okpList.length === 1 ? (
                    <input
                      type="text"
                      className="field-input"
                      value={selectedOKPD2}
                      readOnly
                      style={{ backgroundColor: '#f5f5f5', cursor: 'not-allowed' }}
                      title="ОКПД-2 автоматически определен"
                    />
                  ) : okpList.length > 1 ? (
                    <select
                      className="field-input"
                      value={selectedOKPD2}
                      onChange={(e) => setSelectedOKPD2(e.target.value)}
                    >
                      <option value="">Выберите ОКПД-2 из найденных</option>
                      {okpList.map((okp) => (
                        <option key={okp.okpd2_code} value={okp.okpd2_code}>
                          {okp.okpd2_code} — {okp.keywords}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="text"
                      className="field-input"
                      value=""
                      placeholder="Введите название оборудования выше"
                      readOnly
                      style={{ backgroundColor: '#f5f5f5', cursor: 'not-allowed' }}
                    />
                  )}
                </div>
              </div>
            </div>

            <div className="parameters-card">
              <h2 className="card-title">Выберите параметры для агента:</h2>
              {loadingParameters ? (
                <p style={{ textAlign: 'center', padding: '20px' }}>Загрузка параметров...</p>
              ) : parameters.length === 0 ? (
                <p style={{ textAlign: 'center', padding: '20px', color: '#999' }}>
                  Словарь параметров пуст. Добавьте параметры в настройках.
                </p>
              ) : (
                <div className="parameters-table-container">
                  <table className="parameters-table">
                    <thead>
                      <tr>
                        <th>Имя параметра</th>
                        <th>Единица измерения</th>
                        <th>Описание</th>
                        <th className="checkbox-column">Выбор</th>
                      </tr>
                    </thead>
                    <tbody>
                      {parameters.map((param) => (
                        <tr key={param.id} className="param-row">
                          <td>{param.name}</td>
                          <td>{param.unit || '-'}</td>
                          <td>{param.description || '-'}</td>
                          <td className="checkbox-cell">
                            <input
                              type="checkbox"
                              checked={selectedParams[param.id] || false}
                              onChange={() => handleParamToggle(param.id)}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <div className="parameters-footer">
                <button className="btn-create-agent" onClick={handleCreateAgent}>
                  Создать агента
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      <AddParametersModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSave={handleSaveParameters}
      />
    </div>
  );
}

export default BranchPage;
