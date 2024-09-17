using Microsoft.AspNetCore.Components;
using MoS.Web.Models;
using MudBlazor;

namespace MoS.Web.Components;

public partial class ResultsDisplay
{
    private MudExpansionPanels? _panels;

    [CascadingParameter]
    public int Decimals { get; set; }

    [Parameter]
    [EditorRequired]
    public required CalculateData CalculateData { get; set; }

    [Parameter]
    [EditorRequired]
    public required CalculateResult CalculateResult { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (_panels != null)
            {
                await _panels.ExpandAllAsync();
            }
        }
    }

    private async Task CopyToClipboard(string value)
    {
        await ClipboardService.CopyToClipboard(value);
    }
}
