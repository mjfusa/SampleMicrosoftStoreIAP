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
        
        private async void Button_GetStoreIdCollections_Click(object sender, RoutedEventArgs e)
        {
#if USE_LOCAL
            var res = await WindowsStoreHelper.GetMSStoreCollectionsToken(txtCollectionsToken.Text);
#else
            var res = await WindowsStoreHelper.GetMSStoreCollectionsToken();
#endif
            txtMSIDCollectionsToken.Text = res;
        }

        private async void DoPurchase_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sp = (StoreProductEx)e.ClickedItem;
            try
            {
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
                        UIHelpers.ShowError(res);
                    }
                });
            var cancelCommand = new UICommand("Cancel", cmd => { return; });

            MessageDialog md = new MessageDialog($"{sp.storeProduct.ProductKind} Purchase", $"Purchase {sp.storeProduct.Title}");
            md.Options = MessageDialogOptions.None;
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
                UIHelpers.ShowError(ex.Message);
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
