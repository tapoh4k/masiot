import React from 'react';
import './FunctionInfoModal.css';

const FunctionInfoModal = ({ isOpen, onClose, functionData }) => {
  if (!isOpen || !functionData) return null;

  const params = Array.isArray(functionData.needParam)
    ? functionData.needParam
    : (functionData.needParam ? JSON.parse(functionData.needParam) : []);

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content function-info-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>
            <span className="function-icon">{functionData.icon || '⚙️'}</span>
            {functionData.label || functionData.functionName}
          </h2>
          <button className="close-button" onClick={onClose}>&times;</button>
        </div>

        <div className="modal-body">
          <div className="info-section">
            <label className="info-label">Описание</label>
            <div className="info-value">
              {functionData.description || 'Нет описания'}
            </div>
          </div>

          <div className="info-section">
            <label className="info-label">Класс функции</label>
            <div className="info-value code">
              {functionData.functionClass || 'Не указан'}
            </div>
          </div>

          <div className="info-section">
            <label className="info-label">Имя метода</label>
            <div className="info-value code">
              {functionData.functionName || 'Не указано'}
            </div>
          </div>

          <div className="info-section">
            <label className="info-label">Входные параметры</label>
            <div className="params-display">
              {params.length > 0 ? (
                params.map((param, index) => (
                  <div key={index} className="param-badge">
                    {typeof param === 'string' ? param : `${param.name} (${param.type})`}
                  </div>
                ))
              ) : (
                <div className="info-value">Нет параметров</div>
              )}
            </div>
          </div>

          <div className="info-section">
            <label className="info-label">Возвращаемое значение</label>
            <div className="info-value code">
              {functionData.paramReturn || functionData.returns || 'Не указано'}
            </div>
          </div>

          {functionData.config && Object.keys(functionData.config).length > 0 && (
            <div className="info-section">
              <label className="info-label">Конфигурация</label>
              <pre className="config-display">
                {JSON.stringify(functionData.config, null, 2)}
              </pre>
            </div>
          )}

          <div className="info-section">
            <label className="info-label">Тип узла</label>
            <div className="info-value">
              {functionData.type || 'function'}
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn-cancel" onClick={onClose}>
            Закрыть
          </button>
        </div>
      </div>
    </div>
  );
};

export default FunctionInfoModal;
