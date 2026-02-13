const { useState, useEffect } = React;

// Toggle Switch Component
const ToggleSwitch = ({ checked, onChange }) => (
  <label className="toggle-switch" onClick={e => e.stopPropagation()}>
    <input type="checkbox" checked={checked} onChange={onChange} />
    <span className="toggle-slider" />
  </label>
);

// Feature Section Component - Collapsible with toggle
const FeatureSection = ({ title, enabled, onToggle, children, description }) => (
  <div className="feature-section">
    <div className="feature-header" onClick={onToggle}>
      <div className="feature-title">
        <span>{title}</span>
        {description && <span className="feature-desc">{description}</span>}
      </div>
      <ToggleSwitch checked={enabled} onChange={onToggle} />
    </div>
    {enabled && <div className="feature-content">{children}</div>}
  </div>
);

// Key-Value Editor Component
const KeyValueEditor = ({ items, onChange, keyPlaceholder = "Key", valuePlaceholder = "Value" }) => {
  const addItem = () => onChange([...items, { key: '', value: '' }]);
  const removeItem = (index) => onChange(items.filter((_, i) => i !== index));
  const updateItem = (index, field, value) => {
    const newItems = [...items];
    newItems[index] = { ...newItems[index], [field]: value };
    onChange(newItems);
  };

  return (
    <div className="key-value-list">
      {items.map((item, index) => (
        <div key={index} className="key-value-row">
          <input
            className="form-input"
            type="text"
            value={item.key}
            onChange={e => updateItem(index, 'key', e.target.value)}
            placeholder={keyPlaceholder}
            style={{ flex: '0.4' }}
          />
          <input
            className="form-input"
            type="text"
            value={item.value}
            onChange={e => updateItem(index, 'value', e.target.value)}
            placeholder={valuePlaceholder}
          />
          <button type="button" className="btn btn-danger btn-sm" onClick={() => removeItem(index)}>
            x
          </button>
        </div>
      ))}
      <button type="button" className="btn btn-secondary btn-sm" onClick={addItem}>
        + Add Entry
      </button>
    </div>
  );
};

