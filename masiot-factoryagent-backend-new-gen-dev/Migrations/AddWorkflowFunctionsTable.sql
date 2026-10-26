-- Migration: Add workflow_functions table
-- Date: 2026-03-01

-- Устанавливаем кодировку клиента UTF-8 для корректной работы с русским языком
SET client_encoding TO 'UTF8';

CREATE TABLE IF NOT EXISTS workflow_functions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    function_class VARCHAR(500) NOT NULL,
    function_name VARCHAR(255) NOT NULL,
    params JSONB NOT NULL DEFAULT '[]',
    returns VARCHAR(255),
    icon VARCHAR(10),
    type VARCHAR(50) NOT NULL DEFAULT 'function',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_workflow_functions_name ON workflow_functions(name);
CREATE INDEX idx_workflow_functions_type ON workflow_functions(type);

-- Insert default functions
INSERT INTO workflow_functions (name, description, function_class, function_name, params, returns, icon, type) VALUES
('Add', 'Складывает два числа', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'Add', '["a", "b"]', 'sum', '➕', 'function'),
('Multiply', 'Умножает два числа', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'Multiply', '["x", "y"]', 'product', '✖️', 'function'),
('SayHello', 'Приветствие', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'SayHello', '["name"]', 'greeting', '👋', 'function'),
('LogMessage', 'Логирование', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'LogMessage', '["message"]', 'timestamp', '📝', 'function'),
('GetData', 'Получение данных', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'GetData', '["source"]', 'data', '📥', 'function'),
('ProcessData', 'Обработка данных', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'ProcessData', '["input"]', 'output', '⚙️', 'function'),
('SaveResult', 'Сохранение результата', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'SaveResult', '["data", "destination"]', 'status', '💾', 'function'),
('SendNotification', 'Отправка уведомления', 'Backend.Services.WorkflowFunctions.ExampleWorkflowFunctions', 'SendNotification', '["recipient", "message"]', 'sent', '📧', 'function');
