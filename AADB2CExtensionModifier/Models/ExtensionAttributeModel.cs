using System.ComponentModel;

namespace AADB2CExtensionModifier.Models
{
    public class ExtensionAttributeModel : INotifyPropertyChanged
    {
        private string _attributeName;
        private string _displayName;
        private string _dataType;
        private string _value;
        private string _originalValue;

        public string AttributeName
        {
            get => _attributeName;
            set
            {
                _attributeName = value;
                OnPropertyChanged(nameof(AttributeName));
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(IsModified));
                }
            }
        }

        public string OriginalValue
        {
            get => _originalValue;
            set
            {
                _originalValue = value;
                OnPropertyChanged(nameof(OriginalValue));
                OnPropertyChanged(nameof(IsModified));
            }
        }

        public bool IsModified => _value != _originalValue;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ResetOriginalValue()
        {
            _originalValue = _value;
            OnPropertyChanged(nameof(IsModified));
        }
    }
}
