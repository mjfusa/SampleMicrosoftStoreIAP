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
            restAPIs.Visibility = Visibility.Collapsed;
        }

        private async void Button_Subs_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStorePurchaseToken(txtPurchaseToken.Text);
            txtMSIDPurchaseToken.Text = res;
        }

        public ObservableCollection<StoreProduct> Consumables = new ObservableCollection<StoreProduct>();
        public ObservableCollection<StoreProduct> Durables = new ObservableCollection<StoreProduct>();
        public ObservableCollection<StoreProduct> Subscriptions = new ObservableCollection<StoreProduct>();

        public Status status = new Status();

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
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
                var storeProductQueryResultConsumables = await WindowsStoreHelper.GetConsumableAddOns();
                foreach (StoreProduct product in storeProductQueryResultConsumables.Products.Values)
                {
                    Consumables.Add(product);
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

        private async void DoPurchase_ItemClick(object sender, ItemClickEventArgs e)
        {
            StoreProduct sp = (StoreProduct)e.ClickedItem;

            var noCommand = new UICommand("No", cmd => { return; });
            var yesCommand = new UICommand("Yes", async cmd =>
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
            MessageDialog md = new MessageDialog($"Purchase the {sp.ProductKind} {sp.StoreId}?");
            md.Options = MessageDialogOptions.None;
            md.Commands.Add(yesCommand);
            md.Commands.Add(noCommand);
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

}
