import React, { useState, useEffect } from 'react';
import AgentCreationModal from './AgentCreationModal.jsx';
import DrawingAnalysisModal from './DrawingAnalysisModal.jsx';
import { fileUploadAPI, statisticsAPI, drawingsAPI } from '../services/api.js';
import './MainPage.css';

const DRAWING_ACCEPT =
  '.pdf,.png,.jpg,.jpeg,.webp,.tif,.tiff,.bmp,application/pdf,image/png,image/jpeg,image/webp,image/tiff,image/bmp';

function isDrawingFile(file) {
  if (!file) return false;
  const name = (file.name || '').toLowerCase();
  return /\.(pdf|png|jpe?g|webp|tiff?|bmp)$/i.test(name);
}

function MainPage({ treeNodes, onReloadTree, onAddNode, onToggleEditMode }) {
  const [selectedFile, setSelectedFile] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [toggles, setToggles] = useState({
    createResourceAgents: true,
    createProcessAgents: true,
    generateInstructions: true
  });
  const [fileHistory, setFileHistory] = useState([]);
  const [statistics, setStatistics] = useState({
    agents_count: 0,
    equipment_count: 0,
    processes_count: 0,
    instructions_count: 0
  });

  const [drawingFile, setDrawingFile] = useState(null);
  const [drawingAnalyzing, setDrawingAnalyzing] = useState(false);
  const [drawingError, setDrawingError] = useState('');
  const [drawingResult, setDrawingResult] = useState(null);
  const [drawingHistory, setDrawingHistory] = useState([]);

  useEffect(() => {
    loadFileHistory();
    loadStatistics();
    loadDrawingHistory();
  }, []);

  useEffect(() => {
    loadStatistics();
  }, [treeNodes]);

  const loadFileHistory = async () => {
    try {
      const history = await fileUploadAPI.getAll();
      setFileHistory(history);
    } catch (error) {
      console.error('Ошибка загрузки истории файлов:', error);
    }
  };

  const loadDrawingHistory = async () => {
    try {
      const history = await drawingsAPI.getHistory(30);
      setDrawingHistory(history);
    } catch (error) {
      console.error('Ошибка загрузки истории чертежей:', error);
    }
  };

  const loadStatistics = async () => {
    try {
      const stats = await statisticsAPI.get();
      setStatistics(stats);
    } catch (error) {
      console.error('Ошибка загрузки статистики:', error);
    }
  };

  const getStatusBadge = (status) => {
    const statusConfig = {
      completed: { text: 'Загружено', className: 'status-completed' },
      processing: { text: 'В обработке', className: 'status-processing' },
      error: { text: 'Ошибка', className: 'status-error' },
      skipped: { text: 'Пропущено', className: 'status-skipped' }
    };
    return statusConfig[status] || statusConfig.skipped;
  };

  const handleFileChange = (e) => {
    const file = e.target.files[0];
    if (file && file.name.endsWith('.xlsx')) {
      setSelectedFile(file);
      setShowModal(true);
    } else {
      alert('Пожалуйста, выберите файл формата .xlsx');
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (file && file.name.endsWith('.xlsx')) {
      setSelectedFile(file);
      setShowModal(true);
    } else {
      alert('Пожалуйста, выберите файл формата .xlsx');
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
  };

  const handleClear = () => {
    setSelectedFile(null);
    setShowModal(false);
  };

  const handleProcess = () => {
    if (!selectedFile) {
      alert('Пожалуйста, выберите файл');
      return;
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
  };

  const handleSaveAgents = async (agents) => {
    console.log('Сохраненные агенты:', agents);

    if (selectedFile) {
      try {
        await fileUploadAPI.create({
          file_name: selectedFile.name,
          uploaded_at: new Date().toISOString(),
          status: 'completed'
        });
        await loadFileHistory();

        if (onReloadTree) {
          await onReloadTree();
        }

        await loadStatistics();
      } catch (error) {
        console.error('Ошибка сохранения записи о загрузке:', error);
      }
    }

    setShowModal(false);
  };

  const pickDrawingFile = (file) => {
    setDrawingError('');
    if (!isDrawingFile(file)) {
      setDrawingError('Поддерживаются PDF, PNG, JPG, WEBP, TIFF, BMP');
      return;
    }
    setDrawingFile(file);
  };

  const handleDrawingChange = (e) => {
    const file = e.target.files?.[0];
    if (file) pickDrawingFile(file);
  };

  const handleDrawingDrop = (e) => {
    e.preventDefault();
    const file = e.dataTransfer.files?.[0];
    if (file) pickDrawingFile(file);
  };

  const handleDrawingClear = () => {
    setDrawingFile(null);
    setDrawingError('');
    const input = document.getElementById('drawing-file-input');
    if (input) input.value = '';
  };

  const handleAnalyzeDrawing = async () => {
    if (!drawingFile) {
      setDrawingError('Выберите чертёж');
      return;
    }
    setDrawingAnalyzing(true);
    setDrawingError('');
    try {
      const data = await drawingsAPI.analyze(drawingFile);
      setDrawingResult(data.analysis);
      await loadDrawingHistory();
      await fileUploadAPI.create({
        file_name: `[чертёж] ${drawingFile.name}`,
        uploaded_at: new Date().toISOString(),
        status: 'completed'
      }).catch(() => {});
      await loadFileHistory();
    } catch (error) {
      setDrawingError(error.message || 'Ошибка анализа');
    } finally {
      setDrawingAnalyzing(false);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString('ru-RU', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const handleToggle = (key) => {
    setToggles(prev => ({
      ...prev,
      [key]: !prev[key]
    }));
  };

  return (
    <div className="main-page">
      <div className="page-header">
        <h1 className="page-title">Главная</h1>
        <button className="btn-tree-settings" onClick={() => onToggleEditMode && onToggleEditMode()}>
          Настройка дерева
        </button>
      </div>
      <div className="page-divider"></div>

      <div className="cards-grid">
        <div className="card upload-card">
          <h2 className="card-title">Загрузка XLSX</h2>

          <div
            className="drop-zone"
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onClick={() => document.getElementById('file-input').click()}
          >
            <div className="drop-zone-content">
              <p className="drop-zone-text">
                {selectedFile
                  ? selectedFile.name
                  : 'Перетащите XLSX файл сюда или нажмите, чтобы выбрать'}
              </p>
              <p className="drop-zone-format">.xlsx</p>
            </div>
            <input
              id="file-input"
              type="file"
              accept=".xlsx"
              onChange={handleFileChange}
              style={{ display: 'none' }}
            />
          </div>

          <div className="upload-footer">
            <p className="upload-note">
              Поддерживается загрузка комбинированных и частичных файлов
            </p>
            <div className="upload-actions">
              <button className="btn btn-outline" onClick={handleClear}>
                Очистить
              </button>
              <button className="btn btn-primary" onClick={handleProcess}>
                Запустить обработку
              </button>
            </div>
          </div>
        </div>

        <div className="card upload-card drawing-card">
          <h2 className="card-title">Загрузка чертежей</h2>

          <div
            className={`drop-zone ${drawingAnalyzing ? 'drop-zone-busy' : ''}`}
            onDrop={handleDrawingDrop}
            onDragOver={handleDragOver}
            onClick={() => !drawingAnalyzing && document.getElementById('drawing-file-input').click()}
          >
            <div className="drop-zone-content">
              <p className="drop-zone-text">
                {drawingAnalyzing
                  ? 'Нейросеть анализирует чертёж…'
                  : drawingFile
                    ? drawingFile.name
                    : 'Перетащите чертёж сюда или нажмите, чтобы выбрать'}
              </p>
              <p className="drop-zone-format">.pdf .png .jpg .webp</p>
            </div>
            <input
              id="drawing-file-input"
              type="file"
              accept={DRAWING_ACCEPT}
              onChange={handleDrawingChange}
              style={{ display: 'none' }}
              disabled={drawingAnalyzing}
            />
          </div>

          {drawingError && <p className="drawing-error">{drawingError}</p>}

          <div className="upload-footer">
            <p className="upload-note">
              AI оценит трудоёмкость по ГОСТ/ЕСТД (OrcaRouter GLM / Gemini — ключ в Настройках)
            </p>
            <div className="upload-actions">
              <button className="btn btn-outline" onClick={handleDrawingClear} disabled={drawingAnalyzing}>
                Очистить
              </button>
              <button
                className="btn btn-primary"
                onClick={handleAnalyzeDrawing}
                disabled={drawingAnalyzing || !drawingFile}
              >
                {drawingAnalyzing ? 'Анализ…' : 'Оценить трудоёмкость'}
              </button>
            </div>
          </div>

          {drawingHistory.length > 0 && (
            <div className="drawing-history-mini">
              <div className="drawing-history-title">Последние анализы</div>
              <ul>
                {drawingHistory.slice(0, 5).map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      className="drawing-history-link"
                      onClick={() => setDrawingResult(item)}
                    >
                      <span>{item.file_name}</span>
                      <span className="drawing-history-hours">
                        {item.total_labor_hours != null
                          ? `${Number(item.total_labor_hours).toFixed(2)} ч`
                          : item.status}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </div>

      <div className="card info-card info-card-below">
        <h2 className="card-title">Информационное окно</h2>
        <div className="info-stats-list info-stats-grid">
          <div className="info-stat-row">
            <span className="info-stat-label">Агенты (всего)</span>
            <span className="info-stat-value">{statistics.agents_count}</span>
          </div>
          <div className="info-stat-row">
            <span className="info-stat-label">Тех процесс</span>
            <span className="info-stat-value">{statistics.tech_process_count}</span>
          </div>
          <div className="info-stat-row">
            <span className="info-stat-label">Оборудование</span>
            <span className="info-stat-value">{statistics.equipment_count}</span>
          </div>
          <div className="info-stat-row">
            <span className="info-stat-label">Инструмент</span>
            <span className="info-stat-value">{statistics.instrument_count}</span>
          </div>
          <div className="info-stat-row">
            <span className="info-stat-label">Персонал</span>
            <span className="info-stat-value">{statistics.personnel_count}</span>
          </div>
          <div className="info-stat-row">
            <span className="info-stat-label">Заготовки</span>
            <span className="info-stat-value">{statistics.blank_count}</span>
          </div>
        </div>
      </div>

      <div className="card history-card">
        <h2 className="card-title">История загруженных файлов</h2>
        <div className="table-container">
          <table className="history-table">
            <thead>
              <tr>
                <th>Файл</th>
                <th>Дата и время</th>
                <th>Статус</th>
              </tr>
            </thead>
            <tbody>
              {fileHistory.length === 0 ? (
                <tr>
                  <td colSpan="3" style={{ textAlign: 'center', padding: '20px', color: '#999' }}>
                    История загрузок пуста
                  </td>
                </tr>
              ) : (
                fileHistory.map((item) => {
                  const badge = getStatusBadge(item.status);
                  return (
                    <tr key={item.id}>
                      <td className="file-name">{item.file_name}</td>
                      <td>{formatDate(item.uploaded_at)}</td>
                      <td>
                        <span className={`status-badge ${badge.className}`}>
                          {badge.text}
                        </span>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {showModal && selectedFile && (
        <AgentCreationModal
          file={selectedFile}
          onClose={handleCloseModal}
          onSave={handleSaveAgents}
          treeNodes={treeNodes}
          onReloadTree={onReloadTree}
          onAddNode={onAddNode}
        />
      )}

      {drawingResult && (
        <DrawingAnalysisModal
          analysis={drawingResult}
          onClose={() => setDrawingResult(null)}
        />
      )}
    </div>
  );
}

export default MainPage;
