-- Migration: Add workflow status fields to agents table
-- Date: 2026-03-07

-- Add new columns to agents table
ALTER TABLE agents
ADD COLUMN workflow_applied BOOLEAN NOT NULL DEFAULT false,
ADD COLUMN workflow_status VARCHAR(50) NOT NULL DEFAULT 'not_ready',
ADD COLUMN applied_workflows JSONB NOT NULL DEFAULT '[]'::jsonb;

-- Add index for workflow_status for faster queries
CREATE INDEX idx_agents_workflow_status ON agents(workflow_status);

-- Add comment to columns
COMMENT ON COLUMN agents.workflow_applied IS 'Indicates if workflow has been applied to this agent';
COMMENT ON COLUMN agents.workflow_status IS 'Status of workflow: not_ready, ready, error';
COMMENT ON COLUMN agents.applied_workflows IS 'JSON array of workflow names applied to this agent';
