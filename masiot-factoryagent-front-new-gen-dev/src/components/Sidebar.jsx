import React, { useState } from 'react';
import './Sidebar.css';

function Sidebar({
  treeNodes = [],
  expandedNodes = {},
  navigate,
  onAddNode,
  onToggleNode,
  onMoveNode,
  onDeleteNode,
  isEditMode = false,
  onToggleEditMode,
  currentUser
}) {
  const [newBranchName, setNewBranchName] = useState('');
  const [selectedNode, setSelectedNode] = useState('glavnaya');
  const [draggedNode, setDraggedNode] = useState(null);
  const [dragOverNode, setDragOverNode] = useState(null);
  const [isDragOverRoot, setIsDragOverRoot] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  // Подсчет агентов в узле и его детях
  const countAgents = (node) => {
    let count = node.type === 'agent' ? 1 : 0;
    if (node.children) {
      node.children.forEach(child => {
        count += countAgents(child);
      });
    }
    return count;
  };

  // Проверка, содержит ли узел или его дети совпадение с поиском
  const nodeMatchesSearch = (node, query) => {
    if (!query.trim()) return true;
    const lowerQuery = query.toLowerCase();

    // Проверяем имя узла
    if (node.name.toLowerCase().includes(lowerQuery)) return true;

    // Проверяем детей
    if (node.children) {
      return node.children.some(child => nodeMatchesSearch(child, query));
    }

    return false;
  };

  // Фильтрация узлов по поиску
  const filterNodes = (nodes, query) => {
    if (!query.trim()) return nodes;

    return nodes.filter(node => nodeMatchesSearch(node, query));
  };

  const filteredTreeNodes = filterNodes(treeNodes, searchQuery);

  const handleNodeClick = (node) => {
    if (isEditMode) return;
    setSelectedNode(node.id);

    let targetPath = '';
    if (node.path) {
      targetPath = node.path;
      navigate(node.path);
    } else if (node.type === 'root') {
      targetPath = '/';
      navigate('/');
    } else if (node.type === 'agent') {
      targetPath = `/agent/${node.id}`;
      navigate(targetPath, { state: { agentData: node.agentData, nodeId: node.id } });
    } else if (node.type === 'branch') {
      targetPath = `/branch/${node.id}`;
      navigate(targetPath, { state: { branchName: node.name, nodeId: node.id } });
    }

    // Save last visited path
    if (targetPath) {
      localStorage.setItem('lastVisitedPath', targetPath);
    }
  };

  const handleAddBranch = () => {
    if (newBranchName.trim()) {
      onAddNode && onAddNode(newBranchName.trim());
      setNewBranchName('');
    }
  };

  const handleDragStart = (e, node) => {
    if (!isEditMode) return;
    // Запрещаем перетаскивание root узлов
    if (node.type === 'root') {
      e.preventDefault();
      return;
    }
    setDraggedNode(node.id);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e, nodeId) => {
    if (!isEditMode) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverNode(nodeId);
  };

  const handleDragLeave = () => {
    if (!isEditMode) return;
    setDragOverNode(null);
  };

  const handleDrop = (e, targetNodeId) => {
    if (!isEditMode) return;
    e.preventDefault();
    setDragOverNode(null);

    if (draggedNode && draggedNode !== targetNodeId) {
      onMoveNode && onMoveNode(draggedNode, targetNodeId);
    }
    setDraggedNode(null);
  };

  const handleDragEnd = () => {
    setDraggedNode(null);
    setDragOverNode(null);
    setIsDragOverRoot(false);
  };

  const handleDeleteNode = (e, nodeId) => {
    e.stopPropagation();
    if (window.confirm('Удалить этот элемент?')) {
      onDeleteNode && onDeleteNode(nodeId);
    }
  };

  const handleRootDragOver = (e) => {
    if (!isEditMode || !draggedNode) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setIsDragOverRoot(true);
  };

  const handleRootDragLeave = () => {
    if (!isEditMode) return;
    setIsDragOverRoot(false);
  };

  const handleRootDrop = (e) => {
    if (!isEditMode) return;
    e.preventDefault();
    setIsDragOverRoot(false);

    if (draggedNode) {
      onMoveNode && onMoveNode(draggedNode, '__root__');
    }
    setDraggedNode(null);
  };

  const renderNode = (node, level = 0) => {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = expandedNodes[node.id];
    const isSelected = selectedNode === node.id;
    const isDragOver = dragOverNode === node.id;
    const agentCount = countAgents(node);
    const isHighlighted = searchQuery.trim() && node.name.toLowerCase().includes(searchQuery.toLowerCase());

    return (
      <div key={node.id} className="tree-node-group">
        <div
          className={`tree-node ${isSelected ? 'selected' : ''} ${isDragOver ? 'drag-over' : ''} ${isEditMode && node.type !== 'root' ? 'draggable' : ''} ${isHighlighted ? 'highlighted' : ''}`}
          onClick={() => handleNodeClick(node)}
          draggable={isEditMode && node.type !== 'root'}
          onDragStart={(e) => handleDragStart(e, node)}
          onDragOver={(e) => handleDragOver(e, node.id)}
          onDragLeave={handleDragLeave}
          onDrop={(e) => handleDrop(e, node.id)}
          onDragEnd={handleDragEnd}
        >
          {hasChildren && (
            <span
              className="node-chevron"
              onClick={(e) => {
                e.stopPropagation();
                onToggleNode && onToggleNode(node.id);
              }}
            >
              {isExpanded ? '▼' : '▶'}
            </span>
          )}
          <span className="node-icon">{node.icon}</span>
          <span className={`node-label ${node.type === 'root' ? 'main-label' : ''}`}>
            {node.name}
          </span>
          {node.type === 'branch' && agentCount > 0 && (
            <span className="agent-count">{agentCount}</span>
          )}
          {isEditMode && node.type !== 'root' && (
            <button
              className="node-delete-btn"
              onClick={(e) => handleDeleteNode(e, node.id)}
              title="Удалить"
            >
              ×
            </button>
          )}
        </div>

        {hasChildren && isExpanded && (
          <div className="tree-children">
            {node.children.map(child => renderNode(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="sidebar">
      <div className="sidebar-header">
        <h3 className="add-element-title">Добавить элемент</h3>
        <div className="add-branch-container">
          <input
            type="text"
            value={newBranchName}
            onChange={(e) => setNewBranchName(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleAddBranch()}
            placeholder="Новый элемент…"
            className="add-branch-input"
          />
          <button onClick={handleAddBranch} className="add-branch-btn">
            +
          </button>
        </div>
      </div>

      <div className="search-container">
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="🔍 Поиск агентов..."
          className="search-input"
        />
        {searchQuery && (
          <button
            className="search-clear-btn"
            onClick={() => setSearchQuery('')}
            title="Очистить поиск"
          >
            ×
          </button>
        )}
      </div>

      {isEditMode && (
        <div className="edit-mode-banner">
          <span>🔧 Режим редактирования</span>
          <button className="exit-edit-mode-btn" onClick={onToggleEditMode}>
            Выйти
          </button>
        </div>
      )}

      {currentUser && currentUser.role === 'admin' && (
        <div className="admin-menu">
          <button
            className="admin-menu-item"
            onClick={() => navigate('/users')}
          >
            <span className="admin-menu-icon">👥</span>
            <span>Пользователи</span>
          </button>
        </div>
      )}

      <div className="workflow-menu">
        <button
          className="workflow-menu-item"
          onClick={() => navigate('/hosts')}
        >
          <span className="workflow-menu-icon">🖥️</span>
          <span>Hosts</span>
        </button>
        <button
          className="workflow-menu-item"
          onClick={() => navigate('/workflow-templates')}
        >
          <span className="workflow-menu-icon">🎨</span>
          <span>Редактор шаблонов</span>
        </button>
      </div>

      <div className="tree">
        {filteredTreeNodes.map(node => renderNode(node))}

        {isEditMode && draggedNode && (
          <div
            className={`root-drop-zone ${isDragOverRoot ? 'drag-over' : ''}`}
            onDragOver={handleRootDragOver}
            onDragLeave={handleRootDragLeave}
            onDrop={handleRootDrop}
          >
            <span className="drop-zone-icon">⬆️</span>
            <span className="drop-zone-text">Переместить на первый уровень</span>
          </div>
        )}
      </div>
    </div>
  );
}

export default Sidebar;
