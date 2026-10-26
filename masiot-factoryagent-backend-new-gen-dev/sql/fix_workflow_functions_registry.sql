-- Приведение реестра workflow_functions в соответствие с C#-классами
-- для баз, созданных старой версией schema.sql.

BEGIN;

-- AgentInfoFunctions принимают agentId как string
UPDATE workflow_functions
SET params = replace(params::text, '"type": "int", "description": "ID агента"', '"type": "string", "description": "ID агента"')::jsonb,
    updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.AgentInfoFunctions'
  AND function_name IN ('GetAgentStatus', 'GetAgentParameters', 'GetAgentInfo', 'UpdateAgentStatus');

-- WFListFunctions.CreateWorkflow(agentId, wfType, parameters, initiator) — без wfFunctions
UPDATE workflow_functions
SET params = '[{"name":"agentId","type":"string"},{"name":"wfType","type":"string"},{"name":"parameters","type":"string"},{"name":"initiator","type":"string"}]'::jsonb,
    updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.WFListFunctions'
  AND function_name = 'CreateWorkflow';

-- TimeTableFunctions.CreateScheduleEntry(orderNumber, timeStart, timeEnd, quantity, location, partnerId)
UPDATE workflow_functions
SET params = '[{"name":"orderNumber","type":"string"},{"name":"timeStart","type":"string"},{"name":"timeEnd","type":"string"},{"name":"quantity","type":"double"},{"name":"location","type":"string"},{"name":"partnerId","type":"string"}]'::jsonb,
    updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.TimeTableFunctions'
  AND function_name = 'CreateScheduleEntry';

-- ExampleWorkflowFunctions: несуществующие методы заменяются реальными
DELETE FROM workflow_functions
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions'
  AND function_name IN ('GetData', 'ProcessData', 'SaveResult', 'WaitForEvent');

UPDATE workflow_functions SET params = '[{"name":"a","type":"int"},{"name":"b","type":"int"}]'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions' AND function_name = 'Add';
UPDATE workflow_functions SET params = '[{"name":"x","type":"int"},{"name":"y","type":"int"}]'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions' AND function_name = 'Multiply';
UPDATE workflow_functions SET params = '[{"name":"name","type":"string"}]'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions' AND function_name = 'SayHello';
UPDATE workflow_functions SET params = '[{"name":"message","type":"string"}]'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions' AND function_name = 'LogMessage';
UPDATE workflow_functions SET params = '[{"name":"recipient","type":"string"},{"name":"message","type":"string"}]'::jsonb, updated_at = CURRENT_TIMESTAMP
WHERE function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions' AND function_name = 'SendNotification';

INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type)
SELECT v.name, v.description, 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', v.function_name, v.params::jsonb, v.returns, v.icon, 'Example'
FROM (VALUES
  ('CheckMaterial', 'Проверка наличия материала', 'CheckMaterial', '[{"name":"materialId","type":"string"}]', 'available', '📥'),
  ('StartProcessing', 'Начало обработки детали', 'StartProcessing', '[{"name":"partName","type":"string"},{"name":"machineId","type":"string"}]', 'processId', '⚙️'),
  ('Wait', 'Задержка на заданное число секунд', 'Wait', '[{"name":"seconds","type":"int"}]', 'elapsed', '⏳')
) AS v(name, description, function_name, params, returns, icon)
WHERE NOT EXISTS (
  SELECT 1 FROM workflow_functions f
  WHERE f.function_class = 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions'
    AND f.function_name = v.function_name
);

COMMIT;
