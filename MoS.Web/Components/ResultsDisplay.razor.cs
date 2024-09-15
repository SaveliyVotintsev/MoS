using Microsoft.AspNetCore.Components;
using MoS.Web.Models;

namespace MoS.Web.Components;

public partial class ResultsDisplay
{
    [CascadingParameter]
    public int Decimals { get; set; }

    [Parameter]
    [EditorRequired]
    public required CalculateData CalculateData { get; set; }

    [Parameter]
    [EditorRequired]
    public required CalculateResult CalculateResult { get; set; }
}
