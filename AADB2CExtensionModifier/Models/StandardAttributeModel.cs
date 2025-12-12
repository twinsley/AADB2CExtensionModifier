using System.ComponentModel;

namespace AADB2CExtensionModifier.Models
{
    public class StandardAttributeModel : INotifyPropertyChanged
    {
        private string _propertyName;
        private string _displayName;
        private string _dataType;
        private string _value;
        private string _originalValue;
        private bool _isReadOnly;

        public string PropertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
                OnPropertyChanged(nameof(PropertyName));
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
                if (_value != value && !_isReadOnly)
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

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }

        public bool IsModified => !_isReadOnly && _value != _originalValue;

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
