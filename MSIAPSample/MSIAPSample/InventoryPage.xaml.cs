using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MSIAPHelper;
using MSIAPSample.Views;
using System.Linq;

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
            //gvUnmanagedConsumables.ItemsSource = aov.Consumables;
            gvStoreManagedConsumables.ItemsSource = aov.AcvOwnedStoreManagedConsumables;

        }

        private void btnPurchaseNav_Click(object sender, RoutedEventArgs e)
        {
            var navigation = (Application.Current as App).Navigation;
            var purchaseItem = navigation.GetNavigationViewItems(typeof(MSIAPSample.PurchasePage)).First();
            navigation.SetCurrentNavigationViewItem(purchaseItem);
            
        }

        private async void gridUnmanagedConsumables_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Prompt for number of coins to spend.

            
            // Spend Coins
            var aov = AddOnsView.Instance;
            if (aov.Consumables.Count > 0)
            {
                var res = MSIAPHelper.WindowsStoreHelper.SpendConsumable(aov.Consumables[0].storeProduct.StoreId, 20);
            }

        }
    }
}
