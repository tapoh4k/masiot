# Deployment and Testing Guide

## Prerequisites

### Required Software
- PostgreSQL 12+ (tested with 18.2)
- RabbitMQ 3.8+
- .NET 8.0 SDK
- Git (optional)

### System Requirements
- Windows 10/11 or Linux
- 4GB RAM minimum
- 1GB free disk space

## Database Setup

### 1. Create Database

```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE agenti_db;

# Exit psql
\q
```

### 2. Apply Schema and Data

Execute SQL scripts in order:

```bash
cd C:\project\Masiot

# Apply main schema
psql -U postgres -d agenti_db -f schema.sql

# Register workflow functions
psql -U postgres -d agenti_db -f sql\01_workflow_functions.sql

# Create workflow templates
psql -U postgres -d agenti_db -f sql\02_workflow_templates.sql

# Populate OKPD-2 dictionary
psql -U postgres -d agenti_db -f sql\03_okpd2_sample_data.sql

# Load test data
psql -U postgres -d agenti_db -f sql\04_sample_test_data.sql
```

### 3. Verify Database

```bash
# Check tables
psql -U postgres -d agenti_db -c "\dt"

# Check workflow functions count
psql -U postgres -d agenti_db -c "SELECT COUNT(*) FROM workflow_functions;"

# Check agents count
psql -U postgres -d agenti_db -c "SELECT COUNT(*) FROM agents;"

# Check tech processes
psql -U postgres -d agenti_db -c "SELECT * FROM tech_processes;"
```

Expected results:
- 55 workflow functions
- 14 agents (7 equipment + 6 workers + 1 techprocess agent)
- 2 tech processes with 6 total operations

## RabbitMQ Setup

### 1. Start RabbitMQ

```bash
# Windows
rabbitmq-server

# Linux
sudo systemctl start rabbitmq-server
```

### 2. Create Exchange

```bash
# Create topic exchange for factory agents
rabbitmqadmin declare exchange name=factory_agents type=topic durable=true

# Verify
rabbitmqadmin list exchanges
```

### 3. Enable Management Plugin (optional)

```bash
rabbitmq-plugins enable rabbitmq_management

# Access web UI at http://localhost:15672
# Default credentials: guest/guest
```

## Backend Configuration

### 1. Update Connection Strings

Edit `Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=agenti_db;Username=postgres;Password=YOUR_PASSWORD"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest"
  }
}
```

### 2. Build and Run

```bash
cd Backend
dotnet build
dotnet run
```

Expected output:
```
Starting Factory Agent Backend on port 3001
```

### 3. Verify Backend

Open browser to:
- Health check: http://localhost:3001/api/health
- Swagger UI: http://localhost:3001/swagger

## Testing the System

### 1. Test Basic APIs

#### Get All Agents
```bash
curl http://localhost:3001/api/agents
```

#### Get Agent by ID
```bash
curl http://localhost:3001/api/agents/1
```

#### Get Agents by OKPD-2 Code
```bash
curl http://localhost:3001/api/agents/okpd2/28.41.11
```

#### Get Tech Process
```bash
curl http://localhost:3001/api/techprocess/1
```

#### Get Tech Process Operations
```bash
curl http://localhost:3001/api/techprocess/1/operations
```

### 2. Test Workflow Functions

#### Get All Workflow Functions
```bash
curl http://localhost:3001/api/workflowfunctions
```

#### Get Functions by Category
```bash
curl http://localhost:3001/api/workflowfunctions/category/Planning
```

### 3. Test OKPD-2 Dictionary

#### Get All OKPD-2 Codes
```bash
curl http://localhost:3001/api/okpd2dictionary
```

#### Get OKPD-2 by Code
```bash
curl http://localhost:3001/api/okpd2dictionary/code/28.41.11
```

#### Get Children of OKPD-2 Code
```bash
curl http://localhost:3001/api/okpd2dictionary/28.41/children
```

### 4. Test Workflow Execution (Manual)

#### Create Workflow Instance
```bash
curl -X POST http://localhost:3001/api/wflist \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "techprocess_1",
    "wfType": "WF1-WF5.2: Планирование техпроцесса",
    "wfFunctions": "[]",
    "parameters": "{\"techProcessId\": 1, \"orderNumber\": \"ORDER-001\", \"planType\": \"operational\"}",
    "initiator": "test_user"
  }'
```

#### Get Workflow Status
```bash
curl http://localhost:3001/api/wflist/1
```

