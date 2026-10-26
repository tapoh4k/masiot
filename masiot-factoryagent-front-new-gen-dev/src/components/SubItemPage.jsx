import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import './SubItemPage.css';

function SubItemPage({ treeData, onAddSubItem, onDeleteSubItem }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const parentId = parseInt(id);

  const parentItem = treeData.find(item => item.id === parentId);

  if (!parentItem) {
    return <div className="subitem-page">Элемент не найден</div>;
  }

  const handleCreateBranch = () => {
    onAddSubItem(parentId);
  };

  const handleDeleteBranch = (subItemId) => {
    if (window.confirm('Вы уверены, что хотите удалить эту ветку?')) {
      onDeleteSubItem(parentId, subItemId);
    }
  };

  const handleSubItemClick = (subItemId) => {
    navigate(`/subsubitem/${parentId}/${subItemId}`);
  };

  return (
    <div className="subitem-page">
      <h1>{parentItem.name}</h1>

      <div className="subitem-actions">
        <button className="create-branch-btn" onClick={handleCreateBranch}>
          Создать ветку
        </button>
      </div>

      <div className="branches-list">
        <h3>Подпункты:</h3>
        {parentItem.subItems.length === 0 ? (
          <p className="no-branches">Нет созданных веток</p>
        ) : (
          <div className="branches-grid">
            {parentItem.subItems.map(subItem => (
              <div key={subItem.id} className="branch-card">
                <div className="branch-header">
                  <h4 onClick={() => handleSubItemClick(subItem.id)}>
                    {subItem.name}
                  </h4>
                  <button
                    className="delete-branch-btn"
                    onClick={() => handleDeleteBranch(subItem.id)}
                  >
                    ✕
                  </button>
                </div>
                <div className="branch-info">
                  <p>Агентов: {subItem.agents ? subItem.agents.length : 0}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default SubItemPage;
