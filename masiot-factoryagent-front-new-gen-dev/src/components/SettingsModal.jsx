import React, { useState, useEffect } from 'react';
import { parameterDictionaryAPI, blankOkpd2API, aiSettingsAPI } from '../services/api.js';
import './SettingsModal.css';

function SettingsModal({ isOpen, onClose, currentUser }) {
  const [activeTab, setActiveTab] = useState('params');
  const [okpd2List, setOkpd2List] = useState([]);
  const [loadingOkpd2, setLoadingOkpd2] = useState(false);

  // Состояния для ОКПД-2
  const [okpd2Keywords, setOkpd2Keywords] = useState('');
  const [okpd2Code, setOkpd2Code] = useState('');
  const [okpd2Description, setOkpd2Description] = useState('');
  const [editingOkpd2, setEditingOkpd2] = useState(null);

  // Состояния для словаря параметров
  const [parameterName, setParameterName] = useState('');
  const [parameterDescription, setParameterDescription] = useState('');
  const [parameterType, setParameterType] = useState('string');
  const [parameterUnit, setParameterUnit] = useState('');
  const [parameterList, setParameterList] = useState([]);
  const [editingParameter, setEditingParameter] = useState(null);
  const [loadingParameters, setLoadingParameters] = useState(false);

  // Нейросеть (OrcaRouter GLM / Gemini)
  const [aiProvider, setAiProvider] = useState('orcarouter');
  const [aiHasKey, setAiHasKey] = useState(false);
  const [aiKeyHint, setAiKeyHint] = useState('');
  const [aiModel, setAiModel] = useState('z-ai/glm-5.3-flash-free');
  const [aiApiKeyInput, setAiApiKeyInput] = useState('');
  const [aiShowKey, setAiShowKey] = useState(false);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiSaving, setAiSaving] = useState(false);
  const [aiMessage, setAiMessage] = useState('');

  const defaultModelFor = (provider) =>
    provider === 'gemini' ? 'gemini-2.5-flash' : 'z-ai/glm-5.3-flash-free';

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      // Загружаем параметры при открытии модального окна
      if (activeTab === 'params') {
        loadParameters();
      }
      // Загружаем ОКПД-2 для администраторов
      if (activeTab === 'okpd2' && currentUser?.role === 'admin') {
        loadOkpd2List();
      }
      if (activeTab === 'ai') {
        loadAiSettings();
      }
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, activeTab]);

  const loadAiSettings = async () => {
    try {
      setAiLoading(true);
      setAiMessage('');
      const data = await aiSettingsAPI.get();
      const provider = data.provider || 'orcarouter';
      setAiProvider(provider);
      setAiHasKey(!!data.has_api_key);
      setAiKeyHint(data.api_key_hint || '');
      setAiModel(data.model || defaultModelFor(provider));
      setAiApiKeyInput('');
    } catch (error) {
      console.error('Ошибка загрузки AI settings:', error);
      setAiMessage('Ошибка загрузки: ' + error.message);
    } finally {
      setAiLoading(false);
    }
  };

  const handleProviderChange = (next) => {
    setAiProvider(next);
    setAiModel(defaultModelFor(next));
  };

  const handleSaveAiSettings = async () => {
    try {
      setAiSaving(true);
      setAiMessage('');
      const hadNewKey = !!aiApiKeyInput.trim();
      const payload = {
        provider: aiProvider,
        model: aiModel.trim() || defaultModelFor(aiProvider)
      };
      if (hadNewKey) {
        payload.api_key = aiApiKeyInput.trim();
      }
      const data = await aiSettingsAPI.update(payload);
      setAiProvider(data.provider || aiProvider);
      setAiHasKey(!!data.has_api_key);
      setAiKeyHint(data.api_key_hint || '');
      setAiModel(data.model || defaultModelFor(data.provider || aiProvider));
      setAiApiKeyInput('');
      setAiMessage(hadNewKey || data.has_api_key
        ? 'Сохранено. Можно анализировать чертежи.'
        : 'Провайдер/модель сохранены. API key ещё не задан.');
    } catch (error) {
      console.error('Ошибка сохранения AI settings:', error);
      setAiMessage('Ошибка: ' + error.message);
    } finally {
      setAiSaving(false);
    }
  };

  const handleClearAiKey = async () => {
    if (!window.confirm('Удалить сохранённый API key нейросети?')) return;
    try {
      setAiSaving(true);
      setAiMessage('');
      const data = await aiSettingsAPI.update({
        clear_api_key: true,
        provider: aiProvider,
        model: aiModel
      });
      setAiHasKey(!!data.has_api_key);
      setAiKeyHint(data.api_key_hint || '');
      setAiApiKeyInput('');
      setAiMessage('API key удалён.');
    } catch (error) {
      setAiMessage('Ошибка: ' + error.message);
    } finally {
      setAiSaving(false);
    }
  };

  // Загрузка списка ОКПД-2
  const loadOkpd2List = async () => {
    try {
      setLoadingOkpd2(true);
      const list = await blankOkpd2API.getAll();
      setOkpd2List(list);
    } catch (error) {
      console.error('Ошибка загрузки ОКПД-2:', error);
      alert('Ошибка загрузки ОКПД-2: ' + error.message);
    } finally {
      setLoadingOkpd2(false);
    }
  };

  // Создание/обновление ОКПД-2
  const handleSaveOkpd2 = async () => {
    if (!okpd2Code.trim() || !okpd2Keywords.trim()) {
      alert('Заполните код ОКПД-2 и ключевые слова');
      return;
    }

    try {
      if (editingOkpd2) {
        await blankOkpd2API.update(editingOkpd2.id, {
          keywords: okpd2Keywords,
          okpd2Code: okpd2Code,
          description: okpd2Description
        });
        alert('ОКПД-2 успешно обновлен');
      } else {
        await blankOkpd2API.create({
          keywords: okpd2Keywords,
          okpd2Code: okpd2Code,
          description: okpd2Description
        });
        alert('ОКПД-2 успешно создан');
      }

      setOkpd2Keywords('');
      setOkpd2Code('');
      setOkpd2Description('');
      setEditingOkpd2(null);
      await loadOkpd2List();
    } catch (error) {
      console.error('Ошибка сохранения ОКПД-2:', error);
      alert('Ошибка сохранения ОКПД-2: ' + error.message);
    }
  };

  // Редактирование ОКПД-2
  const handleEditOkpd2 = (okpd2) => {
    setEditingOkpd2(okpd2);
    setOkpd2Keywords(okpd2.keywords);
    setOkpd2Code(okpd2.okpd2Code);
    setOkpd2Description(okpd2.description || '');
  };

  // Удаление ОКПД-2
  const handleDeleteOkpd2 = async (id) => {
    if (!window.confirm(`Удалить запись ОКПД-2?`)) {
      return;
    }

    try {
      await blankOkpd2API.delete(id);
      alert('ОКПД-2 успешно удален');
      await loadOkpd2List();
    } catch (error) {
      console.error('Ошибка удаления ОКПД-2:', error);
      alert('Ошибка удаления ОКПД-2: ' + error.message);
    }
  };

  // Отмена редактирования ОКПД-2
  const handleCancelOkpd2Edit = () => {
    setEditingOkpd2(null);
    setOkpd2Keywords('');
    setOkpd2Code('');
    setOkpd2Description('');
  };

  // Загрузка параметров из БД
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

  // Добавление/обновление параметра
  const handleAddParameter = async () => {
    if (!parameterName.trim()) {
      alert('Пожалуйста, введите название параметра');
      return;
    }

    try {
      const parameterData = {
        name: parameterName.trim(),
        description: parameterDescription.trim() || null,
        type: parameterType,
        unit: parameterUnit.trim() || null
      };

      if (editingParameter) {
        await parameterDictionaryAPI.update(editingParameter.id, parameterData);
        alert('Параметр успешно обновлен');
      } else {
        await parameterDictionaryAPI.create(parameterData);
        alert('Параметр успешно добавлен');
      }

      // Очищаем форму и перезагружаем список
      handleCancelParameterEdit();
      await loadParameters();
    } catch (error) {
      console.error('Ошибка сохранения параметра:', error);
      alert('Ошибка: ' + error.message);
    }
  };

  // Редактирование параметра
  const handleEditParameter = (parameter) => {
    setEditingParameter(parameter);
    setParameterName(parameter.name);
    setParameterDescription(parameter.description || '');
    setParameterType(parameter.type || 'string');
    setParameterUnit(parameter.unit || '');
  };

  // Удаление параметра
  const handleDeleteParameter = async (parameterId) => {
    if (!window.confirm('Вы уверены, что хотите удалить этот параметр?')) {
      return;
    }

    try {
      await parameterDictionaryAPI.delete(parameterId);
      alert('Параметр успешно удален');
      await loadParameters();
    } catch (error) {
      console.error('Ошибка удаления параметра:', error);
      alert('Ошибка удаления: ' + error.message);
    }
  };

  // Отмена редактирования параметра
  const handleCancelParameterEdit = () => {
    setEditingParameter(null);
    setParameterName('');
    setParameterDescription('');
    setParameterType('string');
    setParameterUnit('');
  };

  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="settings-modal-overlay" onClick={onClose}>
      <div className="settings-modal-window" onClick={(e) => e.stopPropagation()}>
        <button className="settings-modal-close-btn" onClick={onClose}>×</button>

        <h2 className="settings-modal-title">Настройки</h2>

        <div className="settings-tabs">
          <button
            className={`settings-tab-button ${activeTab === 'params' ? 'active' : ''}`}
            onClick={() => setActiveTab('params')}
          >
            Настройка словаря параметров
          </button>
          <button
            className={`settings-tab-button ${activeTab === 'ai' ? 'active' : ''}`}
            onClick={() => setActiveTab('ai')}
          >
            Нейросеть
          </button>
          {currentUser?.role === 'admin' && (
            <button
              className={`settings-tab-button ${activeTab === 'okpd2' ? 'active' : ''}`}
              onClick={() => setActiveTab('okpd2')}
            >
              Справочник ОКПД-2 (Администратор)
            </button>
          )}
        </div>
        <div className="settings-tabs-divider"></div>

        <div className="settings-tab-content">
          {activeTab === 'okpd2' && currentUser?.role === 'admin' && (
            <div className="settings-tab-panel">
              <div className="settings-card">
                <h3 className="settings-card-title">
                  {editingOkpd2 ? 'Редактировать запись' : 'Добавить запись ОКПД-2'}
                </h3>
                <div className="okp-form">
                  <div className="form-field">
                    <label className="field-label">Ключевые слова (для поиска)</label>
                    <input
                      type="text"
                      className="field-input"
                      placeholder="Например: круг стальной горячекатаный"
                      value={okpd2Keywords}
                      onChange={(e) => setOkpd2Keywords(e.target.value)}
                    />
                  </div>
                  <div className="form-field">
                    <label className="field-label">Код ОКПД-2</label>
                    <input
                      type="text"
                      className="field-input"
                      placeholder="Например: 25.73.40.130"
                      value={okpd2Code}
                      onChange={(e) => setOkpd2Code(e.target.value)}
                      disabled={editingOkpd2 !== null}
                    />
                  </div>
                  <div className="form-field">
                    <label className="field-label">Описание (необязательно)</label>
                    <textarea
                      className="field-textarea"
                      placeholder="Дополнительное описание"
                      value={okpd2Description}
                      onChange={(e) => setOkpd2Description(e.target.value)}
                      rows="2"
                    />
                  </div>
                  <div className="form-actions">
                    <button className="btn-add-okp" onClick={handleSaveOkpd2}>
                      {editingOkpd2 ? 'Сохранить изменения' : 'Добавить ОКПД-2'}
                    </button>
                    {editingOkpd2 && (
                      <button className="btn-cancel-okp" onClick={handleCancelOkpd2Edit}>
                        Отмена
                      </button>
                    )}
                  </div>
                </div>
              </div>

              <div className="settings-card" style={{ marginTop: '20px' }}>
                <h3 className="settings-card-title">Список ОКПД-2</h3>
                {loadingOkpd2 ? (
                  <p>Загрузка...</p>
                ) : (
                  <div className="okp-list">
                    {okpd2List.length === 0 ? (
                      <p style={{ color: '#666', textAlign: 'center', padding: '20px' }}>
                        Справочник ОКПД-2 пуст. Добавьте записи выше.
                      </p>
                    ) : (
                      <table className="okp-table">
                        <thead>
                          <tr>
                            <th>Ключевые слова</th>
                            <th>Код ОКПД-2</th>
                            <th>Описание</th>
                            <th>Действия</th>
                          </tr>
                        </thead>
                        <tbody>
                          {okpd2List.map((item) => (
                            <tr key={item.id}>
                              <td>{item.keywords}</td>
                              <td>{item.okpd2Code}</td>
                              <td>{item.description || '-'}</td>
                              <td>
                                <button
                                  className="btn-edit-okp"
                                  onClick={() => handleEditOkpd2(item)}
                                >
                                  Изменить
                                </button>
                                <button
                                  className="btn-delete-okp"
                                  onClick={() => handleDeleteOkpd2(item.id)}
                                >
                                  Удалить
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    )}
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'ai' && (
            <div className="settings-tab-panel">
              <div className="settings-card">
                <h3 className="settings-card-title">API для анализа чертежей</h3>
                <p className="ai-settings-help">
                  Рекомендуется бесплатный <strong>OrcaRouter → GLM 5.3 Flash</strong> (vision, $0):{' '}
                  <a href="https://www.orcarouter.ai/" target="_blank" rel="noreferrer">
                    orcarouter.ai
                  </a>
                  {' '}· модель{' '}
                  <a href="https://www.orcarouter.ai/models/z-ai/glm-5.3-flash-free" target="_blank" rel="noreferrer">
                    z-ai/glm-5.3-flash-free
                  </a>
                  . Ключ хранится на сервере зашифрованным.
                </p>

                {aiLoading ? (
                  <p>Загрузка...</p>
                ) : (
                  <div className="okp-form">
                    <div className="form-field">
                      <label className="field-label">Провайдер</label>
                      <select
                        className="field-input"
                        value={aiProvider}
                        onChange={(e) => handleProviderChange(e.target.value)}
                      >
                        <option value="orcarouter">OrcaRouter (GLM бесплатно)</option>
                        <option value="gemini">Google Gemini</option>
                      </select>
                    </div>

                    <div className="form-field">
                      <label className="field-label">Статус ключа</label>
                      <div className={`ai-key-status ${aiHasKey ? 'ai-key-ok' : 'ai-key-missing'}`}>
                        {aiHasKey
                          ? `Задан${aiKeyHint ? ` (${aiKeyHint})` : ''}`
                          : 'Не задан — анализ чертежей недоступен'}
                      </div>
                    </div>

                    <div className="form-field">
                      <label className="field-label">
                        {aiHasKey ? 'Новый API key (оставьте пустым, чтобы не менять)' : 'API key'}
                      </label>
                      <div className="ai-key-input-row">
                        <input
                          type={aiShowKey ? 'text' : 'password'}
                          className="field-input"
                          placeholder={
                            aiProvider === 'gemini'
                              ? 'Ключ из Google AI Studio'
                              : 'Ключ из кабинета OrcaRouter'
                          }
                          value={aiApiKeyInput}
                          onChange={(e) => setAiApiKeyInput(e.target.value)}
                          autoComplete="off"
                        />
                        <button
                          type="button"
                          className="btn-cancel-okp"
                          onClick={() => setAiShowKey((v) => !v)}
                        >
                          {aiShowKey ? 'Скрыть' : 'Показать'}
                        </button>
                      </div>
                      <span className="ai-settings-hint">
                        {aiProvider === 'gemini' ? (
                          <>
                            Ключ:{' '}
                            <a href="https://aistudio.google.com/apikey" target="_blank" rel="noreferrer">
                              aistudio.google.com/apikey
                            </a>
                          </>
                        ) : (
                          <>
                            Регистрация и API key:{' '}
                            <a href="https://www.orcarouter.ai/" target="_blank" rel="noreferrer">
                              www.orcarouter.ai
                            </a>
                            {' '}(для free-tier обычно нужен GitHub в профиле)
                          </>
                        )}
                      </span>
                    </div>

                    <div className="form-field">
                      <label className="field-label">Модель</label>
                      <input
                        type="text"
                        className="field-input"
                        value={aiModel}
                        onChange={(e) => setAiModel(e.target.value)}
                        placeholder={defaultModelFor(aiProvider)}
                      />
                      <span className="ai-settings-hint">
                        {aiProvider === 'gemini'
                          ? 'Например gemini-2.5-flash'
                          : 'Бесплатно: z-ai/glm-5.3-flash-free (vision)'}
                      </span>
                    </div>

                    {aiMessage && <p className="ai-settings-message">{aiMessage}</p>}

                    <div className="form-actions">
                      <button
                        className="btn-add-okp"
                        onClick={handleSaveAiSettings}
                        disabled={aiSaving}
                      >
                        {aiSaving ? 'Сохранение…' : 'Сохранить'}
                      </button>
                      {aiHasKey && (
                        <button
                          className="btn-cancel-okp"
                          onClick={handleClearAiKey}
                          disabled={aiSaving}
                        >
                          Удалить ключ
                        </button>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'params' && (
            <div className="settings-tab-panel">
              <div className="settings-card">
                <h3 className="settings-card-title">
                  {editingParameter ? 'Редактировать параметр' : 'Создать новый параметр'}
                </h3>
                <div className="okp-form">
                  <div className="form-field">
                    <label className="field-label">Название параметра</label>
                    <input
                      type="text"
                      className="field-input"
                      placeholder="Например: Длина"
                      value={parameterName}
                      onChange={(e) => setParameterName(e.target.value)}
                    />
                  </div>
                  <div className="form-field">
                    <label className="field-label">Описание</label>
                    <textarea
                      className="field-textarea"
                      placeholder="Описание параметра"
                      value={parameterDescription}
                      onChange={(e) => setParameterDescription(e.target.value)}
                      rows="2"
                    />
                  </div>
                  <div className="form-field">
                    <label className="field-label">Тип</label>
                    <select
                      className="field-input"
                      value={parameterType}
                      onChange={(e) => setParameterType(e.target.value)}
                    >
                      <option value="string">Строка</option>
                      <option value="number">Число</option>
                      <option value="boolean">Да/Нет</option>
                    </select>
                  </div>
                  <div className="form-field">
                    <label className="field-label">Единица измерения</label>
                    <input
                      type="text"
                      className="field-input"
                      placeholder="Например: мм, кг, шт"
                      value={parameterUnit}
                      onChange={(e) => setParameterUnit(e.target.value)}
                    />
                  </div>
                  <div className="form-actions">
                    {editingParameter && (
                      <button className="btn-cancel-edit" onClick={handleCancelParameterEdit}>
                        Отмена
                      </button>
                    )}
                    <button className="btn-add-okp" onClick={handleAddParameter}>
                      {editingParameter ? 'Сохранить изменения' : 'Добавить параметр'}
                    </button>
                  </div>
                </div>
              </div>

              <div className="settings-card">
                <h3 className="settings-card-title">Словарь параметров</h3>
                <div className="okp-list-container">
                  {loadingParameters ? (
                    <p className="empty-message">Загрузка...</p>
                  ) : parameterList.length === 0 ? (
                    <p className="empty-message">Параметры не добавлены</p>
                  ) : (
                    <table className="okp-table">
                      <thead>
                        <tr>
                          <th>Название</th>
                          <th>Описание</th>
                          <th>Тип</th>
                          <th>Единица измерения</th>
                          <th className="actions-column">Действия</th>
                        </tr>
                      </thead>
                      <tbody>
                        {parameterList.map(param => (
                          <tr key={param.id}>
                            <td>{param.name}</td>
                            <td>{param.description || '-'}</td>
                            <td>
                              {param.type === 'string' ? 'Строка' :
                               param.type === 'number' ? 'Число' :
                               param.type === 'boolean' ? 'Да/Нет' : param.type}
                            </td>
                            <td>{param.unit || '-'}</td>
                            <td className="actions-cell">
                              <button
                                className="btn-edit-okp"
                                onClick={() => handleEditParameter(param)}
                                title="Изменить"
                              >
                                ✏️
                              </button>
                              <button
                                className="btn-delete-okp"
                                onClick={() => handleDeleteParameter(param.id)}
                                title="Удалить"
                              >
                                🗑️
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default SettingsModal;
