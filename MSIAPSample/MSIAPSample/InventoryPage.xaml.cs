using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MSIAPHelper;
using MSIAPSample.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

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
            gvStoreManagedConsumables.ItemsSource = aov.AcvOwnedStoreManagedConsumables;
        }

        private void btnPurchaseNav_Click(object sender, RoutedEventArgs e)
        {
            var navigation = (Application.Current as App).Navigation;
            var purchaseItem = navigation.GetNavigationViewItems(typeof(MSIAPSample.PurchasePage)).First();
            navigation.SetCurrentNavigationViewItem(purchaseItem);
        }

        private async Task<uint> SpendConsumablesPrompt(uint initAmt, string consumableType)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = $"How many {consumableType}s would you like to spend?";
            dialog.PrimaryButtonText = "Spend";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new SpendConsumableUnitsPrompt() { UnitsToSpend = initAmt };
            dialog.XamlRoot = XamlRoot;
            var result = await dialog.ShowAsync();
            var res = (dialog.Content as SpendConsumableUnitsPrompt).UnitsToSpend;
            if (result == ContentDialogResult.Primary)
                return res;
            else
                return 0;
        }

        private async void gridUnmanagedConsumables_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Prompt for number of coins to spend.
            var coinsToSpend = await SpendConsumablesPrompt(10, "Coin");
            if (coinsToSpend == 0)
                return;
            
            // Spend Coins
            var aov = AddOnsView.Instance;
            if (aov.Consumables.Count > 0)
            {
                try
                {
                    await WindowsStoreHelper.SpendConsumable(aov.Consumables[0].storeProduct.StoreId, coinsToSpend);
                    var res = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
                    InventoryAddOnsView.TotalUnmanagedUnits.Total=res.ToString();
                }
                catch (Exception ex)
                {
                    UIHelpers.ShowError(ex.Message);
                }
            }

        }

        private async void gvStoreManagedConsumables_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sp = (StoreProductEx)e.ClickedItem;
            
            // Prompt for number of coins to spend.
            var goldToSpend = await SpendConsumablesPrompt(10, "Gold bar");
            if (goldToSpend == 0)
                return;
            try
            {
                await WindowsStoreHelper.SpendFulfillStoreManagedConsumable(sp.storeProduct.StoreId, goldToSpend);
                var aov = AddOnsView.Instance;
                await aov.UpdateStoreManagedConsumables();
                gvStoreManagedConsumables.ItemsSource = aov.AcvOwnedStoreManagedConsumables;
            }
            catch (Exception ex)
            {
                UIHelpers.ShowError(ex.Message);
            }
        }
    }
}
