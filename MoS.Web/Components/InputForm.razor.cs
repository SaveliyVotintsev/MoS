using MoS.Web.Models;

namespace MoS.Web.Components;

public partial class InputForm
{
    private double k11 { get; set; }
    private double k21 { get; set; }
    private double k31 { get; set; }
    private double k41 { get; set; }
    private double k51 { get; set; }
    private double T31 { get; set; }
    private double T41 { get; set; }

    public VariantData GetData()
    {
        return new VariantData(k11, k21, k31, k41, k51, T31, T41);
    }

    public void SetData(VariantData data)
    {
        (k11, k21, k31, k41, k51, T31, T41) = data;
        StateHasChanged();
    }
}
