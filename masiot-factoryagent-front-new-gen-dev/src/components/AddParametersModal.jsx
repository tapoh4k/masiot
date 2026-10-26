import React, { useState, useEffect } from 'react';
import { parameterDictionaryAPI } from '../services/api.js';
import './AddParametersModal.css';

function AddParametersModal({ isOpen, onClose, onSave }) {
  const [selectedParams, setSelectedParams] = useState({});
  const [parameterList, setParameterList] = useState([]);
  const [loadingParameters, setLoadingParameters] = useState(false);

  // Загрузка параметров из словаря при открытии модалки
  useEffect(() => {
    if (isOpen) {
      loadParameters();
    }
  }, [isOpen]);

  const loadParameters = async () => {
    try {
      setLoadingParameters(true);
      const parameters = await parameterDictionaryAPI.getAll();
      setParameterList(parameters);
    } catch (error) {
      console.error('Ошибка загрузки параметров:', error);
      alert('Ошибка загрузки параметров: ' + error.message);
    } finally {
      setLoadingParameters(false);
    }
  };

  // Блокировка скролла body при открытии модалки
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  // Закрытие по Esc
  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [isOpen, onClose]);

  const handleParamToggle = (paramId) => {
    setSelectedParams(prev => ({
      ...prev,
      [paramId]: !prev[paramId]
    }));
  };

  const handleSaveSelected = () => {
    const selected = parameterList.filter(param => selectedParams[param.id]);
    onSave(selected);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-window" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close-btn" onClick={onClose}>×</button>

        <h2 className="modal-title">Добавить параметры</h2>

        <div className="modal-section">
          <h3 className="section-title">Выберите параметры из словаря:</h3>
          {loadingParameters ? (
            <p style={{ textAlign: 'center', padding: '20px' }}>Загрузка параметров...</p>
          ) : parameterList.length === 0 ? (
            <p style={{ textAlign: 'center', padding: '20px', color: '#999' }}>
              Словарь параметров пуст. Добавьте параметры в настройках.
            </p>
          ) : (
            <div className="existing-params-table-container">
              <table className="existing-params-table">
                <thead>
                  <tr>
                    <th>Имя параметра</th>
                    <th>Единица измерения</th>
                    <th>Описание</th>
                    <th className="checkbox-column">Выбор</th>
                  </tr>
                </thead>
                <tbody>
                  {parameterList.map(param => (
                    <tr key={param.id}>
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
        </div>

        <div className="modal-footer">
          <button className="btn-save-selected" onClick={handleSaveSelected}>
            Добавить выбранные параметры
          </button>
        </div>
      </div>
    </div>
  );
}

export default AddParametersModal;
