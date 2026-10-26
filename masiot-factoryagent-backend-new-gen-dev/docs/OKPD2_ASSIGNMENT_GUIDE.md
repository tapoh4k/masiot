# OKPD-2 Code Assignment Guide

**Date**: 2026-03-21
**Purpose**: Configure agents with OKPD-2 classifications to enable broadcast routing

## Overview

The Factory Agent Backend is deployed and operational. To enable the broadcast routing functionality (WF2 resource discovery), agents need OKPD-2 codes assigned based on their capabilities.

## Current State

- **Total Agents**: 134
- **Agents with OKPD-2 codes**: 0
- **OKPD-2 Dictionary Entries**: 34 codes loaded
- **Backend Status**: Running on port 3001

## Available OKPD-2 Codes

### Equipment Codes (Section 28.41)

| Code | Name | Description |
|------|------|-------------|
| 28.41.11 | Станки токарно-винторезные | Universal lathe machines |
| 28.41.12 | Станки токарные с ЧПУ | CNC lathe machines |
| 28.41.21 | Станки фрезерные универсальные | Universal milling machines |
| 28.41.22 | Станки фрезерные с ЧПУ | CNC milling machines |
| 28.41.3 | Станки сверлильные | Drilling machines |
| 28.41.4 | Станки шлифовальные | Grinding machines |

### Service Codes (Section 25.61)

| Code | Name | Description |
|------|------|-------------|
| 25.61.1 | Услуги токарные | Turning services |
| 25.61.2 | Услуги фрезерные | Milling services |
| 25.61.3 | Услуги сверлильные | Drilling services |
| 25.61.4 | Услуги шлифовальные | Grinding services |

### Profession Codes (Section PROF)

| Code | Name | Description |
|------|------|-------------|
| PROF.01.1 | Токарь 2 разряда | Turner grade 2 |
| PROF.01.2 | Токарь 3 разряда | Turner grade 3 |
| PROF.01.3 | Токарь 4 разряда | Turner grade 4 |
| PROF.01.4 | Токарь 5 разряда | Turner grade 5 |
| PROF.02.1 | Фрезеровщик 2 разряда | Milling operator grade 2 |
| PROF.02.2 | Фрезеровщик 3 разряда | Milling operator grade 3 |
| PROF.02.3 | Фрезеровщик 4 разряда | Milling operator grade 4 |
| PROF.03.1 | Сверловщик 2 разряда | Drilling operator grade 2 |
| PROF.03.2 | Сверловщик 3 разряда | Drilling operator grade 3 |
| PROF.04.1 | Шлифовщик 3 разряда | Grinding operator grade 3 |
| PROF.04.2 | Шлифовщик 4 разряда | Grinding operator grade 4 |

## Assignment Strategy

### Equipment Agents

Equipment agents should be assigned codes from section **28.41** (machines) or **25.61** (services):

- **Lathe machines** → `28.41.11` (universal) or `28.41.12` (CNC)
- **Milling machines** → `28.41.21` (universal) or `28.41.22` (CNC)
- **Drilling machines** → `28.41.3`
- **Grinding machines** → `28.41.4`

### Worker Agents

Worker agents should be assigned codes from section **PROF** based on their profession and skill level:

- **Turners** → `PROF.01.1` through `PROF.01.4` (by grade)
- **Milling operators** → `PROF.02.1` through `PROF.02.3` (by grade)
- **Drilling operators** → `PROF.03.1` or `PROF.03.2` (by grade)
- **Grinding operators** → `PROF.04.1` or `PROF.04.2` (by grade)

### Tech Process Agents

Tech process agents should have `agent_type = 'techprocess'` and typically don't need OKPD-2 codes as they don't respond to resource requests.

## Assignment Methods

### Method 1: Direct SQL Update

```sql
-- Example: Assign OKPD-2 code to a lathe machine
UPDATE agents
SET okpd2_code = '28.41.11'
WHERE node_id = 'stanok-tokarny-1';

-- Example: Assign OKPD-2 code to a turner worker
UPDATE agents
SET okpd2_code = 'PROF.01.3'
WHERE node_id = 'worker-turner-ivanov';

-- Batch update: All agents with "токарн" in name
UPDATE agents
SET okpd2_code = '28.41.11'
WHERE LOWER(name) LIKE '%токарн%'
  AND agent_type = 'resource';
```

### Method 2: API Update (via PUT /api/agents/{id})

```bash
# Update agent with ID 1
curl -X PUT http://localhost:3001/api/agents/1 \
  -H "Content-Type: application/json" \
  -d '{
    "okpd2_code": "28.41.11"
  }'
```

