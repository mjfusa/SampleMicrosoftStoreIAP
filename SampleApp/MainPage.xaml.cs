﻿using CommunityToolkit.Mvvm.ComponentModel;
using MSAppStoreHelper;
using System;
using System.Collections.ObjectModel;
using Windows.Services.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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
            //gridRestAPIs.Visibility = Visibility.Collapsed;
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
                                    Subscriptions.Add(product); //TODO StoreProduct not accurable for active / non-active subscription
                                    var res1 = await WindowsStoreHelper.CheckIfUserHasSubscriptionAsync(product.storeProduct.StoreId);
                                }
                                else
                                {
                                    Durables.Add(product);
                                }
                            }
                            break;
                        case "Consumable": // Store managed consumables
                            var result = await StoreContext.GetDefault().GetConsumableBalanceRemainingAsync(product.storeProduct.StoreId);
                            product.storeManagedConsumableRemainingBalance = result.BalanceRemaining;
                            StoreManagedConsumables.Add(product);
                            break;
                        case "UnmanagedConsumable": // Developer managed consumables
                            var BalanceRemaining = await WindowsStoreHelper.GetUnmangedConsumableBalanceRemainingAsync(product.storeProduct.StoreId);
                            product.storeManagedConsumableRemainingBalance = BalanceRemaining;
                            UnmanagedConsumables.Add(product);
                            break;
                        default:
                            ShowError("Unknown IAP ProductType");
                            break;
                    }
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

        private async void FulFillConsumable(StoreProductEx spex)
        {
            try
            {
                var res = await WindowsStoreHelper.FulfillConsumable(spex.storeProduct.StoreId);
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
            UICommand spendCommand = new UICommand("Spend Consumable", cmd =>
            {
                SpendConsumable(sp.storeProduct.StoreId);
            });
            if (sp.storeProduct.ProductKind == "UnmanagedConsumable")
            {
                fulFillCommand = new UICommand("Fulfill Developer managed Consumable", cmd =>
                {
                    FulFillConsumable(sp);
                });
            }
            else if (sp.storeProduct.ProductKind == "Consumable")
            {
                fulFillCommand = new UICommand("Fulfill Store managed Consumable", cmd =>
                {
                    FulFillConsumable(sp);
                });
            }
            var purchaseCommand = new UICommand("Purchase", async cmd =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            });
            var cancelCommand = new UICommand("Cancel", cmd => { return; });

            MessageDialog md = new MessageDialog($"Purchase the {sp.storeProduct.ProductKind} {sp.storeProduct.StoreId}", "Purchase, Fulfill, or Spend Consumable");
            md.Options = MessageDialogOptions.None;
            if ((sp.storeProduct.ProductKind == "UnmanagedConsumable" || sp.storeProduct.ProductKind == "Consumable") && sp.storeProduct.IsInUserCollection)
            {
                md.Commands.Add(fulFillCommand);
                //md.Commands.Add(spendCommand);
            }
            md.Commands.Add(cancelCommand);
            md.Commands.Add(purchaseCommand);
            var res1 = await md.ShowAsync();
            if (res1.Id == null)
            {
                return;
            }


        }

        private void SpendConsumable(string storeId)
        {

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
