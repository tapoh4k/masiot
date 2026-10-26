import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import './SubSubItemPage.css';

function SubSubItemPage({ treeData, onAddAgent }) {
  const { parentId, id } = useParams();
  const [showAgentModal, setShowAgentModal] = useState(false);
  const [agentParams, setAgentParams] = useState({
    name: '',
    param1: false,
    param2: false,
    param3: false,
    param4: false,
    param5: false
  });

  const parentIdNum = parseInt(parentId);
  const subItemIdNum = parseInt(id);

  const parentItem = treeData.find(item => item.id === parentIdNum);
  const subItem = parentItem?.subItems.find(sub => sub.id === subItemIdNum);

  if (!subItem) {
    return <div className="subsubitem-page">Элемент не найден</div>;
  }

  const handleParamChange = (param) => {
    setAgentParams(prev => ({
      ...prev,
      [param]: !prev[param]
    }));
  };

  const handleNameChange = (e) => {
    setAgentParams(prev => ({
      ...prev,
      name: e.target.value
    }));
  };

  const handleSaveAgent = () => {
    if (!agentParams.name.trim()) {
      alert('Пожалуйста, введите имя агента');
      return;
    }

    const selectedParams = Object.keys(agentParams)
      .filter(key => key !== 'name' && agentParams[key]);

    if (selectedParams.length === 0) {
      alert('Пожалуйста, выберите хотя бы один параметр');
      return;
    }

    onAddAgent(parentIdNum, subItemIdNum, {
      name: agentParams.name,
      parameters: selectedParams
    });

    setAgentParams({
      name: '',
      param1: false,
      param2: false,
      param3: false,
      param4: false,
      param5: false
    });
    setShowAgentModal(false);
    alert('Агент успешно создан!');
  };

  return (
    <div className="subsubitem-page">
      <h1>{subItem.name}</h1>

      <div className="subsubitem-actions">
        <button
          className="create-agent-btn"
          onClick={() => setShowAgentModal(true)}
        >
          Создать агента
        </button>
      </div>

      <div className="agents-section">
        <h3>Созданные агенты:</h3>
        {!subItem.agents || subItem.agents.length === 0 ? (
          <p className="no-agents">Нет созданных агентов</p>
        ) : (
          <div className="agents-list">
            {subItem.agents.map(agent => (
              <div key={agent.id} className="agent-card">
                <div className="agent-icon">👤</div>
                <div className="agent-name">{agent.name}</div>
                <div className="agent-params">
                  Параметры: {agent.parameters.join(', ')}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {showAgentModal && (
        <div className="modal-overlay" onClick={() => setShowAgentModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Создание агента</h2>
              <button
                className="modal-close"
                onClick={() => setShowAgentModal(false)}
              >
                ✕
              </button>
            </div>

            <div className="modal-body">
              <div className="form-group">
                <label>Имя агента:</label>
                <input
                  type="text"
                  value={agentParams.name}
                  onChange={handleNameChange}
                  placeholder="Введите имя агента"
                  className="agent-name-input"
                />
              </div>

              <div className="form-group">
                <label>Выберите параметры:</label>
                <div className="parameters-list">
                  <label className="parameter-item">
                    <input
                      type="checkbox"
                      checked={agentParams.param1}
                      onChange={() => handleParamChange('param1')}
                    />
                    <span>Параметр 1</span>
                  </label>
                  <label className="parameter-item">
                    <input
                      type="checkbox"
                      checked={agentParams.param2}
                      onChange={() => handleParamChange('param2')}
                    />
                    <span>Параметр 2</span>
                  </label>
                  <label className="parameter-item">
                    <input
                      type="checkbox"
                      checked={agentParams.param3}
                      onChange={() => handleParamChange('param3')}
                    />
                    <span>Параметр 3</span>
                  </label>
                  <label className="parameter-item">
                    <input
                      type="checkbox"
                      checked={agentParams.param4}
                      onChange={() => handleParamChange('param4')}
                    />
                    <span>Параметр 4</span>
                  </label>
                  <label className="parameter-item">
                    <input
                      type="checkbox"
                      checked={agentParams.param5}
                      onChange={() => handleParamChange('param5')}
                    />
                    <span>Параметр 5</span>
                  </label>
                </div>
              </div>
            </div>

            <div className="modal-footer">
              <button className="save-agent-btn" onClick={handleSaveAgent}>
                Сохранить выбранные параметры
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default SubSubItemPage;
