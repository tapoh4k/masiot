using System;

namespace Backend.Services.WorkflowFunctions;

/// <summary>
/// Примеры функций для тестирования workflow
/// Эти функции можно вызывать из workflow
/// </summary>
public class ExampleWorkflowFunctions
{
    private readonly ILogger<ExampleWorkflowFunctions> _logger;

    public ExampleWorkflowFunctions(ILogger<ExampleWorkflowFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Простая функция приветствия
    /// Параметры: name (string)
    /// Возвращает: greeting (string)
    /// </summary>
    public string SayHello(string name)
    {
        _logger.LogInformation("Executing SayHello for {Name}", name);
        var greeting = $"Привет, {name}!";
        Console.WriteLine(greeting);
        return greeting;
    }

    /// <summary>
    /// Функция сложения двух чисел
    /// Параметры: a (int), b (int)
    /// Возвращает: sum (int)
    /// </summary>
    public int Add(int a, int b)
    {
        _logger.LogInformation("Executing Add: {A} + {B}", a, b);
        var sum = a + b;
        Console.WriteLine($"Результат: {a} + {b} = {sum}");
        return sum;
    }

    /// <summary>
    /// Функция умножения
    /// Параметры: x (int), y (int)
    /// Возвращает: product (int)
    /// </summary>
    public int Multiply(int x, int y)
    {
        _logger.LogInformation("Executing Multiply: {X} * {Y}", x, y);
        var product = x * y;
        Console.WriteLine($"Результат: {x} * {y} = {product}");
        return product;
    }

    /// <summary>
    /// Функция логирования сообщения
    /// Параметры: message (string)
    /// Возвращает: timestamp (string)
    /// </summary>
    public string LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _logger.LogInformation("LogMessage: {Message} at {Timestamp}", message, timestamp);
        Console.WriteLine($"[{timestamp}] {message}");
        return timestamp;
    }

    /// <summary>
    /// Симуляция проверки материала
    /// Параметры: materialId (string)
    /// Возвращает: available (string) - "yes" или "no"
    /// </summary>
    public string CheckMaterial(string materialId)
    {
        _logger.LogInformation("Checking material {MaterialId}", materialId);

        // Симуляция проверки (в реальности - запрос к БД или внешней системе)
        var available = materialId.Length > 3 ? "yes" : "no";

        Console.WriteLine($"Материал {materialId}: {(available == "yes" ? "доступен" : "недоступен")}");
        return available;
    }

    /// <summary>
    /// Симуляция начала обработки детали
    /// Параметры: partName (string), machineId (string)
    /// Возвращает: processId (string)
    /// </summary>
    public string StartProcessing(string partName, string machineId)
    {
        var processId = $"PROC-{DateTime.Now.Ticks}";
        _logger.LogInformation("Starting processing {PartName} on machine {MachineId}, ProcessId: {ProcessId}",
            partName, machineId, processId);

        Console.WriteLine($"Начата обработка детали '{partName}' на станке '{machineId}'");
        Console.WriteLine($"ID процесса: {processId}");

        return processId;
    }

    /// <summary>
    /// Симуляция отправки уведомления
    /// Параметры: recipient (string), message (string)
    /// Возвращает: status (string) - "sent"
    /// </summary>
    public string SendNotification(string recipient, string message)
    {
        _logger.LogInformation("Sending notification to {Recipient}: {Message}", recipient, message);

        Console.WriteLine($"📧 Уведомление отправлено: {recipient}");
        Console.WriteLine($"   Сообщение: {message}");

        return "sent";
    }

    /// <summary>
    /// Функция задержки (для тестирования длительных процессов)
    /// Параметры: seconds (int)
    /// Возвращает: elapsed (string)
    /// </summary>
    public string Wait(int seconds)
    {
        _logger.LogInformation("Waiting for {Seconds} seconds", seconds);
        Console.WriteLine($"⏳ Ожидание {seconds} секунд...");

        System.Threading.Thread.Sleep(seconds * 1000);

        Console.WriteLine($"✓ Ожидание завершено");
        return $"{seconds}s";
    }
}
