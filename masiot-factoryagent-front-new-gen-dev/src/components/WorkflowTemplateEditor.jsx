import React, { useState, useCallback, useEffect, useMemo } from 'react';
import ReactFlow, {
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  addEdge,
  MarkerType,
} from 'reactflow';
import 'reactflow/dist/style.css';
import './WorkflowTemplateEditor.css';
import { workflowTemplateAPI, workflowFunctionsAPI } from '../services/api';
import FunctionEditorModal from './FunctionEditorModal';
import FunctionInfoModal from './FunctionInfoModal';
import FunctionNode from './nodes/FunctionNode';

const nodeTypes = {
  function: FunctionNode,
};

function normalizeNode(node) {
  const data = node.data || {};
  const functionClass =
    data.functionClass || data.functionclass || data.class || '';
  const functionName =
    data.functionName || data.functionname || data.name || '';
  const needParam = data.needParam || data.params || [];
  const paramReturn = data.paramReturn || data.outputVar || data.returns || '';

  return {
    ...node,
    type: 'function',
    data: {
      label: data.label || functionName || 'Node',
      description: data.description || '',
      functionClass,
      functionName,
      needParam: Array.isArray(needParam) ? needParam : [],
      paramReturn,
      config: data.config || {},
      icon: data.icon || '⚙️',
    },
  };
}

