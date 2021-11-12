using CommunityToolkit.Mvvm.ComponentModel;
using MSAppStoreHelper;
using System;
using System.Collections.ObjectModel;
using Windows.Services.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //public event PropertyChangedEventHandler PropertyChanged; 
        public MainPage()
        {
            this.InitializeComponent();
            gridRestAPIs.Visibility = Visibility.Collapsed;
        }

        private async void Button_Subs_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStorePurchaseToken(txtPurchaseToken.Text);
            txtMSIDPurchaseToken.Text = res;
        }

        public ObservableCollection<StoreProduct> UnmanagedConsumables = new ObservableCollection<StoreProduct>();
        public ObservableCollection<StoreProduct> StoreManagedConsumables = new ObservableCollection<StoreProduct>();
        public ObservableCollection<StoreProduct> Durables = new ObservableCollection<StoreProduct>();
        public ObservableCollection<StoreProduct> Subscriptions = new ObservableCollection<StoreProduct>();

        public Status status = new Status();

        
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetIAP();
        }

        private async void GetIAP()
        {
            Durables.Clear(); 
            UnmanagedConsumables.Clear();  
            StoreManagedConsumables.Clear();   
            Subscriptions.Clear();

            try
            {
                var storeProductQueryResultDurables = await WindowsStoreHelper.GetDurableAddOns();
                foreach (StoreProduct product in storeProductQueryResultDurables.Products.Values)
                {
                    // Get subscriptions and durables
                    foreach (var s in product.Skus)
                    {
                        if (s.IsSubscription)
                        {
                            Subscriptions.Add(product);
                            var res = await WindowsStoreHelper.CheckIfUserHasSubscriptionAsync(product.StoreId);
                        }
                        else
                        {
                            Durables.Add(product);
                        }
                    }
                }
                var storeProductQueryResultConsumables = await WindowsStoreHelper.GetUnmanagedConsumableAddOns();
                foreach (StoreProduct product in storeProductQueryResultConsumables.Products.Values)
                {
                    UnmanagedConsumables.Add(product);
                }
                storeProductQueryResultConsumables = await WindowsStoreHelper.GetStoreManagedConsumableAddOns();
                foreach (StoreProduct product in storeProductQueryResultConsumables.Products.Values)
                {
                    StoreManagedConsumables.Add(product);
                    StoreProductEx spe = new StoreProductEx(product);
                    var result = await StoreContext.GetDefault().GetConsumableBalanceRemainingAsync(product.StoreId);
                    spe.storeManagedConsumableRemainingBalance = result.BalanceRemaining;
                }
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

        private async void FulFillConsumable(string StoreId)
        {
            try
            {
                var res = await WindowsStoreHelper.FulfillConsumable(StoreId);
                GetIAP();

                status.Text = res;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void RefreshIAP()
        {
            UnmanagedConsumables.Clear();


        }

        private async void DoPurchase_ItemClick(object sender, ItemClickEventArgs e)
        {
            StoreProduct sp = (StoreProduct)e.ClickedItem;

            var cancelCommand = new UICommand("Cancel", cmd => { return; });
            UICommand fulFillCommand = null;
            if ( (sp.ProductKind == "UnmanagedConsumable" || sp.ProductKind == "Consumable") && sp.IsInUserCollection)
            {
                if (sp.ProductKind == "UnmanagedConsumable") {
                    fulFillCommand = new UICommand("Fulfill Developer managed Consumable", cmd => {
                        FulFillConsumable(sp.StoreId);
                    });
                } else
                {
                    fulFillCommand = new UICommand("Fulfill Store managed Consumable", cmd => {
                        FulFillConsumable(sp.StoreId);
                    });
                }
            }
            var purchaseCommand = new UICommand("Purchase", async cmd =>
            {
                try
                {
                    var res = await WindowsStoreHelper.Purchase(sp.StoreId);
                    status.Text = res;
                } catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            });
            MessageDialog md = new MessageDialog($"Purchase or fulfill the {sp.ProductKind} {sp.StoreId}");
            md.Options = MessageDialogOptions.None;
            if ((sp.ProductKind == "UnmanagedConsumable" || sp.ProductKind == "Consumable") && sp.IsInUserCollection)
            {
                md.Commands.Add(fulFillCommand);
            }
            md.Commands.Add(purchaseCommand);
            md.Commands.Add(cancelCommand);
            await md.ShowAsync();

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

    public class StoreProductEx
    {
        public StoreProductEx( StoreProductEx product)
        {
            _storeProduct = product._storeProduct;
        }
        public StoreProductEx(StoreProduct product)
        {
            _storeProduct = product;
        }
        public StoreProduct _storeProduct { get; set; }
        public uint storeManagedConsumableRemainingBalance {get;set;}
        
        public static implicit operator StoreProduct(StoreProductEx self)
        {
            return self._storeProduct;
        }
    }
}
