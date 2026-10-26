import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

const SubWFNode = ({ data, isConnectable }) => {
  return (
    <div className="custom-node subwf-node">
      <Handle
        type="target"
        position={Position.Top}
        isConnectable={isConnectable}
      />
      <div className="node-header">
        <span className="node-icon">🔗</span>
        <span className="node-type">SUB-WF</span>
      </div>
      <div className="node-content">
        <div className="node-label">{data.label}</div>
        <div className="node-description">Подцепочка функций</div>
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        isConnectable={isConnectable}
      />
    </div>
  );
};

export default memo(SubWFNode);
