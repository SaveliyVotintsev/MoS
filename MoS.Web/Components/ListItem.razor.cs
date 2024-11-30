using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MoS.Web.Services;
using MudBlazor;

namespace MoS.Web.Components;

public class ListItem<T> : MudListItem<T>
{
    [Inject]
    private IClipboardService ClipboardService { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        OnClick = EventCallback.Factory.Create<MouseEventArgs>(this, async () =>
        {
            string? text = Value?.ToString();
            Console.WriteLine("OnClick = " + ChildContent);

            if (text != null)
            {
                await ClipboardService.CopyToClipboard(text);
            }
        });
    }
}
