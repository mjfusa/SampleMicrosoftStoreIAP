using CommunityToolkit.WinUI.UI;
using Microsoft.StoreServices;
using MSIAPHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MSIAPSample.Views
{
    public class AddOnsView
    {
        private static readonly Lazy<AddOnsView> lazy =
                new Lazy<AddOnsView>(() => new AddOnsView());
        public static AddOnsView Instance { get { return lazy.Value; } }
        private AddOnsView()
        {

        }

        private ObservableCollection<CollectionsItem> AllProducts = new ObservableCollection<CollectionsItem>();
        private ObservableCollection<StoreProductEx> durables = new ObservableCollection<StoreProductEx>();
        private static ObservableCollection<StoreProductEx> subscriptions = new ObservableCollection<StoreProductEx>();
        private static ObservableCollection<StoreProductEx> consumables = new ObservableCollection<StoreProductEx>();
        private UnmanagedUnitsRemaining totalUnmanagedUnits = new UnmanagedUnitsRemaining();
        private static ObservableCollection<StoreProductEx> storeManagedConsumables = new ObservableCollection<StoreProductEx>();

        public bool bInitialized = false;

        private AdvancedCollectionView acvOwnedDurables;
        private AdvancedCollectionView acvOwnedSubscriptions;
        private AdvancedCollectionView acvOwnedStoreManagedConsumables;
        public AdvancedCollectionView AcvOwnedDurables
        {
            get => acvOwnedDurables;
            set => acvOwnedDurables = value;
        }
        public AdvancedCollectionView AcvOwnedSubscriptions
        {
            get => acvOwnedSubscriptions;
            set => acvOwnedSubscriptions = value;
        }
        public  AdvancedCollectionView AcvOwnedStoreManagedConsumables { get => acvOwnedStoreManagedConsumables; set => acvOwnedStoreManagedConsumables = value; }

        public ObservableCollection<StoreProductEx> Durables { get => durables; set => durables = value; }
        public ObservableCollection<StoreProductEx> Subscriptions { get => subscriptions; set => subscriptions = value; }
        public ObservableCollection<StoreProductEx> Consumables { get => consumables; set => consumables = value; }
        public  UnmanagedUnitsRemaining TotalUnmanagedUnits { get => totalUnmanagedUnits; set => totalUnmanagedUnits = value; }
        public ObservableCollection<StoreProductEx>  StoreManagedConsumables { get => storeManagedConsumables; set => storeManagedConsumables = value; }

        public async Task<bool> UpdateStoreManagedConsumablesService()
        {
            var sManagedConsumables = await WindowsStoreHelper.GetProductsFromService();
            foreach (var s in sManagedConsumables.Items)
            {
                AllProducts.Add(s);
            }


            return true;
        }
    public async Task<bool> UpdateStoreManagedConsumables()
        {
            var sManagedConsumables = await WindowsStoreHelper.GetStoreManagedConsumablesAsync();

            List<StoreProductEx> updateSP = null;
            foreach (var s in sManagedConsumables.Values)
            {
                if (!StoreManagedConsumables.Contains(s))
                {
                    StoreManagedConsumables.Add(s);
                } 
            }

            AcvOwnedStoreManagedConsumables = new AdvancedCollectionView(storeManagedConsumables);
            AcvOwnedStoreManagedConsumables.Filter = x => ((StoreProductEx)x).storeProduct.IsInUserCollection == true;

            return true;
        }
        public async Task<bool> UpdateConsumables()
        {
            var devManagedConsumables = await WindowsStoreHelper.GetConsumablesAsync();
            foreach (var s in devManagedConsumables)
            {
                // Note these should not be added to inventory page. They will appear if that they have not been fulfilled.
                // In this sample, consumables are immediately fulfiled.
                if (!Consumables.Contains(s.Value))
                {
                   Consumables.Add(s.Value);
                }
            }
            var balResult = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
            Instance.TotalUnmanagedUnits.Total = balResult.ToString();

            return true;
        }
        public async Task<bool> UpdateDurables()
        {
            var durables = await WindowsStoreHelper.GetDurables();
            foreach (var d in durables)
            {
                if (d.Value.storeProduct.Skus[0].IsSubscription)
                {
                    if (!Subscriptions.Contains(d.Value))
                    {
                        var bRes = await WindowsStoreHelper.IsSubscriptionIsInUserCollection(d.Value.storeProduct.StoreId);
                        d.Value.SubscriptionIsInUserCollection.Value = bRes;
                        Subscriptions.Add(d.Value);
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
            AcvOwnedDurables.Filter = x => ((StoreProductEx)x).InUserCollectionEx.Value == true;

            AcvOwnedSubscriptions = new AdvancedCollectionView(Subscriptions);
            AcvOwnedSubscriptions.Filter = x => ((StoreProductEx)x).SubscriptionIsInUserCollection.Value == true;

            return true;
        }
    }
}

