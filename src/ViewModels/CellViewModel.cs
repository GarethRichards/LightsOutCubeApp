using CommunityToolkit.Mvvm.ComponentModel;

namespace LightsOutCube.ViewModels
{
    public class CellViewModel : ObservableObject
    {
        public int Index { get; }

        private bool _isOn;
        public bool IsOn
        {
            get => _isOn;
            set => SetProperty(ref _isOn, value);
        }

        public CellViewModel(int index)
        {
            Index = index;
            _isOn = false;
        }
    }
}