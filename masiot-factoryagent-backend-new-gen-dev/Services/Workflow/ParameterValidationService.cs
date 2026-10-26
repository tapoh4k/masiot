using System.Globalization;
using Backend.DTOs;

namespace Backend.Services.Workflow;

public interface IParameterValidationService
{
    ValidationResult ValidateParameters(List<WorkflowParameterDto> parameterDefinitions, Dictionary<string, object> parameterValues);
    object? ConvertParameter(string parameterName, string parameterType, object value);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ParameterValidationService : IParameterValidationService
{
    public ValidationResult ValidateParameters(List<WorkflowParameterDto> parameterDefinitions, Dictionary<string, object> parameterValues)
    {
        var result = new ValidationResult { IsValid = true };

        foreach (var paramDef in parameterDefinitions)
        {
            if (!parameterValues.ContainsKey(paramDef.Name))
            {
                result.IsValid = false;
                result.Errors.Add($"Параметр '{paramDef.Name}' отсутствует");
                continue;
            }

            var value = parameterValues[paramDef.Name];
            var convertedValue = ConvertParameter(paramDef.Name, paramDef.Type, value);

            if (convertedValue == null && value != null)
            {
                result.IsValid = false;
                result.Errors.Add($"Параметр '{paramDef.Name}' имеет неверный тип. Ожидается: {paramDef.Type}");
            }
        }

        return result;
    }

    public object? ConvertParameter(string parameterName, string parameterType, object value)
    {
        if (value == null)
            return null;

        try
        {
            return parameterType.ToLower() switch
            {
                "int" => ConvertToInt(value),
                "string" => value.ToString(),
                "bool" => ConvertToBool(value),
                "float" or "double" => ConvertToDouble(value),
                "date" => ConvertToDate(value),
                _ => value
            };
        }
        catch
        {
            return null;
        }
    }

    private int ConvertToInt(object value)
    {
        if (value is int intValue)
            return intValue;
        if (value is long longValue)
            return Convert.ToInt32(longValue);
        if (value is string strValue && int.TryParse(strValue, out var parsed))
            return parsed;

        throw new InvalidCastException();
    }

    private bool ConvertToBool(object value)
    {
        if (value is bool boolValue)
            return boolValue;
        if (value is string strValue && bool.TryParse(strValue, out var parsed))
            return parsed;

        throw new InvalidCastException();
    }

    private double ConvertToDouble(object value)
    {
        if (value is double doubleValue)
            return doubleValue;
        if (value is float floatValue)
            return floatValue;
        if (value is int intValue)
            return intValue;
        if (value is long longValue)
            return longValue;
        if (value is string strValue && double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new InvalidCastException();
    }

    private DateTime ConvertToDate(object value)
    {
        if (value is DateTime dateValue)
            return dateValue;
        if (value is string strValue && DateTime.TryParse(strValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        throw new InvalidCastException();
    }
}
