import React, { useState, useEffect, useRef } from 'react';
import * as XLSX from 'xlsx';
import { agentAPI, blankOkpd2API, messagesAPI, hostAPI } from '../services/api.js';
import './AgentCreationModal.css';

// Должно совпадать с TechProcessController.TpFolderName на бэкенде
const TP_FOLDER_NAME = 'Технологические процессы';
const FOLDER_BY_AGENT_TYPE = {
  tech_process: TP_FOLDER_NAME,
  equipment: 'Оборудование',
  instrument: 'Инструмент',
  personnel: 'Персонал',
  blank: 'Заготовки'
};
const UNKNOWN_OKPD2 = '00.00.00.000';
const MAX_PARAMETER_VALUE_LENGTH = 255;

const isTpSheetName = (sheetName) => /^ТП-\d+/i.test(String(sheetName || '').trim());
const getTpCode = (text) => (String(text || '').trim().match(/^(ТП-\d+)/i)?.[1] || '').toUpperCase();
const normalizeName = (value) => String(value ?? '').replace(/\s+/g, ' ').trim().toLowerCase();
const agentKey = (type, name) => `${type}|${normalizeName(name)}`;

const collectExistingAgents = (nodes) => {
  const keys = new Set();
  const tpCodes = new Set();
  const walk = (list) => {
    for (const node of list || []) {
      if (node.type === 'agent') {
        const type = node.agentData?.type || 'other';
        const name = node.agentData?.name ?? node.name;
        keys.add(agentKey(type, name));
        if (type === 'tech_process' && getTpCode(name)) {
          tpCodes.add(getTpCode(name));
        }
      }
      if (node.children?.length) walk(node.children);
    }
  };
  walk(nodes);
  return { keys, tpCodes };
};

const formatTechProcessResult = (result) => {
  const lines = [
    `Техпроцессы → папка «${TP_FOLDER_NAME}»`,
    `  создано: ${result.created_count ?? 0} (операций: ${result.operations_count ?? 0})`
  ];
  if (result.skipped_count) lines.push(`  уже были загружены: ${result.skipped_count}`);
  if (result.empty_sheets?.length) lines.push(`  листы без операций: ${result.empty_sheets.join(', ')}`);
  return lines.join('\n');
};

