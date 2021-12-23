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
            return true;
        }
    }
}
