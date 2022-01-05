using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;

namespace MSIAPHelper.Models
{
    public class InGameItem : IInGameItem
    {
        private Guid _gameId;
        public Guid GameId
        {
            get { return _gameId; }
        }

        private ItemType _category;
        public ItemType Category
        {
            get { return _category; }
        }

        private string _name;
        public string Name  // read-write instance property
        {
            get => _name;
        }
        private string _description;
        public string Description
        {
            get => _description;    
        }

        private Uri _imageUri;
        public Uri ImageUri
        {
            get => _imageUri;
        }

        private uint _cost;
        public uint Cost
        {
            get => _cost;
        }

        private CurrencyType _itemcurrencyType;
        public CurrencyType ItemCurrencyType
        {
            get => _itemcurrencyType;
        }

        private bool _isinUserCollection;
        public bool IsInUserCollection
        {
            get => _isinUserCollection;
            set => _isinUserCollection = value;
        }
        public InGameItem(ItemType category, string name, string description, Uri imageUri, uint cost, CurrencyType currencyType, bool isinUsercollection=false)
        {
            _category = category;
            _name = name;
            _description = description;
            _imageUri = imageUri;
            _cost = cost;
            _itemcurrencyType = currencyType;
            _isinUserCollection = isinUsercollection;
        }
    }
}
