using System.Reflection;
using System.Windows.Controls;

namespace D2RSaveVault.Views;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();

        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = info?.Split('+', 2)[0];
        if (string.IsNullOrWhiteSpace(version))
            version = asm.GetName().Version?.ToString(3);

        VersionText.Text = $"Version {version ?? "0.0.0"}";
    }
}