// YARP Admin Dashboard
const YarpAdminDashboard = () => {
  const [routes, setRoutes] = useState([]);
  const [clusters, setClusters] = useState([]);
  const [activeTab, setActiveTab] = useState('routes');
  const [loading, setLoading] = useState(true);
  const [editingRoute, setEditingRoute] = useState(null);
  const [editingCluster, setEditingCluster] = useState(null);
  const [notification, setNotification] = useState(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [routesRes, clustersRes] = await Promise.all([
        fetch('/api/yarp-admin/routes'),
        fetch('/api/yarp-admin/clusters')
      ]);
      setRoutes(await routesRes.json());
      setClusters(await clustersRes.json());
    } catch (err) {
      showNotification('Failed to fetch configuration', 'error');
    }
    setLoading(false);
  };

  const showNotification = (message, type = 'success') => {
    setNotification({ message, type });
    setTimeout(() => setNotification(null), 3000);
  };

  const handleSaveRoute = async (route) => {
    try {
      const method = routes.find(r => r.routeId === route.routeId) ? 'PUT' : 'POST';
      const res = await fetch(`/api/yarp-admin/routes${method === 'PUT' ? `/${route.routeId}` : ''}`, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(route)
      });
      if (res.ok) {
        showNotification('Route saved successfully');
        fetchData();
        setEditingRoute(null);
      }
    } catch (err) {
      showNotification('Failed to save route', 'error');
    }
  };

  const handleDeleteRoute = async (routeId) => {
    if (!confirm('Are you sure you want to delete this route?')) return;
    try {
      await fetch(`/api/yarp-admin/routes/${routeId}`, { method: 'DELETE' });
      showNotification('Route deleted');
      fetchData();
    } catch (err) {
      showNotification('Failed to delete route', 'error');
    }
  };

  const handleSaveCluster = async (cluster) => {
    try {
      const method = clusters.find(c => c.clusterId === cluster.clusterId) ? 'PUT' : 'POST';
      const res = await fetch(`/api/yarp-admin/clusters${method === 'PUT' ? `/${cluster.clusterId}` : ''}`, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(cluster)
      });
      if (res.ok) {
        showNotification('Cluster saved successfully');
        fetchData();
        setEditingCluster(null);
      }
    } catch (err) {
      showNotification('Failed to save cluster', 'error');
    }
  };

  const handleDeleteCluster = async (clusterId) => {
    if (!confirm('Are you sure you want to delete this cluster?')) return;
    try {
      await fetch(`/api/yarp-admin/clusters/${clusterId}`, { method: 'DELETE' });
      showNotification('Cluster deleted');
      fetchData();
    } catch (err) {
      showNotification('Failed to delete cluster', 'error');
    }
  };

  const handleApplyConfig = async () => {
    try {
      const res = await fetch('/api/yarp-admin/apply', { method: 'POST' });
      if (res.ok) {
        showNotification('Configuration applied successfully');
      }
    } catch (err) {
      showNotification('Failed to apply configuration', 'error');
    }
  };

  return (
    <div className="yarp-admin">
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&family=Outfit:wght@300;400;500;600;700&display=swap');
        
        * { box-sizing: border-box; margin: 0; padding: 0; }
        
        .yarp-admin {
          font-family: 'Outfit', sans-serif;
          min-height: 100vh;
          background: linear-gradient(135deg, #0a0a0f 0%, #1a1a2e 50%, #0f0f1a 100%);
          color: #e4e4e7;
          position: relative;
          overflow-x: hidden;
        }
        
        .yarp-admin::before {
          content: '';
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: 
            radial-gradient(ellipse 800px 600px at 20% 20%, rgba(59, 130, 246, 0.08) 0%, transparent 50%),
            radial-gradient(ellipse 600px 400px at 80% 80%, rgba(168, 85, 247, 0.06) 0%, transparent 50%);
          pointer-events: none;
          z-index: 0;
        }
        
        .grid-overlay {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background-image: 
            linear-gradient(rgba(255,255,255,0.02) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255,255,255,0.02) 1px, transparent 1px);
          background-size: 60px 60px;
          pointer-events: none;
          z-index: 0;
        }
        
        .container {
          position: relative;
          z-index: 1;
          max-width: 1400px;
          margin: 0 auto;
          padding: 2rem;
        }
        
        .header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 3rem;
          padding-bottom: 2rem;
          border-bottom: 1px solid rgba(255,255,255,0.06);
        }
        
        .logo-section {
          display: flex;
          align-items: center;
          gap: 1rem;
        }
        
        .logo {
          width: 56px;
          height: 56px;
          background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
          border-radius: 16px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-family: 'JetBrains Mono', monospace;
          font-weight: 700;
          font-size: 1.25rem;
          color: white;
          box-shadow: 0 8px 32px rgba(99, 102, 241, 0.3);
          position: relative;
          overflow: hidden;
        }
        
        .logo::after {
          content: '';
          position: absolute;
          top: -50%;
          left: -50%;
          width: 200%;
          height: 200%;
          background: linear-gradient(45deg, transparent, rgba(255,255,255,0.1), transparent);
          transform: rotate(45deg);
          animation: shimmer 3s infinite;
        }
        
        @keyframes shimmer {
          0% { transform: translateX(-100%) rotate(45deg); }
          100% { transform: translateX(100%) rotate(45deg); }
        }
        
        .title-group h1 {
          font-size: 1.75rem;
          font-weight: 600;
          letter-spacing: -0.02em;
          background: linear-gradient(135deg, #fff 0%, #a1a1aa 100%);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }
        
        .title-group p {
          font-size: 0.875rem;
          color: #71717a;
          margin-top: 0.25rem;
        }
        
        .header-actions {
          display: flex;
          gap: 0.75rem;
        }
        
        .btn {
          font-family: 'Outfit', sans-serif;
          font-size: 0.875rem;
          font-weight: 500;
          padding: 0.75rem 1.25rem;
          border-radius: 10px;
          border: none;
          cursor: pointer;
          transition: all 0.2s ease;
          display: inline-flex;
          align-items: center;
          gap: 0.5rem;
        }
        
        .btn-primary {
          background: linear-gradient(135deg, #3b82f6 0%, #6366f1 100%);
          color: white;
          box-shadow: 0 4px 16px rgba(99, 102, 241, 0.3);
        }
        
        .btn-primary:hover {
          transform: translateY(-2px);
          box-shadow: 0 8px 24px rgba(99, 102, 241, 0.4);
        }
        
        .btn-secondary {
          background: rgba(255,255,255,0.05);
          color: #e4e4e7;
          border: 1px solid rgba(255,255,255,0.1);
        }
        
        .btn-secondary:hover {
          background: rgba(255,255,255,0.1);
          border-color: rgba(255,255,255,0.2);
        }
        
        .btn-danger {
          background: rgba(239, 68, 68, 0.1);
          color: #f87171;
          border: 1px solid rgba(239, 68, 68, 0.2);
        }
        
        .btn-danger:hover {
          background: rgba(239, 68, 68, 0.2);
        }
        
        .btn-sm {
          padding: 0.5rem 0.875rem;
          font-size: 0.8125rem;
        }
        
        .tabs {
          display: flex;
          gap: 0.5rem;
          margin-bottom: 2rem;
          background: rgba(255,255,255,0.03);
          padding: 0.5rem;
          border-radius: 14px;
          width: fit-content;
        }
        
        .tab {
          font-family: 'Outfit', sans-serif;
          font-size: 0.9375rem;
          font-weight: 500;
          padding: 0.75rem 1.5rem;
          border-radius: 10px;
          border: none;
          background: transparent;
          color: #71717a;
          cursor: pointer;
          transition: all 0.2s ease;
        }
        
        .tab:hover {
          color: #a1a1aa;
        }
        
        .tab.active {
          background: rgba(255,255,255,0.08);
          color: #fff;
          box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        }
        
        .card {
          background: rgba(255,255,255,0.03);
          border: 1px solid rgba(255,255,255,0.06);
          border-radius: 20px;
          padding: 1.5rem;
          margin-bottom: 1rem;
          backdrop-filter: blur(10px);
          transition: all 0.3s ease;
        }
        
        .card:hover {
          border-color: rgba(255,255,255,0.1);
          background: rgba(255,255,255,0.04);
        }
        
        .card-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 1rem;
        }
        
        .card-title {
          font-family: 'JetBrains Mono', monospace;
          font-size: 1rem;
          font-weight: 600;
          color: #fff;
          display: flex;
          align-items: center;
          gap: 0.75rem;
        }
        
        .status-badge {
          font-size: 0.6875rem;
          font-weight: 500;
          padding: 0.25rem 0.625rem;
          border-radius: 6px;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }
        
        .status-active {
          background: rgba(34, 197, 94, 0.15);
          color: #4ade80;
        }
        
        .status-inactive {
          background: rgba(251, 146, 60, 0.15);
          color: #fb923c;
        }
        
        .card-meta {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 1rem;
          margin-top: 1rem;
          padding-top: 1rem;
          border-top: 1px solid rgba(255,255,255,0.06);
        }
        
        .meta-item {
          display: flex;
          flex-direction: column;
          gap: 0.25rem;
        }
        
        .meta-label {
          font-size: 0.75rem;
          color: #71717a;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }
        
        .meta-value {
          font-family: 'JetBrains Mono', monospace;
          font-size: 0.875rem;
          color: #a1a1aa;
          word-break: break-all;
        }
        
        .card-actions {
          display: flex;
          gap: 0.5rem;
        }
        
        .empty-state {
          text-align: center;
          padding: 4rem 2rem;
          color: #52525b;
        }
        
        .empty-state h3 {
          font-size: 1.125rem;
          margin-bottom: 0.5rem;
          color: #71717a;
        }
        
        .modal-overlay {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: rgba(0,0,0,0.7);
          backdrop-filter: blur(4px);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 100;
          animation: fadeIn 0.2s ease;
        }
        
        @keyframes fadeIn {
          from { opacity: 0; }
          to { opacity: 1; }
        }
        
        .modal {
          background: #1a1a2e;
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 24px;
          padding: 2rem;
          width: 90%;
          max-width: 600px;
          max-height: 90vh;
          overflow-y: auto;
          animation: slideUp 0.3s ease;
        }
        
        @keyframes slideUp {
          from { transform: translateY(20px); opacity: 0; }
          to { transform: translateY(0); opacity: 1; }
        }
        
        .modal h2 {
          font-size: 1.25rem;
          font-weight: 600;
          margin-bottom: 1.5rem;
          color: #fff;
        }
        
        .form-group {
          margin-bottom: 1.25rem;
        }
        
        .form-label {
          display: block;
          font-size: 0.8125rem;
          font-weight: 500;
          color: #a1a1aa;
          margin-bottom: 0.5rem;
        }
        
        .form-input {
          width: 100%;
          padding: 0.75rem 1rem;
          font-family: 'JetBrains Mono', monospace;
          font-size: 0.875rem;
          background: rgba(255,255,255,0.05);
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 10px;
          color: #e4e4e7;
          transition: all 0.2s ease;
        }
        
        .form-input:focus {
          outline: none;
          border-color: #6366f1;
          box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
        }
        
        .form-input::placeholder {
          color: #52525b;
        }
        
        .form-actions {
          display: flex;
          gap: 0.75rem;
          justify-content: flex-end;
          margin-top: 2rem;
          padding-top: 1.5rem;
          border-top: 1px solid rgba(255,255,255,0.06);
        }
        
        .destinations-list {
          display: flex;
          flex-direction: column;
          gap: 0.75rem;
          margin-top: 0.5rem;
        }
        
        .destination-row {
          display: flex;
          gap: 0.5rem;
          align-items: center;
        }
        
        .destination-row .form-input {
          flex: 1;
        }
        
        .notification {
          position: fixed;
          bottom: 2rem;
          right: 2rem;
          padding: 1rem 1.5rem;
          border-radius: 12px;
          font-size: 0.875rem;
          font-weight: 500;
          z-index: 200;
          animation: slideIn 0.3s ease;
        }
        
        @keyframes slideIn {
          from { transform: translateX(100%); opacity: 0; }
          to { transform: translateX(0); opacity: 1; }
        }
        
        .notification.success {
          background: rgba(34, 197, 94, 0.15);
          border: 1px solid rgba(34, 197, 94, 0.3);
          color: #4ade80;
        }
        
        .notification.error {
          background: rgba(239, 68, 68, 0.15);
          border: 1px solid rgba(239, 68, 68, 0.3);
          color: #f87171;
        }
        
        .loading {
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 4rem;
        }
        
        .spinner {
          width: 40px;
          height: 40px;
          border: 3px solid rgba(255,255,255,0.1);
          border-top-color: #6366f1;
          border-radius: 50%;
          animation: spin 1s linear infinite;
        }
        
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
        
        .icon {
          width: 18px;
          height: 18px;
        }

        .toggle-switch {
          position: relative;
          display: inline-block;
          width: 44px;
          height: 24px;
          flex-shrink: 0;
        }

        .toggle-switch input {
          opacity: 0;
          width: 0;
          height: 0;
        }

        .toggle-slider {
          position: absolute;
          cursor: pointer;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background: rgba(255,255,255,0.1);
          border-radius: 24px;
          transition: all 0.3s ease;
        }

        .toggle-slider::before {
          position: absolute;
          content: '';
          height: 18px;
          width: 18px;
          left: 3px;
          bottom: 3px;
          background: #71717a;
          border-radius: 50%;
          transition: all 0.3s ease;
        }

        .toggle-switch input:checked + .toggle-slider {
          background: linear-gradient(135deg, #3b82f6 0%, #6366f1 100%);
        }

        .toggle-switch input:checked + .toggle-slider::before {
          transform: translateX(20px);
          background: white;
        }

        .feature-section {
          background: rgba(255,255,255,0.02);
          border: 1px solid rgba(255,255,255,0.06);
          border-radius: 12px;
          margin-bottom: 1rem;
          overflow: hidden;
        }

        .feature-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 0.875rem 1rem;
          cursor: pointer;
          transition: background 0.2s ease;
        }

        .feature-header:hover {
          background: rgba(255,255,255,0.02);
        }

        .feature-title {
          display: flex;
          flex-direction: column;
          gap: 0.125rem;
        }

        .feature-title > span:first-child {
          font-size: 0.875rem;
          font-weight: 500;
          color: #e4e4e7;
        }

        .feature-desc {
          font-size: 0.75rem;
          color: #71717a;
        }

        .feature-content {
          padding: 0 1rem 1rem 1rem;
          border-top: 1px solid rgba(255,255,255,0.06);
          animation: expandIn 0.2s ease;
        }

        @keyframes expandIn {
          from { opacity: 0; transform: translateY(-8px); }
          to { opacity: 1; transform: translateY(0); }
        }

        .key-value-list {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;
          margin-top: 0.75rem;
        }

        .key-value-row {
          display: flex;
          gap: 0.5rem;
          align-items: center;
        }

        .key-value-row .form-input {
          flex: 1;
        }

        .form-row {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 1rem;
        }

        .form-row-3 {
          display: grid;
          grid-template-columns: 1fr 1fr 1fr;
          gap: 1rem;
        }

        .warning-text {
          font-size: 0.75rem;
          color: #fb923c;
          margin-top: 0.25rem;
        }

        .inline-toggle {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 0.75rem 0;
        }

        .inline-toggle-label {
          font-size: 0.8125rem;
          color: #a1a1aa;
        }

        .section-divider {
          height: 1px;
          background: rgba(255,255,255,0.06);
          margin: 1rem 0;
        }

        .sub-section {
          padding: 0.75rem;
          background: rgba(255,255,255,0.02);
          border-radius: 8px;
          margin-top: 0.75rem;
        }

        .sub-section-title {
          font-size: 0.8125rem;
          font-weight: 500;
          color: #a1a1aa;
          margin-bottom: 0.75rem;
          display: flex;
          align-items: center;
          justify-content: space-between;
        }
      `}</style>

      <div className="grid-overlay" />
      
      <div className="container">
        <header className="header">
          <div className="logo-section">
            <div className="logo">YA</div>
            <div className="title-group">
              <h1>YARP Admin</h1>
              <p>Reverse Proxy Configuration Manager</p>
            </div>
          </div>
          <div className="header-actions">
            <button className="btn btn-secondary" onClick={fetchData}>
              <svg className="icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              Refresh
            </button>
            <button className="btn btn-primary" onClick={handleApplyConfig}>
              <svg className="icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              Apply Config
            </button>
          </div>
        </header>

        <div className="tabs">
          <button 
            className={`tab ${activeTab === 'routes' ? 'active' : ''}`}
            onClick={() => setActiveTab('routes')}
          >
            Routes ({routes.length})
          </button>
          <button 
            className={`tab ${activeTab === 'clusters' ? 'active' : ''}`}
            onClick={() => setActiveTab('clusters')}
          >
            Clusters ({clusters.length})
          </button>
        </div>

        {loading ? (
          <div className="loading">
            <div className="spinner" />
          </div>
        ) : (
          <>
            {activeTab === 'routes' && (
              <div>
                <div style={{ marginBottom: '1.5rem' }}>
                  <button 
                    className="btn btn-primary"
                    onClick={() => setEditingRoute({ routeId: '', clusterId: '', match: { path: '' } })}
                  >
                    <svg className="icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Add Route
                  </button>
                </div>
                
                {routes.length === 0 ? (
                  <div className="empty-state">
                    <h3>No routes configured</h3>
                    <p>Add your first route to get started</p>
                  </div>
                ) : (
                  routes.map(route => (
                    <div key={route.routeId} className="card">
                      <div className="card-header">
                        <div className="card-title">
                          {route.routeId}
                          <span className={`status-badge ${route.enabled !== false ? 'status-active' : 'status-inactive'}`}>
                            {route.enabled !== false ? 'Active' : 'Inactive'}
                          </span>
                        </div>
                        <div className="card-actions">
                          <button className="btn btn-secondary btn-sm" onClick={() => setEditingRoute(route)}>
                            Edit
                          </button>
                          <button className="btn btn-danger btn-sm" onClick={() => handleDeleteRoute(route.routeId)}>
                            Delete
                          </button>
                        </div>
                      </div>
                      <div className="card-meta">
                        <div className="meta-item">
                          <span className="meta-label">Path Pattern</span>
                          <span className="meta-value">{route.match?.path || 'N/A'}</span>
                        </div>
                        <div className="meta-item">
                          <span className="meta-label">Cluster</span>
                          <span className="meta-value">{route.clusterId}</span>
                        </div>
                        <div className="meta-item">
                          <span className="meta-label">Methods</span>
                          <span className="meta-value">{route.match?.methods?.join(', ') || 'All'}</span>
                        </div>
                        <div className="meta-item">
                          <span className="meta-label">Order</span>
                          <span className="meta-value">{route.order ?? 'Default'}</span>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}

            {activeTab === 'clusters' && (
              <div>
                <div style={{ marginBottom: '1.5rem' }}>
                  <button 
                    className="btn btn-primary"
                    onClick={() => setEditingCluster({ clusterId: '', destinations: {} })}
                  >
                    <svg className="icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Add Cluster
                  </button>
                </div>
                
                {clusters.length === 0 ? (
                  <div className="empty-state">
                    <h3>No clusters configured</h3>
                    <p>Add your first cluster to define backend destinations</p>
                  </div>
                ) : (
                  clusters.map(cluster => (
                    <div key={cluster.clusterId} className="card">
                      <div className="card-header">
                        <div className="card-title">
                          {cluster.clusterId}
                          <span className="status-badge status-active">
                            {Object.keys(cluster.destinations || {}).length} destinations
                          </span>
                        </div>
                        <div className="card-actions">
                          <button className="btn btn-secondary btn-sm" onClick={() => setEditingCluster(cluster)}>
                            Edit
                          </button>
                          <button className="btn btn-danger btn-sm" onClick={() => handleDeleteCluster(cluster.clusterId)}>
                            Delete
                          </button>
                        </div>
                      </div>
                      <div className="card-meta">
                        <div className="meta-item">
                          <span className="meta-label">Load Balancing Policy</span>
                          <span className="meta-value">{cluster.loadBalancingPolicy || 'RoundRobin'}</span>
                        </div>
                        <div className="meta-item">
                          <span className="meta-label">Destinations</span>
                          <span className="meta-value">
                            {Object.entries(cluster.destinations || {}).map(([key, dest]) => (
                              <div key={key}>{key}: {dest.address}</div>
                            ))}
                          </span>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </>
        )}
      </div>

      {editingRoute && (
        <RouteModal 
          route={editingRoute} 
          clusters={clusters}
          onSave={handleSaveRoute} 
          onClose={() => setEditingRoute(null)} 
        />
      )}

      {editingCluster && (
        <ClusterModal 
          cluster={editingCluster} 
          onSave={handleSaveCluster} 
          onClose={() => setEditingCluster(null)} 
        />
      )}

      {notification && (
        <div className={`notification ${notification.type}`}>
          {notification.message}
        </div>
      )}
    </div>
  );
};

// Route Editor Modal
const RouteModal = ({ route, clusters, onSave, onClose }) => {
  // Parse existing headers into UI format
  const parseHeaders = (headers) => {
    if (!headers || !Array.isArray(headers)) return [];
    return headers.map(h => ({
      name: h.name || '',
      values: h.values?.join(', ') || '',
      mode: h.mode || 'ExactHeader',
      isCaseSensitive: h.isCaseSensitive || false
    }));
  };

  // Parse existing query params into UI format
  const parseQueryParams = (params) => {
    if (!params || !Array.isArray(params)) return [];
    return params.map(p => ({
      name: p.name || '',
      values: p.values?.join(', ') || '',
      mode: p.mode || 'Exact',
      isCaseSensitive: p.isCaseSensitive || false
    }));
  };

  // Parse transforms into UI format
  const parseTransforms = (transforms) => {
    if (!transforms || !Array.isArray(transforms)) return [];
    return transforms.map(t => {
      const entries = Object.entries(t);
      return entries.length > 0 ? { key: entries[0][0], value: entries[0][1] } : { key: '', value: '' };
    });
  };

  // Parse metadata into UI format
  const parseMetadata = (metadata) => {
    if (!metadata) return [];
    return Object.entries(metadata).map(([key, value]) => ({ key, value }));
  };

  const [form, setForm] = useState({
    // Basic settings
    routeId: route.routeId || '',
    clusterId: route.clusterId || '',
    path: route.match?.path || '',
    methods: route.match?.methods?.join(', ') || '',
    order: route.order ?? '',
    enabled: route.enabled !== false,
    // Advanced matching
    hosts: route.match?.hosts?.join(', ') || '',
    headers: parseHeaders(route.match?.headers),
    queryParameters: parseQueryParams(route.match?.queryParameters),
    // Policies
    authorizationPolicy: route.authorizationPolicy || '',
    corsPolicy: route.corsPolicy || '',
    rateLimiterPolicy: route.rateLimiterPolicy || '',
    timeoutPolicy: route.timeoutPolicy || '',
    // Transforms
    transforms: parseTransforms(route.transforms),
    // Metadata
    metadata: parseMetadata(route.metadata)
  });

  // Feature toggles
  const [advancedMatchingEnabled, setAdvancedMatchingEnabled] = useState(
    !!(route.match?.hosts?.length || route.match?.headers?.length || route.match?.queryParameters?.length)
  );
  const [policiesEnabled, setPoliciesEnabled] = useState(
    !!(route.authorizationPolicy || route.corsPolicy || route.rateLimiterPolicy || route.timeoutPolicy)
  );
  const [transformsEnabled, setTransformsEnabled] = useState(!!(route.transforms?.length));
  const [metadataEnabled, setMetadataEnabled] = useState(!!(route.metadata && Object.keys(route.metadata).length));

  // Header management
  const addHeader = () => setForm({...form, headers: [...form.headers, { name: '', values: '', mode: 'ExactHeader', isCaseSensitive: false }]});
  const removeHeader = (index) => setForm({...form, headers: form.headers.filter((_, i) => i !== index)});
  const updateHeader = (index, field, value) => {
    const newHeaders = [...form.headers];
    newHeaders[index] = { ...newHeaders[index], [field]: value };
    setForm({...form, headers: newHeaders});
  };

  // Query param management
  const addQueryParam = () => setForm({...form, queryParameters: [...form.queryParameters, { name: '', values: '', mode: 'Exact', isCaseSensitive: false }]});
  const removeQueryParam = (index) => setForm({...form, queryParameters: form.queryParameters.filter((_, i) => i !== index)});
  const updateQueryParam = (index, field, value) => {
    const newParams = [...form.queryParameters];
    newParams[index] = { ...newParams[index], [field]: value };
    setForm({...form, queryParameters: newParams});
  };

  const handleSubmit = (e) => {
    e.preventDefault();

    // Build match object
    const match = {
      path: form.path,
      methods: form.methods ? form.methods.split(',').map(m => m.trim()).filter(m => m) : undefined
    };

    // Add advanced matching if enabled
    if (advancedMatchingEnabled) {
      if (form.hosts) {
        match.hosts = form.hosts.split(',').map(h => h.trim()).filter(h => h);
      }
      if (form.headers.length > 0) {
        match.headers = form.headers
          .filter(h => h.name)
          .map(h => ({
            name: h.name,
            values: h.values ? h.values.split(',').map(v => v.trim()).filter(v => v) : undefined,
            mode: h.mode,
            isCaseSensitive: h.isCaseSensitive
          }));
        if (match.headers.length === 0) delete match.headers;
      }
      if (form.queryParameters.length > 0) {
        match.queryParameters = form.queryParameters
          .filter(p => p.name)
          .map(p => ({
            name: p.name,
            values: p.values ? p.values.split(',').map(v => v.trim()).filter(v => v) : undefined,
            mode: p.mode,
            isCaseSensitive: p.isCaseSensitive
          }));
        if (match.queryParameters.length === 0) delete match.queryParameters;
      }
    }

    // Build route config
    const routeConfig = {
      routeId: form.routeId,
      clusterId: form.clusterId,
      order: form.order ? parseInt(form.order) : undefined,
      enabled: form.enabled,
      match
    };

    // Add policies if enabled
    if (policiesEnabled) {
      if (form.authorizationPolicy) routeConfig.authorizationPolicy = form.authorizationPolicy;
      if (form.corsPolicy) routeConfig.corsPolicy = form.corsPolicy;
      if (form.rateLimiterPolicy) routeConfig.rateLimiterPolicy = form.rateLimiterPolicy;
      if (form.timeoutPolicy) routeConfig.timeoutPolicy = form.timeoutPolicy;
    }

    // Add transforms if enabled
    if (transformsEnabled && form.transforms.length > 0) {
      routeConfig.transforms = form.transforms
        .filter(t => t.key)
        .map(t => ({ [t.key]: t.value }));
      if (routeConfig.transforms.length === 0) delete routeConfig.transforms;
    }

    // Add metadata if enabled
    if (metadataEnabled && form.metadata.length > 0) {
      routeConfig.metadata = {};
      form.metadata.forEach(m => {
        if (m.key) routeConfig.metadata[m.key] = m.value;
      });
      if (Object.keys(routeConfig.metadata).length === 0) delete routeConfig.metadata;
    }

    onSave(routeConfig);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()} style={{ maxWidth: '700px' }}>
        <h2>{route.routeId ? 'Edit Route' : 'Add Route'}</h2>
        <form onSubmit={handleSubmit}>
          {/* Basic Settings */}
          <div className="form-group">
            <label className="form-label">Route ID</label>
            <input
              className="form-input"
              type="text"
              value={form.routeId}
              onChange={e => setForm({...form, routeId: e.target.value})}
              placeholder="my-route"
              required
              disabled={!!route.routeId}
            />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Cluster ID</label>
              <select
                className="form-input"
                value={form.clusterId}
                onChange={e => setForm({...form, clusterId: e.target.value})}
                required
              >
                <option value="">Select a cluster...</option>
                {clusters.map(c => (
                  <option key={c.clusterId} value={c.clusterId}>{c.clusterId}</option>
                ))}
              </select>
            </div>
            <div className="form-group">
              <label className="form-label">Order (priority)</label>
              <input
                className="form-input"
                type="number"
                value={form.order}
                onChange={e => setForm({...form, order: e.target.value})}
                placeholder="0"
              />
            </div>
          </div>
          <div className="form-group">
            <label className="form-label">Path Pattern</label>
            <input
              className="form-input"
              type="text"
              value={form.path}
              onChange={e => setForm({...form, path: e.target.value})}
              placeholder="/api/{**catch-all}"
              required
            />
          </div>
          <div className="form-group">
            <label className="form-label">HTTP Methods (comma-separated, leave empty for all)</label>
            <input
              className="form-input"
              type="text"
              value={form.methods}
              onChange={e => setForm({...form, methods: e.target.value})}
              placeholder="GET, POST, PUT, DELETE"
            />
          </div>
          <div className="form-group">
            <div className="inline-toggle">
              <span className="inline-toggle-label">Route Enabled</span>
              <ToggleSwitch checked={form.enabled} onChange={e => setForm({...form, enabled: e.target.checked})} />
            </div>
          </div>

          <div className="section-divider" />

          {/* Advanced Matching */}
          <FeatureSection
            title="Advanced Matching"
            description="Match by hosts, headers, or query parameters"
            enabled={advancedMatchingEnabled}
            onToggle={() => setAdvancedMatchingEnabled(!advancedMatchingEnabled)}
          >
            <div className="form-group" style={{ marginTop: '0.75rem' }}>
              <label className="form-label">Hosts (comma-separated)</label>
              <input
                className="form-input"
                type="text"
                value={form.hosts}
                onChange={e => setForm({...form, hosts: e.target.value})}
                placeholder="example.com, api.example.com"
              />
            </div>

            <div className="sub-section">
              <div className="sub-section-title">
                <span>Headers</span>
                <button type="button" className="btn btn-secondary btn-sm" onClick={addHeader}>+ Add</button>
              </div>
              {form.headers.map((header, index) => (
                <div key={index} className="key-value-row" style={{ marginBottom: '0.5rem' }}>
                  <input
                    className="form-input"
                    type="text"
                    value={header.name}
                    onChange={e => updateHeader(index, 'name', e.target.value)}
                    placeholder="Header Name"
                    style={{ flex: '0.3' }}
                  />
                  <input
                    className="form-input"
                    type="text"
                    value={header.values}
                    onChange={e => updateHeader(index, 'values', e.target.value)}
                    placeholder="Values (comma-separated)"
                    style={{ flex: '0.4' }}
                  />
                  <select
                    className="form-input"
                    value={header.mode}
                    onChange={e => updateHeader(index, 'mode', e.target.value)}
                    style={{ flex: '0.25' }}
                  >
                    <option value="ExactHeader">Exact</option>
                    <option value="HeaderPrefix">Prefix</option>
                    <option value="Contains">Contains</option>
                    <option value="Exists">Exists</option>
                    <option value="NotExists">Not Exists</option>
                  </select>
                  <button type="button" className="btn btn-danger btn-sm" onClick={() => removeHeader(index)}>x</button>
                </div>
              ))}
            </div>

            <div className="sub-section">
              <div className="sub-section-title">
                <span>Query Parameters</span>
                <button type="button" className="btn btn-secondary btn-sm" onClick={addQueryParam}>+ Add</button>
              </div>
              {form.queryParameters.map((param, index) => (
                <div key={index} className="key-value-row" style={{ marginBottom: '0.5rem' }}>
                  <input
                    className="form-input"
                    type="text"
                    value={param.name}
                    onChange={e => updateQueryParam(index, 'name', e.target.value)}
                    placeholder="Param Name"
                    style={{ flex: '0.3' }}
                  />
                  <input
                    className="form-input"
                    type="text"
                    value={param.values}
                    onChange={e => updateQueryParam(index, 'values', e.target.value)}
                    placeholder="Values (comma-separated)"
                    style={{ flex: '0.4' }}
                  />
                  <select
                    className="form-input"
                    value={param.mode}
                    onChange={e => updateQueryParam(index, 'mode', e.target.value)}
                    style={{ flex: '0.25' }}
                  >
                    <option value="Exact">Exact</option>
                    <option value="Prefix">Prefix</option>
                    <option value="Contains">Contains</option>
                    <option value="Exists">Exists</option>
                    <option value="NotExists">Not Exists</option>
                  </select>
                  <button type="button" className="btn btn-danger btn-sm" onClick={() => removeQueryParam(index)}>x</button>
                </div>
              ))}
            </div>
          </FeatureSection>

          {/* Policies */}
          <FeatureSection
            title="Policies"
            description="Authorization, CORS, Rate Limiting, Timeout"
            enabled={policiesEnabled}
            onToggle={() => setPoliciesEnabled(!policiesEnabled)}
          >
            <div className="form-row" style={{ marginTop: '0.75rem' }}>
              <div className="form-group">
                <label className="form-label">Authorization Policy</label>
                <input
                  className="form-input"
                  type="text"
                  value={form.authorizationPolicy}
                  onChange={e => setForm({...form, authorizationPolicy: e.target.value})}
                  placeholder="policy-name"
                />
              </div>
              <div className="form-group">
                <label className="form-label">CORS Policy</label>
                <input
                  className="form-input"
                  type="text"
                  value={form.corsPolicy}
                  onChange={e => setForm({...form, corsPolicy: e.target.value})}
                  placeholder="cors-policy"
                />
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Rate Limiter Policy</label>
                <input
                  className="form-input"
                  type="text"
                  value={form.rateLimiterPolicy}
                  onChange={e => setForm({...form, rateLimiterPolicy: e.target.value})}
                  placeholder="rate-limit-policy"
                />
              </div>
              <div className="form-group">
                <label className="form-label">Timeout Policy</label>
                <input
                  className="form-input"
                  type="text"
                  value={form.timeoutPolicy}
                  onChange={e => setForm({...form, timeoutPolicy: e.target.value})}
                  placeholder="timeout-policy"
                />
              </div>
            </div>
          </FeatureSection>

          {/* Transforms */}
          <FeatureSection
            title="Request Transforms"
            description="Modify requests before forwarding"
            enabled={transformsEnabled}
            onToggle={() => setTransformsEnabled(!transformsEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              <KeyValueEditor
                items={form.transforms}
                onChange={(transforms) => setForm({...form, transforms})}
                keyPlaceholder="Transform Type (e.g., PathPrefix)"
                valuePlaceholder="Value (e.g., /api)"
              />
            </div>
          </FeatureSection>

          {/* Metadata */}
          <FeatureSection
            title="Metadata"
            description="Custom key-value pairs"
            enabled={metadataEnabled}
            onToggle={() => setMetadataEnabled(!metadataEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              <KeyValueEditor
                items={form.metadata}
                onChange={(metadata) => setForm({...form, metadata})}
                keyPlaceholder="Key"
                valuePlaceholder="Value"
              />
            </div>
          </FeatureSection>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary">Save Route</button>
          </div>
        </form>
      </div>
    </div>
  );
};

// Cluster Editor Modal
const ClusterModal = ({ cluster, onSave, onClose }) => {
  // Parse metadata into UI format
  const parseMetadata = (metadata) => {
    if (!metadata) return [];
    return Object.entries(metadata).map(([key, value]) => ({ key, value }));
  };

  const [form, setForm] = useState({
    // Basic settings
    clusterId: cluster.clusterId || '',
    loadBalancingPolicy: cluster.loadBalancingPolicy || 'RoundRobin',
    destinations: Object.entries(cluster.destinations || {}).map(([key, dest]) => ({
      id: key,
      address: dest.address,
      health: dest.health || ''
    })),
    // Session Affinity
    sessionAffinityEnabled: cluster.sessionAffinity?.enabled || false,
    sessionAffinityPolicy: cluster.sessionAffinity?.policy || 'Cookie',
    sessionAffinityFailurePolicy: cluster.sessionAffinity?.failurePolicy || 'Redistribute',
    sessionAffinityKeyName: cluster.sessionAffinity?.affinityKeyName || '.Yarp.Affinity',
    // Passive Health Check
    passiveHealthEnabled: cluster.healthCheck?.passive?.enabled || false,
    passiveHealthPolicy: cluster.healthCheck?.passive?.policy || 'TransportFailureRate',
    passiveHealthReactivationPeriod: cluster.healthCheck?.passive?.reactivationPeriod || '00:02:00',
    // Active Health Check
    activeHealthEnabled: cluster.healthCheck?.active?.enabled || false,
    activeHealthInterval: cluster.healthCheck?.active?.interval || '00:00:15',
    activeHealthTimeout: cluster.healthCheck?.active?.timeout || '00:00:10',
    activeHealthPolicy: cluster.healthCheck?.active?.policy || 'ConsecutiveFailures',
    activeHealthPath: cluster.healthCheck?.active?.path || '/health',
    availableDestinationsPolicy: cluster.healthCheck?.availableDestinationsPolicy || '',
    // HTTP Client
    maxConnectionsPerServer: cluster.httpClient?.maxConnectionsPerServer || '',
    enableMultipleHttp2Connections: cluster.httpClient?.enableMultipleHttp2Connections || false,
    dangerousAcceptAnyServerCertificate: cluster.httpClient?.dangerousAcceptAnyServerCertificate || false,
    // HTTP Request
    activityTimeout: cluster.httpRequest?.activityTimeout || '00:01:40',
    httpVersion: cluster.httpRequest?.version || '',
    versionPolicy: cluster.httpRequest?.versionPolicy || '',
    allowResponseBuffering: cluster.httpRequest?.allowResponseBuffering || false,
    // Metadata
    metadata: parseMetadata(cluster.metadata)
  });

  // Feature toggles
  const [sessionAffinityEnabled, setSessionAffinityEnabled] = useState(cluster.sessionAffinity?.enabled || false);
  const [healthCheckEnabled, setHealthCheckEnabled] = useState(
    !!(cluster.healthCheck?.passive?.enabled || cluster.healthCheck?.active?.enabled)
  );
  const [httpClientEnabled, setHttpClientEnabled] = useState(
    !!(cluster.httpClient?.maxConnectionsPerServer || cluster.httpClient?.enableMultipleHttp2Connections || cluster.httpClient?.dangerousAcceptAnyServerCertificate)
  );
  const [httpRequestEnabled, setHttpRequestEnabled] = useState(
    !!(cluster.httpRequest?.activityTimeout || cluster.httpRequest?.version || cluster.httpRequest?.versionPolicy)
  );
  const [metadataEnabled, setMetadataEnabled] = useState(!!(cluster.metadata && Object.keys(cluster.metadata).length));

  const addDestination = () => {
    setForm({
      ...form,
      destinations: [...form.destinations, { id: `dest${form.destinations.length + 1}`, address: '', health: '' }]
    });
  };

  const removeDestination = (index) => {
    setForm({
      ...form,
      destinations: form.destinations.filter((_, i) => i !== index)
    });
  };

  const updateDestination = (index, field, value) => {
    const newDests = [...form.destinations];
    newDests[index] = { ...newDests[index], [field]: value };
    setForm({ ...form, destinations: newDests });
  };

  const handleSubmit = (e) => {
    e.preventDefault();

    // Build destinations
    const destinations = {};
    form.destinations.forEach(d => {
      if (d.id && d.address) {
        destinations[d.id] = { address: d.address };
        if (d.health) destinations[d.id].health = d.health;
      }
    });

    // Build cluster config
    const clusterConfig = {
      clusterId: form.clusterId,
      loadBalancingPolicy: form.loadBalancingPolicy,
      destinations
    };

    // Add session affinity if enabled
    if (sessionAffinityEnabled) {
      clusterConfig.sessionAffinity = {
        enabled: true,
        policy: form.sessionAffinityPolicy,
        failurePolicy: form.sessionAffinityFailurePolicy,
        affinityKeyName: form.sessionAffinityKeyName
      };
    }

    // Add health checks if enabled
    if (healthCheckEnabled) {
      clusterConfig.healthCheck = {};

      if (form.passiveHealthEnabled) {
        clusterConfig.healthCheck.passive = {
          enabled: true,
          policy: form.passiveHealthPolicy,
          reactivationPeriod: form.passiveHealthReactivationPeriod
        };
      }

      if (form.activeHealthEnabled) {
        clusterConfig.healthCheck.active = {
          enabled: true,
          interval: form.activeHealthInterval,
          timeout: form.activeHealthTimeout,
          policy: form.activeHealthPolicy,
          path: form.activeHealthPath
        };
      }

      if (form.availableDestinationsPolicy) {
        clusterConfig.healthCheck.availableDestinationsPolicy = form.availableDestinationsPolicy;
      }

      if (Object.keys(clusterConfig.healthCheck).length === 0) {
        delete clusterConfig.healthCheck;
      }
    }

    // Add HTTP client config if enabled
    if (httpClientEnabled) {
      clusterConfig.httpClient = {};
      if (form.maxConnectionsPerServer) {
        clusterConfig.httpClient.maxConnectionsPerServer = parseInt(form.maxConnectionsPerServer);
      }
      if (form.enableMultipleHttp2Connections) {
        clusterConfig.httpClient.enableMultipleHttp2Connections = true;
      }
      if (form.dangerousAcceptAnyServerCertificate) {
        clusterConfig.httpClient.dangerousAcceptAnyServerCertificate = true;
      }
      if (Object.keys(clusterConfig.httpClient).length === 0) {
        delete clusterConfig.httpClient;
      }
    }

    // Add HTTP request config if enabled
    if (httpRequestEnabled) {
      clusterConfig.httpRequest = {};
      if (form.activityTimeout) {
        clusterConfig.httpRequest.activityTimeout = form.activityTimeout;
      }
      if (form.httpVersion) {
        clusterConfig.httpRequest.version = form.httpVersion;
      }
      if (form.versionPolicy) {
        clusterConfig.httpRequest.versionPolicy = form.versionPolicy;
      }
      if (form.allowResponseBuffering) {
        clusterConfig.httpRequest.allowResponseBuffering = true;
      }
      if (Object.keys(clusterConfig.httpRequest).length === 0) {
        delete clusterConfig.httpRequest;
      }
    }

    // Add metadata if enabled
    if (metadataEnabled && form.metadata.length > 0) {
      clusterConfig.metadata = {};
      form.metadata.forEach(m => {
        if (m.key) clusterConfig.metadata[m.key] = m.value;
      });
      if (Object.keys(clusterConfig.metadata).length === 0) {
        delete clusterConfig.metadata;
      }
    }

    onSave(clusterConfig);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()} style={{ maxWidth: '700px' }}>
        <h2>{cluster.clusterId ? 'Edit Cluster' : 'Add Cluster'}</h2>
        <form onSubmit={handleSubmit}>
          {/* Basic Settings */}
          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Cluster ID</label>
              <input
                className="form-input"
                type="text"
                value={form.clusterId}
                onChange={e => setForm({...form, clusterId: e.target.value})}
                placeholder="my-cluster"
                required
                disabled={!!cluster.clusterId}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Load Balancing Policy</label>
              <select
                className="form-input"
                value={form.loadBalancingPolicy}
                onChange={e => setForm({...form, loadBalancingPolicy: e.target.value})}
              >
                <option value="RoundRobin">Round Robin</option>
                <option value="Random">Random</option>
                <option value="LeastRequests">Least Requests</option>
                <option value="PowerOfTwoChoices">Power of Two Choices</option>
                <option value="FirstAlphabetical">First Alphabetical</option>
              </select>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Destinations</label>
            <div className="destinations-list">
              {form.destinations.map((dest, index) => (
                <div key={index} className="destination-row">
                  <input
                    className="form-input"
                    type="text"
                    value={dest.id}
                    onChange={e => updateDestination(index, 'id', e.target.value)}
                    placeholder="Destination ID"
                    style={{ flex: '0.25' }}
                  />
                  <input
                    className="form-input"
                    type="text"
                    value={dest.address}
                    onChange={e => updateDestination(index, 'address', e.target.value)}
                    placeholder="https://localhost:5001"
                    style={{ flex: '0.45' }}
                  />
                  <input
                    className="form-input"
                    type="text"
                    value={dest.health}
                    onChange={e => updateDestination(index, 'health', e.target.value)}
                    placeholder="Health URL (optional)"
                    style={{ flex: '0.25' }}
                  />
                  <button
                    type="button"
                    className="btn btn-danger btn-sm"
                    onClick={() => removeDestination(index)}
                  >
                    x
                  </button>
                </div>
              ))}
              <button type="button" className="btn btn-secondary btn-sm" onClick={addDestination}>
                + Add Destination
              </button>
            </div>
          </div>

          <div className="section-divider" />

          {/* Session Affinity */}
          <FeatureSection
            title="Session Affinity"
            description="Sticky sessions to route requests to the same destination"
            enabled={sessionAffinityEnabled}
            onToggle={() => setSessionAffinityEnabled(!sessionAffinityEnabled)}
          >
            <div className="form-row" style={{ marginTop: '0.75rem' }}>
              <div className="form-group">
                <label className="form-label">Policy</label>
                <select
                  className="form-input"
                  value={form.sessionAffinityPolicy}
                  onChange={e => setForm({...form, sessionAffinityPolicy: e.target.value})}
                >
                  <option value="Cookie">Cookie</option>
                  <option value="CustomHeader">Custom Header</option>
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Failure Policy</label>
                <select
                  className="form-input"
                  value={form.sessionAffinityFailurePolicy}
                  onChange={e => setForm({...form, sessionAffinityFailurePolicy: e.target.value})}
                >
                  <option value="Redistribute">Redistribute</option>
                  <option value="Return503">Return 503</option>
                </select>
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Affinity Key Name</label>
              <input
                className="form-input"
                type="text"
                value={form.sessionAffinityKeyName}
                onChange={e => setForm({...form, sessionAffinityKeyName: e.target.value})}
                placeholder=".Yarp.Affinity"
              />
            </div>
          </FeatureSection>

          {/* Health Checks */}
          <FeatureSection
            title="Health Checks"
            description="Monitor destination health"
            enabled={healthCheckEnabled}
            onToggle={() => setHealthCheckEnabled(!healthCheckEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              {/* Passive Health Check */}
              <div className="sub-section">
                <div className="sub-section-title">
                  <span>Passive Health Check</span>
                  <ToggleSwitch
                    checked={form.passiveHealthEnabled}
                    onChange={e => setForm({...form, passiveHealthEnabled: e.target.checked})}
                  />
                </div>
                {form.passiveHealthEnabled && (
                  <div className="form-row">
                    <div className="form-group">
                      <label className="form-label">Policy</label>
                      <select
                        className="form-input"
                        value={form.passiveHealthPolicy}
                        onChange={e => setForm({...form, passiveHealthPolicy: e.target.value})}
                      >
                        <option value="TransportFailureRate">Transport Failure Rate</option>
                      </select>
                    </div>
                    <div className="form-group">
                      <label className="form-label">Reactivation Period</label>
                      <input
                        className="form-input"
                        type="text"
                        value={form.passiveHealthReactivationPeriod}
                        onChange={e => setForm({...form, passiveHealthReactivationPeriod: e.target.value})}
                        placeholder="00:02:00"
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Active Health Check */}
              <div className="sub-section">
                <div className="sub-section-title">
                  <span>Active Health Check</span>
                  <ToggleSwitch
                    checked={form.activeHealthEnabled}
                    onChange={e => setForm({...form, activeHealthEnabled: e.target.checked})}
                  />
                </div>
                {form.activeHealthEnabled && (
                  <>
                    <div className="form-row">
                      <div className="form-group">
                        <label className="form-label">Interval</label>
                        <input
                          className="form-input"
                          type="text"
                          value={form.activeHealthInterval}
                          onChange={e => setForm({...form, activeHealthInterval: e.target.value})}
                          placeholder="00:00:15"
                        />
                      </div>
                      <div className="form-group">
                        <label className="form-label">Timeout</label>
                        <input
                          className="form-input"
                          type="text"
                          value={form.activeHealthTimeout}
                          onChange={e => setForm({...form, activeHealthTimeout: e.target.value})}
                          placeholder="00:00:10"
                        />
                      </div>
                    </div>
                    <div className="form-row">
                      <div className="form-group">
                        <label className="form-label">Policy</label>
                        <select
                          className="form-input"
                          value={form.activeHealthPolicy}
                          onChange={e => setForm({...form, activeHealthPolicy: e.target.value})}
                        >
                          <option value="ConsecutiveFailures">Consecutive Failures</option>
                        </select>
                      </div>
                      <div className="form-group">
                        <label className="form-label">Health Path</label>
                        <input
                          className="form-input"
                          type="text"
                          value={form.activeHealthPath}
                          onChange={e => setForm({...form, activeHealthPath: e.target.value})}
                          placeholder="/health"
                        />
                      </div>
                    </div>
                  </>
                )}
              </div>

              <div className="form-group" style={{ marginTop: '0.75rem' }}>
                <label className="form-label">Available Destinations Policy</label>
                <select
                  className="form-input"
                  value={form.availableDestinationsPolicy}
                  onChange={e => setForm({...form, availableDestinationsPolicy: e.target.value})}
                >
                  <option value="">Default</option>
                  <option value="HealthyAndUnknown">Healthy And Unknown</option>
                  <option value="HealthyOrPanic">Healthy Or Panic</option>
                </select>
              </div>
            </div>
          </FeatureSection>

          {/* HTTP Client */}
          <FeatureSection
            title="HTTP Client"
            description="Configure outbound connection settings"
            enabled={httpClientEnabled}
            onToggle={() => setHttpClientEnabled(!httpClientEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              <div className="form-group">
                <label className="form-label">Max Connections Per Server</label>
                <input
                  className="form-input"
                  type="number"
                  value={form.maxConnectionsPerServer}
                  onChange={e => setForm({...form, maxConnectionsPerServer: e.target.value})}
                  placeholder="Leave empty for default"
                />
              </div>
              <div className="inline-toggle">
                <span className="inline-toggle-label">Enable Multiple HTTP/2 Connections</span>
                <ToggleSwitch
                  checked={form.enableMultipleHttp2Connections}
                  onChange={e => setForm({...form, enableMultipleHttp2Connections: e.target.checked})}
                />
              </div>
              <div className="inline-toggle">
                <span className="inline-toggle-label">Accept Any Server Certificate</span>
                <ToggleSwitch
                  checked={form.dangerousAcceptAnyServerCertificate}
                  onChange={e => setForm({...form, dangerousAcceptAnyServerCertificate: e.target.checked})}
                />
              </div>
              {form.dangerousAcceptAnyServerCertificate && (
                <p className="warning-text">Warning: This setting disables SSL certificate validation. Use only in development.</p>
              )}
            </div>
          </FeatureSection>

          {/* HTTP Request */}
          <FeatureSection
            title="HTTP Request"
            description="Configure request forwarding settings"
            enabled={httpRequestEnabled}
            onToggle={() => setHttpRequestEnabled(!httpRequestEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Activity Timeout</label>
                  <input
                    className="form-input"
                    type="text"
                    value={form.activityTimeout}
                    onChange={e => setForm({...form, activityTimeout: e.target.value})}
                    placeholder="00:01:40"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">HTTP Version</label>
                  <select
                    className="form-input"
                    value={form.httpVersion}
                    onChange={e => setForm({...form, httpVersion: e.target.value})}
                  >
                    <option value="">Default</option>
                    <option value="1.0">HTTP/1.0</option>
                    <option value="1.1">HTTP/1.1</option>
                    <option value="2">HTTP/2</option>
                    <option value="3">HTTP/3</option>
                  </select>
                </div>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label className="form-label">Version Policy</label>
                  <select
                    className="form-input"
                    value={form.versionPolicy}
                    onChange={e => setForm({...form, versionPolicy: e.target.value})}
                  >
                    <option value="">Default</option>
                    <option value="RequestVersionOrLower">Request Version Or Lower</option>
                    <option value="RequestVersionOrHigher">Request Version Or Higher</option>
                    <option value="RequestVersionExact">Request Version Exact</option>
                  </select>
                </div>
                <div className="form-group">
                  <div className="inline-toggle" style={{ marginTop: '1.5rem' }}>
                    <span className="inline-toggle-label">Allow Response Buffering</span>
                    <ToggleSwitch
                      checked={form.allowResponseBuffering}
                      onChange={e => setForm({...form, allowResponseBuffering: e.target.checked})}
                    />
                  </div>
                </div>
              </div>
            </div>
          </FeatureSection>

          {/* Metadata */}
          <FeatureSection
            title="Metadata"
            description="Custom key-value pairs"
            enabled={metadataEnabled}
            onToggle={() => setMetadataEnabled(!metadataEnabled)}
          >
            <div style={{ marginTop: '0.75rem' }}>
              <KeyValueEditor
                items={form.metadata}
                onChange={(metadata) => setForm({...form, metadata})}
                keyPlaceholder="Key"
                valuePlaceholder="Value"
              />
            </div>
          </FeatureSection>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary">Save Cluster</button>
          </div>
        </form>
      </div>
    </div>
  );
};

// Component is rendered by the HTML page