### 5. Test RabbitMQ Messaging

#### Send Message to Agent
```bash
curl -X POST http://localhost:3001/api/rabbitmq/send-to-agent \
  -H "Content-Type: application/json" \
  -d '{
    "agentId": "agent_1",
    "messageType": "test",
    "payload": {"message": "Hello Agent"}
  }'
```

#### Broadcast to OKPD-2 Code
```bash
curl -X POST http://localhost:3001/api/rabbitmq/broadcast-okpd2 \
  -H "Content-Type: application/json" \
  -d '{
    "okpd2Code": "28.41.11",
    "messageType": "resource_request",
    "payload": {
      "operation": "turning",
      "duration": 45,
      "startTime": "2026-03-22T08:00:00Z"
    }
  }'
```

## Monitoring

### 1. Check Logs

Backend logs are written to:
- Console output
- `Backend/logs/backend-YYYY-MM-DD.log`

### 2. Monitor RabbitMQ

Access RabbitMQ Management UI:
- URL: http://localhost:15672
- Username: guest
- Password: guest

Check:
- Exchanges: `factory_agents` should exist
- Queues: Agent queues should appear when agents connect
- Messages: Monitor message flow

### 3. Database Monitoring

```bash
# Check active workflows
psql -U postgres -d agenti_db -c "SELECT * FROM wf_list WHERE status = 0;"

# Check timetable entries
psql -U postgres -d agenti_db -c "SELECT * FROM timetable ORDER BY created_at DESC LIMIT 10;"

# Check agent status
psql -U postgres -d agenti_db -c "SELECT id, name, status, agent_type FROM agents;"
```

## Troubleshooting

### Database Connection Issues

**Problem**: Backend can't connect to PostgreSQL

**Solution**:
1. Check PostgreSQL is running: `pg_isready -h localhost -p 5432`
2. Verify credentials in `appsettings.json`
3. Check PostgreSQL logs: `tail -f /var/log/postgresql/postgresql-*.log`

### RabbitMQ Connection Issues

**Problem**: Backend can't connect to RabbitMQ

**Solution**:
1. Check RabbitMQ is running: `rabbitmqctl status`
2. Verify credentials in `appsettings.json`
3. Check RabbitMQ logs: `tail -f /var/log/rabbitmq/rabbit@*.log`

### Build Errors

**Problem**: Backend fails to build

**Solution**:
1. Clean and rebuild: `dotnet clean && dotnet build`
2. Restore packages: `dotnet restore`
3. Check .NET SDK version: `dotnet --version` (should be 8.0+)

### Workflow Execution Issues

**Problem**: Workflows not executing

**Solution**:
1. Check workflow functions are registered: `SELECT COUNT(*) FROM workflow_functions;`
2. Verify function whitelist in `WorkflowFunctionWhitelist.cs`
3. Check backend logs for errors
4. Verify workflow template JSON is valid

## Next Steps

### 1. Create Agent Client Application

Agents need a client application to:
- Connect to RabbitMQ
- Listen for messages on their routing keys
- Respond with availability/status
- Execute assigned tasks

### 2. Implement SPR (Decision System)

The SPR system needs to:
- Receive candidate lists from workflows
- Apply business rules for resource selection
- Return approval/rejection decisions
- Consider factors: cost, time, quality, availability

### 3. Create Frontend Dashboard

Build a web interface to:
- Visualize workflow execution
- Monitor agent status
- View timetable/schedule
- Manage OKPD-2 dictionary
- Create and edit workflow templates

### 4. Add Authentication

Implement proper authentication:
- JWT tokens
- Role-based access control
- User management
- API key authentication for agents

### 5. Production Deployment

Prepare for production:
- Use environment variables for secrets
- Set up reverse proxy (nginx)
- Configure SSL/TLS
- Set up monitoring (Prometheus, Grafana)
- Configure backup strategy
- Set up CI/CD pipeline

## Support

For issues or questions:
- Check logs first
- Review this guide
- Check GitHub issues: https://github.com/anthropics/claude-code/issues
- Consult documentation: https://docs.claude.com/en/docs/claude-code

## Summary

You now have:
- ✅ Complete backend with 55 workflow functions
- ✅ Database with schema and test data
- ✅ RabbitMQ messaging infrastructure
- ✅ OKPD-2 classification system
- ✅ Sample tech processes and agents
- ✅ Workflow templates for planning process

The system is ready for:
- Agent client development
- SPR system integration
- Frontend dashboard creation
- End-to-end testing
