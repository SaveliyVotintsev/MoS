using Microsoft.AspNetCore.Components;
using MoS.Web.Models;
using MoS.Web.Services;
using MudBlazor;

namespace MoS.Web.Components;

public partial class VariantsTable
{
    private bool _isTableExpanded;

    [Inject]
    private IVariantService VariantService { get; set; } = null!;

    private async Task<TableData<KeyValuePair<int, VariantData>>> ServerReload(TableState state, CancellationToken token)
    {
        Dictionary<int, VariantData> data = await VariantService.LoadVariantsAsync(token);

        return new TableData<KeyValuePair<int, VariantData>>
        {
            Items = data,
            TotalItems = data.Count
        };
    }
}