function WorkflowTemplateEditor() {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [templates, setTemplates] = useState([]);
  const [availableFunctions, setAvailableFunctions] = useState([]);
  const [selectedTemplate, setSelectedTemplate] = useState(null);
  const [workflowName, setWorkflowName] = useState('');
  const [workflowDescription, setWorkflowDescription] = useState('');
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [showFunctionInfo, setShowFunctionInfo] = useState(false);
  const [showFunctionEditor, setShowFunctionEditor] = useState(false);
  const [selectedNode, setSelectedNode] = useState(null);
  const [editingFunction, setEditingFunction] = useState(null);
  const [reactFlowInstance, setReactFlowInstance] = useState(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadTemplates();
    loadFunctions();
  }, []);

  const loadTemplates = async () => {
    try {
      const data = await workflowTemplateAPI.getAll();
      setTemplates(Array.isArray(data) ? data : []);
      return Array.isArray(data) ? data : [];
    } catch (error) {
      console.error('Ошибка загрузки шаблонов:', error);
      return [];
    }
  };

  const loadFunctions = async () => {
    try {
      const data = await workflowFunctionsAPI.getAll();
      const filtered = (Array.isArray(data) ? data : []).filter(
        (f) => f.type !== 'Example'
      );
      setAvailableFunctions(filtered);
    } catch (error) {
      console.error('Ошибка загрузки функций:', error);
    }
  };

  const functionsByType = useMemo(() => {
    const groups = {};
    for (const func of availableFunctions) {
      const key = func.type || 'Other';
      if (!groups[key]) groups[key] = [];
      groups[key].push(func);
    }
    return Object.entries(groups).sort(([a], [b]) => a.localeCompare(b, 'ru'));
  }, [availableFunctions]);

  const onConnect = useCallback(
    (params) =>
      setEdges((eds) =>
        addEdge(
          {
            ...params,
            markerEnd: { type: MarkerType.ArrowClosed },
            animated: true,
          },
          eds
        )
      ),
    [setEdges]
  );

  const onDragOver = useCallback((event) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  const onDrop = useCallback(
    (event) => {
      event.preventDefault();
      if (!reactFlowInstance) return;

      const functionData = JSON.parse(
        event.dataTransfer.getData('application/reactflow')
      );
      const reactFlowBounds = event.target.getBoundingClientRect();
      const position = reactFlowInstance.project({
        x: event.clientX - reactFlowBounds.left,
        y: event.clientY - reactFlowBounds.top,
      });

      const newNode = {
        id: `node-${Date.now()}`,
        type: 'function',
        position,
        data: {
          label: functionData.name,
          description: functionData.description,
          functionClass: functionData.functionClass || functionData.class || '',
          functionName: functionData.functionName || functionData.name || '',
          needParam: functionData.params || [],
          paramReturn: functionData.returns || '',
          config: {},
          icon: functionData.icon,
        },
      };

      setNodes((nds) => nds.concat(newNode));
    },
    [reactFlowInstance, setNodes]
  );

  const onDragStart = (event, func) => {
    event.dataTransfer.setData('application/reactflow', JSON.stringify(func));
    event.dataTransfer.effectAllowed = 'move';
  };

  const applyTemplateToCanvas = (data) => {
    const loadedNodes = (data.nodes || [])
      .filter((n) => {
        const d = n.data || {};
        return (
          n.type === 'function' ||
          d.functionClass ||
          d.functionclass ||
          d.functionName ||
          d.functionname
        );
      })
      .map(normalizeNode);

    setNodes(loadedNodes);
    setEdges(data.edges || []);
    setWorkflowName(data.name || '');
    setWorkflowDescription(data.description || '');
    setSelectedTemplate(data);
  };

  const handleSelectTemplate = async (template) => {
    try {
      const data = await workflowTemplateAPI.getById(template.id);
      applyTemplateToCanvas(data);
    } catch (error) {
      console.error('Ошибка загрузки:', error);
      alert('Ошибка загрузки workflow: ' + error.message);
    }
  };

  const handleSave = async () => {
    const name = workflowName.trim();
    if (!name) {
      alert('Введите название workflow');
      return;
    }

    const duplicate = templates.find(
      (t) =>
        t.name.toLowerCase() === name.toLowerCase() &&
        (!selectedTemplate || t.id !== selectedTemplate.id)
    );
    if (duplicate) {
      alert(`Шаблон с именем «${name}» уже существует`);
      return;
    }

    setSaving(true);
    try {
      const workflowData = {
        name,
        description: workflowDescription,
        nodes,
        edges,
      };

      let saved;
      if (selectedTemplate?.id) {
        saved = await workflowTemplateAPI.update(selectedTemplate.id, workflowData);
      } else {
        saved = await workflowTemplateAPI.create(workflowData);
      }

      const list = await loadTemplates();
      const selected =
        list.find((t) => t.id === saved.id) ||
        saved;
      setSelectedTemplate(selected);
      setWorkflowName(selected.name || name);
      setWorkflowDescription(selected.description || workflowDescription);
      setShowSaveDialog(false);
      alert(selectedTemplate?.id ? '✓ Workflow обновлён' : '✓ Workflow создан');
    } catch (error) {
      console.error('Ошибка сохранения:', error);
      alert('Ошибка сохранения workflow: ' + error.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (templateId) => {
    if (!window.confirm('Удалить этот workflow из базы данных?')) return;

    try {
      await workflowTemplateAPI.delete(templateId);
      await loadTemplates();
      if (selectedTemplate && selectedTemplate.id === templateId) {
        handleNew();
      }
    } catch (error) {
      console.error('Ошибка удаления:', error);
      alert('Ошибка удаления workflow: ' + error.message);
    }
  };

  const handleNew = () => {
    setNodes([]);
    setEdges([]);
    setWorkflowName('');
    setWorkflowDescription('');
    setSelectedTemplate(null);
  };

  const handleDeleteNode = () => {
    const selectedNodes = nodes.filter((node) => node.selected);
    if (selectedNodes.length === 0) {
      alert('Выберите узел для удаления (кликните на узел)');
      return;
    }
    const nodeIds = selectedNodes.map((n) => n.id);
    setNodes((nds) => nds.filter((n) => !nodeIds.includes(n.id)));
    setEdges((eds) =>
      eds.filter((e) => !nodeIds.includes(e.source) && !nodeIds.includes(e.target))
    );
  };

  const handleNodeDoubleClick = (event, node) => {
    setSelectedNode(node);
    setShowFunctionInfo(true);
  };

  const handleCreateFunction = () => {
    setEditingFunction(null);
    setShowFunctionEditor(true);
  };

  const handleEditFunction = (func) => {
    setEditingFunction(func);
    setShowFunctionEditor(true);
  };

  const handleSaveFunction = async (functionData) => {
    try {
      if (editingFunction) {
        await workflowFunctionsAPI.update(editingFunction.id, functionData);
      } else {
        await workflowFunctionsAPI.create(functionData);
      }
      setShowFunctionEditor(false);
      setEditingFunction(null);
      loadFunctions();
    } catch (error) {
      console.error('Ошибка сохранения функции:', error);
      alert('Ошибка сохранения функции: ' + error.message);
    }
  };

  const handleDeleteFunction = async (funcId) => {
    if (!window.confirm('Удалить эту функцию?')) return;

    try {
      await workflowFunctionsAPI.delete(funcId);
      loadFunctions();
    } catch (error) {
      console.error('Ошибка удаления функции:', error);
      alert('Ошибка удаления функции: ' + error.message);
    }
  };

  return (
    <div className="workflow-template-editor">
      <div className="workflow-editor-header">
        <div className="header-left">
          <h2>Редактор шаблонов Workflow</h2>
          {selectedTemplate ? (
            <span className="current-template">
              Редактируется: {selectedTemplate.name}
            </span>
          ) : (
            <span className="current-template">Новый шаблон (не сохранён)</span>
          )}
        </div>
        <div className="workflow-editor-actions">
          <button onClick={handleNew} className="btn-new" title="Создать новый workflow">
            Новый
          </button>
          <button
            onClick={() => setShowSaveDialog(true)}
            className="btn-save"
            title="Сохранить в БД"
          >
            Сохранить
          </button>
          <button
            onClick={handleDeleteNode}
            className="btn-delete-node"
            title="Удалить выбранный узел"
          >
            Удалить узел
          </button>
        </div>
      </div>

      <div className="workflow-editor-content">
        <div className="editor-sidebar">
          <div className="templates-panel">
            <div className="panel-header">
              <h3>Шаблоны</h3>
              <span className="panel-count">{templates.length}</span>
            </div>
            <div className="templates-list">
              {templates.length === 0 ? (
                <div className="no-templates-inline">Нет шаблонов</div>
              ) : (
                templates.map((template) => (
                  <div
                    key={template.id}
                    className={`template-list-item ${
                      selectedTemplate?.id === template.id ? 'active' : ''
                    }`}
                  >
                    <button
                      type="button"
                      className="template-list-select"
                      onClick={() => handleSelectTemplate(template)}
                      title={template.description || template.name}
                    >
                      <span className="template-list-name">{template.name}</span>
                      {template.description && (
                        <span className="template-list-desc">{template.description}</span>
                      )}
                    </button>
                    <button
                      type="button"
                      className="btn-delete-template-inline"
                      onClick={() => handleDelete(template.id)}
                      title="Удалить"
                    >
                      ×
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="function-palette">
            <div className="palette-header">
              <h3>Функции</h3>
              <button
                onClick={handleCreateFunction}
                className="btn-add-function"
                title="Создать новую функцию"
              >
                + Добавить
              </button>
            </div>

            <div className="function-list scrollable">
              {functionsByType.map(([type, funcs]) => (
                <div key={type} className="palette-type-group">
                  <div className="palette-type-title">{type}</div>
                  {funcs.map((func) => (
                    <div
                      key={func.id}
                      className="function-item"
                      draggable
                      onDragStart={(e) => onDragStart(e, func)}
                      title={`${func.description || ''}\nКласс: ${func.functionClass || ''}`}
                    >
                      <div className="function-item-icon">{func.icon}</div>
                      <div className="function-item-content">
                        <div className="function-item-name">{func.name}</div>
                        <div className="function-item-description">
                          {func.description}
                        </div>
                      </div>
                      <div className="function-item-actions">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleEditFunction(func);
                          }}
                          className="btn-edit-function"
                          title="Редактировать"
                        >
                          ✎
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteFunction(func.id);
                          }}
                          className="btn-delete-function"
                          title="Удалить"
                        >
                          ×
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="flow-canvas">
          {nodes.length === 0 && (
            <div className="empty-canvas-hint">
              <div className="hint-text">
                <h3>Создание шаблона workflow</h3>
                <p>Выберите шаблон слева или перетащите функции на холст</p>
                <p>Соедините узлы стрелками — порядок выполнения сверху вниз по связям</p>
              </div>
            </div>
          )}
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onDrop={onDrop}
            onDragOver={onDragOver}
            onInit={setReactFlowInstance}
            onNodeDoubleClick={handleNodeDoubleClick}
            nodeTypes={nodeTypes}
            fitView
          >
            <Controls />
            <MiniMap />
            <Background variant="dots" gap={12} size={1} />
          </ReactFlow>
        </div>
      </div>

      <div className="workflow-editor-footer">
        <span>Узлов: {nodes.length}</span>
        <span>Связей: {edges.length}</span>
        <span>Шаблонов: {templates.length}</span>
      </div>

      {showSaveDialog && (
        <div className="modal-overlay" onClick={() => setShowSaveDialog(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>{selectedTemplate ? 'Обновить шаблон' : 'Создать шаблон'}</h3>
            <div className="form-group">
              <label>Название:</label>
              <input
                type="text"
                value={workflowName}
                onChange={(e) => setWorkflowName(e.target.value)}
                placeholder="Например: Планирование техпроцесса"
                autoFocus
              />
            </div>
            <div className="form-group">
              <label>Описание:</label>
              <textarea
                value={workflowDescription}
                onChange={(e) => setWorkflowDescription(e.target.value)}
                placeholder="Краткое описание workflow"
                rows={3}
              />
            </div>
            <div className="modal-info">
              <p>
                Узлов: {nodes.length}, Связей: {edges.length}
                {selectedTemplate?.id ? ` · id ${selectedTemplate.id}` : ' · новый'}
              </p>
            </div>
            <div className="modal-actions">
              <button onClick={handleSave} className="btn-primary" disabled={saving}>
                {saving
                  ? 'Сохранение…'
                  : selectedTemplate
                    ? 'Обновить'
                    : 'Создать'}
              </button>
              <button
                onClick={() => setShowSaveDialog(false)}
                className="btn-secondary"
                disabled={saving}
              >
                Отмена
              </button>
            </div>
          </div>
        </div>
      )}

      <FunctionInfoModal
        isOpen={showFunctionInfo}
        onClose={() => {
          setShowFunctionInfo(false);
          setSelectedNode(null);
        }}
        functionData={selectedNode?.data}
      />

      <FunctionEditorModal
        isOpen={showFunctionEditor}
        onClose={() => {
          setShowFunctionEditor(false);
          setEditingFunction(null);
        }}
        onSave={handleSaveFunction}
        editFunction={editingFunction}
      />
    </div>
  );
}

export default WorkflowTemplateEditor;
