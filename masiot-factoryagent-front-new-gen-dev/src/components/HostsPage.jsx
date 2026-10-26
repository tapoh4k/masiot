import React, { useEffect, useState } from 'react';
import { hostAPI } from '../services/api.js';
import './HostsPage.css';

const TYPE_LABELS = {
  cluster: 'Cluster',
  single: 'Single',
  specific: 'Specific'
};

function HostsPage() {
  const [hosts, setHosts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({
    name: '',
    type: 'cluster',
    max_agents: 100,
    deploy_target: 'docker',
    status: 'offline'
  });

  const load = async () => {
    try {
      const data = await hostAPI.getAll();
      setHosts(data);
    } catch (e) {
      console.error(e);
      alert(e.message || 'Ошибка загрузки хостов');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ name: '', type: 'cluster', max_agents: 100, deploy_target: 'docker', status: 'offline' });
    setShowModal(true);
  };

  const openEdit = (host) => {
    setEditing(host);
    setForm({
      name: host.name,
      type: host.type,
      max_agents: host.max_agents,
      deploy_target: host.deploy_target,
      status: host.status
    });
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editing) {
        await hostAPI.update(editing.id, {
          name: form.name,
          status: form.status,
          max_agents: form.type === 'single' ? 1 : Number(form.max_agents),
          deploy_target: form.deploy_target
        });
      } else {
        await hostAPI.create({
          name: form.name,
          type: form.type,
          max_agents: form.type === 'single' ? 1 : Number(form.max_agents),
          deploy_target: form.deploy_target
        });
      }
      setShowModal(false);
      await load();
    } catch (err) {
      alert(err.message || 'Ошибка сохранения');
    }
  };

  const handleDelete = async (host) => {
    if (!window.confirm(`Удалить Host «${host.name}»?`)) return;
    try {
      await hostAPI.delete(host.id);
      await load();
    } catch (err) {
      alert(err.message || 'Ошибка удаления');
    }
  };

  if (loading) {
    return <div className="hosts-page"><p>Загрузка…</p></div>;
  }

  return (
    <div className="hosts-page">
      <div className="hosts-header">
        <h1>Hosts</h1>
        <button type="button" className="btn-create" onClick={openCreate}>+ Создать Host</button>
      </div>

      <div className="hosts-grid">
        {hosts.map((host) => (
          <article key={host.id} className="host-card">
            <div className="host-card-top">
              <h2>{host.name}</h2>
              <span className={`host-status host-status-${host.status}`}>{host.status}</span>
            </div>
            <div className="host-meta">
              <span className="host-type">{TYPE_LABELS[host.type] || host.type}</span>
              <span>{host.agents_count} / {host.max_agents} агентов</span>
            </div>
            <p className="host-routing">{host.routing_key}</p>
            <p className="host-deploy">deploy: {host.deploy_target}</p>
            <div className="host-actions">
              <button type="button" onClick={() => openEdit(host)}>Изменить</button>
              <button type="button" className="danger" onClick={() => handleDelete(host)}>Удалить</button>
            </div>
          </article>
        ))}
        {hosts.length === 0 && (
          <p className="hosts-empty">Хостов пока нет. Создайте Default Cluster или новый Host.</p>
        )}
      </div>

      {showModal && (
        <div className="hosts-modal-overlay" onClick={() => setShowModal(false)}>
          <div className="hosts-modal" onClick={(e) => e.stopPropagation()}>
            <h3>{editing ? 'Редактировать Host' : 'Новый Host'}</h3>
            <form onSubmit={handleSubmit}>
              <label>
                Имя
                <input
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </label>
              {!editing && (
                <label>
                  Тип
                  <select
                    value={form.type}
                    onChange={(e) => {
                      const type = e.target.value;
                      setForm({
                        ...form,
                        type,
                        max_agents: type === 'single' ? 1 : form.max_agents,
                        deploy_target: type === 'specific' ? 'custom' : form.deploy_target
                      });
                    }}
                  >
                    <option value="cluster">Cluster (N агентов, Docker)</option>
                    <option value="single">Single (1 агент)</option>
                    <option value="specific">Specific (ЧПУ / кастом)</option>
                  </select>
                </label>
              )}
              {form.type !== 'single' && (
                <label>
                  Макс. агентов
                  <input
                    type="number"
                    min={1}
                    value={form.max_agents}
                    onChange={(e) => setForm({ ...form, max_agents: e.target.value })}
                  />
                </label>
              )}
              <label>
                Deploy target
                <select
                  value={form.deploy_target}
                  onChange={(e) => setForm({ ...form, deploy_target: e.target.value })}
                >
                  <option value="docker">docker</option>
                  <option value="machine">machine</option>
                  <option value="custom">custom</option>
                </select>
              </label>
              {editing && (
                <label>
                  Статус
                  <select
                    value={form.status}
                    onChange={(e) => setForm({ ...form, status: e.target.value })}
                  >
                    <option value="offline">offline</option>
                    <option value="online">online</option>
                    <option value="unknown">unknown</option>
                  </select>
                </label>
              )}
              <div className="hosts-modal-actions">
                <button type="button" onClick={() => setShowModal(false)}>Отмена</button>
                <button type="submit" className="btn-create">Сохранить</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default HostsPage;
