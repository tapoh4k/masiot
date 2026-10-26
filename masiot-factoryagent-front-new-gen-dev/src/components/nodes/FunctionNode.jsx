import React, { memo } from 'react';
import { Handle, Position } from 'reactflow';

const FunctionNode = ({ data, isConnectable }) => {
  return (
    <div className="custom-node function-node">
      <Handle
        type="target"
        position={Position.Top}
        isConnectable={isConnectable}
      />
      <div className="node-header">
        <span className="node-icon">⚙️</span>
        <span className="node-type">Функция</span>
      </div>
      <div className="node-content">
        <div className="node-label">{data.label}</div>
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        isConnectable={isConnectable}
      />
    </div>
  );
};

export default memo(FunctionNode);
