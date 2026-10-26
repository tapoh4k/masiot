import React, { useState } from 'react';
import './NodeConfigModal.css';

const NodeConfigModal = ({ node, isOpen, onClose, onSave }) => {
  const [label, setLabel] = useState(node?.data?.label || '');
  const [description, setDescription] = useState(node?.data?.description || '');
  const [jsonConfig, setJsonConfig] = useState(
    JSON.stringify(node?.data?.config || {}, null, 2)
  );

  if (!isOpen || !node) return null;

  const handleSave = () => {
    try {
      const config = JSON.parse(jsonConfig);
      onSave({
        ...node,
        data: {
          ...node.data,
          label,
          description,
          config,
        },
      });
      onClose();
    } catch (error) {
      alert('Ошибка в JSON конфигурации');
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content node-config-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Настройка узла</h2>
          <button className="close-button" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          <div className="form-group">
            <label>Название узла</label>
            <input
              type="text"
              className="form-input"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              placeholder="Введите название"
            />
          </div>

          <div className="form-group">
            <label>Описание</label>
            <textarea
              className="form-textarea"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Введите описание узла"
              rows={3}
            />
          </div>

          <div className="form-group">
            <label>JSON конфигурация</label>
            <textarea
              className="form-textarea json-editor"
              value={jsonConfig}
              onChange={(e) => setJsonConfig(e.target.value)}
              placeholder='{"key": "value"}'
              rows={10}
              spellCheck={false}
            />
            <p className="form-hint">
              Введите JSON конфигурацию для узла (параметры, настройки и т.д.)
            </p>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn-cancel" onClick={onClose}>
            Отмена
          </button>
          <button className="btn-save-config" onClick={handleSave}>
            Сохранить
          </button>
        </div>
      </div>
    </div>
  );
};

export default NodeConfigModal;
