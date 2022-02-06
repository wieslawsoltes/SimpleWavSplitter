using Avalonia.Web.Blazor;

namespace SimpleWavSplitter.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<SimpleWavSplitter.App>()
            .SetupWithSingleViewLifetime();
    }
}
