using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MSIAPHelper;
using MSIAPSample.Views;
using System;
using System.Diagnostics;
using Windows.UI.Popups;
using WinRT;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MSIAPSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PurchasePage : Page
    {
        public AddOnsView PurchaseAddOnsView { get => AddOnsView.Instance; }
        private static bool bInitialized = false;
        public PurchasePage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            lvUnmanagedConsumablesMenuFlyout = Resources["lvUnmanagedConsumablesMenuFlyout"] as MenuFlyout;
        }

    private async void Button_Subs_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStorePurchaseToken(txtPurchaseToken.Text);
            txtMSIDPurchaseToken.Text = res;
        }
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();
        public Status status = new Status();
        MenuFlyout lvUnmanagedConsumablesMenuFlyout;
        
        private async void ShowError(string errorMsg)
        {
            var okCommand = new UICommand("OK", cmd => { return; });
            MessageDialog md = new MessageDialog($"{errorMsg}");

            IInitializeWithWindow initWindow = ((object)md).As<IInitializeWithWindow>();
            var hwnd = (long)System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            initWindow.Initialize(hwnd);

            md.Title = "An error occurred";
            md.Options = MessageDialogOptions.None;
            md.Commands.Add(okCommand);
            await md.ShowAsync();

        }

        private async void Button_GetStoreIdCollections_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStoreCollectionsToken(txtCollectionsToken.Text);
            txtMSIDCollectionsToken.Text = res;
        }

        private async void FulFillConsumable(StoreProductEx spex, uint unitsToSpend)
        {
            try
            {
                var res = await WindowsStoreHelper.FulfillConsumable(spex.storeProduct.StoreId, unitsToSpend);
                if (spex.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
                {
                    await PurchaseAddOnsView.UpdateConsumables();
                }
                if (spex.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
                {
                    await PurchaseAddOnsView.UpdateStoreManagedConsumables();
                }
                status.Text = res;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private async void DoPurchase_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sp = (StoreProductEx)e.ClickedItem;
            UICommand fulFillCommand = null;
            const uint unitsToSpend = 75;
            try
            {
                if (sp.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
                {
                    fulFillCommand = new UICommand($"Spend {unitsToSpend} Units", async cmd =>
                    {
                        try
                        {
                            await WindowsStoreHelper.SpendConsumable(sp.storeProduct.StoreId, unitsToSpend);
                        }
                        catch (Exception ex)
                        {
                            ShowError(ex.Message);
                        }
                        if (sp.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
                        {
                            await PurchaseAddOnsView.UpdateConsumables();
                        }
                        if (sp.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
                        {
                            await PurchaseAddOnsView.UpdateStoreManagedConsumables();
                        }
                    });
                }
                else if (sp.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
                {
                    fulFillCommand = new UICommand($"Spend {unitsToSpend} Units", cmd =>
                    {
                        FulFillConsumable(sp, unitsToSpend);
                    });
                }
                var purchaseCommand = new UICommand("Purchase", async cmd =>
                {
                    var res = await WindowsStoreHelper.Purchase(sp.storeProduct.StoreId);
                    status.Text = res;
                    if (res.Contains("Succeeded"))
                    {
                        switch (sp.storeProduct.ProductKind)
                        {
                            case AddOnKind.StoreManagedConsumable:
                                await PurchaseAddOnsView.UpdateStoreManagedConsumables();
                                break;
                            case AddOnKind.DeveloperManagedConsumable:
                                await PurchaseAddOnsView.UpdateConsumables();
                                break;
                            case AddOnKind.Durable:
                                await PurchaseAddOnsView.UpdateDurables();
                                break;
                            default:
                                Debug.Assert(false, "Unknown ProductKind in Purchase");
                                break;

                        }
                    }
                    else
                    {
                        ShowError(res);
                    }
                });
            var cancelCommand = new UICommand("Cancel", cmd => { return; });

            MessageDialog md = new MessageDialog($"Purchase the {sp.storeProduct.ProductKind} {sp.storeProduct.StoreId}", "Purchase or Spend Consumable units");
            md.Options = MessageDialogOptions.None;
            if ((sp.storeProduct.ProductKind == "Consumable") && (sp.storeProduct.IsInUserCollection))
            {
                if (sp.storeManagedConsumableRemainingBalance > 0)
                {
                    md.Commands.Add(fulFillCommand);
                }
            }
            if (sp.storeProduct.ProductKind == "UnmanagedConsumable" )
            {
                if (sp.developerManagedConsumable.UnmanagedUnitsRemaining > 0)
                {
                    md.Commands.Add(fulFillCommand);
                }
            }

            md.Commands.Add(purchaseCommand);
            md.Commands.Add(cancelCommand);
            IInitializeWithWindow initWindow = ((object)md).As<IInitializeWithWindow>();
            var hwnd = (long)System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            initWindow.Initialize(hwnd);
            var res1 = await md.ShowAsync();
            if (res1.Id == null)
            {
                return;
            }

            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }


        }

        private async void Page_Loaded(object sender, RoutedEventArgs args)
        {
            var aov = AddOnsView.Instance;
            if (!bInitialized)
            {
                WindowsStoreHelper.InitializeStoreContext();
                await aov.UpdateDurables();
                await aov.UpdateConsumables();
                await aov.UpdateStoreManagedConsumables();

                foreach (var menuFlyoutItem in lvUnmanagedConsumablesMenuFlyout.Items)
                {
                    if (menuFlyoutItem.Name == "showAllProperties")
                    {
                        menuFlyoutItem.Tapped += menuFlyoutItem_Tapped;
                    }
                }

                bInitialized = true;
            }

        }

        private void menuFlyoutItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // TODO show all properties of StoreProduct
        }

    }

    public class Status : ObservableObject
    {
        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

    }

    public class UnmanagedUnitsRemaining : ObservableObject
    {
        private string _total;
        private string _totalFormatted;
        public string Total
        {
            get => _total;
            set {
            _total= value;
                TotalFormatted =value;    
            }
        }
        public string TotalFormatted
        {
            get => $"Coin balance: {_total}";
            set => SetProperty(ref _totalFormatted, value);
        }
    }

}
