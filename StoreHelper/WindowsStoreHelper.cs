﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MSAppStoreHelper
{
    //[ComImport]
    //[Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public interface IInitializeWithWindow
    //{
    //    void Initialize(long hwnd);
    //}

    public sealed class WindowsStoreHelper
    {
        static WindowsStoreHelper()
        {
            InitializeStoreContext();
        }
        private static StoreContext _storeContext = null;
        private static StoreAppLicense _storeAppLicense = null;
        private static bool _isActive = false;
        private static bool _isTrial = false;
        //private static StoreProductQueryResult _durables = null;
        private static IDictionary<string, StoreProductEx> _unmanagedConsumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _storeManagedConsumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _allAddOns = new Dictionary<string, StoreProductEx>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, ref StringBuilder packageFullName);
        public static bool IsRunningAsUwp()
        {

            StringBuilder sb = new StringBuilder(1024);
            int length = 0;
            int result = GetCurrentPackageFullName(ref length, ref sb);

            return result != 15700;
        }

        public static bool InitializeStoreContext()// long hwd=0)
        {

            Debug.WriteLine("StoreContext.GetDefault...");
            if (_storeContext == null)
            {
                _storeContext = StoreContext.GetDefault();
                return true;
            }


            //IInitializeWithWindow initWindow = (IInitializeWithWindow)(object)_storeContext;
            //initWindow.Initialize(hwd);

            return _storeContext != null;
        }


        public static IAsyncOperation<bool> HasLicenseAsync()
        {
            return hasLicenseAsync().AsAsyncOperation();
        }


        private static async Task<bool> hasLicenseAsync()
        {
            if (_storeAppLicense == null)
                _storeAppLicense = await _storeContext.GetAppLicenseAsync();
            _isActive = _storeAppLicense.IsActive;
            return _isActive;
        }

        public static IAsyncOperation<bool> CheckIfUserHasSubscriptionAsync(string subscriptionId)
        {
            return checkIfUserHasSubscriptionAsync(subscriptionId).AsAsyncOperation();
        }
        private static async Task<bool> checkIfUserHasSubscriptionAsync(string subscriptionId)
        {
            StoreAppLicense appLicense = await _storeContext.GetAppLicenseAsync();

            // Check if the customer has the rights to the subscription.
            foreach (var addOnLicense in appLicense.AddOnLicenses)
            {
                StoreLicense license = addOnLicense.Value;
                if (license.SkuStoreId.StartsWith(subscriptionId))
                {
                    if (license.IsActive)
                    {
                        // The expiration date is available in the license.ExpirationDate property.
                        return true;
                    }
                }
            }
            // The customer does not have a license to the subscription.
            return false;
        }

        public static IAsyncOperation<uint> GetUnmangedConsumableBalanceRemainingAsync(string storeId)
        {
            return getUnmangedConsumableBalanceRemainingAsync(storeId).AsAsyncOperation();
        }
        private static async Task<uint> getUnmangedConsumableBalanceRemainingAsync(string storeId)
        {


            return (uint)0;
        }
        public static IAsyncOperation<bool> IsTrialAsync()
        {
            return isTrialAsync().AsAsyncOperation();
        }
        private static async Task<bool> isTrialAsync()
        {
            if (_storeAppLicense == null)
                _storeAppLicense = await _storeContext.GetAppLicenseAsync();
            _isTrial = _storeAppLicense.IsTrial;
            return _isTrial;
        }

        public static IAsyncOperation<string> FulfillConsumable(string StoreId,uint unitsToFulFil=1)
        {
            return fulfillConsumable(StoreId, unitsToFulFil).AsAsyncOperation();
        }
        private static async Task<string> fulfillConsumable(string StoreId, uint unitsToFulFil)
        {
            StoreProduct product = null;

            if (!_allAddOns.ContainsKey(StoreId))
            {
                product = GetConsumableProduct(StoreId);
            }
            else
            {
                product = _allAddOns[StoreId].storeProduct;
            }

            Debug.Assert(product != null);
            StoreConsumableResult res = null;
            if (product.IsInUserCollection)
            {
                if (product.ProductKind == "UnmanagedConsumable" || product.ProductKind == "Consumable")
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    Guid strTrackingId = Guid.NewGuid();
                    if (localSettings.Values.ContainsKey(StoreId))
                    {
                        strTrackingId = (Guid)localSettings.Values[StoreId];
                    }
                    else
                    {
                        localSettings.Values[key: StoreId] = strTrackingId;
                    }
                    res = await _storeContext.ReportConsumableFulfillmentAsync(StoreId, unitsToFulFil, strTrackingId);
                    if (res.ExtendedError != null)
                    {
                        throw new Exception(res.ExtendedError.Message);
                    }
                    else
                    {
                        localSettings.Values.Remove(StoreId);
                    }
                }
            }

            string result = "Fulfilment error";
            if (product.ProductKind == "Consumable")
                result = (res != null) ? $"Consumable Balance remaining {res.BalanceRemaining}" : result;
            else
                result = (res != null) ? $"Consumable status: {res.Status}" : result;

            return $"{result}";

        }
   
        //public static IAsyncOperation<StoreProductQueryResult> GetStoreManagedConsumableAddOns()
        //{
        //    return getStoreManagedConsumableAddOns().AsAsyncOperation();
        //}
        //private static async Task<StoreProductQueryResult> getStoreManagedConsumableAddOns()
        //{
        //    string[] productKinds = { "Consumable" };
        //    List<String> filterList = new List<string>(productKinds);
        //        _storeManagedConsumables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
        //    return _storeManagedConsumables;
        //}


        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetUnmanagedConsumableAddOns()
        {
            return getUnmanagedConsumableAddOns().AsAsyncOperation();
        }
        private static async Task<IDictionary<string, StoreProductEx>> getUnmanagedConsumableAddOns()
        {
            if (_allAddOns.Count == 0)
            {
                await GetAllAddOns();
            }

            foreach(var p in _allAddOns.Values)
            {

                if (p.storeProduct.ProductKind == "UnmanagedConsumable")
                {
                    uint units = GetUnmanagedUnits(p);
                    _unmanagedConsumables.Add(p.storeProduct.StoreId, p);
                }
            }

            return _unmanagedConsumables;
        }

        private static uint GetUnmanagedUnits(StoreProductEx p)
        {
            var addon = _allAddOns[p.storeProduct.StoreId]
        }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetAllAddOns()
        {
            return getAllAddOns().AsAsyncOperation();
        }
        private static async Task<IDictionary<string,StoreProductEx>> getAllAddOns()
        {
            _allAddOns.Clear();
            string[] productKinds = { "Consumable", "Durable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var p in res.Products.Values)
            {
                _allAddOns.Add(p.StoreId, new StoreProductEx(p));
            }

            return _allAddOns;
        }

        private IList<StoreProduct> UserSubscriptions = new List<StoreProduct>();

        public static IAsyncOperation<IList<StoreProduct>> GetPurchasedSubscriptionProductAsync()
        {
            return getPurchasedSubscriptionProductAsync().AsAsyncOperation();
        }
        private static async Task<IList<StoreProduct>> getPurchasedSubscriptionProductAsync()
        {
            IList<StoreProduct> UserSubs = new List<StoreProduct>();
            // Load the sellable add-ons for this app and check if the trial is still 
            // available for this customer. If they previously acquired a trial they won't 
            // be able to get a trial again, and the StoreProduct.Skus property will 
            // only contain one SKU.

            StoreProductQueryResult result =
                await _storeContext.GetAssociatedStoreProductsAsync(new string[] { "Durable" });

            if (result.ExtendedError != null)
            {
                throw new Exception(ReportError((uint)result.ExtendedError.HResult));
            }

            // Look for the product that represents the subscription.
            foreach (var item in result.Products)
            {
                StoreProduct product = item.Value;
                if ( (product.IsInUserCollection) && (product.Skus[0].IsSubscription) ) 
                {
                    UserSubs.Add(product);
                }
            }
            
             return UserSubs;
        }


        public static IAsyncOperation<string> Purchase(string StoreId)
        {
            return purchase(StoreId).AsAsyncOperation();
        }
        private static async Task<string> purchase(string StoreId)
        {
            if (_storeContext == null)
            {
                throw (new Exception("Store context not initialized"));
            }

            var result = await _storeContext.RequestPurchaseAsync(StoreId);
            if (result.ExtendedError != null)
            {
                throw new Exception(ReportError((uint)result.ExtendedError.HResult));
            }

            return $"{result.Status}";
        }

        private static StoreProduct GetConsumableProduct(string StoreId)
        {

            StoreProduct product = null;
            if (_unmanagedConsumables.ContainsKey(StoreId))
            {
                product = _unmanagedConsumables[StoreId].storeProduct;
                return product;
            }

            if (_storeManagedConsumables.ContainsKey(StoreId))
            {
                product = _storeManagedConsumables[StoreId].storeProduct;
                return product;
            }
            
            return product;
        }

        public static IAsyncOperation<string> GetMSStorePurchaseToken(string purchaseToken)
        {
            return getMSStorePurchaseToken(purchaseToken).AsAsyncOperation();
        }

        private static async Task<string> getMSStorePurchaseToken(string purchaseToken)
        {
            var res = await _storeContext.GetCustomerPurchaseIdAsync(purchaseToken, "abcd");
            return res;
        }

        public static IAsyncOperation<string> GetMSStoreCollectionsToken(string collectionsToken)
        {
            return getMSStoreCollectionsToken(collectionsToken).AsAsyncOperation();
        }

        private static async Task<string> getMSStoreCollectionsToken(string collectionsToken)
        {
            var res = await _storeContext.GetCustomerCollectionsIdAsync(collectionsToken, " ");
            return res;
        }
        static string ReportError (System.UInt32 hResult)
        {
            string result;
            switch (hResult) {
                case 0x803f6107:
                    result = $"Error {hResult}. App must be published to the Store hidden - not to private audience. " +
                            "App must be installed on the development machine once to aquire license. It can then be uninstalled, and you can resume development.  See https://aka.ms/testmsiap";
                    break;
                case 0x80072EE7:
                    result = "Network error. Please check that you are connected to the internet.";
                    break;
                default:
                    result = "Error code: " + hResult.ToString();
                break;

            }
            return result;
        }
        

    }

    //public sealed class StoreProductEx1
    //{
    //    //public StoreProductEx1(StoreProductEx1 product)
    //    //{
    //    //    _storeProduct = product._storeProduct;
    //    //}
    //    public StoreProductEx1(StoreProduct product)
    //    {
    //        _storeProduct = product;
    //    }
    //    public StoreProduct _storeProduct { get; set; }
    //    public uint storeManagedConsumableRemainingBalance { get; set; }

    //    //public static implicit operator StoreProduct(StoreProductEx1 self)
    //    //{
    //    //    return self._storeProduct;
    //    //}
    //}

    public sealed class StoreProductEx
    {
        //public StoreProductEx(StoreProductEx product)
        //{
        //    _storeProduct = product._storeProduct;
        //}
        public StoreProductEx(StoreProduct product)
        {
            _storeProduct = product;
        }
        private StoreProduct _storeProduct { get; set; }
        public StoreProduct storeProduct { get { return _storeProduct; } set { _storeProduct = value; } }
        public uint storeManagedConsumableRemainingBalance { get; set; }

        public ImageSource GetImageUri()
        {
            if (_storeProduct.Images.Count == 0)
            {
                return null;
            }
            var bmp = new BitmapImage();
            bmp.UriSource = _storeProduct.Images[0].Uri;
            return bmp;

        }

        //public static implicit operator StoreProduct(StoreProductEx self)
        //{
        //    return self._storeProduct;
        //}
    }

}
