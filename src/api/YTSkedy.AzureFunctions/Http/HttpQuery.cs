using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace YTSkedy.AzureFunctions.Http;

internal static class HttpQuery
{
    internal static bool TryGetSingleValue(
        HttpRequest request,
        string name,
        out string? value,
        out IActionResult error)
    {
        value = null;
        error = new EmptyResult();

        if (!request.Query.TryGetValue(name, out var values) || values.Count == 0)
        {
            return true;
        }

        if (values.Count > 1)
        {
            error = new BadRequestObjectResult(
                $"Query parameter '{name}' must have a single value.");
            return false;
        }

        var rawValue = values[0];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' must not be empty.");
            return false;
        }

        value = rawValue;
        return true;
    }

    internal static bool TryParseRequiredInt(
        HttpRequest request,
        string name,
        out int value,
        out IActionResult error)
    {
        value = 0;

        if (!TryGetSingleValue(request, name, out var rawValue, out error))
        {
            return false;
        }

        if (rawValue is null)
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' is required.");
            return false;
        }

        if (!int.TryParse(
                rawValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value))
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' must be an integer.");
            return false;
        }

        return true;
    }

    internal static bool TryParseOptionalInt(
        HttpRequest request,
        string name,
        out int value,
        out bool hasValue,
        out IActionResult error,
        string invalidMessage)
    {
        value = 0;
        hasValue = false;

        if (!TryGetSingleValue(request, name, out var rawValue, out error))
        {
            return false;
        }

        if (rawValue is null)
        {
            return true;
        }

        hasValue = true;
        if (!int.TryParse(
                rawValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value))
        {
            error = new BadRequestObjectResult(invalidMessage);
            return false;
        }

        return true;
    }

    internal static bool TryValidateRange(
        string name,
        int value,
        int minValue,
        int maxValue,
        out IActionResult error)
    {
        error = new EmptyResult();

        if (value >= minValue && value <= maxValue)
        {
            return true;
        }

        error = new BadRequestObjectResult(
            $"Query parameter '{name}' must be between {minValue} and {maxValue}.");
        return false;
    }
}
