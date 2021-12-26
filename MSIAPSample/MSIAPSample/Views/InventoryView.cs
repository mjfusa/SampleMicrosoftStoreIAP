using MSIAPHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIAPSample.Views
{
    public class InventoryView
    {
        public ObservableCollection<StoreProductEx> OwnedDurables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> OwnedSubscriptions = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> OwnedStoreManagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> OwnedUnmangedConsumables = new ObservableCollection<StoreProductEx>();
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();

        public static bool bInitialized = false;
        public InventoryView()
        {
            MSIAPHelper.WindowsStoreHelper.InitializeStoreContext();
        }

        //public ObservableCollection<StoreProductEx> OwnedDurables
        //{
        //    get { return InventoryView._OwnedDurables; }
        //}

        public async Task<bool> Initialize()
        {
            if (InventoryView.bInitialized)
                return false;
            OwnedDurables.Clear();
            OwnedSubscriptions.Clear();
            OwnedStoreManagedConsumables.Clear();
            OwnedUnmangedConsumables.Clear();

            var durables = await WindowsStoreHelper.GetPurchasedDurables();
            foreach (var d in durables)
            {
                OwnedDurables.Add(d.Value);
            }
            var subscriptions = await WindowsStoreHelper.GetPurchasedSubscriptionProductAsync();
            foreach (var s in subscriptions.Values)
            {
                OwnedSubscriptions.Add(s);
            }
            var storeManagedConsumables = await WindowsStoreHelper.GetPurchasedStoreManagedConsumablesAsync();
            foreach (var s in storeManagedConsumables.Values)
            {
                OwnedStoreManagedConsumables.Add(s);
            }

            var devManagedConsumables = await WindowsStoreHelper.GetPurchasedDeveloperManagedConsumablesAsync();
            foreach (var s in devManagedConsumables.Values)
            {
                // Note these should not be added to inventory page. They will appear if that have not been fulfilled.
                // In this sample, consumables are immediately fulfiled.
                if (s.storeProduct.IsInUserCollection)
                {
                    OwnedUnmangedConsumables.Add(s);
                }
            }

            var balResult = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
            TotalUnmanagedUnits.Total = balResult.ToString();
            InventoryView.bInitialized = true;

            return true;
        }
    }
}
