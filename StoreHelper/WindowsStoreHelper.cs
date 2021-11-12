using System;
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
        private static StoreProductQueryResult _durables = null;
        private static StoreProductQueryResult _unmanagedConsumables = null;
        private static StoreProductQueryResult _storeManagedConsumables = null;
        private static StoreProductQueryResult _allAddOns = null;

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

        public static IAsyncOperation<string> FulfillConsumable(string StoreId)
        {
            return fulfillConsumable(StoreId).AsAsyncOperation();
        }
        private static async Task<string> fulfillConsumable(string StoreId)
        {
            StoreProduct product = null;

            if (!_durables.Products.ContainsKey(StoreId))
            {
                product = CheckConsumableIfFulfilled(StoreId);
            }
            else
            {
                product = _durables.Products[StoreId];
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
                    res = await _storeContext.ReportConsumableFulfillmentAsync(StoreId, 1, strTrackingId);
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

            RefreshIAP();

            return $"{result}";

        }

        public static IAsyncOperation<StoreProductQueryResult> GetDurableAddOns()
        {
            return getDurableAddOns().AsAsyncOperation();
        }
        private static async Task<StoreProductQueryResult> getDurableAddOns()
        {
            string[] productKinds = { "Durable" };
            List<String> filterList = new List<string>(productKinds);
            if (_durables == null)
            {
                _durables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            if (_durables.ExtendedError != null)
            {
                throw new Exception (ReportError((uint)_durables.ExtendedError.HResult));
            }

            return _durables;

        }
        public static IAsyncOperation<StoreProductQueryResult> GetStoreManagedConsumableAddOns()
        {
            return getStoreManagedConsumableAddOns().AsAsyncOperation();
        }
        private static async Task<StoreProductQueryResult> getStoreManagedConsumableAddOns()
        {
            string[] productKinds = { "Consumable" };
            List<String> filterList = new List<string>(productKinds);
            if (_storeManagedConsumables == null)
            {
                _storeManagedConsumables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            return _storeManagedConsumables;
        }


        public static IAsyncOperation<StoreProductQueryResult> GetUnmanagedConsumableAddOns()
        {
            return getUnmanagedConsumableAddOns().AsAsyncOperation();
        }
        private static async Task<StoreProductQueryResult> getUnmanagedConsumableAddOns()
        {
            string[] productKinds = { "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);
            if (_unmanagedConsumables == null)
            {
                _unmanagedConsumables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            return _unmanagedConsumables;
        }
        public static IAsyncOperation<StoreProductQueryResult> GetAllAddOns()
        {
            return getAllAddOns().AsAsyncOperation();
        }
        private static async Task<StoreProductQueryResult> getAllAddOns()
        {
            string[] productKinds = { "Consumable", "Durable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);
            if (_allAddOns == null)
            {
                _allAddOns = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
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

            RefreshIAP();

            return $"{result.Status}";
        }

        private static void RefreshIAP()
        {
            _ = GetDurableAddOns();
            _ = GetUnmanagedConsumableAddOns();
            _ = GetStoreManagedConsumableAddOns();

        }

        private static StoreProduct CheckConsumableIfFulfilled(string StoreId)
        {

            StoreProduct product = null;
            if (_unmanagedConsumables.Products.ContainsKey(StoreId))
            {
                product = _unmanagedConsumables.Products[StoreId];
                return product;
            }

            if (_storeManagedConsumables.Products.ContainsKey(StoreId))
            {
                product = _storeManagedConsumables.Products[StoreId];
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
        public static IAsyncOperation<IReadOnlyList<StorePackageUpdate>> GetPackageUpdates()
        {
            return getPackageUpdates().AsAsyncOperation();
        }

        // Updates / Package Management
        private static async Task<IReadOnlyList<StorePackageUpdate>> getPackageUpdates()
        {
            IReadOnlyList<StorePackageUpdate> res1 = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
            return res1;
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


}
