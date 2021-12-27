using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MSIAPHelper;
using MSIAPSample.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MSIAPSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InventoryPage : Page
    {
        public InventoryPage()
        {
            this.InitializeComponent();
        }
        //private InventoryView inventoryViewPage = new InventoryView();

        public InventoryView InventoryViewPage = new InventoryView();//{ get => inventoryViewPage; set => inventoryViewPage = value; }
        public AddOnsView AddOnsViewPage = new AddOnsView();//{ get => inventoryViewPage; set => inventoryViewPage = value; }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InventoryViewPage.Initialize();
            await AddOnsViewPage.Initialize();
            lvDurables.ItemsSource = AddOnsViewPage.AcvOwnedDurables;
        }

    }
}
