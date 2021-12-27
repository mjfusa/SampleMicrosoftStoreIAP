using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Data;
using MSIAPHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIAPSample.Views
{
    public class AddOnsView
    {
        private ObservableCollection<StoreProductEx> Durables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> Subscriptions = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> StoreManagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> UnmangedConsumables = new ObservableCollection<StoreProductEx>();
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();

        public static bool bInitialized = false;

        private AdvancedCollectionView acvOwnedDurables;
        public AdvancedCollectionView AcvOwnedDurables
        {
            get => acvOwnedDurables;

            set => acvOwnedDurables = value;
        }
        public async Task<bool> Initialize()
        {
            Durables.Clear();

            var durables = await WindowsStoreHelper.GetAllDurables();
            foreach (var d in durables)
            {
                Durables.Add(d.Value);
            }
            
            AcvOwnedDurables = new AdvancedCollectionView(Durables);
            AcvOwnedDurables.Filter = x => ((StoreProductEx)x).storeProduct.IsInUserCollection== true;
            return true;
        }
    }
}
