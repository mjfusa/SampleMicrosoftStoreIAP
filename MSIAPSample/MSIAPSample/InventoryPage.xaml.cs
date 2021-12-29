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
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }


        public AddOnsView InventoryAddOnsView { get => AddOnsView.Instance; }


        private static bool bInitialized = false;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var aov = AddOnsView.Instance;
            if (!bInitialized)
            {
                await aov.UpdateDurables();
                await aov.UpdateConsumables();
                await aov.UpdateStoreManagedConsumables();
                bInitialized = true;
            }

            lvDurables.ItemsSource = aov.AcvOwnedDurables;
            gvSubscriptions.ItemsSource = aov.AcvOwnedSubscriptions;
            gvUnmanagedConsumables.ItemsSource = aov.Consumables;
            gvStoreManagedConsumables.ItemsSource = aov.AcvOwnedStoreManagedConsumables;

        }

        private void btnPurchaseNav_Click(object sender, RoutedEventArgs e)
        {
            var navigation = (Application.Current as App).Navigation;
            var purchaseItem = navigation.GetNavigationViewItems(typeof(MSIAPSample.PurchasePage)).First();
            navigation.SetCurrentNavigationViewItem(purchaseItem);
            
        }
    }
}
