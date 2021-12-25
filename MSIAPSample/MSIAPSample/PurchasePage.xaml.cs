using Microsoft.Toolkit.Mvvm;
using MSIAPHelper;
using System;
using System.Collections.ObjectModel;
using Windows.Services.Store;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WinRT;    
using WinRT.Interop;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MSIAPSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PurchasePage : Page
    {
        //public event PropertyChangedEventHandler PropertyChanged; 
        public PurchasePage()
        {
            this.InitializeComponent();
            lvUnmanagedConsumablesMenuFlyout = Resources["lvUnmanagedConsumablesMenuFlyout"] as MenuFlyout;
        }

    private async void Button_Subs_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStorePurchaseToken(txtPurchaseToken.Text);
            txtMSIDPurchaseToken.Text = res;
        }
        public ObservableCollection<StoreProductEx> UnmanagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> StoreManagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> Durables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> Subscriptions = new ObservableCollection<StoreProductEx>();
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();
        public Status status = new Status();
        private bool mainWindowActivated=false;
        MenuFlyout lvUnmanagedConsumablesMenuFlyout;
        private async void GetIAP()
        {
            Durables.Clear();
            UnmanagedConsumables.Clear();
            StoreManagedConsumables.Clear();
            Subscriptions.Clear();

            try
            {
                var storeProductQueryResultDurables = await WindowsStoreHelper.GetAllAddOns();
                foreach (StoreProductEx product in storeProductQueryResultDurables.Values)
                {
                    switch (product.storeProduct.ProductKind)
                    {
                        case "Durable":
                            foreach (var s in product.storeProduct.Skus)
                            {
                                if (s.IsSubscription)
                                {
                                    await WindowsStoreHelper.CheckIfUserHasSubscriptionAsync(product.storeProduct.StoreId);
                                    Subscriptions.Add(product); 
                                }
                                else
                                {
                                    Durables.Add(product);
                                }
                            }
                            break;
                        case "Consumable": // Store managed consumables
                            var result = await StoreContext.GetDefault().GetConsumableBalanceRemainingAsync(product.storeProduct.StoreId); // TODO Move to StoreHelper
                            product.storeManagedConsumableRemainingBalance = result.BalanceRemaining;
                            StoreManagedConsumables.Add(product);
                            break;
                        case "UnmanagedConsumable": // Developer managed consumables
                            UnmanagedConsumables.Add(product);
                            break;
                        default:
                            ShowError("Unknown IAP ProductType");
                            break;
                    }
                }
                var balResult = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
                TotalUnmanagedUnits.Total = balResult.ToString();
                
            }
            catch (Exception err)
            {
                ShowError(err.Message);
            }
        }

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
                GetIAP();
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
                        GetIAP();
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
                        GetIAP();
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

        private void Page_Loaded(object sender, RoutedEventArgs args)
        {
            if (!mainWindowActivated)
            {
                WindowsStoreHelper.InitializeStoreContext();
                GetIAP();
                mainWindowActivated = true;
                foreach (var menuFlyoutItem in lvUnmanagedConsumablesMenuFlyout.Items)
                {
                    if (menuFlyoutItem.Name == "showAllProperties")
                    {
                        menuFlyoutItem.Tapped += menuFlyoutItem_Tapped;
                    }
                }
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
