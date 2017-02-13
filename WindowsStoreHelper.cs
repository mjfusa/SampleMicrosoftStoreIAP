using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace UWPStoreHelper
{
    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    public class WindowsStoreHelper
    {
        public WindowsStoreHelper()
        {
        }
        private static StoreContext _storeContext = null;
        private static StoreAppLicense _storeAppLicense = null;
        private static bool _isActive = false;
        private static bool _isTrial = false;
        private static StoreProductQueryResult _durables = null;
        private static StoreProductQueryResult _consumables = null;
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

        public static bool InitializeStoreContext(IntPtr hwd)
        {

            Debug.WriteLine("StoreContext.GetDefault...");
            _storeContext = StoreContext.GetDefault();

            IInitializeWithWindow initWindow = (IInitializeWithWindow)(object)_storeContext;
            initWindow.Initialize(hwd);

            return _storeContext != null;
        }

        public static async Task<bool> HasLicenseAsync()
        {
            if (_storeAppLicense == null)
                _storeAppLicense = await _storeContext.GetAppLicenseAsync();
            _isActive = _storeAppLicense.IsActive;
            return _isActive;
        }


        public static async Task<bool> IsTrialAsync()
        {
            if (_storeAppLicense == null)
                _storeAppLicense = await _storeContext.GetAppLicenseAsync();
            _isTrial = _storeAppLicense.IsTrial;
            return _isTrial;
        }

        public static async Task<StoreProductQueryResult> GetDurableAddOns()
        {
            string[] productKinds = { "Durable" };
            List<String> filterList = new List<string>(productKinds);
            if (_durables == null)
            {
                _durables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            if (_storeAppLicense == null)
                _storeAppLicense = await _storeContext.GetAppLicenseAsync();

            var addOnLicenses = _storeAppLicense.AddOnLicenses; // Not workind use IsInUserCollection
            foreach (KeyValuePair<string, StoreLicense> item in addOnLicenses)
            {
                var t = item.Key;
            }

            return _durables;

        }
        public static async Task<StoreProductQueryResult> GetConsumableAddOns()
        {
            string[] productKinds = { "Consumable" };
            List<String> filterList = new List<string>(productKinds);
            if (_consumables == null)
            {
                _consumables = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            return _consumables;
        }
        public static async Task<StoreProductQueryResult> GetAllAddOns()
        {
            string[] productKinds = { "Consumable", "Durable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);
            if (_allAddOns == null)
            {
                _allAddOns = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            }

            return _allAddOns;
        }

        public static async Task<StorePurchaseResult> PurchaseDurable(string StoreId)
        {
            if (_storeContext == null)
            {
                throw (new Exception("Store context not initialized"));
            }

            var result = await _storeContext.RequestPurchaseAsync(StoreId);
            if (result.ExtendedError != null)
            {
                throw new Exception(result.ExtendedError.Message);
            }
            return result;
        }


    }

}
