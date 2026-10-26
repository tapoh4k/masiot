import React from 'react';
import './DrawingAnalysisModal.css';

function formatHours(value) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '—';
  return `${Number(value).toFixed(2)} ч`;
}

function formatMin(value) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '—';
  return `${Number(value).toFixed(1)} мин`;
}

function DrawingAnalysisModal({ analysis, onClose }) {
  if (!analysis) return null;

  const result = analysis.result || {};
  const operations = Array.isArray(result.operations) ? result.operations : [];
  const assumptions = Array.isArray(result.assumptions) ? result.assumptions : [];
  const gosts = Array.isArray(result.gost_references) ? result.gost_references : [];
  const risks = Array.isArray(result.risks) ? result.risks : [];

  const confidence = (analysis.confidence || result.confidence || '—').toLowerCase();

  return (
    <div className="drawing-modal-overlay" onClick={onClose}>
      <div className="drawing-modal" onClick={(e) => e.stopPropagation()}>
        <div className="drawing-modal-header">
          <div>
            <h2>Трудоёмкость по чертежу</h2>
            <p className="drawing-modal-subtitle">{analysis.file_name}</p>
          </div>
          <button type="button" className="drawing-modal-close" onClick={onClose} aria-label="Закрыть">
            ×
          </button>
        </div>

        <div className="drawing-modal-meta">
          <div className="drawing-meta-item">
            <span className="drawing-meta-label">Деталь</span>
            <span className="drawing-meta-value">{analysis.part_name || result.part_name || '—'}</span>
          </div>
          <div className="drawing-meta-item">
            <span className="drawing-meta-label">Обозначение</span>
            <span className="drawing-meta-value">{analysis.drawing_number || result.drawing_number || '—'}</span>
          </div>
          <div className="drawing-meta-item">
            <span className="drawing-meta-label">Материал</span>
            <span className="drawing-meta-value">{analysis.material || result.material || '—'}</span>
          </div>
          <div className="drawing-meta-item">
            <span className="drawing-meta-label">Габариты</span>
            <span className="drawing-meta-value">{result.overall_dimensions || '—'}</span>
          </div>
          <div className="drawing-meta-item highlight">
            <span className="drawing-meta-label">Итого трудоёмкость</span>
            <span className="drawing-meta-value">
              {formatHours(analysis.total_labor_hours ?? result.total_labor_hours)}
            </span>
          </div>
          <div className="drawing-meta-item">
            <span className="drawing-meta-label">Уверенность</span>
            <span className={`drawing-confidence confidence-${confidence}`}>{confidence}</span>
          </div>
        </div>

        {result.summary && (
          <div className="drawing-summary">
            <h3>Вывод</h3>
            <p>{result.summary}</p>
          </div>
        )}

        <div className="drawing-ops-section">
          <h3>Операции</h3>
          {operations.length === 0 ? (
            <p className="drawing-empty">Операции не распознаны</p>
          ) : (
            <div className="drawing-ops-table-wrap">
              <table className="drawing-ops-table">
                <thead>
                  <tr>
                    <th>№</th>
                    <th>Операция</th>
                    <th>Оборудование</th>
                    <th>Профессия</th>
                    <th>Тпз</th>
                    <th>Тшт</th>
                    <th>Часы</th>
                  </tr>
                </thead>
                <tbody>
                  {operations.map((op, idx) => (
                    <tr key={`${op.op_no || idx}-${op.name}`}>
                      <td>{op.op_no ?? idx + 1}</td>
                      <td>
                        <div className="op-name">{op.name}</div>
                        {op.notes && <div className="op-notes">{op.notes}</div>}
                      </td>
                      <td>{op.equipment || '—'}</td>
                      <td>{op.profession || '—'}</td>
                      <td>{formatMin(op.setup_min)}</td>
                      <td>{formatMin(op.piece_min)}</td>
                      <td>{formatHours(op.labor_hours)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="drawing-lists-grid">
          {gosts.length > 0 && (
            <div className="drawing-list-block">
              <h3>ГОСТ / нормы</h3>
              <ul>
                {gosts.map((g) => (
                  <li key={g}>{g}</li>
                ))}
              </ul>
            </div>
          )}
          {assumptions.length > 0 && (
            <div className="drawing-list-block">
              <h3>Допущения</h3>
              <ul>
                {assumptions.map((a) => (
                  <li key={a}>{a}</li>
                ))}
              </ul>
            </div>
          )}
          {risks.length > 0 && (
            <div className="drawing-list-block">
              <h3>Риски</h3>
              <ul>
                {risks.map((r) => (
                  <li key={r}>{r}</li>
                ))}
              </ul>
            </div>
          )}
        </div>

        <div className="drawing-modal-footer">
          <span className="drawing-model-hint">
            Модель: {analysis.model_name || 'Gemini'} · оценка ориентировочная, требует проверки технолога
          </span>
          <button type="button" className="btn btn-primary" onClick={onClose}>
            Закрыть
          </button>
        </div>
      </div>
    </div>
  );
}

export default DrawingAnalysisModal;
