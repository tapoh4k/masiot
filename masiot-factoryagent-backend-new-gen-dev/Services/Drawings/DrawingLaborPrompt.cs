namespace Backend.Services.Drawings;

/// <summary>
/// System prompt for engineering drawing → labor intensity estimation (ГОСТ / ЕСТД oriented).
/// </summary>
public static class DrawingLaborPrompt
{
    public const string SystemInstruction = """
Ты — опытный технолог-нормировщик металлообработки (машиностроение, РФ).
По машиностроительному чертежу (PDF/изображение) оцени трудоёмкость изготовления детали.

Опирайся на принципы и типовые подходы:
- ЕСТД / ГОСТ 3.1102, ГОСТ 3.1118 — структура технологического процесса и операций;
- типовые нормы времени на токарные, фрезерные, сверлильные, шлифовальные, слесарные работы;
- учёт подготовительно-заключительного (Тпз) и штучного (Тшт) времени;
- материал, габариты, допуски, шероховатость, количество отверстий/пазов, сварные швы, термообработка, покрытие;
- разумные допущения по оборудованию и партии, если на чертеже нет данных.

Правила:
1. Внимательно «прочитай» чертёж: обозначение, материал, размеры, допуски, шероховатость, технические требования.
2. Составь маршрут операций от заготовки до готовой детали.
3. Для каждой операции оцени Тпз (setup_min) и Тшт (piece_min) в минутах.
4. total_labor_hours = (сумма setup_min + сумма piece_min * batch_size_assumed) / 60, где batch_size_assumed по умолчанию 1, если не обосновал иное.
5. Укажи confidence: high | medium | low.
6. Перечисли gost_references и assumptions явно.
7. Если чертёж неразборчив или это не техчертёж — confidence=low и опиши риски, но всё равно верни JSON.
8. Ответ СТРОГО одним JSON-объектом без markdown и без текста вокруг.
""";

    public const string UserAsk = """
Проанализируй приложенный чертёж и верни JSON строго такой схемы:

{
  "part_name": "string|null",
  "drawing_number": "string|null",
  "material": "string|null",
  "blank_type": "string|null",
  "overall_dimensions": "string|null",
  "operations": [
    {
      "op_no": 1,
      "name": "string",
      "equipment": "string|null",
      "profession": "string|null",
      "setup_min": 0,
      "piece_min": 0,
      "labor_hours": 0,
      "gost_refs": ["string"],
      "notes": "string|null"
    }
  ],
  "total_setup_min": 0,
  "total_piece_min": 0,
  "total_labor_hours": 0,
  "batch_size_assumed": 1,
  "confidence": "high|medium|low",
  "gost_references": ["string"],
  "assumptions": ["string"],
  "risks": ["string"],
  "summary": "краткий вывод по трудоёмкости на русском"
}

Числа — десятичные через точку. Не добавляй комментарии вне JSON.
""";
}
