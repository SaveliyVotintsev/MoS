using Microsoft.AspNetCore.Components;
using MoS.Web.Models;
using MoS.Web.Services;

namespace MoS.Web.Components;

public partial class VariantSelector
{
    private const string DefaultVariant = "-1";

    [Parameter]
    [EditorRequired]
    public EventCallback<VariantData> OnVariantSelected { get; set; }

    [Inject]
    private IVariantService VariantService { get; set; } = null!;

    private Dictionary<int, VariantData>? Variants { get; set; }
    private string SelectedVariant { get; set; } = DefaultVariant;

    protected override async Task OnInitializedAsync()
    {
        Variants = await VariantService.LoadVariantsAsync();
    }

    private async Task OnSelectedVariant()
    {
        string variant = SelectedVariant;

        if (string.IsNullOrEmpty(variant))
        {
            return;
        }

        if (Variants == null || Variants.TryGetValue(int.Parse(variant), out VariantData? variantData) == false)
        {
            return;
        }

        await OnVariantSelected.InvokeAsync(variantData);
    }
}
