import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

const WFNode = ({ data, isConnectable }) => {
  return (
    <div className="custom-node wf-node">
      <Handle
        type="target"
        position={Position.Top}
        isConnectable={isConnectable}
      />
      <div className="node-header">
        <span className="node-icon">📦</span>
        <span className="node-type">WF</span>
      </div>
      <div className="node-content">
        <div className="node-label">{data.label}</div>
        <div className="node-description">Готовая цепочка функций</div>
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        isConnectable={isConnectable}
      />
    </div>
  );
};

export default memo(WFNode);