const AgentCreationModal = ({ file, onClose, onSave, treeNodes, onReloadTree, onAddNode }) => {
  const [agents, setAgents] = useState([]);
  const [filteredAgents, setFilteredAgents] = useState([]);
  const [columns, setColumns] = useState([]);
  const [loading, setLoading] = useState(true);
  const [currentFile, setCurrentFile] = useState(file);
  const [workbook, setWorkbook] = useState(null);
  const [currentSheet, setCurrentSheet] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [isCreating, setIsCreating] = useState(false);
  const [creationProgress, setCreationProgress] = useState({ current: 0, total: 0, message: '' });
  const [hosts, setHosts] = useState([]);
  const [selectedHostId, setSelectedHostId] = useState('');

  const tableScrollRef = useRef(null);

  useEffect(() => {
    hostAPI.getAll()
      .then((data) => {
        setHosts(data);
        if (data.length) setSelectedHostId(String(data[0].id));
      })
      .catch((e) => console.error('Ошибка загрузки Hosts:', e));
  }, []);

  useEffect(() => {
    if (currentFile) {
      parseXLSX(currentFile);
    }
  }, [currentFile, currentSheet, treeNodes]); // Добавили treeNodes в зависимости

  useEffect(() => {
    filterAgents();
  }, [agents, searchQuery, statusFilter]);

  const parseXLSX = async (file) => {
    try {
      setLoading(true);
      const data = await file.arrayBuffer();
      const wb = XLSX.read(data, { type: 'array' });
      setWorkbook(wb);

      const sheet = readSheet(wb, wb.SheetNames[currentSheet]);
      if (sheet) {
        const existing = collectExistingAgents(treeNodes);
        setColumns(sheet.headers);
        setAgents(sheet.rows.map(row => {
          const status = getRowStatus(sheet, row, existing);
          return { ...row, status, editable: status === 'new' };
        }));
      } else {
        setColumns([]);
        setAgents([]);
      }
      setLoading(false);
    } catch (error) {
      console.error('Error parsing XLSX:', error);
      setLoading(false);
    }
  };

  const filterAgents = () => {
    let filtered = [...agents];

    if (statusFilter !== 'all') {
      filtered = filtered.filter(agent => agent.status === statusFilter);
    }

    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(agent =>
        Object.values(agent.data).some(value =>
          String(value).toLowerCase().includes(query)
        )
      );
    }

    setFilteredAgents(filtered);
  };

  const handleCellChange = (agentId, column, value) => {
    setAgents(prev => prev.map(agent =>
      agent.id === agentId && agent.editable
        ? { ...agent, data: { ...agent.data, [column]: value } }
        : agent
    ));
  };

  const handleFileUpload = (event) => {
    const newFile = event.target.files[0];
    if (newFile && newFile.name.endsWith('.xlsx')) {
      setCurrentFile(newFile);
      setCurrentSheet(0);
    }
  };

  const handleClearFile = () => {
    setCurrentFile(null);
    setAgents([]);
    setFilteredAgents([]);
    setColumns([]);
    setWorkbook(null);
  };

  const handleSheetChange = (direction) => {
    if (!workbook) return;
    const newSheet = currentSheet + direction;
    if (newSheet >= 0 && newSheet < workbook.SheetNames.length) {
      setCurrentSheet(newSheet);
    }
  };

  // Папка ищется по точному названию: иначе "Инструмент" мог попасть в "Мерительный инструмент" и т.п.
  const findBranchIdByName = (folderName) => {
    const target = normalizeName(folderName);
    const searchInNodes = (nodes) => {
      for (const node of nodes || []) {
        if (node.type === 'branch' && normalizeName(node.name) === target) {
          return node.id;
        }
        const found = searchInNodes(node.children);
        if (found) return found;
      }
      return null;
    };

    return searchInNodes(treeNodes);
  };

  const getFolderName = (sheet) => FOLDER_BY_AGENT_TYPE[sheet.agentType] || String(sheet.sheetName).trim();

  // Функция для получения ID узла "Unclassified"
  const getUnclassifiedNodeId = () => {
    const searchInNodes = (nodes) => {
      for (const node of nodes) {
        if (node.id === 'unclassified' || node.name === 'Unclassified') {
          return node.id;
        }
        if (node.children && node.children.length > 0) {
          const found = searchInNodes(node.children);
          if (found) return found;
        }
      }
      return null;
    };

    return searchInNodes(treeNodes || []);
  };

  // Функция для поиска индекса колонки по ключевым словам
  const findColumnIndex = (headers, keywords) => {
    for (let i = 0; i < headers.length; i++) {
      const header = String(headers[i]).toLowerCase();
      for (const keyword of keywords) {
        if (header.includes(keyword.toLowerCase())) {
          return i;
        }
      }
    }
    return -1;
  };

  // Функция для определения типа агента по названию листа
  const determineAgentType = (sheetName) => {
    const name = sheetName.toLowerCase();

    if (isTpSheetName(sheetName) || name.includes('техпроцесс') || name.includes('тех процесс') || name.includes('tech process')) {
      return 'tech_process';
    } else if (name.includes('оборудование') || name.includes('equipment')) {
      return 'equipment';
    } else if (name.includes('инструмент') || name.includes('instrument') || name.includes('tool')) {
      return 'instrument';
    } else if (name.includes('персонал') || name.includes('personnel') || name.includes('staff')) {
      return 'personnel';
    } else if (name.includes('заготовк') || name.includes('blank') || name.includes('workpiece')) {
      return 'blank';
    }

    return 'other';
  };

  const normalizeHeader = (header) =>
    String(header || '').toLowerCase().replace(/\s+/g, ' ').trim();

  const findColumnIndexByPredicates = (headers, predicates) => {
    for (const predicate of predicates) {
      for (let i = 0; i < headers.length; i++) {
        if (predicate(normalizeHeader(headers[i]))) {
          return i;
        }
      }
    }

    return -1;
  };

  const isOkpd2Header = (header) =>
    header.includes('окпд-2') ||
    header.includes('окпд2') ||
    header.includes('okpd-2') ||
    header.includes('okpd2');

  const isOkpHeader = (header) =>
    header === 'код окп' ||
    header === 'окп' ||
    /^код\s+окп(\s|$)/.test(header);

  const getAgentColumnIndexes = (headers, agentType) => {
    const quantityColumnIndex = findColumnIndex(headers, ['количество', 'кол-во', 'кол.', 'quantity', 'qty']);

    if (agentType === 'blank') {
      return {
        nameColumnIndex: findColumnIndex(headers, ['наименование', 'название', 'name']),
        okpd2ColumnIndex: findColumnIndex(headers, ['обозначение', 'окпд-2', 'окпд2', 'okpd2', 'okpd-2', 'код']),
        quantityColumnIndex
      };
    }

    const namePredicates = {
      equipment: [
        (header) => header.includes('наименование оборудования'),
        (header) => header.includes('оборудование'),
        (header) => header.includes('наименование'),
        (header) => header.includes('название') || header.includes('name')
      ],
      instrument: [
        (header) => header.includes('наименование инструмента'),
        (header) => header.includes('инструмент'),
        (header) => header.includes('наименование'),
        (header) => header.includes('название') || header.includes('name')
      ],
      personnel: [
        (header) => header.includes('фио') || header.includes('должность'),
        (header) => header.includes('профессия'),
        (header) => header.includes('персонал'),
        (header) => header.includes('наименование'),
        (header) => header.includes('название') || header.includes('name')
      ]
    };

    const codePredicates = {
      equipment: [
        isOkpHeader,
        (header) => header.includes('код классификатора'),
        isOkpd2Header,
        (header) => header === 'код'
      ],
      instrument: [
        (header) => header.includes('код классификатора'),
        isOkpHeader,
        isOkpd2Header,
        (header) => header === 'код'
      ],
      personnel: [
        (header) => header.includes('код профессии') || header.includes('окпдтр'),
        isOkpd2Header,
        (header) => header.includes('код классификатора'),
        (header) => header === 'код'
      ],
      other: [
        isOkpd2Header,
        (header) => header.includes('код классификатора'),
        (header) => header === 'код',
        (header) => header.includes('обозначение')
      ]
    };

    return {
      nameColumnIndex: findColumnIndexByPredicates(
        headers,
        namePredicates[agentType] || [
          (header) => header.includes('наименование'),
          (header) => header.includes('название') || header.includes('name'),
          (header) => header.includes('оборудование')
        ]
      ),
      okpd2ColumnIndex: findColumnIndexByPredicates(headers, codePredicates[agentType] || codePredicates.other),
      quantityColumnIndex
    };
  };

  const extractClassifierCode = (value) => {
    const text = String(value || '').trim();
    if (!text) return '';

    const dottedCodes = text.match(/\b\d{2}\.\d{2}(?:\.\d{1,3}){1,2}\b/g);
    if (dottedCodes && dottedCodes.length > 0) {
      return dottedCodes.join(', ');
    }

    const numericCode = text.match(/\b\d{4,6}\b/);
    if (numericCode) {
      return numericCode[0];
    }

    const firstPart = text.split(';')[0].split(',')[0].trim();
    return /\d/.test(firstPart) ? firstPart : '';
  };

  const isClassifierParameterHeader = (header, agentType) => {
    if (!['equipment', 'instrument', 'personnel'].includes(agentType)) {
      return false;
    }

    const normalizedHeader = normalizeHeader(header);
    return isOkpd2Header(normalizedHeader) ||
      isOkpHeader(normalizedHeader) ||
      normalizedHeader.includes('код классификатора') ||
      normalizedHeader.includes('код профессии') ||
      normalizedHeader.includes('окпдтр');
  };

  const collectAdditionalParameters = (agent, headers, usedColumnIndexes, agentType) => {
    const usedIndexes = new Set(usedColumnIndexes.filter(index => index >= 0));

    return headers.reduce((parameters, header, index) => {
      const value = agent.data[header];

      if (
        header === '№' ||
        usedIndexes.has(index) ||
        isClassifierParameterHeader(header, agentType) ||
        value === undefined ||
        value === null ||
        value === ''
      ) {
        return parameters;
      }

      const text = String(value);
      const isTooLong = text.length > MAX_PARAMETER_VALUE_LENGTH;
      parameters.push({
        id: index,
        name: header,
        unit: '',
        value: isTooLong ? `${text.slice(0, MAX_PARAMETER_VALUE_LENGTH - 1)}…` : text,
        description: isTooLong ? text : ''
      });

      return parameters;
    }, []);
  };

  const readSheet = (wb, sheetName) => {
    const jsonData = XLSX.utils.sheet_to_json(wb.Sheets[sheetName], { header: 1 });
    if (jsonData.length === 0) return null;

    const originalHeaders = jsonData[0];
    const hasNumberColumn = originalHeaders.includes('№');
    const headers = hasNumberColumn ? originalHeaders : ['№', ...originalHeaders];
    const agentType = determineAgentType(sheetName);
    const { nameColumnIndex, okpd2ColumnIndex, quantityColumnIndex } = getAgentColumnIndexes(headers, agentType);

    const rows = jsonData.slice(1)
      .filter(row => row.some(cell => String(cell ?? '').trim() !== ''))
      .map((row, index) => ({
        id: index + 1,
        data: headers.reduce((acc, header, idx) => {
          if (header === '№') {
            acc[header] = index + 1;
          } else {
            const dataIdx = hasNumberColumn ? idx : idx - 1;
            acc[header] = row[dataIdx] ?? '';
          }
          return acc;
        }, {})
      }));

    return {
      sheetName,
      headers,
      agentType,
      nameIndex: nameColumnIndex !== -1 ? nameColumnIndex : 1,
      okpd2Index: okpd2ColumnIndex !== -1 ? okpd2ColumnIndex : 2,
      quantityIndex: quantityColumnIndex,
      rows
    };
  };

  // Для заготовок имя агента = полное описание из "Обозначения" ("Лист стальной 10 мм..."),
  // по нему же ищется ОКПД-2 в blank_okpd2_mapping. Для остальных — "Наименование".
  const getRowIdentity = (sheet, row) => {
    const nameData = String(row.data[sheet.headers[sheet.nameIndex]] ?? '').trim();
    const codeData = String(row.data[sheet.headers[sheet.okpd2Index]] ?? '').trim();
    const name = sheet.agentType === 'blank'
      ? (codeData || nameData || 'Заготовка')
      : (nameData || codeData || 'Новый агент');
    return { name, codeData };
  };

  // Совпадение только по типу и имени: ОКПД-2 не уникален (9 гаечных ключей с одним кодом)
  const getRowStatus = (sheet, row, existing) => {
    if (isTpSheetName(sheet.sheetName)) {
      return existing.tpCodes.has(getTpCode(sheet.sheetName)) ? 'exists' : 'new';
    }
    return existing.keys.has(agentKey(sheet.agentType, getRowIdentity(sheet, row).name)) ? 'exists' : 'new';
  };

  const findBlankOkpd2 = async (blankName, cache) => {
    const key = normalizeName(blankName);
    if (cache.has(key)) return cache.get(key);

    let code = '';
    try {
      const result = await blankOkpd2API.findByBlankName(blankName);
      code = result?.okpd2Code || '';
    } catch (error) {
      console.warn(`[BLANK API] Ошибка поиска ОКПД-2 для "${blankName}":`, error);
    }
    cache.set(key, code);
    return code;
  };

  const createImportContext = (existing, total) => ({
    existing,
    total,
    processed: 0,
    created: 0,
    alreadyExists: 0,
    withoutCode: [],
    failed: [],
    folderIds: new Map(),
    blankCodes: new Map()
  });

  const resolveFolderId = async (folderName, ctx) => {
    if (ctx.folderIds.has(folderName)) return ctx.folderIds.get(folderName);

    let folderId = findBranchIdByName(folderName);
    if (!folderId && onAddNode) {
      setCreationProgress({ current: ctx.processed, total: ctx.total, message: `Создание папки "${folderName}"...` });
      folderId = await onAddNode(folderName);
    }
    if (!folderId) {
      folderId = getUnclassifiedNodeId();
    }
    ctx.folderIds.set(folderName, folderId);
    return folderId;
  };

  const createAgentsForSheet = async (sheet, rows, ctx) => {
    const folderName = getFolderName(sheet);
    const folderId = await resolveFolderId(folderName, ctx);

    for (const row of rows) {
      const { name, codeData } = getRowIdentity(sheet, row);
      const key = agentKey(sheet.agentType, name);
      ctx.processed++;

      if (ctx.existing.keys.has(key)) {
        ctx.alreadyExists++;
        continue;
      }
      if (!folderId) {
        ctx.failed.push(`${name}: не найдена папка "${folderName}"`);
        continue;
      }

      setCreationProgress({
        current: ctx.processed,
        total: ctx.total,
        message: `«${folderName}»: ${name}`
      });

      const okpd2Code = sheet.agentType === 'blank'
        ? await findBlankOkpd2(name, ctx.blankCodes)
        : extractClassifierCode(codeData);
      const finalCode = okpd2Code || UNKNOWN_OKPD2;
      const quantity = sheet.quantityIndex >= 0
        ? parseInt(row.data[sheet.headers[sheet.quantityIndex]], 10) || 1
        : 1;
      // У заготовок имя взято из "Обозначения", а "Наименование" (Круг/Лист/Труба) остается параметром
      const usedIndexes = sheet.agentType === 'blank'
        ? [sheet.okpd2Index, sheet.quantityIndex]
        : [sheet.nameIndex, sheet.okpd2Index, sheet.quantityIndex];

      if (!selectedHostId) {
        ctx.failed.push(`${name}: не выбран Host`);
        continue;
      }

      try {
        await agentAPI.create({
          node_id: folderId,
          host_id: Number(selectedHostId),
          name,
          type: sheet.agentType,
          okpd2_code: finalCode,
          quantity,
          queue_number: Date.now() + ctx.processed,
          parameters: [
            {
              id: 'okpd2_code',
              name: 'Код ОКПД-2',
              unit: '',
              value: finalCode,
              description: 'Общероссийский классификатор продукции по видам экономической деятельности'
            },
            ...collectAdditionalParameters(row, sheet.headers, usedIndexes, sheet.agentType)
          ]
        });
        ctx.existing.keys.add(key);
        ctx.created++;
        if (!okpd2Code) ctx.withoutCode.push(name);
      } catch (error) {
        console.error(`Ошибка создания агента "${name}":`, error);
        ctx.failed.push(`${name}: ${error.message}`);
      }
    }
  };

  const formatImportReport = (ctx) => {
    const lines = [`Агенты: создано ${ctx.created}`];
    if (ctx.alreadyExists) lines.push(`  уже существовали: ${ctx.alreadyExists}`);
    if (ctx.withoutCode.length) {
      lines.push(`  без кода ОКПД-2 (записан ${UNKNOWN_OKPD2}): ${ctx.withoutCode.length}`);
      ctx.withoutCode.slice(0, 5).forEach(name => lines.push(`    • ${name}`));
    }
    if (ctx.failed.length) {
      lines.push(`  ОШИБКИ: ${ctx.failed.length}`);
      ctx.failed.slice(0, 10).forEach(item => lines.push(`    • ${item}`));
      if (ctx.failed.length > 10) lines.push(`    … и еще ${ctx.failed.length - 10} (см. консоль)`);
    }
    return lines.join('\n');
  };

  const finishImport = async (report) => {
    onSave(agents);
    if (onReloadTree) {
      await onReloadTree();
    }
    setIsCreating(false);
    alert(report);
    onClose();
  };

  const collectOkpd2MappingsFromRows = (agentRows, headers, finalNameIndex, finalOkpd2Index, agentType, sheetName) => {
    if (agentType === 'blank') {
      return [];
    }

    const seen = new Set();

    return agentRows.reduce((items, agent) => {
      const nameColumnData = finalNameIndex >= 0 ? String(agent.data[headers[finalNameIndex]] || '').trim() : '';
      const okpd2ColumnData = finalOkpd2Index >= 0 ? String(agent.data[headers[finalOkpd2Index]] || '') : '';
      const code = extractClassifierCode(okpd2ColumnData);

      if (!nameColumnData || !code || code === '00.00.00.000') {
        return items;
      }

      const key = `${code.toLowerCase()}|${nameColumnData.toLowerCase()}`;
      if (seen.has(key)) {
        return items;
      }

      seen.add(key);
      items.push({
        keywords: nameColumnData,
        okpd2Code: code,
        description: `Автоматически добавлено из XLSX, лист "${sheetName}"`
      });

      return items;
    }, []);
  };

  const saveOkpd2Mappings = async (items) => {
    if (!items || items.length === 0) {
      return;
    }

    try {
      const result = await blankOkpd2API.bulkUpsert(items);
      console.log(`[OKPD2 DICTIONARY] Сохранено: ${result.created || 0}, пропущено: ${result.skipped || 0}`);
    } catch (error) {
      console.warn('[OKPD2 DICTIONARY] Bulk endpoint недоступен, пробуем сохранить по одной записи:', error);
      const results = await Promise.allSettled(items.map(item => blankOkpd2API.create(item)));
      const created = results.filter(result => result.status === 'fulfilled').length;
      const failed = results.length - created;
      console.log(`[OKPD2 DICTIONARY] Сохранено по одной записи: ${created}, ошибок: ${failed}`);
    }
  };

  const attachTechProcesses = async (sheetName) => {
    setCreationProgress({
      current: 0,
      total: 0,
      message: sheetName
        ? `Загрузка техпроцесса "${sheetName}" на сервер...`
        : 'Загрузка техпроцессов на сервер (все операции каждого ТП)...'
    });
    return messagesAPI.attachTechProcess(currentFile, sheetName);
  };

  const handleCreateAgentsFromCurrentPage = async () => {
    if (!workbook) {
      alert('Файл не загружен');
      return;
    }

    const sheetName = workbook.SheetNames[currentSheet];

    if (isTpSheetName(sheetName)) {
      setIsCreating(true);
      try {
        const result = await attachTechProcesses(sheetName);
        await finishImport(formatTechProcessResult(result));
      } catch (error) {
        console.error('Ошибка загрузки техпроцесса:', error);
        setIsCreating(false);
        alert(`Ошибка загрузки техпроцесса: ${error.message}`);
      }
      return;
    }

    const newAgents = filteredAgents.filter(agent => agent.status === 'new');
    if (newAgents.length === 0) {
      alert('Нет новых агентов для создания на текущей странице');
      return;
    }

    setIsCreating(true);
    try {
      const sheet = readSheet(workbook, sheetName);
      await saveOkpd2Mappings(
        collectOkpd2MappingsFromRows(agents, sheet.headers, sheet.nameIndex, sheet.okpd2Index, sheet.agentType, sheetName)
      );

      const ctx = createImportContext(collectExistingAgents(treeNodes), newAgents.length);
      await createAgentsForSheet(sheet, newAgents, ctx);

      console.log('[IMPORT] Итог текущей страницы:', ctx);
      await finishImport(`${formatImportReport(ctx)}\n\nПапка: «${getFolderName(sheet)}»`);
    } catch (error) {
      console.error('Ошибка создания агентов:', error);
      setIsCreating(false);
      alert(`Произошла ошибка при создании агентов: ${error.message}`);
    }
  };

  const handleCreateAgentsFromAllSheets = async () => {
    if (!workbook) {
      alert('Файл не загружен');
      return;
    }

    const tpSheetNames = workbook.SheetNames.filter(isTpSheetName);
    const regularSheets = workbook.SheetNames
      .filter(name => !isTpSheetName(name))
      .map(name => readSheet(workbook, name))
      .filter(sheet => sheet && sheet.rows.length > 0);

    const plan = [
      tpSheetNames.length > 0 ? `• ${tpSheetNames.length} техпроцессов → «${TP_FOLDER_NAME}»` : null,
      ...regularSheets.map(sheet => `• ${sheet.rows.length} записей листа «${sheet.sheetName}» → «${getFolderName(sheet)}»`)
    ].filter(Boolean).join('\n');

    if (!window.confirm(`Создать агентов из всего файла?\n\n${plan}`)) {
      return;
    }

    setIsCreating(true);
    const reportParts = [];

    try {
      if (tpSheetNames.length > 0) {
        try {
          const result = await attachTechProcesses();
          reportParts.push(formatTechProcessResult(result));
        } catch (error) {
          console.error('Ошибка загрузки техпроцессов:', error);
          reportParts.push(`Техпроцессы: ОШИБКА — ${error.message}`);
        }
      }

      const totalRows = regularSheets.reduce((sum, sheet) => sum + sheet.rows.length, 0);
      const ctx = createImportContext(collectExistingAgents(treeNodes), totalRows);

      for (const sheet of regularSheets) {
        await saveOkpd2Mappings(
          collectOkpd2MappingsFromRows(sheet.rows, sheet.headers, sheet.nameIndex, sheet.okpd2Index, sheet.agentType, sheet.sheetName)
        );
        await createAgentsForSheet(sheet, sheet.rows, ctx);
      }

      if (regularSheets.length > 0) {
        reportParts.push(formatImportReport(ctx));
      }

      console.log('[IMPORT] Итог всего файла:', ctx);
      await finishImport(`Импорт завершен\n\n${reportParts.join('\n\n')}`);
    } catch (error) {
      console.error('Ошибка создания агентов из всех листов:', error);
      setIsCreating(false);
      alert(`Произошла ошибка при создании агентов из всех листов: ${error.message}`);
    }
  };

  const handleDownloadReport = () => {
    const wb = XLSX.utils.book_new();
    const wsData = [
      columns,
      ...agents.map(agent => [
        ...columns.slice(0, -1).map(col => agent.data[col]),
        agent.status === 'exists' ? 'Уже существует' : 'Новый'
      ])
    ];
    const ws = XLSX.utils.aoa_to_sheet(wsData);
    XLSX.utils.book_append_sheet(wb, ws, 'Отчет');
    XLSX.writeFile(wb, 'agent_report.xlsx');
  };

  const stats = {
    new: agents.filter(a => a.status === 'new').length,
    exists: agents.filter(a => a.status === 'exists').length,
    errors: 0
  };

  if (!currentFile) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h2>Создание агентов из XLSX</h2>
            <p className="modal-subtitle">Загрузите XLSX файл. Система покажет листы и таблицы, затем вы сможете заменить/добавить данные и запустить агентов.</p>
          </div>
          <button className="close-button" onClick={onClose}>×</button>
        </div>

        {loading ? (
          <div className="loading">Загрузка...</div>
        ) : (
          <>
            <div className="controls-section">
              <div className="upload-section">
                <h3>Загрузка XLSX</h3>
                <div className="upload-box">
                  <div className="file-info">
                    <span className="file-icon">📄</span>
                    <span className="file-name">{currentFile.name}</span>
                    <span className="file-status">Файл загружен</span>
                  </div>
                  <div className="upload-buttons">
                    <label htmlFor="file-reload" className="button-upload">
                      Загрузить / Обновить
                    </label>
                    <input
                      type="file"
                      id="file-reload"
                      accept=".xlsx"
                      onChange={handleFileUpload}
                      style={{ display: 'none' }}
                    />
                    <button className="button-clear" onClick={handleClearFile}>
                      Очистить
                    </button>
                  </div>
                </div>
              </div>

              <div className="preview-section">
                <h3>Предпросмотр импортированных листов</h3>
                <div className="preview-box">
                  <div className="pagination-row">
                    <button
                      className="page-button"
                      onClick={() => handleSheetChange(-1)}
                      disabled={currentSheet === 0}
                    >
                      ‹
                    </button>
                    <button
                      className="page-button"
                      onClick={() => handleSheetChange(1)}
                      disabled={!workbook || currentSheet >= workbook.SheetNames.length - 1}
                    >
                      ›
                    </button>
                    <span className="page-info">
                      страница {currentSheet + 1} из {workbook ? workbook.SheetNames.length : 1}
                    </span>
                  </div>
                  <div className="search-row">
                    <input
                      type="text"
                      className="search-input"
                      placeholder="Поиск по таблице..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                    />
                    <select
                      className="filter-select"
                      value={statusFilter}
                      onChange={(e) => setStatusFilter(e.target.value)}
                    >
                      <option value="all">Все</option>
                      <option value="new">Новый</option>
                      <option value="exists">Уже существует</option>
                    </select>
                    <select
                      className="filter-select"
                      value={selectedHostId}
                      onChange={(e) => setSelectedHostId(e.target.value)}
                      title="Host для создаваемых агентов"
                    >
                      <option value="">Host…</option>
                      {hosts.map((h) => (
                        <option key={h.id} value={h.id}>
                          {h.name} ({h.type})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            </div>

            <div className="table-header">
              <h3>Таблица: {workbook ? workbook.SheetNames[currentSheet] : ''}</h3>
            </div>

            <div className="table-wrapper">
              <div className="table-scroll" ref={tableScrollRef}>
                <div className="status-legend">
                  <div className="legend-item">
                    <span className="legend-dot legend-dot-new"></span>
                    <span className="legend-label">Новый</span>
                  </div>
                  <div className="legend-item">
                    <span className="legend-dot legend-dot-exists"></span>
                    <span className="legend-label">Уже существует</span>
                  </div>
                </div>
                <table className="agents-table">
                  <thead>
                    <tr>
                      <th className="status-indicator-header"></th>
                      {columns.map((col, idx) => (
                        <th key={idx}>{col}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {filteredAgents.map((agent) => (
                      <tr key={agent.id}>
                        <td className={`status-indicator ${agent.status === 'new' ? 'status-indicator-new' : 'status-indicator-exists'}`}></td>
                        {columns.map((col, idx) => (
                          <td key={idx}>
                            {agent.status === 'new' && col !== '№' ? (
                              <input
                                type="text"
                                value={agent.data[col] || ''}
                                onChange={(e) => handleCellChange(agent.id, col, e.target.value)}
                                className="cell-input"
                              />
                            ) : (
                              <span className="cell-readonly">{agent.data[col]}</span>
                            )}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="modal-footer">
              <div className="stats">
                Новых записей: {stats.new}, Уже существует: {stats.exists}, Ошибок {stats.errors}
              </div>
              <div className="footer-buttons">
                <button className="button-create" onClick={handleCreateAgentsFromCurrentPage}>
                  Создать агентов с текущей страницы
                </button>
                <button className="button-create-all" onClick={handleCreateAgentsFromAllSheets}>
                  Создать агентов из всего файла
                </button>
                <button className="button-download" onClick={handleDownloadReport}>
                  Скачать отчет
                </button>
              </div>
            </div>
          </>
        )}

        {isCreating && (
          <div className="progress-overlay">
            <div className="progress-container">
              <div className="progress-spinner"></div>
              <div className="progress-message">{creationProgress.message}</div>
              {creationProgress.total > 0 && (
                <div className="progress-counter">
                  {creationProgress.current} / {creationProgress.total}
                </div>
              )}
              {creationProgress.total > 0 && (
                <div className="progress-bar-container">
                  <div
                    className="progress-bar-fill"
                    style={{ width: `${(creationProgress.current / creationProgress.total) * 100}%` }}
                  ></div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default AgentCreationModal;
