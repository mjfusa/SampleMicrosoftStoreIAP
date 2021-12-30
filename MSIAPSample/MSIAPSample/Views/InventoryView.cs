﻿using CommunityToolkit.WinUI.UI;
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
        //private ObservableCollection<StoreProductEx> ownedDurables = new ObservableCollection<StoreProductEx>();
        //public ObservableCollection<StoreProductEx> OwnedSubscriptions = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> OwnedStoreManagedConsumables = new ObservableCollection<StoreProductEx>();
        public ObservableCollection<StoreProductEx> OwnedUnmangedConsumables = new ObservableCollection<StoreProductEx>();
        public UnmanagedUnitsRemaining TotalUnmanagedUnits = new UnmanagedUnitsRemaining();

        public static bool bInitialized = false;

        //public ObservableCollection<StoreProductEx> OwnedDurables { get => ownedDurables; set => ownedDurables = value; }
        //private AdvancedCollectionView acvOwnedDurables;
        //public AdvancedCollectionView AcvOwnedDurables { 
        //    get => acvOwnedDurables; 
        //    set => acvOwnedDurables = value; }

        public InventoryView()
        {
            MSIAPHelper.WindowsStoreHelper.InitializeStoreContext();
        }

    }
}
