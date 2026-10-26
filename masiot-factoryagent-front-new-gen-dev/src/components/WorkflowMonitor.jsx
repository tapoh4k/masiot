import React, { useState, useEffect } from 'react';
import { workflowAPI } from '../services/api';
import './WorkflowMonitor.css';

function WorkflowMonitor() {
  const [workflows, setWorkflows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('all'); // all, running, completed, error
  const [selectedWorkflow, setSelectedWorkflow] = useState(null);

  useEffect(() => {
    loadWorkflows();
    const interval = setInterval(loadWorkflows, 3000); // Обновление каждые 3 секунды
    return () => clearInterval(interval);
  }, [filter]);

  const loadWorkflows = async () => {
    try {
      let data;
      if (filter === 'all') {
        data = await workflowAPI.getAll();
      } else {
        const statusMap = { running: 0, completed: 1, error: -1 };
        data = await workflowAPI.getByStatus(statusMap[filter]);
      }
      setWorkflows(data);
      setLoading(false);
    } catch (error) {
      console.error('Ошибка загрузки workflows:', error);
      setLoading(false);
    }
  };

  const handleContinue = async (wfId, fromStep) => {
    try {
      await workflowAPI.continue(wfId, fromStep);
      alert('Workflow продолжен');
      loadWorkflows();
    } catch (error) {
      console.error('Ошибка продолжения workflow:', error);
      alert('Ошибка: ' + error.message);
    }
  };

  const handleDelete = async (wfId) => {
    if (window.confirm('Удалить этот workflow?')) {
      try {
        await workflowAPI.delete(wfId);
        alert('Workflow удален');
        loadWorkflows();
      } catch (error) {
        console.error('Ошибка удаления workflow:', error);
        alert('Ошибка: ' + error.message);
      }
    }
  };

  const getStatusText = (status) => {
    switch (status) {
      case 0: return 'Выполняется';
      case 1: return 'Завершен';
      case -1: return 'Ошибка';
      default: return 'Неизвестно';
    }
  };

  const getStatusClass = (status) => {
    switch (status) {
      case 0: return 'status-running';
      case 1: return 'status-completed';
      case -1: return 'status-error';
      default: return 'status-unknown';
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU');
  };

  const calculateDuration = (startTime) => {
    const start = new Date(startTime);
    const now = new Date();
    const diff = Math.floor((now - start) / 1000); // секунды

    if (diff < 60) return `${diff} сек`;
    if (diff < 3600) return `${Math.floor(diff / 60)} мин`;
    return `${Math.floor(diff / 3600)} ч ${Math.floor((diff % 3600) / 60)} мин`;
  };

  return (
    <div className="workflow-monitor">
      <div className="monitor-header">
        <h1>Мониторинг Workflow</h1>
        <button className="btn-refresh" onClick={loadWorkflows}>
          Обновить
        </button>
      </div>

      <div className="monitor-filters">
        <button
          className={`filter-btn ${filter === 'all' ? 'active' : ''}`}
          onClick={() => setFilter('all')}
        >
          Все ({workflows.length})
        </button>
        <button
          className={`filter-btn ${filter === 'running' ? 'active' : ''}`}
          onClick={() => setFilter('running')}
        >
          Выполняются
        </button>
        <button
          className={`filter-btn ${filter === 'completed' ? 'active' : ''}`}
          onClick={() => setFilter('completed')}
        >
          Завершены
        </button>
        <button
          className={`filter-btn ${filter === 'error' ? 'active' : ''}`}
          onClick={() => setFilter('error')}
        >
          Ошибки
        </button>
      </div>

      {loading ? (
        <div className="loading">Загрузка...</div>
      ) : workflows.length === 0 ? (
        <div className="empty-state">
          <p>Нет активных workflow</p>
        </div>
      ) : (
        <div className="workflows-grid">
          {workflows.map((wf) => (
            <div key={wf.id} className={`workflow-card ${getStatusClass(wf.status)}`}>
              <div className="card-header">
                <h3>{wf.wf_type || wf.wfType}</h3>
                <span className={`status-badge ${getStatusClass(wf.status)}`}>
                  {getStatusText(wf.status)}
                </span>
              </div>

              <div className="card-body">
                <div className="info-row">
                  <span className="label">ID:</span>
                  <span className="value">{wf.id}</span>
                </div>
                <div className="info-row">
                  <span className="label">Агент:</span>
                  <span className="value">{wf.agent_id || wf.agentId}</span>
                </div>
                <div className="info-row">
                  <span className="label">Шаг:</span>
                  <span className="value">{wf.step}</span>
                </div>
                <div className="info-row">
                  <span className="label">Начало:</span>
                  <span className="value">{formatDate(wf.time_start || wf.timeStart)}</span>
                </div>
                {wf.status === 0 && (
                  <div className="info-row">
                    <span className="label">Длительность:</span>
                    <span className="value">{calculateDuration(wf.time_start || wf.timeStart)}</span>
                  </div>
                )}
                {wf.initiator && (
                  <div className="info-row">
                    <span className="label">Инициатор:</span>
                    <span className="value">{wf.initiator}</span>
                  </div>
                )}
              </div>

              <div className="card-actions">
                <button
                  className="btn-details"
                  onClick={() => setSelectedWorkflow(wf)}
                >
                  Детали
                </button>
                {wf.status === -1 && (
                  <button
                    className="btn-continue"
                    onClick={() => handleContinue(wf.id, wf.step)}
                  >
                    Продолжить
                  </button>
                )}
                <button
                  className="btn-delete-small"
                  onClick={() => handleDelete(wf.id)}
                >
                  Удалить
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedWorkflow && (
        <div className="modal-overlay" onClick={() => setSelectedWorkflow(null)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Детали Workflow #{selectedWorkflow.id}</h2>
              <button className="btn-close" onClick={() => setSelectedWorkflow(null)}>×</button>
            </div>
            <div className="modal-body">
              <div className="detail-section">
                <h3>Основная информация</h3>
                <div className="detail-grid">
                  <div className="detail-item">
                    <span className="detail-label">Тип:</span>
                    <span className="detail-value">{selectedWorkflow.wf_type || selectedWorkflow.wfType}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Агент:</span>
                    <span className="detail-value">{selectedWorkflow.agent_id || selectedWorkflow.agentId}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Статус:</span>
                    <span className="detail-value">{getStatusText(selectedWorkflow.status)}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Шаг:</span>
                    <span className="detail-value">{selectedWorkflow.step}</span>
                  </div>
                </div>
              </div>

              <div className="detail-section">
                <h3>Параметры</h3>
                <pre className="json-display">
                  {JSON.stringify(
                    typeof selectedWorkflow.parameters === 'string'
                      ? JSON.parse(selectedWorkflow.parameters)
                      : selectedWorkflow.parameters,
                    null,
                    2
                  )}
                </pre>
              </div>

              <div className="detail-section">
                <h3>Функции</h3>
                <pre className="json-display">
                  {JSON.stringify(
                    typeof selectedWorkflow.wf_functions === 'string'
                      ? JSON.parse(selectedWorkflow.wf_functions)
                      : selectedWorkflow.wf_functions,
                    null,
                    2
                  )}
                </pre>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default WorkflowMonitor;
