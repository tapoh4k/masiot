import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, useNavigate, Navigate } from 'react-router-dom';
import Sidebar from './components/Sidebar.jsx';
import Header from './components/Header.jsx';
import MainPage from './components/MainPage.jsx';
import BranchPage from './components/BranchPage.jsx';
import AgentPage from './components/AgentPage.jsx';
import LoginPage from './components/LoginPage.jsx';
import UsersPage from './components/UsersPage.jsx';
import HostsPage from './components/HostsPage.jsx';
import WorkflowTemplateEditor from './components/WorkflowTemplateEditor.jsx';
import { treeAPI, agentAPI } from './services/api.js';
import './App.css';

function AppContent() {
  const navigate = useNavigate();
  const [currentUser, setCurrentUser] = useState(null);
  const [isEditMode, setIsEditMode] = useState(false);
  const [loading, setLoading] = useState(true);

  // Динамическая структура дерева
  const [treeNodes, setTreeNodes] = useState([
    {
      id: 'glavnaya',
      name: 'Главная',
      icon: '🏠',
      type: 'root',
      path: '/',
      children: []
    },
    {
      id: 'zavod',
      name: 'Завод',
      icon: '🏭',
      type: 'branch',
      children: []
    },
    {
      id: 'struktura-zavoda',
      name: 'Структура завода',
      icon: '📋',
      type: 'branch',
      children: [
        {
          id: 'tokarny-ceh',
          name: 'Токарный цех',
          icon: '🏢',
          type: 'branch',
          children: []
        },
        {
          id: 'sborocny-ceh',
          name: 'Сборочный цех',
          icon: '🏢',
          type: 'branch',
          children: []
        }
      ]
    },
    {
      id: 'unclassified',
      name: 'Unclassified',
      icon: '📁',
      type: 'branch',
      children: []
    }
  ]);

  const [expandedNodes, setExpandedNodes] = useState(() => {
    const saved = localStorage.getItem('expandedNodes');
    return saved ? JSON.parse(saved) : {};
  });

  // Save expanded nodes to localStorage when they change
  useEffect(() => {
    localStorage.setItem('expandedNodes', JSON.stringify(expandedNodes));
  }, [expandedNodes]);

  // Проверка авторизации при загрузке
  useEffect(() => {
    const storedUser = localStorage.getItem('currentUser');
    if (storedUser) {
      setCurrentUser(JSON.parse(storedUser));
    }
  }, []);

  // Загрузка данных из БД при монтировании
  useEffect(() => {
    if (currentUser) {
      loadTreeFromDB();
    }
  }, [currentUser]);

  const handleLogin = (user) => {
    setCurrentUser(user);
  };

  const handleLogout = () => {
    localStorage.removeItem('currentUser');
    setCurrentUser(null);
    navigate('/login');
  };

  // Restore last visited page on mount
  useEffect(() => {
    if (currentUser) {
      const lastPath = localStorage.getItem('lastVisitedPath');
      if (lastPath && lastPath !== '/login') {
        navigate(lastPath);
      }
    }
  }, [currentUser]);

  const loadTreeFromDB = async () => {
    try {
      setLoading(true);
      const nodes = await treeAPI.getAll();

      if (nodes.length > 0) {
        // Преобразуем плоский список в иерархическую структуру
        const treeStructure = await buildTreeStructure(nodes);
        setTreeNodes(treeStructure);
      } else {
        // Если БД пустая, инициализируем начальными данными
        await initializeDefaultTree();
      }
    } catch (error) {
      console.error('Ошибка загрузки дерева:', error);
      // Используем локальные данные при ошибке
    } finally {
      setLoading(false);
    }
  };

  const buildTreeStructure = async (flatNodes) => {
    const nodeMap = {};
    const rootNodes = [];

    // Создаем карту всех узлов
    flatNodes.forEach(node => {
      nodeMap[node.id] = {
        ...node,
        children: []
      };
    });

    // Загружаем данные агентов для узлов типа 'agent' (старые данные)
    const agentNodes = flatNodes.filter(node => node.type === 'agent');
    await Promise.all(
      agentNodes.map(async (node) => {
        try {
          const agentData = await agentAPI.getByNodeId(node.id);
          if (agentData) {
            const agents = Array.isArray(agentData) ? agentData : [agentData];
            const agent = agents[0]; // Берем первого агента
            nodeMap[node.id].agentData = {
              id: agent.id,
              name: node.name,
              type: agent.type,
              host_id: agent.host_id,
              host_name: agent.host_name,
              okpd2_code: agent.okpd2_code,
              quantity: agent.quantity,
              queueNumber: agent.queue_number,
              parameters: agent.parameters || []
            };
          }
        } catch (error) {
          console.error(`Ошибка загрузки данных агента ${node.id}:`, error);
        }
      })
    );

    // Строим иерархию из узлов дерева
    flatNodes.forEach(node => {
      if (node.parent_id && nodeMap[node.parent_id]) {
        nodeMap[node.parent_id].children.push(nodeMap[node.id]);
      } else {
        rootNodes.push(nodeMap[node.id]);
      }
    });

    // Загружаем новых агентов для папок (узлов типа 'branch')
    const branchNodes = flatNodes.filter(node => node.type === 'branch' || node.type === 'root');
    for (const node of branchNodes) {
      try {
        const agentData = await agentAPI.getByNodeId(node.id);
        if (agentData) {
          const agents = Array.isArray(agentData) ? agentData : [agentData];

          // Добавляем каждого агента как дочерний элемент
          agents.forEach(agent => {
            const agentNode = {
              id: `agent-${agent.id}`,
              name: agent.name || agent.okpd2_code || 'Агент',
              icon: '🔑',
              type: 'agent',
              agentData: {
                id: agent.id,
                name: agent.name || agent.okpd2_code,
                type: agent.type,
                host_id: agent.host_id,
                host_name: agent.host_name,
                okpd2_code: agent.okpd2_code,
                quantity: agent.quantity,
                queueNumber: agent.queue_number,
                parameters: agent.parameters || []
              },
              children: []
            };
            nodeMap[node.id].children.push(agentNode);
          });
        }
      } catch (error) {
        // Нет агентов для этого узла - это нормально
      }
    }

    // Сортируем корневые узлы: 'root' тип всегда первый
    rootNodes.sort((a, b) => {
      if (a.type === 'root') return -1;
      if (b.type === 'root') return 1;
      return 0;
    });

    return rootNodes;
  };

  const initializeDefaultTree = async () => {
    // Сохраняем начальную структуру в БД
    const defaultNodes = treeNodes;
    for (const node of defaultNodes) {
      await saveNodeRecursively(node, null);
    }
  };

  const saveNodeRecursively = async (node, parentId) => {
    try {
      // Если это агент, создаем только запись в таблице agents, БЕЗ узла дерева
      if (node.type === 'agent' && node.agentData) {
        await agentAPI.create({
          node_id: parentId, // Агент привязан к родительской папке
          okpd2_code: node.agentData.okpd2_code,
          quantity: node.agentData.quantity,
          queue_number: node.agentData.queueNumber,
          parameters: node.agentData.parameters
        });
        return; // Не создаем узел дерева для агента
      }

      // Создаем узел дерева только для папок (branch) и root
      await treeAPI.create({
        id: node.id,
        name: node.name,
        icon: node.icon,
        type: node.type,
        parent_id: parentId,
        position: 0
      });

      // Рекурсивно сохраняем детей
      if (node.children && node.children.length > 0) {
        for (const child of node.children) {
          await saveNodeRecursively(child, node.id);
        }
      }
    } catch (error) {
      console.error('Ошибка сохранения узла:', error);
    }
  };

  const toggleEditMode = () => {
    setIsEditMode(prev => !prev);
  };

  const toggleNode = (nodeId) => {
    setExpandedNodes(prev => ({
      ...prev,
      [nodeId]: !prev[nodeId]
    }));
  };

  const addNodeToTree = async (nodeName) => {
    if (!nodeName.trim()) return null;

    const newNode = {
      id: `node-${Date.now()}`,
      name: nodeName,
      icon: '📁',
      type: 'branch',
      children: []
    };

    try {
      await treeAPI.create({
        id: newNode.id,
        name: newNode.name,
        icon: newNode.icon,
        type: newNode.type,
        parent_id: null,
        position: 0
      });

      // Добавляем новый узел, сохраняя 'root' тип первым
      setTreeNodes(prev => {
        const rootNode = prev.find(n => n.type === 'root');
        const otherNodes = prev.filter(n => n.type !== 'root');

        if (rootNode) {
          return [rootNode, ...otherNodes, newNode];
        }
        return [...prev, newNode];
      });

      // Возвращаем ID созданного узла
      return newNode.id;
    } catch (error) {
      console.error('Ошибка создания узла:', error);
      return null;
    }
  };

  const findNodeById = (nodes, nodeId) => {
    for (const node of nodes) {
      if (node.id === nodeId) return node;
      if (node.children && node.children.length > 0) {
        const found = findNodeById(node.children, nodeId);
        if (found) return found;
      }
    }
    return null;
  };

  const moveNode = async (draggedId, targetId) => {
    if (draggedId === targetId) return;

    try {
      const newParentId = targetId === '__root__' ? null : targetId;

      // Проверяем, является ли перетаскиваемый элемент агентом
      if (draggedId.startsWith('agent-')) {
        // Это агент - обновляем его node_id в таблице agents
        const agentId = draggedId.replace('agent-', '');
        await agentAPI.update(agentId, {
          node_id: newParentId  // Используем snake_case для совместимости с backend
        });

        // Перезагружаем дерево из БД
        await loadTreeFromDB();

        // Автоматически раскрываем целевую папку после перемещения
        if (targetId !== '__root__') {
          setExpandedNodes(prev => ({
            ...prev,
            [targetId]: true
          }));
        }
        return;
      }

      // Это узел дерева (папка) - обновляем parent_id в таблице tree_nodes
      // Важно: для parent_id = null используем специальный endpoint, иначе backend не обновит (ParentId==null не проходит условие)
      if (targetId === '__root__') {
        await treeAPI.updateParent(draggedId, null);
      } else {
        await treeAPI.update(draggedId, {
          parent_id: newParentId
        });
      }

      // Автоматически раскрываем целевую папку после перемещения
      if (targetId !== '__root__') {
        setExpandedNodes(prev => ({
          ...prev,
          [targetId]: true
        }));
      }

      // Специальный случай: перемещение на корневой уровень
      if (targetId === '__root__') {
        setTreeNodes(prev => {
          const newTree = JSON.parse(JSON.stringify(prev));
          let draggedNode = null;

          const removeDraggedNode = (nodes) => {
            for (let i = 0; i < nodes.length; i++) {
              if (nodes[i].id === draggedId) {
                draggedNode = nodes[i];
                nodes.splice(i, 1);
                return true;
              }
              if (nodes[i].children && removeDraggedNode(nodes[i].children)) {
                return true;
              }
            }
            return false;
          };

          removeDraggedNode(newTree);
          if (draggedNode) {
            // Добавляем узел, сохраняя root тип первым
            const rootNode = newTree.find(n => n.type === 'root');
            const otherNodes = newTree.filter(n => n.type !== 'root');

            if (rootNode) {
              return [rootNode, ...otherNodes, draggedNode];
            }
            newTree.push(draggedNode);
          }
          return newTree;
        });
        return;
      }

      setTreeNodes(prev => {
        const newTree = JSON.parse(JSON.stringify(prev));

        let draggedNode = null;
        let draggedParent = null;

        const removeDraggedNode = (nodes, parent = null) => {
          for (let i = 0; i < nodes.length; i++) {
            if (nodes[i].id === draggedId) {
              draggedNode = nodes[i];
              draggedParent = parent;
              nodes.splice(i, 1);
              return true;
            }
            if (nodes[i].children && removeDraggedNode(nodes[i].children, nodes[i])) {
              return true;
            }
          }
          return false;
        };

        removeDraggedNode(newTree);

        if (!draggedNode) return prev;

        const addToTarget = (nodes) => {
          for (const node of nodes) {
            if (node.id === targetId) {
              if (!node.children) node.children = [];
              node.children.push(draggedNode);
              return true;
            }
            if (node.children && addToTarget(node.children)) {
              return true;
            }
          }
          return false;
        };

        if (!addToTarget(newTree)) {
          if (draggedParent) {
            draggedParent.children.push(draggedNode);
          } else {
            newTree.push(draggedNode);
          }
          return prev;
        }

        return newTree;
      });
    } catch (error) {
      console.error('Ошибка перемещения узла:', error);
    }
  };

  const addAgent = async (parentId, agentData) => {
    try {
      // Создаем только данные агента, БЕЗ узла дерева
      // Агенты хранятся в таблице agents с node_id, указывающим на родительскую папку
      if (!agentData.host_id) {
        throw new Error('Не выбран Host для агента');
      }

      await agentAPI.create({
        node_id: parentId,
        host_id: agentData.host_id,
        name: agentData.name,
        type: agentData.type || 'equipment',
        profession: agentData.profession,
        okpd2_code: agentData.okpd2_code,
        quantity: agentData.quantity,
        queue_number: agentData.queueNumber,
        parameters: agentData.parameters
      });

      // Перезагружаем дерево из БД, чтобы отобразить изменения
      await loadTreeFromDB();

      return true;
    } catch (error) {
      console.error('Ошибка создания агента:', error);
      throw error;
    }
  };

  const deleteNode = async (nodeId) => {
    try {
      // Проверяем, является ли это агентом
      if (nodeId.startsWith('agent-')) {
        // Извлекаем числовой ID агента
        const agentId = nodeId.replace('agent-', '');
        await agentAPI.delete(agentId);
      } else {
        // Это узел дерева (папка)
        await treeAPI.delete(nodeId);
      }

      setTreeNodes(prev => {
        const newTree = JSON.parse(JSON.stringify(prev));

        const removeNode = (nodes) => {
          for (let i = 0; i < nodes.length; i++) {
            if (nodes[i].id === nodeId) {
              nodes.splice(i, 1);
              return true;
            }
            if (nodes[i].children && removeNode(nodes[i].children)) {
              return true;
            }
          }
          return false;
        };

        removeNode(newTree);
        return newTree;
      });
    } catch (error) {
      console.error('Ошибка удаления узла:', error);
    }
  };

  const deleteAgent = async (agentId) => {
    try {
      await agentAPI.delete(agentId);

      // Перезагружаем дерево из БД, чтобы отобразить изменения
      await loadTreeFromDB();

      return true;
    } catch (error) {
      console.error('Ошибка удаления агента:', error);
      throw error;
    }
  };

  const updateAgent = async (agentId, agentData) => {
    try {
      await agentAPI.update(agentId, {
        name: agentData.name,
        okpd2_code: agentData.okpd2_code,
        quantity: agentData.quantity,
        queue_number: agentData.queueNumber,
        parameters: agentData.parameters
      });

      // Перезагружаем дерево из БД, чтобы отобразить изменения
      await loadTreeFromDB();

      return true;
    } catch (error) {
      console.error('Ошибка обновления агента:', error);
      throw error;
    }
  };

  return (
    <div className="app">
      {!currentUser ? (
        <Routes>
          <Route path="/login" element={<LoginPage onLogin={handleLogin} />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      ) : (
        <>
          <Sidebar
            treeNodes={treeNodes}
            expandedNodes={expandedNodes}
            navigate={navigate}
            onAddNode={addNodeToTree}
            onToggleNode={toggleNode}
            onMoveNode={moveNode}
            onDeleteNode={deleteNode}
            isEditMode={isEditMode}
            onToggleEditMode={toggleEditMode}
            currentUser={currentUser}
          />
          <div className="main-content">
            <Header currentUser={currentUser} onLogout={handleLogout} />
            <div className="content-body">
              <Routes>
                <Route path="/" element={<MainPage treeNodes={treeNodes} onReloadTree={loadTreeFromDB} onAddNode={addNodeToTree} onToggleEditMode={toggleEditMode} />} />
                <Route
                  path="/branch/:branchId"
                  element={
                    <BranchPage
                      onToggleEditMode={toggleEditMode}
                      onAddAgent={addAgent}
                      onDeleteNode={deleteNode}
                      treeNodes={treeNodes}
                    />
                  }
                />
                <Route
                  path="/agent/:agentId"
                  element={
                    <AgentPage
                      treeNodes={treeNodes}
                      onUpdateAgent={updateAgent}
                      onDeleteAgent={deleteAgent}
                      onAddAgent={addAgent}
                      onReloadTree={loadTreeFromDB}
                    />
                  }
                />
                {currentUser.role === 'admin' && (
                  <Route path="/users" element={<UsersPage />} />
                )}
                <Route path="/hosts" element={<HostsPage />} />
                <Route path="/workflow-templates" element={<WorkflowTemplateEditor />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function App() {
  return (
    <Router>
      <AppContent />
    </Router>
  );
}

export default App;
