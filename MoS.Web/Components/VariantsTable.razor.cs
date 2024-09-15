using Microsoft.AspNetCore.Components;
using MoS.Web.Models;
using MoS.Web.Services;

namespace MoS.Web.Components;

public partial class VariantsTable
{
    private bool _isTableExpanded;

    [Inject]
    private IVariantService VariantService { get; set; } = null!;

    private Dictionary<int, VariantData>? Variants { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Variants = await VariantService.LoadVariantsAsync();
    }

    private void ToggleVariantsTable()
    {
        _isTableExpanded = !_isTableExpanded;
    }
}
