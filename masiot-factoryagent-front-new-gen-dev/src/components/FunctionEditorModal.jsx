import React, { useState, useEffect } from 'react';
import './FunctionEditorModal.css';

const FunctionEditorModal = ({ isOpen, onClose, onSave, editFunction = null }) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    functionClass: '',
    functionName: '',
    params: [],
    returns: '',
    icon: '⚙️',
    type: 'function'
  });

  const [paramInput, setParamInput] = useState('');
  const [paramType, setParamType] = useState('string');
  const [paramDescription, setParamDescription] = useState('');

  useEffect(() => {
    if (editFunction) {
      let params = [];
      if (Array.isArray(editFunction.params)) {
        // Check if params are objects (new format) or strings (old format)
        params = editFunction.params.map(p => {
          if (typeof p === 'string') {
            return { name: p, type: 'string', description: '' };
          }
          return p;
        });
      } else if (typeof editFunction.params === 'string') {
        const parsed = JSON.parse(editFunction.params);
        params = parsed.map(p => {
          if (typeof p === 'string') {
            return { name: p, type: 'string', description: '' };
          }
          return p;
        });
      }

      setFormData({
        name: editFunction.name || '',
        description: editFunction.description || '',
        functionClass: editFunction.functionClass || '',
        functionName: editFunction.functionName || '',
        params: params,
        returns: editFunction.returns || '',
        icon: editFunction.icon || '⚙️',
        type: editFunction.type || 'function'
      });
    } else {
      setFormData({
        name: '',
        description: '',
        functionClass: '',
        functionName: '',
        params: [],
        returns: '',
        icon: '⚙️',
        type: 'function'
      });
    }
  }, [editFunction, isOpen]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleAddParam = () => {
    if (paramInput.trim()) {
      setFormData(prev => ({
        ...prev,
        params: [...prev.params, {
          name: paramInput.trim(),
          type: paramType,
          description: paramDescription.trim()
        }]
      }));
      setParamInput('');
      setParamType('string');
      setParamDescription('');
    }
  };

  const handleRemoveParam = (index) => {
    setFormData(prev => ({
      ...prev,
      params: prev.params.filter((_, i) => i !== index)
    }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onSave(formData);
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content function-editor-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{editFunction ? 'Редактировать функцию' : 'Новая функция'}</h2>
          <button className="close-button" onClick={onClose}>&times;</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="form-group">
              <label>Название *</label>
              <input
                type="text"
                name="name"
                className="form-input"
                value={formData.name}
                onChange={handleChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Описание</label>
              <textarea
                name="description"
                className="form-textarea"
                value={formData.description}
                onChange={handleChange}
                rows="3"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Класс функции *</label>
                <input
                  type="text"
                  name="functionClass"
                  className="form-input"
                  value={formData.functionClass}
                  onChange={handleChange}
                  placeholder="Backend.Services.WorkflowFunctions.MyClass"
                  required
                />
              </div>

              <div className="form-group">
                <label>Имя метода *</label>
                <input
                  type="text"
                  name="functionName"
                  className="form-input"
                  value={formData.functionName}
                  onChange={handleChange}
                  placeholder="MyMethod"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label>Параметры</label>
              <div className="param-input-group">
                <input
                  type="text"
                  className="form-input"
                  value={paramInput}
                  onChange={(e) => setParamInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddParam())}
                  placeholder="Имя параметра"
                  style={{ flex: 2 }}
                />
                <select
                  className="form-input"
                  value={paramType}
                  onChange={(e) => setParamType(e.target.value)}
                  style={{ flex: 1 }}
                >
                  <option value="string">Строка</option>
                  <option value="int">Целое число</option>
                  <option value="float">Дробное число</option>
                  <option value="bool">Да/Нет</option>
                  <option value="date">Дата</option>
                </select>
                <input
                  type="text"
                  className="form-input"
                  value={paramDescription}
                  onChange={(e) => setParamDescription(e.target.value)}
                  placeholder="Описание (опционально)"
                  style={{ flex: 2 }}
                />
                <button type="button" className="btn-add-param" onClick={handleAddParam}>
                  Добавить
                </button>
              </div>
              <div className="params-list">
                {formData.params.map((param, index) => (
                  <div key={index} className="param-tag">
                    <span>
                      <strong>{typeof param === 'string' ? param : param.name}</strong>
                      {typeof param === 'object' && param.type && (
                        <span className="param-type"> ({param.type})</span>
                      )}
                      {typeof param === 'object' && param.description && (
                        <span className="param-desc"> - {param.description}</span>
                      )}
                    </span>
                    <button type="button" onClick={() => handleRemoveParam(index)}>&times;</button>
                  </div>
                ))}
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Возвращаемое значение</label>
                <input
                  type="text"
                  name="returns"
                  className="form-input"
                  value={formData.returns}
                  onChange={handleChange}
                  placeholder="result"
                />
              </div>

              <div className="form-group">
                <label>Иконка</label>
                <input
                  type="text"
                  name="icon"
                  className="form-input"
                  value={formData.icon}
                  onChange={handleChange}
                  maxLength="10"
                />
              </div>
            </div>

            <div className="form-group">
              <label>Тип</label>
              <select
                name="type"
                className="form-input"
                value={formData.type}
                onChange={handleChange}
              >
                <option value="function">Function</option>
                <option value="wf">WF</option>
                <option value="subwf">Sub-WF</option>
              </select>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose}>
              Отмена
            </button>
            <button type="submit" className="btn-save-config">
              {editFunction ? 'Сохранить' : 'Создать'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default FunctionEditorModal;