### Method 3: Bulk Assignment Script

Create a PowerShell or Python script to:
1. Fetch all agents via API
2. Analyze agent names/types
3. Assign appropriate OKPD-2 codes
4. Update via API

## Verification

After assignment, verify the configuration:

```bash
# Check agents by OKPD-2 code
curl http://localhost:3001/api/agents/okpd2/28.41.11

# Count agents with OKPD-2 codes
psql -U postgres -d agenti_db -c "
  SELECT
    COUNT(*) as total,
    COUNT(okpd2_code) as with_okpd2,
    COUNT(*) - COUNT(okpd2_code) as without_okpd2
  FROM agents;
"

# List OKPD-2 code distribution
psql -U postgres -d agenti_db -c "
  SELECT
    okpd2_code,
    COUNT(*) as agent_count
  FROM agents
  WHERE okpd2_code IS NOT NULL
  GROUP BY okpd2_code
  ORDER BY okpd2_code;
"
```

## Testing Broadcast Routing

Once OKPD-2 codes are assigned, test the broadcast functionality:

```bash
# Test broadcast to lathe machines
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

Expected behavior:
- Message published to RabbitMQ with routing key `okpd2.28.41.11`
- All agents with `okpd2_code = '28.41.11'` should receive the message
- Agents should respond with availability information

## Example Assignment Scenarios

### Scenario 1: Metal Workshop

```sql
-- Lathe machines
UPDATE agents SET okpd2_code = '28.41.11' WHERE node_id IN ('stanok-tokarny-1', 'stanok-tokarny-2');
UPDATE agents SET okpd2_code = '28.41.12' WHERE node_id = 'stanok-tokarny-cnc-1';

-- Milling machines
UPDATE agents SET okpd2_code = '28.41.21' WHERE node_id = 'stanok-frezerny-1';
UPDATE agents SET okpd2_code = '28.41.22' WHERE node_id = 'stanok-frezerny-cnc-1';

-- Drilling machines
UPDATE agents SET okpd2_code = '28.41.3' WHERE node_id = 'stanok-sverlilny-1';

-- Workers
UPDATE agents SET okpd2_code = 'PROF.01.3' WHERE profession = 'Токарь' AND name LIKE '%3 разряд%';
UPDATE agents SET okpd2_code = 'PROF.02.2' WHERE profession = 'Фрезеровщик' AND name LIKE '%2 разряд%';
```

### Scenario 2: Using OKP Field as Reference

If agents have existing `okp` field values that indicate their type:

```sql
-- Map OKP codes to OKPD-2 codes
UPDATE agents SET okpd2_code = '28.41.11' WHERE okp LIKE '%Токарн%';
UPDATE agents SET okpd2_code = '28.41.21' WHERE okp LIKE '%Фрезерн%';
UPDATE agents SET okpd2_code = '28.41.3' WHERE okp LIKE '%Сверл%';
UPDATE agents SET okpd2_code = '28.41.4' WHERE okp LIKE '%Шлифов%';
```

## Multiple OKPD-2 Codes

Some agents may support multiple operations. Use the `okpd2_codes` JSONB field:

```sql
-- Agent that can do both turning and milling
UPDATE agents
SET okpd2_codes = '["28.41.11", "28.41.21"]'::jsonb
WHERE node_id = 'universal-machine-1';
```

When using `okpd2_codes`, the agent will receive broadcasts for ALL listed codes.

## Workflow Integration

Once OKPD-2 codes are assigned:

1. **WF2 Resource Request** will broadcast to appropriate OKPD-2 codes
2. **Agents** will receive messages on their routing keys
3. **Agents** respond with availability (creates timetable entries with status=20)
4. **WF3** collects candidates and sends to SPR
5. **WF4** processes SPR decision (status 20→30 approved, 20→40 rejected)

## Next Steps

1. **Analyze existing agents** - Review the 134 agents to understand their types
2. **Create assignment script** - Automate OKPD-2 code assignment based on agent properties
3. **Assign codes** - Execute bulk assignment
4. **Verify** - Check that all resource agents have appropriate codes
5. **Test** - Send test broadcast messages
6. **Deploy agent clients** - Create agent applications to listen and respond to messages

## References

- [DEPLOYMENT_SUCCESS.md](DEPLOYMENT_SUCCESS.md) - Current deployment status
- [ActualProcess.md](../masiot-agent-wiki/ActualProcess.md) - WF1-WF5.2 process specification
- API Documentation: http://localhost:3001/swagger
