using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MSIAPHelper.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.Storage;
using WinRT;

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
        public static string UserId = ""; // userId == App Receipt Id
        private static StoreContext? _storeContext = null;
        private static StoreAppLicense _storeAppLicense = null;
        private static bool _isActive = false;
        private static bool _isTrial = false;
        private static IDictionary<string, StoreProductEx> _Consumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _StoreManagedConsumables = new Dictionary<string, StoreProductEx>();
        private static IDictionary<string, StoreProductEx> _Durables = new Dictionary<string, StoreProductEx>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetCurrentPackageFullName(ref int packageFullNameLength, ref StringBuilder packageFullName);
        public static bool IsRunningAsUwp()
        {

            StringBuilder sb = new StringBuilder(1024);
            int length = 0;
            int result = GetCurrentPackageFullName(ref length, ref sb);

            return result != 15700;
        }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetStoreManagedConsumablesAsync()
        {
            return getStoreManagedConsumablesAsync().AsAsyncOperation();
        }
        public static async Task<IDictionary<string, StoreProductEx>> getStoreManagedConsumablesAsync()
        {
            string[] productKinds = { AddOnKind.StoreManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var product in res.Products.Values)
            {
                if (!_StoreManagedConsumables.ContainsKey(product.StoreId))
                {
                    _StoreManagedConsumables.Add(product.StoreId, new StoreProductEx(product));
                    var result = await _storeContext.GetConsumableBalanceRemainingAsync(product.StoreId);
                    _StoreManagedConsumables[product.StoreId].storeManagedConsumableRemainingBalance = result.BalanceRemaining;
                }
                else
                {
                    var storeBal = await _storeContext.GetConsumableBalanceRemainingAsync(product.StoreId);
                    if (storeBal.BalanceRemaining != _StoreManagedConsumables[product.StoreId].storeManagedConsumableRemainingBalance)
                    {
                        _StoreManagedConsumables.Remove(product.StoreId);
                        _StoreManagedConsumables[product.StoreId] = new StoreProductEx(product);
                        _StoreManagedConsumables[product.StoreId].storeManagedConsumableRemainingBalance = storeBal.BalanceRemaining;
                    }
                }
            }
            return _StoreManagedConsumables;
        }

        public static async Task<bool> InitializeStoreContext()
        {
         
            Debug.WriteLine("StoreContext.GetDefault...");
            //if (_storeContext == null)
            //{
            _storeContext = StoreContext.GetDefault();
            IInitializeWithWindow initWindow = ((object)_storeContext).As<IInitializeWithWindow>(); ;
            var hwnd = (long)System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            initWindow.Initialize(hwnd); // TODO Temp workaround

            var AppReceipt = await CurrentApp.GetAppReceiptAsync();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(AppReceipt);
            XmlNode node = xmlDoc.DocumentElement;
            var cn = node.ChildNodes;
            foreach (XmlNode node2 in cn)
            {
                if (node2.Name == "AppReceipt")
                {
                    UserId = node2.Attributes["Id"].Value;
                    break;
                }
            }  


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

        public static IAsyncOperation<bool> IsSubscriptionIsInUserCollection(string storeId)
        {
            return checkIfSubscriptionIsInUserCollection(storeId).AsAsyncOperation();
        }

        public static async Task<bool> checkIfSubscriptionIsInUserCollection(string subscriptionId)
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
                        return true;
                    }
                }
            }
            // The customer does not have a license to the subscription.
            return false;
        }


        //private static async Task<StoreProductEx> ProcessSubscription(string subscriptionId, StoreProductEx sp)
        //{
        //    StoreAppLicense appLicense = await _storeContext.GetAppLicenseAsync();
        //    StoreProductEx result = sp;
        //    // Check if the customer has the rights to the subscription.
        //    foreach (var addOnLicense in appLicense.AddOnLicenses)
        //    {
        //        StoreLicense license = addOnLicense.Value;
        //        if (license.SkuStoreId.StartsWith(subscriptionId))
        //        {
        //            result.SubscriptionIsInUserCollection.Value = false;
        //            if (license.IsActive)
        //            {
        //                // The expiration date is available in the license.ExpirationDate property.
        //                var baseTime = license.ExpirationDate;
        //                try
        //                {
        //                    var tzLocal = TimeZoneInfo.Local;
        //                    var timeLocal = TimeZoneInfo.ConvertTimeFromUtc(baseTime.DateTime, tzLocal);
        //                    result.ExpirationDate = timeLocal.ToShortDateString() + " " + timeLocal.ToShortTimeString();
        //                }
        //                catch (TimeZoneNotFoundException)
        //                {
        //                    result.ExpirationDate = baseTime.DateTime.ToShortDateString() + " " + baseTime.DateTime.ToShortTimeString();
        //                    Console.WriteLine("Unable to create DateTimeOffset based on U.S. Central Standard Time.");
        //                }
        //                result.SubscriptionIsInUserCollection.Value = true;
        //                return result;
        //            }
        //        }
        //    }
        //    // The customer does not have a license to the subscription.
        //    return result;

        //}
        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetConsumablesAsync1()
        {
            return getConsumablesAsync1().AsAsyncOperation();
        }
        public static async Task<IDictionary<string, StoreProductEx>> getConsumablesAsync1()
        {
            string[] productKinds = { AddOnKind.DeveloperManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            var devManagedConsumables = new Dictionary<string, StoreProductEx>();
            foreach (var product in res.Products.Values)
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
                devManagedConsumables.Add(sp.storeProduct.StoreId, sp);
            }
            return devManagedConsumables;
        }


        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetConsumablesAsync()
        {
            return getConsumablesAsync().AsAsyncOperation();
        }
        private static async Task<IDictionary<string, StoreProductEx>> getConsumablesAsync()
        {
            string[] productKinds = { AddOnKind.DeveloperManagedConsumable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var product in res.Products.Values)
            {
                if (!_Consumables.ContainsKey(product.StoreId))
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
                    _Consumables.Add(sp.storeProduct.StoreId, sp);
                }
            }
            return _Consumables;
        }

        public static IAsyncOperation<uint> GetStoreManagedConsumableBalanceAsync(string StoreId)
        {
            return getStoreManagedConsumableBalanceAsync(StoreId).AsAsyncOperation();
        }
        public static async Task<uint> getStoreManagedConsumableBalanceAsync(string StoreId)
        {
            var result = await _storeContext.GetConsumableBalanceRemainingAsync(StoreId);
            return result.BalanceRemaining;
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
            var product = GetProduct(StoreId);
            if (product.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
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
            var product = GetProduct(StoreId);
            if (TotalUnmangedUnitsRemaining < unitsToFulfill)
            {
                throw new Exception("Insufficient units");
            }
            if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
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
            var product = GetProduct(StoreId);

            Debug.Assert(product != null);
            Debug.Assert(product.storeProduct.ProductKind != AddOnKind.Durable);

            StoreConsumableResult? res = null;
            if (product.storeProduct.IsInUserCollection)
            {
                if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable || product.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
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
                    if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
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
            if (product.storeProduct.ProductKind == AddOnKind.StoreManagedConsumable)
                result = (res != null) ? $"Consumable Balance remaining {res.BalanceRemaining}" : result;
            else
                result = (res != null) ? $"Consumable status: {res.Status}" : result;

            return $"{result}";

        }

        public static IAsyncOperation<IDictionary<string, StoreProductEx>> GetDurables()
        {
            return getDurables().AsAsyncOperation();
        }

        private static async Task<IDictionary<string, StoreProductEx>> getDurables()
        {
            string[] productKinds = { AddOnKind.Durable };
            List<String> filterList = new List<string>(productKinds);
            var res = await _storeContext.GetAssociatedStoreProductsAsync(filterList);

            foreach (var product in res.Products.Values)
            {
                if (!_Durables.ContainsKey(product.StoreId))
                {
                    _Durables.Add(product.StoreId, new StoreProductEx(product));
                }
                else
                {
                    if (_Durables[product.StoreId].InUserCollectionEx != null)
                    {
                        if (_Durables[product.StoreId].InUserCollectionEx.Value != product.IsInUserCollection)
                        {
                            if (_Durables[product.StoreId].storeProduct.Skus[0].IsSubscription)
                            {
                                _Durables[product.StoreId].SubscriptionIsInUserCollection.Value = product.IsInUserCollection;
                            }
                            _Durables[product.StoreId].InUserCollectionEx.Value = product.IsInUserCollection;
                        }
                    }
                }
            }

            return _Durables;
        }

        public static uint TotalUnmangedUnitsRemaining { get; private set; }
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

            var product = GetProduct(StoreId);

            if (product.storeProduct.ProductKind == AddOnKind.DeveloperManagedConsumable)
            {
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

        private static StoreProductEx GetProduct(string StoreId)
        {
            StoreProductEx? product = null;
            if (_Consumables.ContainsKey(StoreId))
            {
                product = _Consumables[StoreId];
                return product;
            }

            if (_StoreManagedConsumables.ContainsKey(StoreId))
            {
                product = _StoreManagedConsumables[StoreId];
                return product;
            }

            if (_Durables.ContainsKey(StoreId))
            {
                product = _Durables[StoreId];
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

        public static IAsyncOperation<string> GetMSStoreCollectionsToken()
        {
            var CollectionsToken = GetCollectionsTokenFromService().AsAsyncOperation();
            return CollectionsToken;
            
            //return getMSStoreCollectionsToken(collectionsToken).AsAsyncOperation();
        }

        public static IAsyncOperation<string> GetProductsFromService()
        {
           return getProductsFromService().AsAsyncOperation();
        }
        private static async Task<string> getProductsFromService()
        {
            var CollectionsToken = await GetCollectionsTokenFromService(); 
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(UserId);
            var data = new StringContent($"{{\"UserCollectionsId\":\"{CollectionsToken}\"}}", Encoding.UTF8, "application/json");

            
            var response = await client.PostAsync(new Uri("https://localhost:5001/collections/query"), data);
            //ClientAccessTokensResponse accessTokens = null;

            return "";
        }

        private static async Task<string> GetCollectionsTokenFromService()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(UserId);
            var response = await client.GetAsync(new Uri("https://localhost:5001/collections/RetrieveAccessTokens"));
            ClientAccessTokensResponse accessTokens =null;
            if (response.StatusCode==System.Net.HttpStatusCode.OK)
            {
                var tokenContent=await response.Content.ReadAsStringAsync();
                accessTokens = JsonSerializer.Deserialize<ClientAccessTokensResponse>(tokenContent);
            }
            if (accessTokens==null)
            {
                return "";
            } else
            {
                foreach(var token in accessTokens.AccessTokens)
                {
                    if (token.Audience.Contains("collections"))
                        return token.Token;
                }
                return "";
            }

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
            InUserCollectionEx = new IsInUserCollectionEx(product.IsInUserCollection);
            SubscriptionIsInUserCollection = new IsInUserCollectionEx(product.IsInUserCollection);
        }

        public DeveloperManagedConsumable developerManagedConsumable;
        [DefaultValue(false)]
        public IsInUserCollectionEx InUserCollectionEx { get; set; }
        private StoreProduct _storeProduct { get; set; }
        public StoreProduct storeProduct { get { return _storeProduct; } set { _storeProduct = value; } }
        public uint storeManagedConsumableRemainingBalance { get; set; }
        public IsInUserCollectionEx SubscriptionIsInUserCollection { get; set; }
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

    public class IsInUserCollectionEx : ObservableObject
    {
        public IsInUserCollectionEx(bool inCollection)
        {
            Value = inCollection;
        }
        private bool _inUserCollection;
        public bool Value
        {
            get => _inUserCollection;
            set => SetProperty(ref _inUserCollection, value);
        }
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
