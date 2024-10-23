using System.Text.Json;
using Lavalink4NET.Filters;
using Lavalink4NET.Protocol.Models.Filters;

namespace Lunaris2.Handler.MusicPlayer.PlayCommand;

public class NormalizationFilter : IFilterOptions
{
    private double MaxAmplitude { get; set; }
    private bool Adaptive { get; set; }

    public NormalizationFilter(double maxAmplitude, bool adaptive)
    {
        MaxAmplitude = maxAmplitude;
        Adaptive = adaptive;
    }

    public bool IsDefault => MaxAmplitude == 1.0 && !Adaptive;

    public void Apply(ref PlayerFilterMapModel filterMap)
    {
        filterMap.AdditionalFilters ??= new Dictionary<string, JsonElement>();
        var normalizationFilter = new
        {
            maxAmplitude = MaxAmplitude,
            adaptive = Adaptive
        };
        filterMap.AdditionalFilters["normalization"] = JsonSerializer.SerializeToElement(normalizationFilter);
    }
}