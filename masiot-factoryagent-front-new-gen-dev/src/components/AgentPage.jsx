import React, { useState, useEffect } from 'react';
import { useLocation, useParams, useNavigate } from 'react-router-dom';
import AddParametersModal from './AddParametersModal.jsx';
import { API_URL, workflowTemplateAPI, messagesAPI, workflowAPI, drawingsAPI } from '../services/api.js';
import './AgentPage.css';

function AgentPage({ treeNodes, onUpdateAgent, onDeleteAgent, onAddAgent, onReloadTree }) {
  const location = useLocation();
  const navigate = useNavigate();
  const { agentId } = useParams();
  const nodeId = location.state?.nodeId || agentId;

  // Функция поиска агента в дереве
  const findAgentInTree = (nodes, targetId) => {
    if (!nodes || !Array.isArray(nodes)) return null;

    for (const node of nodes) {
      if (node.id === targetId && node.type === 'agent') {
        return node.agentData;
      }
      if (node.children && node.children.length > 0) {
        const found = findAgentInTree(node.children, targetId);
        if (found) return found;
      }
    }
    return null;
  };

  // Получаем agentData из location.state или ищем в дереве
  const agentDataFromState = location.state?.agentData;
  const agentDataFromTree = findAgentInTree(treeNodes, nodeId);

  const initialAgentData = agentDataFromState || agentDataFromTree || {
    name: 'Станок вертикально-сверлильный',
    okpd2_code: 'ОКПД-2-12345',
    quantity: 1,
    queueNumber: 1,
    parameters: []
  };

  // State for agent data that can be updated
  const [agentData, setAgentData] = useState(initialAgentData);

  // IMPORTANT: when user clicks another agent in the tree, route changes but component instance may stay.
  // Sync local state with new agent selection.
  useEffect(() => {
    if (agentDataFromState) {
      setAgentData(agentDataFromState);
      return;
    }
    if (agentDataFromTree) {
      setAgentData(agentDataFromTree);
      return;
    }
  }, [nodeId]);

  // Load fresh agent data from API by numeric id when it changes
  useEffect(() => {
    const loadById = async () => {
      const numericId = agentDataFromState?.id || agentDataFromTree?.id;
      if (!numericId) return;
      try {
        const res = await fetch(`${API_URL}/agents/${numericId}`);
        if (res.ok) {
          const a = await res.json();
          console.log('[AgentPage] Loaded agent data:', a);
          console.log('[AgentPage] Agent parameters:', a.parameters);
          setAgentData(a);
        }
      } catch (err) {
        console.error('[AgentPage] Error loading agent:', err);
      }
    };
    loadById();
  }, [agentDataFromState?.id, agentDataFromTree?.id, nodeId]);

  // Load active tab from localStorage or default to 'info'
  const [activeTab, setActiveTab] = useState(() => {
    const savedTab = localStorage.getItem('agentPageActiveTab');
    return savedTab || 'info';
  });

  const [isParametersModalOpen, setIsParametersModalOpen] = useState(false);

  const [techProcess, setTechProcess] = useState(null);
  const [tpDrawingFile, setTpDrawingFile] = useState(null);
  const [tpDrawingAnalyzing, setTpDrawingAnalyzing] = useState(false);
  const [tpDrawingError, setTpDrawingError] = useState('');
  const [tpDrawingSummary, setTpDrawingSummary] = useState('');

  useEffect(() => {
    const isTp = agentData?.type === 'tech_process' || agentData?.agent_type === 'tech_process';
    if (!isTp || !agentData?.id) {
      setTechProcess(null);
      return;
    }

    let cancelled = false;
    messagesAPI.getTechProcessOperations(agentData.id)
      .then(data => { if (!cancelled) setTechProcess(data); })
      .catch(error => {
        console.warn('[AgentPage] Операции техпроцесса не загружены:', error);
        if (!cancelled) setTechProcess(null);
      });
    return () => { cancelled = true; };
  }, [agentData?.id, agentData?.type]);

  const isDrawingFile = (file) =>
    file && /\.(pdf|png|jpe?g|webp|tiff?|bmp)$/i.test(file.name || '');

  const handleTpDrawingChange = (e) => {
    const file = e.target.files?.[0];
    setTpDrawingError('');
    setTpDrawingSummary('');
    if (!file) return;
    if (!isDrawingFile(file)) {
      setTpDrawingError('Поддерживаются PDF, PNG, JPG, WEBP, TIFF, BMP');
      return;
    }
    setTpDrawingFile(file);
  };

  const handleAnalyzeTpDrawing = async () => {
    if (!tpDrawingFile) {
      setTpDrawingError('Выберите чертёж');
      return;
    }
    if (!agentData?.id) {
      setTpDrawingError('Агент не загружен');
      return;
    }
    setTpDrawingAnalyzing(true);
    setTpDrawingError('');
    setTpDrawingSummary('');
    try {
      const data = await drawingsAPI.analyzeTechProcess(agentData.id, tpDrawingFile);
      if (data.tech_process) setTechProcess(data.tech_process);
      if (data.matches === false) {
        const what = data.drawing_what ? `На чертеже: ${data.drawing_what}. ` : '';
        setTpDrawingError(
          `${what}${data.mismatch_reason || data.summary || 'Чертёж не соответствует этому техпроцессу.'} Время не изменено.`
        );
        return;
      }
      setTpDrawingSummary(data.summary || data.message || 'Время операций обновлено');
    } catch (error) {
      setTpDrawingError(error.message || 'Ошибка анализа');
    } finally {
      setTpDrawingAnalyzing(false);
    }
  };

  const formatOpTime = (v) => {
    if (v === null || v === undefined || v === '') return '—';
    const n = Number(v);
    if (Number.isNaN(n)) return String(v);
    return Number.isInteger(n) ? String(n) : n.toFixed(1);
  };

  const formatToolCodes = (codes) => {
    if (!codes) return '—';
    if (Array.isArray(codes)) return codes.filter(Boolean).join(', ') || '—';
    return String(codes).split(';').filter(Boolean).join(', ') || '—';
  };

  // Save active tab to localStorage when it changes
  useEffect(() => {
    localStorage.setItem('agentPageActiveTab', activeTab);
  }, [activeTab]);

  // Состояния для Workflow
  const [searchWorkflow, setSearchWorkflow] = useState('');
  const [workflowFilter, setWorkflowFilter] = useState('all'); // 'all' или 'selected'
  const [selectedWorkflowIds, setSelectedWorkflowIds] = useState([]); // Изначально пусто
  const [workflows, setWorkflows] = useState([]);
  const [loadingWorkflows, setLoadingWorkflows] = useState(false);

  // Загрузка workflow из БД и данных агента
  useEffect(() => {
    const loadWorkflows = async () => {
      try {
        setLoadingWorkflows(true);

        // Fetch fresh agent data from API using node_id
        if (nodeId) {
          try {
            const response = await fetch(`${API_URL}/agents/${nodeId}`);
            if (response.ok) {
              const agents = await response.json();
              if (agents.length > 0) {
                setAgentData(agents[0]);
              }
            }
          } catch (error) {
            console.error('Ошибка загрузки данных агента:', error);
          }
        }

        const workflowsData = await workflowTemplateAPI.getAll();
        const cleaned = (Array.isArray(workflowsData) ? workflowsData : []).filter(
          (wf) => !String(wf.name || '').toLowerCase().startsWith('smoke')
        );
        setWorkflows(cleaned);
      } catch (error) {
        console.error('Ошибка загрузки workflow:', error);
      } finally {
        setLoadingWorkflows(false);
      }
    };

    loadWorkflows();
  }, [nodeId]);

  // Update selected workflow IDs when agentData or workflows change
  useEffect(() => {
    if (workflows.length > 0 && agentData.applied_workflows && Array.isArray(agentData.applied_workflows)) {
      const appliedWorkflowIds = workflows
        .filter(wf => agentData.applied_workflows.includes(wf.name))
        .map(wf => wf.id);
      setSelectedWorkflowIds(appliedWorkflowIds);
    }
  }, [agentData.applied_workflows, workflows]);

  const [parameters, setParameters] = useState(agentData.parameters || []);

  // Обновляем параметры при смене агента
  useEffect(() => {
    console.log('[AgentPage] Updating parameters:', agentData.parameters);
    setParameters(agentData.parameters || []);
  }, [agentData.id, agentData.parameters]);

  const handleParameterChange = (paramId, newValue) => {
    setParameters(prev =>
      prev.map(param =>
        param.id === paramId ? { ...param, value: newValue } : param
      )
    );
  };

  const handleDeleteParameter = (paramId) => {
    if (window.confirm('Удалить этот параметр?')) {
      setParameters(prev => prev.filter(param => param.id !== paramId));
    }
  };

  const handleSaveParameters = async () => {
    const updatedAgentData = {
      ...agentData,
      parameters: parameters
    };

    if (onUpdateAgent && agentData.id) {
      try {
        await onUpdateAgent(agentData.id, updatedAgentData);
        alert('Параметры сохранены');
      } catch (error) {
        console.error('Ошибка сохранения параметров:', error);
        alert('Ошибка при сохранении параметров: ' + error.message);
      }
    } else {
      console.log('Сохранение параметров:', parameters);
      alert('Параметры сохранены (локально)');
    }
  };

  const handleAddParametersFromModal = (selectedParamIds) => {
    // Получаем список всех доступных параметров из AddParametersModal
    const allAvailableParams = [
      { id: 1, name: 'Длина', unit: 'мм', description: 'Описание длины' },
      { id: 2, name: 'Рабочая область', unit: 'мм', description: 'Размер рабочей области' },
      { id: 3, name: 'Габаритные размеры (ДxШxВ)', unit: 'мм', description: 'Габаритные размеры' },
      { id: 4, name: 'Тип рабочего стола', unit: 'Материал, дополнительный параметр', description: 'Тип рабочего стола' },
      { id: 5, name: 'Мощность шпинделя', unit: 'Вт', description: 'Мощность шпинделя' },
      { id: 6, name: 'Тип привода', unit: 'Тип', description: 'Тип привода' },
      { id: 7, name: 'Технология 3D-печати', unit: 'Тип технологии', description: 'Тип технологии печати' },
      { id: 8, name: 'Область печати', unit: 'мм', description: 'Область печати принтера' },
      { id: 9, name: 'Диаметр сопла принтера', unit: 'мм', description: 'Диаметр сопла 3D-принтера' },
      { id: 10, name: 'Максимальный диаметр гибки', unit: 'мм', description: 'Максимальный диаметр труб и арматуры' },
      { id: 11, name: 'Максимальный диаметр сверления', unit: 'мм', description: 'Диаметр отверстий' },
      { id: 12, name: 'Тип шпинделя', unit: 'Указание типа', description: 'Тип используемого шпинделя' },
      { id: 13, name: 'Глубина сверления', unit: 'мм', description: 'Максимальная глубина сверления' },
      { id: 14, name: 'Скорость покраски', unit: 'м/ч', description: 'Производительность линии' },
      { id: 15, name: 'Максимальная толщина реза', unit: 'мм', description: 'Толщина листа' },
      { id: 16, name: 'Усилие гибки', unit: 'тонн', description: 'Максимальное усилие при гибке' },
      { id: 17, name: 'Усилие резки', unit: 'тонн', description: 'Усилие при резке' },
      { id: 18, name: 'РМЦ', unit: 'мм', description: 'Расстояние между центрами' },
      { id: 19, name: 'Частота вращения шпинделя', unit: 'об/мин', description: 'Частота вращения' },
      { id: 20, name: 'Система ЧПУ', unit: 'Тип ЧПУ', description: 'ЧПУ оборудования' },
      { id: 21, name: 'Крутящий момент главного шпинделя', unit: 'Нм', description: 'Крутящий момент' },
      { id: 22, name: 'Мощность', unit: 'кВт', description: 'Мощность оборудования' },
      { id: 23, name: 'Размер рабочего стола', unit: 'мм', description: 'Размер рабочего стола' },
      { id: 24, name: 'Усиление', unit: 'кН', description: 'Способность пресса усиливать силу' },
      { id: 25, name: 'Длина гиба', unit: 'мм', description: 'Максимальная длина листа' },
      { id: 26, name: 'Скорость подачи', unit: 'мм/мин', description: 'Скорость перемещения инструмента' },
      { id: 27, name: 'Количество осей', unit: 'шт', description: 'Количество управляемых осей' },
      { id: 28, name: 'Тип охлаждения', unit: 'Тип', description: 'Система охлаждения' },
      { id: 29, name: 'Вес оборудования', unit: 'кг', description: 'Общий вес станка' },
      { id: 30, name: 'Габариты', unit: 'мм', description: 'Габаритные размеры' },
      { id: 31, name: 'Точность позиционирования', unit: 'мм', description: 'Точность перемещения' },
      { id: 32, name: 'Диаметр обработки', unit: 'мм', description: 'Максимальный диаметр заготовки' },
    ];

    const newParameters = selectedParamIds
      .map(id => allAvailableParams.find(p => p.id === parseInt(id)))
      .filter(p => p && !parameters.find(existing => existing.id === p.id))
      .map(p => ({
        ...p,
        value: ''
      }));

    setParameters(prev => [...prev, ...newParameters]);
    setIsParametersModalOpen(false);
  };

  const handleDeleteAgent = async () => {
    if (window.confirm('Вы уверены, что хотите удалить этого агента?')) {
      if (onDeleteAgent && agentData.id) {
        try {
          await onDeleteAgent(agentData.id);
          alert('Агент удален');
          navigate('/');
        } catch (error) {
          console.error('Ошибка удаления агента:', error);
          alert('Ошибка при удалении агента: ' + error.message);
        }
      } else {
        alert('Ошибка: не удалось удалить агента');
      }
    }
  };

  const handleDownloadArchive = () => {
    // Создаем данные для архива
    const archiveData = {
      agent: {
        name: agentData.name,
        okpd2_code: agentData.okpd2_code,
        quantity: agentData.quantity,
        queueNumber: agentData.queueNumber
      },
      parameters: parameters,
      workflows: selectedWorkflowIds.map(id =>
        workflows.find(wf => wf.id === id)?.name
      ).filter(Boolean),
      exportDate: new Date().toISOString()
    };

    // Создаем blob и скачиваем
    const blob = new Blob([JSON.stringify(archiveData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `agent_${agentData.name.replace(/\s+/g, '_')}_${Date.now()}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    alert('Архив успешно скачан');
  };

  const handleCreateAgent = async () => {
    // Находим родительский узел текущего агента
    const findParentNode = (nodes, targetId, parent = null) => {
      for (const node of nodes) {
        if (node.id === targetId) {
          return parent;
        }
        if (node.children && node.children.length > 0) {
          const found = findParentNode(node.children, targetId, node);
          if (found !== null) return found;
        }
      }
      return null;
    };

    const parentNode = findParentNode(treeNodes, nodeId);

    if (!parentNode) {
      alert('Ошибка: не удалось найти родительский узел');
      return;
    }

    // Создаем копию агента с теми же параметрами
    const newAgentData = {
      name: agentData.name,
      okpd2_code: agentData.okpd2_code,
      quantity: 1,
      queueNumber: Date.now(),
      parameters: parameters.map(p => ({
        id: p.id,
        name: p.name,
        unit: p.unit,
        value: p.value,
        description: p.description
      }))
    };

    try {
      if (onAddAgent && parentNode.id) {
        await onAddAgent(parentNode.id, newAgentData);
        alert('✓ Агент успешно создан!');
      } else {
        alert('Ошибка: не удалось создать агента');
      }
    } catch (error) {
      console.error('Ошибка создания агента:', error);
      alert('Ошибка при создании агента: ' + error.message);
    }
  };

  const [isStartingWorkflow, setIsStartingWorkflow] = useState(false);
  const [isStoppingWorkflow, setIsStoppingWorkflow] = useState(false);
  const [activeWorkflowId, setActiveWorkflowId] = useState(null);
  const [activeWorkflowType, setActiveWorkflowType] = useState(null);
  const [workflowNotice, setWorkflowNotice] = useState(null); // { type: 'info'|'success'|'error', text }

  const hasPendingDraft = agentData?.pending_message?.status === 'draft';
  const hasAppliedWorkflows = Array.isArray(agentData?.applied_workflows) && agentData.applied_workflows.length > 0;
  const isWorkflowRunning =
    agentData?.workflow_status === 'running' ||
    (activeWorkflowId != null && agentData?.workflow_status === 'running');
  const canExecuteWorkflow =
    !!agentData?.id &&
    !isWorkflowRunning &&
    !isStartingWorkflow &&
    (hasPendingDraft || hasAppliedWorkflows || selectedWorkflowIds.length > 0);

  const persistAgentStatus = async (numericAgentId, status) => {
    if (!numericAgentId) return;
    try {
      await fetch(`${API_URL}/agents/${numericAgentId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workflow_status: status })
      });
    } catch (_) { /* ignore */ }
  };

  const findRunningWorkflow = async (numericAgentId) => {
    if (!numericAgentId) return null;
    const list = await workflowAPI.getByAgent(`agent.${numericAgentId}`);
    const items = Array.isArray(list) ? list : [];
    return items
      .filter((w) => Number(w.status) === 0)
      .sort((a, b) => (b.id || 0) - (a.id || 0))[0] || null;
  };

  const applyRunningState = (wf) => {
    setActiveWorkflowId(wf.id);
    setActiveWorkflowType(wf.wfType || wf.wf_type || null);
    setAgentData((prev) => ({ ...prev, workflow_status: 'running' }));
  };

  const applyFinishedState = async (numericAgentId, statusCode = 1) => {
    const nextStatus = Number(statusCode) === -1 ? 'error' : 'ready';
    setActiveWorkflowId(null);
    setActiveWorkflowType(null);
    setAgentData((prev) => ({ ...prev, workflow_status: nextStatus }));
    await persistAgentStatus(numericAgentId, nextStatus);
    return nextStatus;
  };

  const refreshActiveWorkflow = async (numericAgentId = agentData?.id) => {
    if (!numericAgentId) {
      setActiveWorkflowId(null);
      setActiveWorkflowType(null);
      return null;
    }
    try {
      const running = await findRunningWorkflow(numericAgentId);
      if (running) {
        applyRunningState(running);
        return running;
      }
      if (agentData?.workflow_status === 'running') {
        await applyFinishedState(numericAgentId, 1);
      } else {
        setActiveWorkflowId(null);
        setActiveWorkflowType(null);
      }
      return null;
    } catch (error) {
      console.warn('Не удалось загрузить активные workflow агента:', error);
      return null;
    }
  };

  // Подтянуть running workflow при смене агента / вкладки
  useEffect(() => {
    if (!agentData?.id) return;
    refreshActiveWorkflow(agentData.id);
  }, [agentData?.id, activeTab]);

  // Пока выполняется — опрашиваем конкретный wfId
  useEffect(() => {
    if (!agentData?.id || !activeWorkflowId || agentData.workflow_status !== 'running') {
      return undefined;
    }

    const tick = async () => {
      try {
        const wf = await workflowAPI.getById(activeWorkflowId);
        if (!wf || Number(wf.status) !== 0) {
          const code = Number(wf?.status);
          await applyFinishedState(agentData.id, code);
          if (code === -2) {
            setWorkflowNotice({ type: 'info', text: `Workflow #${activeWorkflowId} остановлен` });
          } else if (code === -1) {
            setWorkflowNotice({ type: 'error', text: `Workflow #${activeWorkflowId} завершился с ошибкой` });
          } else {
            setWorkflowNotice({
              type: 'success',
              text: `Workflow #${activeWorkflowId} завершён${activeWorkflowType ? `: ${activeWorkflowType}` : ''}`
            });
          }
        }
      } catch (error) {
        console.warn('Ошибка опроса workflow:', error);
      }
    };

    const timer = setInterval(tick, 1000);
    return () => clearInterval(timer);
  }, [activeWorkflowId, agentData?.id, agentData?.workflow_status, activeWorkflowType]);

  // Обработчики для Workflow
  const handleWorkflowToggle = (workflowId) => {
    setSelectedWorkflowIds(prev => {
      if (prev.includes(workflowId)) {
        return prev.filter(id => id !== workflowId);
      } else {
        return [...prev, workflowId];
      }
    });
  };

  const resolveWorkflowTypeToRun = () => {
    const selectedNames = selectedWorkflowIds
      .map((id) => workflows.find((wf) => wf.id === id)?.name)
      .filter(Boolean);
    if (selectedNames.length > 0) return selectedNames[0];
    if (hasAppliedWorkflows) return agentData.applied_workflows[0];
    if (hasPendingDraft) {
      return agentData.pending_message.workflow_type || 'TECH_PROCESS_DEFAULT_WORKFLOW';
    }
    return null;
  };

  const handleExecuteWorkflow = async () => {
    if (!agentData?.id) {
      setWorkflowNotice({ type: 'error', text: 'Агент не загружен' });
      return;
    }

    setIsStartingWorkflow(true);
    setWorkflowNotice({ type: 'info', text: 'Запуск workflow…' });
    try {
      let wfId = null;
      let wfType = resolveWorkflowTypeToRun();

      if (hasPendingDraft) {
        const result = await messagesAPI.startTechProcess(agentData.id);
        wfType = result?.workflow_type || agentData.pending_message?.workflow_type || wfType;
        setAgentData((prev) => ({ ...prev, pending_message: null, workflow_status: 'running' }));
        await persistAgentStatus(agentData.id, 'running');

        // Ждём появления экземпляра в wf_list
        for (let i = 0; i < 10; i++) {
          await new Promise((r) => setTimeout(r, 300));
          const running = await findRunningWorkflow(agentData.id);
          if (running) {
            wfId = running.id;
            applyRunningState(running);
            break;
          }
          // Может уже завершиться — берём последний по агенту
          try {
            const list = await workflowAPI.getByAgent(`agent.${agentData.id}`);
            const latest = (Array.isArray(list) ? list : [])
              .sort((a, b) => (b.id || 0) - (a.id || 0))[0];
            if (latest && Number(latest.status) !== 0) {
              wfId = latest.id;
              await applyFinishedState(agentData.id, latest.status);
              setWorkflowNotice({
                type: Number(latest.status) === -1 ? 'error' : 'success',
                text: `Workflow #${latest.id} ${Number(latest.status) === -1 ? 'ошибка' : 'уже завершён'}: ${wfType || latest.wfType || latest.wf_type || ''}`
              });
              return;
            }
          } catch (_) { /* ignore */ }
        }
      } else {
        if (!wfType) {
          setWorkflowNotice({ type: 'error', text: 'Выберите или привяжите шаблон workflow' });
          return;
        }

        const parameters = {};
        if (agentData.tech_process_id) {
          parameters.techProcessId = agentData.tech_process_id;
        }

        const result = await workflowAPI.start({
          wf_type: wfType,
          agent_id: `agent.${agentData.id}`,
          parameters,
          initiator: 'ui'
        });
        wfId = result?.wfId || result?.wf_id || null;
        setActiveWorkflowId(wfId);
        setActiveWorkflowType(wfType);
        setAgentData((prev) => ({ ...prev, workflow_status: 'running' }));
        await persistAgentStatus(agentData.id, 'running');
      }

      if (!wfId) {
        setWorkflowNotice({ type: 'error', text: 'Не удалось получить id запущенного workflow' });
        await applyFinishedState(agentData.id, 1);
        return;
      }

      // Сразу проверяем: короткий шаблон может уже закончиться
      await new Promise((r) => setTimeout(r, 400));
      try {
        const wf = await workflowAPI.getById(wfId);
        if (wf && Number(wf.status) === 0) {
          applyRunningState({ ...wf, id: wfId, wfType: wfType || wf.wfType || wf.wf_type });
          setWorkflowNotice({
            type: 'info',
            text: `Выполняется #${wfId}${wfType ? `: ${wfType}` : ''} — можно остановить`
          });
        } else {
          await applyFinishedState(agentData.id, wf?.status ?? 1);
          setWorkflowNotice({
            type: Number(wf?.status) === -1 ? 'error' : 'success',
            text: `Workflow #${wfId} ${Number(wf?.status) === -1 ? 'завершился с ошибкой' : 'завершён мгновенно'}${wfType ? `: ${wfType}` : ''}`
          });
        }
      } catch (_) {
        setWorkflowNotice({
          type: 'info',
          text: `Workflow #${wfId} запущен${wfType ? `: ${wfType}` : ''}`
        });
      }

      if (onReloadTree) await onReloadTree();
    } catch (error) {
      console.error('Ошибка запуска workflow:', error);
      setWorkflowNotice({ type: 'error', text: 'Ошибка запуска: ' + error.message });
      await applyFinishedState(agentData.id, 1);
    } finally {
      setIsStartingWorkflow(false);
    }
  };

  const handleStopWorkflow = async () => {
    setIsStoppingWorkflow(true);
    try {
      let wfIdToStop = activeWorkflowId;

      if (!wfIdToStop && agentData?.id) {
        const running = await findRunningWorkflow(agentData.id);
        wfIdToStop = running?.id || null;
      }

      if (wfIdToStop) {
        try {
          await workflowAPI.stop(wfIdToStop);
        } catch (stopError) {
          console.warn('stop API:', stopError);
        }
        await applyFinishedState(agentData.id, -2);
        setWorkflowNotice({ type: 'info', text: `Workflow #${wfIdToStop} остановлен` });
      } else {
        await applyFinishedState(agentData.id, 1);
        setWorkflowNotice({ type: 'info', text: 'Активный workflow не найден — статус сброшен в «Готов»' });
      }

      if (onReloadTree) await onReloadTree();
    } catch (error) {
      console.error('Ошибка остановки workflow:', error);
      setWorkflowNotice({ type: 'error', text: 'Ошибка остановки: ' + error.message });
    } finally {
      setIsStoppingWorkflow(false);
    }
  };

  const handleApplyWorkflows = async () => {
    if (selectedWorkflowIds.length === 0) {
      setWorkflowNotice({ type: 'error', text: 'Выберите хотя бы один шаблон workflow' });
      return;
    }

    try {
      const selectedWorkflowNames = selectedWorkflowIds
        .map((id) => workflows.find((wf) => wf.id === id)?.name)
        .filter(Boolean);

      if (selectedWorkflowNames.length === 0) {
        setWorkflowNotice({ type: 'error', text: 'Выбранные шаблоны не найдены. Обновите список.' });
        return;
      }

      if (selectedWorkflowNames.length !== selectedWorkflowIds.length) {
        setWorkflowNotice({ type: 'error', text: 'Часть выбранных шаблонов больше не существует.' });
        return;
      }

      const updateData = {
        workflow_applied: true,
        workflow_status: 'ready',
        applied_workflows: selectedWorkflowNames,
      };

      const response = await fetch(`${API_URL}/agents/${agentData.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updateData),
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.message || 'Ошибка обновления агента');
      }

      const updatedAgent = await response.json();
      setAgentData(updatedAgent);
      setWorkflowNotice({
        type: 'success',
        text: `Шаблоны привязаны: ${selectedWorkflowNames.join(', ')}`
      });
    } catch (error) {
      console.error('Ошибка применения workflow:', error);
      setWorkflowNotice({ type: 'error', text: 'Ошибка привязки: ' + error.message });
    }
  };

  // Фильтрация workflow
  const filteredWorkflows = workflows.filter(wf => {
    const matchesSearch = wf.name.toLowerCase().includes(searchWorkflow.toLowerCase());
    const matchesFilter = workflowFilter === 'all' ||
                         (workflowFilter === 'selected' && selectedWorkflowIds.includes(wf.id));
    return matchesSearch && matchesFilter;
  });

  return (
    <div className="agent-page">
      {/* Навигация по вкладкам */}
      <div className="agent-tabs">
        <button
          className={`agent-tab-button ${activeTab === 'info' ? 'active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          Информация об агенте
        </button>
        <button
          className={`agent-tab-button ${activeTab === 'workflow' ? 'active' : ''}`}
          onClick={() => setActiveTab('workflow')}
        >
          Настройка Workflow
        </button>
        <button
          className={`agent-tab-button ${activeTab === 'create' ? 'active' : ''}`}
          onClick={() => setActiveTab('create')}
        >
          Создать агента
        </button>
      </div>
      <div className="agent-tabs-divider"></div>

      {/* Заголовок агента */}
      <h1 className="agent-title">{agentData.name}</h1>

      {/* Контент вкладок */}
      <div className="agent-tab-content">
        {activeTab === 'info' && (
          <div className="agent-tab-panel">
            {/* Кнопки действий */}
            <div className="agent-actions">
              <button className="btn-save-params" onClick={handleSaveParameters}>
                Сохранить параметры
              </button>
              <button className="btn-delete-agent" onClick={handleDeleteAgent}>
                Удалить агента
              </button>
              <button className="btn-download-archive" onClick={handleDownloadArchive}>
                Скачать архив
              </button>
            </div>
            {/* Таблица параметров агента */}
            <div className="agent-params-card">
              <div className="params-card-header">
                <h2 className="params-card-title">Параметры агента</h2>
                <button
                  className="btn-add-parameters"
                  onClick={() => setIsParametersModalOpen(true)}
                >
                  + Добавить параметры
                </button>
              </div>
              <div className="agent-params-table-container">
                <table className="agent-params-table">
                  <thead>
                    <tr>
                      <th>Имя параметра</th>
                      <th>Ед. изм.</th>
                      <th>Значение</th>
                      <th>Описание</th>
                      <th className="actions-column">Действия</th>
                    </tr>
                  </thead>
                  <tbody>
                    {parameters.length === 0 ? (
                      <tr>
                        <td colSpan="5" className="empty-message">
                          Параметры не добавлены
                        </td>
                      </tr>
                    ) : (
                      parameters.map((param) => (
                        <tr key={param.id} className="param-row">
                          <td>{param.name}</td>
                          <td>{param.unit}</td>
                          <td>
                            {param.name === 'Код ОКПД-2' || param.name.toLowerCase().includes('окпд') ? (
                              <span className="param-value-readonly" style={{ padding: '8px', display: 'block' }}>
                                {param.value || '—'}
                              </span>
                            ) : (
                              <input
                                type="text"
                                className="param-value-input"
                                value={param.value}
                                onChange={(e) =>
                                  handleParameterChange(param.id, e.target.value)
                                }
                              />
                            )}
                          </td>
                          <td>{param.description}</td>
                          <td className="actions-cell">
                            {param.name === 'Код ОКПД-2' || param.name.toLowerCase().includes('окпд') ? (
                              <span style={{ color: '#999', fontSize: '12px' }}>—</span>
                            ) : (
                              <button
                                className="btn-delete-param"
                                onClick={() => handleDeleteParameter(param.id)}
                                title="Удалить параметр"
                              >
                                🗑️
                              </button>
                            )}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            {techProcess && (
              <div className="agent-params-card" style={{ marginTop: 24 }}>
                <div className="params-card-header tp-ops-header">
                  <h2 className="params-card-title">
                    Операции техпроцесса {techProcess.code} ({techProcess.operations.length})
                  </h2>
                  <div className="tp-drawing-actions">
                    <input
                      id="tp-drawing-input"
                      type="file"
                      accept=".pdf,.png,.jpg,.jpeg,.webp,.tif,.tiff,.bmp,application/pdf,image/*"
                      onChange={handleTpDrawingChange}
                      style={{ display: 'none' }}
                      disabled={tpDrawingAnalyzing}
                    />
                    <button
                      type="button"
                      className="btn-add-parameters btn-outline-tp"
                      onClick={() => document.getElementById('tp-drawing-input')?.click()}
                      disabled={tpDrawingAnalyzing}
                    >
                      {tpDrawingFile ? tpDrawingFile.name : 'Загрузить чертёж'}
                    </button>
                    <button
                      type="button"
                      className="btn-add-parameters"
                      onClick={handleAnalyzeTpDrawing}
                      disabled={tpDrawingAnalyzing || !tpDrawingFile}
                    >
                      {tpDrawingAnalyzing ? 'Анализ…' : 'Оценить время'}
                    </button>
                  </div>
                </div>
                <p className="tp-drawing-hint">
                  Нейросеть проставит наладку / операцию / обслуживание по чертежу и маршруту операций
                </p>
                {tpDrawingError && <p className="tp-drawing-error">{tpDrawingError}</p>}
                {tpDrawingSummary && !tpDrawingError && (
                  <p className="tp-drawing-summary">{tpDrawingSummary}</p>
                )}
                <div className="agent-params-table-container">
                  <table className="agent-params-table">
                    <thead>
                      <tr>
                        <th>№</th>
                        <th>Тип операции</th>
                        <th>Оборудование</th>
                        <th>Инструменты</th>
                        <th>Профессия</th>
                        <th>Наладка / операция / обслуж., мин</th>
                        <th>Описание</th>
                      </tr>
                    </thead>
                    <tbody>
                      {techProcess.operations.map((op, idx) => (
                        <tr key={op.id || idx} className="param-row">
                          <td>{op.op_no != null ? String(op.op_no).padStart(3, '0') : '—'}</td>
                          <td>{op.op_type || '—'}</td>
                          <td>{op.equipment_code || '—'}</td>
                          <td>{formatToolCodes(op.tool_codes)}</td>
                          <td>{op.profession_code || '—'}</td>
                          <td>
                            {[op.setup_time_min, op.process_time_min, op.service_time_min]
                              .map(formatOpTime)
                              .join(' / ')}
                          </td>
                          <td style={{ whiteSpace: 'pre-wrap' }}>{op.description || '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab === 'workflow' && (
          <div className="agent-tab-panel">
            <h2 className="workflow-main-title">Шаблоны Workflow</h2>
            <p className="workflow-agent-name">{agentData.name}</p>
            <p className="workflow-apply-hint">
              Выберите шаблоны и нажмите «Применить» — они привяжутся к агенту.
              «Выполнить» запускает экземпляр. Короткий шаблон может завершиться сразу —
              тогда появится сообщение о завершении; «Остановить» видно только пока status = running.
            </p>

            {workflowNotice && (
              <div className={`workflow-notice workflow-notice-${workflowNotice.type}`}>
                <span>{workflowNotice.text}</span>
                <button
                  type="button"
                  className="workflow-notice-close"
                  onClick={() => setWorkflowNotice(null)}
                  aria-label="Закрыть"
                >
                  ×
                </button>
              </div>
            )}

            <div className="workflow-status-panel">
              <div className="workflow-status-row">
                <span className="status-label">Статус привязки:</span>
                <span className={`status-badge ${agentData.workflow_applied ? 'applied' : 'not-applied'}`}>
                  {agentData.workflow_applied ? '✓ Привязано' : '○ Не привязано'}
                </span>
              </div>
              <div className="workflow-status-row">
                <span className="status-label">Готовность:</span>
                <span className={`status-badge status-${agentData.workflow_status || 'not_ready'}`}>
                  {agentData.workflow_status === 'ready' ? '✓ Готов' :
                   agentData.workflow_status === 'running' ? '▶ Выполняется' :
                   agentData.workflow_status === 'error' ? '✗ Ошибка' :
                   '○ Не готов'}
                </span>
              </div>
              {agentData.applied_workflows && agentData.applied_workflows.length > 0 && (
                <div className="workflow-status-row">
                  <span className="status-label">Привязанные шаблоны:</span>
                  <span className="applied-workflows-list">
                    {agentData.applied_workflows.join(', ')}
                  </span>
                </div>
              )}
              {agentData.pending_message?.status === 'draft' && (
                <div className="workflow-status-row">
                  <span className="status-label">Черновик задачи:</span>
                  <span className="applied-workflows-list">
                    {agentData.pending_message.workflow_type || 'TECH_PROCESS_DEFAULT_WORKFLOW'}
                  </span>
                </div>
              )}
              {activeWorkflowId && (
                <div className="workflow-status-row">
                  <span className="status-label">Активный экземпляр:</span>
                  <span className="applied-workflows-list">
                    #{activeWorkflowId}{activeWorkflowType ? ` — ${activeWorkflowType}` : ''}
                  </span>
                </div>
              )}
            </div>

            <div className="workflow-toolbar">
              <input
                type="text"
                className="workflow-search"
                placeholder="Поиск шаблона..."
                value={searchWorkflow}
                onChange={(e) => setSearchWorkflow(e.target.value)}
              />
              <select
                className="workflow-filter"
                value={workflowFilter}
                onChange={(e) => setWorkflowFilter(e.target.value)}
              >
                <option value="all">Все шаблоны</option>
                <option value="selected">Только выбранные</option>
              </select>
              <button
                type="button"
                className="btn-open-editor"
                onClick={() => navigate('/workflow-templates')}
              >
                Открыть редактор
              </button>
            </div>

            <div className="workflow-list-container">
              <table className="workflow-table">
                <thead>
                  <tr>
                    <th className="workflow-checkbox-column">Выбрать</th>
                    <th>Название</th>
                    <th>Описание</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredWorkflows.length === 0 ? (
                    <tr>
                      <td colSpan="3" className="empty-message">
                        Шаблоны не найдены
                      </td>
                    </tr>
                  ) : (
                    filteredWorkflows.map((workflow) => (
                      <tr key={workflow.id} className="workflow-row">
                        <td className="workflow-checkbox-cell">
                          <input
                            type="checkbox"
                            checked={selectedWorkflowIds.includes(workflow.id)}
                            onChange={() => handleWorkflowToggle(workflow.id)}
                          />
                        </td>
                        <td>{workflow.name}</td>
                        <td className="workflow-description-cell">
                          {workflow.description || '—'}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            <div className="workflow-actions">
              {canExecuteWorkflow && (
                <button
                  type="button"
                  className="btn-start-workflow"
                  onClick={handleExecuteWorkflow}
                  disabled={isStartingWorkflow || isStoppingWorkflow}
                >
                  {isStartingWorkflow ? 'Запуск…' : 'Выполнить'}
                </button>
              )}
              {isWorkflowRunning && (
                <button
                  type="button"
                  className="btn-stop-workflow"
                  onClick={handleStopWorkflow}
                  disabled={isStoppingWorkflow || isStartingWorkflow}
                  title={activeWorkflowId ? `Остановить #${activeWorkflowId}` : 'Сбросить залипший статус выполнения'}
                >
                  {isStoppingWorkflow ? 'Остановка…' : 'Остановить'}
                </button>
              )}
              <button className="btn-save-workflow" onClick={handleApplyWorkflows}>
                Применить к агенту ({selectedWorkflowIds.length})
              </button>
            </div>
          </div>
        )}

        {activeTab === 'create' && (
          <div className="agent-tab-panel">
            <div className="agent-params-card">
              <h2 className="params-card-title">Создать агента</h2>
              <p className="create-agent-description">
                Нажмите кнопку ниже, чтобы создать нового агента на основе текущих настроек.
              </p>
              <div className="create-agent-info">
                <div className="info-row">
                  <span className="info-label">Имя агента:</span>
                  <span className="info-value">{agentData.name}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">ОКПД-2:</span>
                  <span className="info-value">{agentData.okpd2_code}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Host:</span>
                  <span className="info-value">{agentData.host_name || agentData.host_id || '—'}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Количество:</span>
                  <span className="info-value">{agentData.quantity}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Параметров:</span>
                  <span className="info-value">{parameters.length}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Выбрано Workflow:</span>
                  <span className="info-value">{selectedWorkflowIds.length}</span>
                </div>
              </div>
              <div className="create-agent-actions">
                <button className="btn-create-agent-final" onClick={handleCreateAgent}>
                  Создать агента
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Модальное окно для добавления параметров */}
      <AddParametersModal
        isOpen={isParametersModalOpen}
        onClose={() => setIsParametersModalOpen(false)}
        onSave={handleAddParametersFromModal}
      />
    </div>
  );
}

export default AgentPage;
