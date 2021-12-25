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

        public InventoryView()
        {
            MSIAPHelper.WindowsStoreHelper.InitializeStoreContext();
        }

        public async Task<bool> Initialize()
        {
            var durables = await WindowsStoreHelper.GetPurchasedDurables();
            foreach (var d in durables)
            {
                OwnedDurables.Add(d.Value);
            }
            var subscriptions = await WindowsStoreHelper.GetPurchasedSubscriptionProductAsync();
            foreach (var s in subscriptions)
            {
                //OwnedDurables.Add(new StoreProductEx(s));
            }
            var storeManagedConsumables = await WindowsStoreHelper.GetTotalUnmangedConsumableBalanceRemainingAsync();
            foreach (var s in subscriptions)
            {
                //OwnedDurables.Add(new StoreProductEx(s));
            }

            return true;
        }
    }
}
