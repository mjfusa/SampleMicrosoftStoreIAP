using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.Storage;
using WinRT;
using System.Linq;
using System.Text.Json;
using Windows.UI.WebUI;
using System.Collections.ObjectModel;

namespace MSIAPHelper
{
    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(long hwnd);
    }

    public sealed class WindowsStoreHelper
    {
        static WindowsStoreHelper()
        {
            InitializeStoreContext();
        }
        public const string ConstUnmangagedConsumable = "UnmanagedConsumable";
        private static StoreContext? _storeContext = null;
        private static StoreAppLicense _storeAppLicense = null;
        private static bool _isActive = false;
        private static bool _isTrial = false;
        private static IDictionary<string, StoreProductEx> _storeManagedConsumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _ownedStoreManagedConsumables = new Dictionary<string, StoreProductEx>();

        private static IDictionary<string, StoreProductEx> _ownedDeveloperManagedConsumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _allAddOns = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _userSubs = new Dictionary<string, StoreProductEx>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, ref StringBuilder packageFullName);
        public static bool IsRunningAsUwp()
        {

            StringBuilder sb = new StringBuilder(1024);
            int length = 0;
            int result = GetCurrentPackageFullName(ref length, ref sb);

            return result != 15700;
        }

        public static bool InitializeStoreContext()
        {

            Debug.WriteLine("StoreContext.GetDefault...");
            //if (_storeContext == null)
            //{
            _storeContext = StoreContext.GetDefault();
            IInitializeWithWindow initWindow = ((object)_storeContext).As<IInitializeWithWindow>(); ;
            var hwnd = (long)System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            initWindow.Initialize(hwnd); // TODO Temp workaround
                                         //if (hwnd != 0)
                                         //{
                                         //    initWindow.Initialize(hwnd);
                                         //} else
                                         //{
                                         //    _storeContext = null;
                                         //}
                                         //}

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

        //public static IAsyncOperation<bool> CheckIfUserHasSubscriptionAsync(string subscriptionId)
        //{
        //    return checkIfUserHasSubscriptionAsync(subscriptionId).AsAsyncOperation();
        //}

        private static async Task<StoreProductEx> ProcessSubscription(string subscriptionId, StoreProductEx sp)
        {
            StoreAppLicense appLicense = await _storeContext.GetAppLicenseAsync();
            StoreProductEx result = sp;
            // Check if the customer has the rights to the subscription.
            foreach (var addOnLicense in appLicense.AddOnLicenses)
            {
                StoreLicense license = addOnLicense.Value;
                if (license.SkuStoreId.StartsWith(subscriptionId))
                {
                    result.SubscriptionIsInUserCollection = false;
                    if (license.IsActive)
                    {
                        // The expiration date is available in the license.ExpirationDate property.
                        var baseTime = license.ExpirationDate;
                        try
                        {
                            var tzLocal = TimeZoneInfo.Local;
                            var timeLocal = TimeZoneInfo.ConvertTimeFromUtc(baseTime.DateTime, tzLocal);
                            result.ExpirationDate = timeLocal.ToShortDateString() + " " + timeLocal.ToShortTimeString();
                        }
                        catch (TimeZoneNotFoundException)
                        {
                            result.ExpirationDate = baseTime.DateTime.ToShortDateString() + " " + baseTime.DateTime.ToShortTimeString();
                            Console.WriteLine("Unable to create DateTimeOffset based on U.S. Central Standard Time.");
                        }
                        result.SubscriptionIsInUserCollection = true;
                        return result;
                    }
                }
            }
            // The customer does not have a license to the subscription.
            return result;

        }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetPurchasedDeveloperManagedConsumablesAsync()
        {
            return getPurchasedDeveloperManagedConsumablesAsync().AsAsyncOperation();
        }
        private static async Task<IDictionary<string, StoreProductEx>> getPurchasedDeveloperManagedConsumablesAsync()
        {
            string[] productKinds = { AddOnKind.DeveloperManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var product in res.Products.Values)
            {
                if (!_ownedDeveloperManagedConsumables.ContainsKey(product.StoreId))
                {
                    if (product.IsInUserCollection)
                    {
                        // This consumable has not been fulfilled! There was an error in purchase flow
                        Debug.Assert(false);
                    }

                    var sp = new StoreProductEx(product);
            
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings.Values.ContainsKey("Units" + product.StoreId))
                    {
                        var deSer = JsonSerializer.Deserialize<DeveloperManagedConsumable>((localSettings.Values["Units" + product.StoreId]) as string);
                        sp.developerManagedConsumable = deSer;
                    }
                    else
                    {
                        int i = 0;
                        int iResult;
                        while (int.TryParse(product.InAppOfferToken.AsSpan(i, 1), out iResult))
                        {
                            i++;
                        }
                        uint units;
                        if (i == 0)
                        {
                            units = 100;
                        }
                        else
                        {
                            units = uint.Parse(product.InAppOfferToken.Substring(0, i));
                        }


                        DeveloperManagedConsumable.Kind dmcKind = DeveloperManagedConsumable.Kind.Unknown;
                        var result = "";
                        var arr = product.InAppOfferToken.Split(' ');
                        if (arr.Count() > 1)
                        {
                            result = arr[1];
                        }
                        switch (result.ToLower())
                        {
                            case "coin":
                                dmcKind = DeveloperManagedConsumable.Kind.Coins;
                                break;
                            case "coins":
                                dmcKind = DeveloperManagedConsumable.Kind.Coins;
                                break;
                            default:
                                dmcKind = DeveloperManagedConsumable.Kind.Unknown;
                                break;
                        }

                        sp.developerManagedConsumable = new DeveloperManagedConsumable(units, dmcKind);

                    }
                    _ownedDeveloperManagedConsumables.Add(sp.storeProduct.StoreId, sp);
                }
            }
            return _ownedDeveloperManagedConsumables;
        }

        public static IAsyncOperation<uint> GetStoreManagedConsumableBalanceAsync()
        {
            return getTotalUnmangedConsumableBalanceRemainingAsync().AsAsyncOperation();
        }

        public static IAsyncOperation<uint> GetTotalUnmangedConsumableBalanceRemainingAsync()
        {
            return getTotalUnmangedConsumableBalanceRemainingAsync().AsAsyncOperation();
        }
        private static async Task<uint> getTotalUnmangedConsumableBalanceRemainingAsync()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("TotalUnits"))
            {
                TotalUnmangedUnitsRemaining = (uint)localSettings.Values["TotalUnits"];
            }
            return TotalUnmangedUnitsRemaining;
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

        public static IAsyncOperation<string> SpendConsumable(string StoreId, uint unitsToSpend = 1)
        {
            var product = GetConsumableProduct(StoreId);
            if (product.ProductKind == AddOnKind.StoreManagedConsumable)
            {
                return fulfillConsumable(StoreId, unitsToSpend).AsAsyncOperation();
            }
            else
            {
                return spendDeveloperManagedConsumable(StoreId, unitsToSpend).AsAsyncOperation();
            }
        }
        private static async Task<string> spendDeveloperManagedConsumable(string StoreId, uint unitsToFulfill)
        {
            var product = GetConsumableProduct(StoreId);
            if (TotalUnmangedUnitsRemaining < unitsToFulfill)
            {
                throw new Exception("Insufficient units");
            }
            if (product.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {

                TotalUnmangedUnitsRemaining -= unitsToFulfill;
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

                localSettings.Values["TotalUnits"] = TotalUnmangedUnitsRemaining;
            }
            return TotalUnmangedUnitsRemaining.ToString();
#if FULFILL_WHEN_CONSUMED
            if (_allAddOns[StoreId].UnmanagedUnitsRemaining < unitsToFulfill)
            {
                throw new Exception("Insufficient units");
                //uint newUnits = unitsToFulfill - _allAddOns[StoreId].UnmanagedUnitsRemaining;
                //unitsToFulfill = newUnits;
            }

            if (product.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {
                _allAddOns[StoreId].UnmanagedUnitsRemaining -= unitsToFulfill;
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["Units" + product.StoreId] = _allAddOns[StoreId].UnmanagedUnitsRemaining;
            }

            return _allAddOns[StoreId].UnmanagedUnitsRemaining.ToString();
#endif
        }

        public static IAsyncOperation<string> FulfillConsumable(string StoreId, uint unitsToFulFil = 1)
        {
            return fulfillConsumable(StoreId, unitsToFulFil).AsAsyncOperation();
        }
        private static async Task<string> fulfillConsumable(string StoreId, uint unitsToSpend)
        {
            StoreProduct? product = null;

            Debug.Assert(_allAddOns[StoreId].storeProduct.ProductKind != AddOnKind.Durable);

            product = GetConsumableProduct(StoreId);

            Debug.Assert(product != null);
            StoreConsumableResult? res = null;
            if (product.IsInUserCollection)
            {
                if (product.ProductKind == AddOnKind.DeveloperManagedConsumable || product.ProductKind == AddOnKind.StoreManagedConsumable)
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
                    uint unitsToFulfill = unitsToSpend;
                    if (product.ProductKind == AddOnKind.DeveloperManagedConsumable)
                    {
                        unitsToFulfill = 1;
                    }
                    res = await _storeContext.ReportConsumableFulfillmentAsync(StoreId, unitsToFulfill, strTrackingId);
                    if (res.Status != StoreConsumableStatus.Succeeded)
                    {
                        throw new Exception(res.Status.ToString());
                    }
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
            if (product.ProductKind == AddOnKind.StoreManagedConsumable)
                result = (res != null) ? $"Consumable Balance remaining {res.BalanceRemaining}" : result;
            else
                result = (res != null) ? $"Consumable status: {res.Status}" : result;

            return $"{result}";

        }
        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetPurchasedStoreManagedConsumablesAsync()
        {
            return getPurchasedStoreManagedConsumablesAsync().AsAsyncOperation();
        }
        public static async Task<IDictionary<string, StoreProductEx>> getPurchasedStoreManagedConsumablesAsync()
        {
            string[] productKinds = { AddOnKind.StoreManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var product in res.Products.Values)
            {
                if (!_ownedStoreManagedConsumables.ContainsKey(product.StoreId))
                {
                    _ownedStoreManagedConsumables.Add(product.StoreId, new StoreProductEx(product));
                    var result = await _storeContext.GetConsumableBalanceRemainingAsync(product.StoreId);
                    _ownedStoreManagedConsumables[product.StoreId].storeManagedConsumableRemainingBalance = result.BalanceRemaining;
                }


            }
            return _ownedStoreManagedConsumables;

        }

        private static DeveloperManagedConsumable.Kind GetUnmanagedCosumableKind(StoreProduct p)
        {
            var product = _allAddOns[p.StoreId];
            var result = "";
            if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {
                var arr = product.storeProduct.InAppOfferToken.Split(' ');
                if (arr.Count() > 1)
                {
                    result = arr[1];
                }
                switch (result.ToLower())
                {
                    case "coin":
                        return DeveloperManagedConsumable.Kind.Coins;
                    case "coins":
                        return DeveloperManagedConsumable.Kind.Coins;
                    default:
                        return DeveloperManagedConsumable.Kind.Unknown;
                }
            }
            return DeveloperManagedConsumable.Kind.Unknown;
        }
        private static uint GetUnmanagedUnits(StoreProduct p)
        {
            var product = _allAddOns[p.StoreId];
            if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {
                int i = 0;
                int iResult = 0;
                while (int.TryParse(product.storeProduct.InAppOfferToken.AsSpan(i, 1), out iResult))
                {
                    i++;
                }
                if (i == 0)
                {
                    return 100;
                }
                else
                {
                    return UInt32.Parse(product.storeProduct.InAppOfferToken.Substring(0, i));
                }
            }
            return 0;
        }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetAllAddOns()
        {
            return getAllAddOns().AsAsyncOperation();
        }



        private static async Task<IDictionary<string, StoreProductEx>> getAllAddOns()
        {
            _allAddOns.Clear();
            string[] productKinds = { AddOnKind.StoreManagedConsumable, AddOnKind.Durable, AddOnKind.DeveloperManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            if (res.ExtendedError != null)
            {
                throw new Exception(ReportError((uint)res.ExtendedError.HResult));
            }

            foreach (var product in res.Products.Values)
            {
                var sp = new StoreProductEx(product);
                if (product.ProductKind == AddOnKind.DeveloperManagedConsumable)
                {
                    if (product.IsInUserCollection)
                    {
                        // This consumable has not been fulfilled! There was an error in purchase flow
                        Debug.Assert(false);
                    }

                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    if (localSettings.Values.ContainsKey("Units" + product.StoreId))
                    {
                        var deSer = JsonSerializer.Deserialize<DeveloperManagedConsumable>((localSettings.Values["Units" + product.StoreId]) as string);
                        sp.developerManagedConsumable = deSer;
                    }
                    else
                    {
                        int i = 0;
                        int iResult;
                        while (int.TryParse(product.InAppOfferToken.AsSpan(i, 1), out iResult))
                        {
                            i++;
                        }
                        uint units;
                        if (i == 0)
                        {
                            units = 100;
                        }
                        else
                        {
                            units = uint.Parse(product.InAppOfferToken.Substring(0, i));
                        }


                        DeveloperManagedConsumable.Kind dmcKind = DeveloperManagedConsumable.Kind.Unknown;
                        var result = "";
                        var arr = product.InAppOfferToken.Split(' ');
                        if (arr.Count() > 1)
                        {
                            result = arr[1];
                        }
                        switch (result.ToLower())
                        {
                            case "coin":
                                dmcKind = DeveloperManagedConsumable.Kind.Coins;
                                break;
                            case "coins":
                                dmcKind = DeveloperManagedConsumable.Kind.Coins;
                                break;
                            default:
                                dmcKind = DeveloperManagedConsumable.Kind.Unknown;
                                break;
                        }

                        sp.developerManagedConsumable = new DeveloperManagedConsumable(units, dmcKind);

                    }
                    //TotalUnmangedUnitsRemaining += sp.developerManagedConsumable.UnmanagedUnitsRemaining;

                }
                else if (product.ProductKind == AddOnKind.Durable)
                {
                    if (product.Skus[0].IsSubscription)
                    {
                        var result = await ProcessSubscription(product.StoreId, sp);
                        sp = result;
                    }
                }


                _allAddOns.Add(product.StoreId, sp);
            }
            return _allAddOns;
        }



        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetAllDurables()
        {
            return getAllDurables().AsAsyncOperation();
        }

        private static async Task<IDictionary<string, StoreProductEx>> getAllDurables()
        {
            string[] productKinds = { AddOnKind.Durable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);
            IDictionary<string, StoreProductEx> result = new Dictionary<string, StoreProductEx>();
            foreach (var product in res.Products.Values)
            {
                result.Add(product.StoreId, new StoreProductEx(product));
            }

            return result;
        }


        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetPurchasedDurables()
        {
            return getPurchasedDurables().AsAsyncOperation();
        }

        private static async Task<IDictionary<string, StoreProductEx>> getPurchasedDurables()
        {
            IDictionary<string, StoreProductEx> result = new Dictionary<string, StoreProductEx>();
            if (_allAddOns.Count == 0)
            {
                await GetAllAddOns();
            }
            foreach (var a in _allAddOns)
            {
                if ((a.Value.storeProduct.IsInUserCollection) && (a.Value.storeProduct.ProductKind == "Durable"))
                {
                    result.Add(a);
                }
            }
            return result;
        }

        public static uint TotalUnmangedUnitsRemaining { get; private set; }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetPurchasedSubscriptionProductAsync()
        {
            return getPurchasedSubscriptionProductAsync().AsAsyncOperation();
        }
        private static async Task<IDictionary<string, StoreProductEx>> getPurchasedSubscriptionProductAsync()
        {
            // Load the sellable add-ons for this app and check if the trial is still 
            // available for this customer. If they previously acquired a trial they won't 
            // be able to get a trial again, and the StoreProduct.Skus property will 
            // only contain one SKU.

            foreach (var a in _allAddOns)
            {
                if (a.Value.SubscriptionIsInUserCollection == true)
                {
                    if (!_userSubs.ContainsKey(a.Value.storeProduct.StoreId))
                    {
                        _userSubs.Add(a);
                    }
                }
            }


            return _userSubs;
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

            InitializeStoreContext();

            var result = await _storeContext.RequestPurchaseAsync(StoreId);
            if (result.ExtendedError != null)
            {
                throw new Exception(ReportError((uint)result.ExtendedError.HResult));
            }
            if (result.Status != StorePurchaseStatus.Succeeded)
            {
                return $"{result.Status}";
            }

            if (_allAddOns[StoreId].storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {
                var product = _allAddOns[StoreId];
                product.developerManagedConsumable.UnmanagedUnitsRemaining += product.developerManagedConsumable.Units;
                TotalUnmangedUnitsRemaining += product.developerManagedConsumable.UnmanagedUnitsRemaining;

                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings; // todo save to cloud
                var ser = JsonSerializer.Serialize(product.developerManagedConsumable);
                localSettings.Values["Units" + StoreId] = ser;
                localSettings.Values["TotalUnits"] = TotalUnmangedUnitsRemaining;
                await _storeContext.ReportConsumableFulfillmentAsync(StoreId, 1, Guid.NewGuid());
            }

            return $"{result.Status}";
        }

        private static StoreProduct GetConsumableProduct(string StoreId)
        {
            StoreProduct? product = null;
            if (_allAddOns.ContainsKey(StoreId))
            {
                product = _allAddOns[StoreId].storeProduct;
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
        static string ReportError(System.UInt32 hResult)
        {
            string result;
            switch (hResult)
            {
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

    public sealed class StoreProductEx
    {
        public StoreProductEx(StoreProduct product)
        {
            _storeProduct = product;
            //if ((_storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable) && _storeProduct.IsInUserCollection)
            //{
            //    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //    if (localSettings.Values.ContainsKey("Units" + product.StoreId))
            //    {
            //        var deSer = JsonSerializer.Deserialize<DeveloperManagedConsumable>((localSettings.Values["Units" + product.StoreId]) as string);
            //        developerManagedConsumable = new DeveloperManagedConsumable(deSer.UnmanagedUnitsRemaining, deSer.dmcKind);
            //    }
            //}
        }

        public DeveloperManagedConsumable developerManagedConsumable;
        private StoreProduct _storeProduct { get; set; }
        public StoreProduct storeProduct { get { return _storeProduct; } set { _storeProduct = value; } }
        public uint storeManagedConsumableRemainingBalance { get; set; }
        public bool SubscriptionIsInUserCollection { get; set; }
        public string ExpirationDate { get; internal set; }

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
    public static class AddOnKind
    {
        public const string Durable = "Durable";
        public const string StoreManagedConsumable = "Consumable";
        public const string DeveloperManagedConsumable = "UnmanagedConsumable";
    }

    public class DeveloperManagedConsumable
    {
        public enum Kind { Coins, Unknown };

        public Kind dmcKind = Kind.Unknown;

        public DeveloperManagedConsumable(uint units, Kind dmcKind)
        {
            Units = units;
            UnmanagedUnitsRemaining = 0;
            this.dmcKind = dmcKind;
        }
        public DeveloperManagedConsumable()
        {

        }
        public uint UnmanagedUnitsRemaining { get; set; }
        public uint Units { get; }
    }

    public static partial class Extensions
    {
        /// <summary>
        ///     A string extension method that query if '@this' is Alpha.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <returns>true if Alpha, false if not.</returns>
        public static bool IsAlpha(this string @this)
        {
            return !Regex.IsMatch(@this, "[^a-zA-Z]");
        }
    }
}
