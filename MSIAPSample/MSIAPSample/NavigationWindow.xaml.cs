using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MSIAPSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NavigationWindow : Window
    {
        public NavigationWindow()
        {
            this.InitializeComponent();
            NavigationView.SelectedItem = NavigationView.MenuItems.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>().First();

        }
        public void SetCurrentNavigationViewItem(NavigationViewItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.Tag == null)
            {
                return;
            }

            var windowClassname = "MSIAPSample." + item.Tag.ToString();
            
            contentFrame.Navigate(Type.GetType(windowClassname), item.Content);
            NavigationView.Header = item.Content;
            NavigationView.SelectedItem = item;
        }
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                //contentFrame.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
                if (selectedItem != null)
                {
                    //string selectedItemTag = ((string)selectedItem.Tag);
                    //sender.Header = "Sample Page " + selectedItemTag.Substring(selectedItemTag.Length - 1);
                    //string pageName = "MSIAPSample." + selectedItemTag;
                    //Type pageType = Type.GetType(pageName);
                    //var t = new InventoryWindow();
                    //contentFrame.Navigate(pageType);
                    SetCurrentNavigationViewItem(selectedItem as NavigationViewItem);

                }
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            //SetCurrentNavigationViewItem(args.SelectedItemContainer as NavigationViewItem);
            //SetCurrentNavigationViewItem(GetNavigationViewItems(typeof(InventoryPage)).First());
        }

        public List<NavigationViewItem> GetNavigationViewItems()
        {
            var result = new List<NavigationViewItem>();
            var items = NavigationView.MenuItems.Select(i => (NavigationViewItem)i).ToList();
            items.AddRange(NavigationView.FooterMenuItems.Select(i => (NavigationViewItem)i));
            result.AddRange(items);

            foreach (NavigationViewItem mainItem in items)
            {
                result.AddRange(mainItem.MenuItems.Select(i => (NavigationViewItem)i));
            }

            return result;
        }
        
        public List<NavigationViewItem> GetNavigationViewItems(Type type)
        {
            return GetNavigationViewItems().Where(i => i.Tag.ToString() == type.FullName).ToList();
        }
        public List<NavigationViewItem> GetNavigationViewItems(Type type, string title)
        {
            return GetNavigationViewItems(type).Where(ni => ni.Content.ToString() == title).ToList();
        }
    }

}
