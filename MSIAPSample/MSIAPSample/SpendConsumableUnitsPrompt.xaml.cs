using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MSIAPSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SpendConsumableUnitsPrompt : Page
    {
        [DefaultValue(1)]
        public uint UnitsToSpend { get; set; }
        [DefaultValue("Coin")]
        public string UnitsName { get; set; }
        public SpendConsumableUnitsPrompt()
        {
            this.InitializeComponent();

        }
    }
}
