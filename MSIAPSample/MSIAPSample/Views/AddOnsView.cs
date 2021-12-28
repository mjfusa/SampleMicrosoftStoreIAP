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
    public class AddOnsView
    {
        private static ObservableCollection<StoreProductEx> durables = new ObservableCollection<StoreProductEx>();
        private static ObservableCollection<StoreProductEx> subscriptions = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> StoreManagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> UnmangedConsumables = new ObservableCollection<StoreProductEx>();
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();

        public static bool bInitialized = false;

        private AdvancedCollectionView acvOwnedDurables;
        private AdvancedCollectionView acvOwnedSubscriptions;
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
        public ObservableCollection<StoreProductEx> Durables { get => durables; set => durables = value; }
        public ObservableCollection<StoreProductEx> Subscriptions { get => subscriptions; set => subscriptions = value; }

        public async Task<bool> Initialize()
        {
            Durables.Clear();

            var durables = await WindowsStoreHelper.GetAllDurables();
            foreach (var d in durables)
            {
                if (d.Value.storeProduct.Skus[0].IsSubscription)
                {
                    Subscriptions.Add(d.Value);
                    var res = await WindowsStoreHelper.CheckIfSubscriptionIsInUserCollection(d.Value.storeProduct.StoreId);
                    if (true==res)
                    {
                        d.Value.SubscriptionIsInUserCollection = true;
                    }
                } else
                {
                    Durables.Add(d.Value);
                }

            }


            AcvOwnedDurables = new AdvancedCollectionView(Durables);
            AcvOwnedSubscriptions = new AdvancedCollectionView(Subscriptions);
            AcvOwnedDurables.Filter = x => ((StoreProductEx)x).storeProduct.IsInUserCollection == true;
            AcvOwnedSubscriptions.Filter = x => ((StoreProductEx)x).SubscriptionIsInUserCollection == true;
            return true;
        }
    }
}
