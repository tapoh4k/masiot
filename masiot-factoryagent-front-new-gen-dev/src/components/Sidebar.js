import React, { useState } from 'react';
import './Sidebar.css';

function Sidebar({ treeData, navigate }) {
  const [expandedItems, setExpandedItems] = useState({});
  const [expandedSubItems, setExpandedSubItems] = useState({});

  const toggleItem = (itemId) => {
    setExpandedItems(prev => ({
      ...prev,
      [itemId]: !prev[itemId]
    }));
  };

  const toggleSubItem = (subItemId) => {
    setExpandedSubItems(prev => ({
      ...prev,
      [subItemId]: !prev[subItemId]
    }));
  };

  const handleMainClick = (item) => {
    if (item.isMain) {
      navigate('/');
    }
  };

  const handleSubItemClick = (parentId, subItemId) => {
    navigate(`/subsubitem/${parentId}/${subItemId}`);
  };

  return (
    <div className="sidebar">
      <div className="sidebar-header">
        <h2>Структура</h2>
      </div>
      <div className="tree">
        {treeData.map(item => (
          <div key={item.id} className="tree-item">
            <div className="tree-item-header">
              <span
                className="tree-item-toggle"
                onClick={() => toggleItem(item.id)}
              >
                {item.subItems.length > 0 && (expandedItems[item.id] ? '▼' : '▶')}
              </span>
              <span
                className="tree-item-name"
                onClick={() => handleMainClick(item)}
              >
                {item.name}
              </span>
            </div>

            {expandedItems[item.id] && item.subItems.length > 0 && (
              <div className="tree-subitems">
                {item.subItems.map(subItem => (
                  <div key={subItem.id} className="tree-subitem">
                    <div className="tree-subitem-header">
                      <span
                        className="tree-subitem-toggle"
                        onClick={() => toggleSubItem(subItem.id)}
                      >
                        {subItem.agents && subItem.agents.length > 0 && (expandedSubItems[subItem.id] ? '▼' : '▶')}
                      </span>
                      <span
                        className="tree-subitem-name"
                        onClick={() => handleSubItemClick(item.id, subItem)}
                      >
                        {subItem.name}
                      </span>
                    </div>

                    {expandedSubItems[subItem.id] && subItem.agents && subItem.agents.length > 0 && (
                      <div className="tree-agents">
                        {subItem.agents.map(agent => (
                          <div key={agent.id} className="tree-agent">
                            <span className="agent-icon">👤</span>
                            <span className="agent-name">{agent.name || 'Агент'}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

export default Sidebar;
