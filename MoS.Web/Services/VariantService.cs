using MoS.Web.Models;
using System.Globalization;

namespace MoS.Web.Services;

public interface IVariantService
{
    Task<Dictionary<int, VariantData>> LoadVariantsAsync(CancellationToken cancellationToken = default);
}

public class VariantService(HttpClient httpClient, ILogger<VariantService> logger) : IVariantService
{
    public async Task<Dictionary<int, VariantData>> LoadVariantsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace("Начало загрузки вариантов");
            string fileContent = await httpClient.GetStringAsync("data/variants.txt", cancellationToken);
            return ParseVariants(fileContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке вариантов");
            return new Dictionary<int, VariantData>();
        }
    }

    private static Dictionary<int, VariantData> ParseVariants(string fileContent)
    {
        Dictionary<int, VariantData> variants = new();
        string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ');

            if (parts.Length == 8 && int.TryParse(parts[0], out int key))
            {
                variants[key] = new VariantData(double.Parse(parts[1], CultureInfo.InvariantCulture),
                    double.Parse(parts[2], CultureInfo.InvariantCulture),
                    double.Parse(parts[3], CultureInfo.InvariantCulture),
                    double.Parse(parts[4], CultureInfo.InvariantCulture),
                    double.Parse(parts[5], CultureInfo.InvariantCulture),
                    double.Parse(parts[6], CultureInfo.InvariantCulture),
                    double.Parse(parts[7], CultureInfo.InvariantCulture));
            }
        }

        return variants;
    }
}
