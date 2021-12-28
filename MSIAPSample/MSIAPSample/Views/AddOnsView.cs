using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Data;
using MSIAPHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;

namespace MSIAPSample.Views
{
    public static class AddOnsView
    {
        private static ObservableCollection<StoreProductEx> durables = new ObservableCollection<StoreProductEx>();
        private static ObservableCollection<StoreProductEx> subscriptions = new ObservableCollection<StoreProductEx>();
        private static ObservableCollection<StoreProductEx> consumables = new ObservableCollection<StoreProductEx>();
        private static UnmanagedUnitsRemaining totalUnmanagedUnits = new UnmanagedUnitsRemaining();
        private static ObservableCollection<StoreProductEx> storeManagedConsumables = new ObservableCollection<StoreProductEx>();

        public static bool bInitialized = false;

        private static AdvancedCollectionView acvOwnedDurables;
        private static AdvancedCollectionView acvOwnedSubscriptions;
        private static AdvancedCollectionView acvOwnedStoreManagedConsumables;
        public static AdvancedCollectionView AcvOwnedDurables
        {
            get => acvOwnedDurables;

            set => acvOwnedDurables = value;
        }
        public static AdvancedCollectionView AcvOwnedSubscriptions
        {
            get => acvOwnedSubscriptions;

            set => acvOwnedSubscriptions = value;
        }
        public static AdvancedCollectionView AcvOwnedStoreManagedConsumables { get => acvOwnedStoreManagedConsumables; set => acvOwnedStoreManagedConsumables = value; }

        public static ObservableCollection<StoreProductEx> Durables { get => durables; set => durables = value; }
        public static ObservableCollection<StoreProductEx> Subscriptions { get => subscriptions; set => subscriptions = value; }
        public static ObservableCollection<StoreProductEx> Consumables { get => consumables; set => consumables = value; }
        public static UnmanagedUnitsRemaining TotalUnmanagedUnits { get => totalUnmanagedUnits; set => totalUnmanagedUnits = value; }
        public static ObservableCollection<StoreProductEx>  StoreManagedConsumables { get => storeManagedConsumables; set => storeManagedConsumables = value; }

        public async static Task<bool> UpdateStoreManagedConsumables()
        {
            
            var sManagedConsumables = await WindowsStoreHelper.GetStoreManagedConsumablesAsync();
            
            foreach (var s in sManagedConsumables.Values)
            {
                if (!StoreManagedConsumables.Contains(s)) 
                { 
                    StoreManagedConsumables.Add(s);
                }
            }

            return true;
        }
        public static async Task<bool> UpdateConsumables()
        {
            var devManagedConsumables = await WindowsStoreHelper.GetConsumablesAsync();
            foreach (var s in devManagedConsumables.Values)
            {
                // Note these should not be added to inventory page. They will appear if that they have not been fulfilled.
                // In this sample, consumables are immediately fulfiled.
                if (!Consumables.Contains(s))
                {
                    if (s.storeProduct.IsInUserCollection)
                    {
                        Consumables.Add(s);
                    }
                }
            }
            var balResult = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
            TotalUnmanagedUnits.Total = balResult.ToString();

            return true;
        }
        public static async Task<bool> UpdateDurables()
        {
            var durables = await WindowsStoreHelper.GetAllDurables();
            foreach (var d in durables)
            {
                if (d.Value.storeProduct.Skus[0].IsSubscription)
                {
                    if (!Subscriptions.Contains(d.Value))
                    {
                        Subscriptions.Add(d.Value);
                        var res = await WindowsStoreHelper.CheckIfSubscriptionIsInUserCollection(d.Value.storeProduct.StoreId);
                        if (true == res)
                        {
                            d.Value.SubscriptionIsInUserCollection = true;
                        }
                    }
                } else
                {
                    if (!Durables.Contains(d.Value))
                    {
                        Durables.Add(d.Value);
                    }
                }

            }


            AcvOwnedDurables = new AdvancedCollectionView(Durables);
            AcvOwnedDurables.Filter = x => ((StoreProductEx)x).storeProduct.IsInUserCollection == true;

            AcvOwnedSubscriptions = new AdvancedCollectionView(Subscriptions);
            AcvOwnedSubscriptions.Filter = x => ((StoreProductEx)x).SubscriptionIsInUserCollection == true;

            AcvOwnedStoreManagedConsumables = new AdvancedCollectionView(storeManagedConsumables);
            AcvOwnedStoreManagedConsumables.Filter = x => ((StoreProductEx)x).storeProduct.IsInUserCollection == true;

            return true;
        }
    }
}

