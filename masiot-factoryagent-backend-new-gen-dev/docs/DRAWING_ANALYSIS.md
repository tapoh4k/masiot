# Анализ чертежей → трудоёмкость

На главной странице рядом с XLSX есть блок **«Загрузка чертежей»**.  
Файл уходит на `POST /api/drawings/analyze`. Backend вызывает vision-модель и возвращает оценку трудоёмкости (ЕСТД / ГОСТ-ориентированный промпт).

## Рекомендуется: OrcaRouter → GLM 5.3 Flash (бесплатно)

1. Зарегистрируйся на https://www.orcarouter.ai/  
2. Возьми API key в кабинете (для free часто нужен GitHub в профиле).  
3. В приложении: **Настройки → Нейросеть** → провайдер **OrcaRouter**, модель `z-ai/glm-5.3-flash-free`, вставь ключ → Сохранить.

Endpoint: `https://api.orcarouter.ai/v1/chat/completions` (OpenAI-compatible).  
Модель: https://www.orcarouter.ai/models/z-ai/glm-5.3-flash-free — vision, $0, rate-limited.

## Альтернатива: Google Gemini

Провайдер **Gemini** в тех же настройках, ключ с https://aistudio.google.com/apikey , модель `gemini-2.5-flash`.

## Форматы

PDF, PNG, JPG, WEBP, TIFF, BMP.  
DWG/DXF — сначала экспорт в PDF.

## API

- `POST /api/drawings/analyze` — multipart `file`
- `GET /api/drawings` — история
- `GET /api/drawings/{id}` — один результат

Результаты пишутся в таблицу `drawing_analyses`.
