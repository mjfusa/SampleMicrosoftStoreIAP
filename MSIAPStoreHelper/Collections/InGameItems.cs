using Microsoft.Toolkit.Collections;
using Microsoft.UI.Xaml.Data;
using MSIAPHelper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIAPHelper.Collections
{
    public class InGameItems
    {
        ObservableCollection<InGameItem> Weapons= new ObservableCollection<InGameItem>();
        private static string GetGroupName(InGameItem gameItem) => gameItem.Category.ToString().ToUpper();
        public InGameItems()
        {
            Weapons.Add(new InGameItem(ItemType.Tool, "Sword", "Defend your kindom with this mighty sword", new Uri("http://www.microsoft.com"), 100, CurrencyType.Coin));
            Weapons.Add(new InGameItem(ItemType.Tool, "Mace", "Don't get hit by this", new Uri("http://www.microsoft.com"), 100, CurrencyType.Coin));

            // Group the contacts by first letter
            var grouped = Weapons.GroupBy(GetGroupName).OrderBy(g => g.Key);

            // Create an observable grouped collection
            ObservableGroupedCollection<string, InGameItem>? contactsSource = new ObservableGroupedCollection<string, InGameItem>(grouped);

            // Set up the CollectionViewSource
            var cvs = new CollectionViewSource
            {
                IsSourceGrouped = true,
                Source = contactsSource,
            };
        }
    
    }
}
