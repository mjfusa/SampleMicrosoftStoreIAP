using MSAppStoreHelper;
using Windows.System.UserProfile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using System.Security.Principal;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //var res2 = await WindowsStoreHelper.HasLicenseAsync();
            //txtHasLic.Text = res2.ToString();

            //var res = await WindowsStoreHelper.GetPurchasedSubscriptionProductAsync();
            //var res1= await WindowsStoreHelper.CheckIfUserHasSubscriptionAsync("9PHFB37XTFPV");

            //txtRes.Text = res1.ToString();

        }

        private void Button_Buy_Click(object sender, RoutedEventArgs e)
        {
            var res = WindowsStoreHelper.PurchaseDurable("9PHFB37XTFPV");// "SUBSCRIPTION -NO-FREETRIAL");

        }

        private async void Button_Subs_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStorePurchaseToken(txtPurchaseToken.Text);
            txtMSIDPurchaseToken.Text = res;
        }

        private async void GetUserInfo()
        {
            var users = await User.FindAllAsync();
            List<string> userProps = new List<string>() { "AccountName",
            "DisplayName",
            "DomainName",
            "FirstName",
            "GuestHost",
            "LastName",
            "PrincipalName",
            "SessionInitiationProtocolUri",
            };

            foreach (var user in users)
            {
                var userResults = await user.GetPropertiesAsync(userProps);
            }


            var name = WindowsIdentity.GetCurrent().Name;
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetUserInfo();
        }

        private async void Button_GetStoreIdCollections_Click(object sender, RoutedEventArgs e)
        {
            var res = await WindowsStoreHelper.GetMSStoreCollectionsToken(txtCollectionsToken.Text);
            txtMSIDCollectionsToken.Text = res;
        }
    }
}
