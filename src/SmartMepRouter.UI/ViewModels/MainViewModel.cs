using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartMepRouter.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the main modeless Smart MEP Router dialog window.
    /// Exposes connector bindings, obstacle filter checklist, geometry options, and status.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _startConnectorText = "Not selected";
        private string _endConnectorText = "Not selected";
        private bool _isStartPicked;
        private bool _isEndPicked;

        private bool _includeColumns = true;
        private bool _includeFraming = true;
        private bool _includeWalls = true;
        private bool _includeCeilings = false;
        private double _clearanceMarginMm = 100.0;

        private bool _isRectangular = true;
        private string _selectedAllowedBends = "90° Orthogonal Only";
        private string _selectedOptimization = "Low Fitting Count (Fewer Bends)";

        private string _statusMessage = "Ready. Select start and end connectors in the Revit view.";
        private bool _isPreviewReady;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string StartConnectorText
        {
            get => _startConnectorText;
            set { SetField(ref _startConnectorText, value); }
        }

        public string EndConnectorText
        {
            get => _endConnectorText;
            set { SetField(ref _endConnectorText, value); }
        }

        public bool IsStartPicked
        {
            get => _isStartPicked;
            set
            {
                if (SetField(ref _isStartPicked, value))
                {
                    OnPropertyChanged(nameof(CanPreview));
                }
            }
        }

        public bool IsEndPicked
        {
            get => _isEndPicked;
            set
            {
                if (SetField(ref _isEndPicked, value))
                {
                    OnPropertyChanged(nameof(CanPreview));
                }
            }
        }

        public bool IncludeColumns
        {
            get => _includeColumns;
            set { SetField(ref _includeColumns, value); }
        }

        public bool IncludeFraming
        {
            get => _includeFraming;
            set { SetField(ref _includeFraming, value); }
        }

        public bool IncludeWalls
        {
            get => _includeWalls;
            set { SetField(ref _includeWalls, value); }
        }

        public bool IncludeCeilings
        {
            get => _includeCeilings;
            set { SetField(ref _includeCeilings, value); }
        }

        public double ClearanceMarginMm
        {
            get => _clearanceMarginMm;
            set { SetField(ref _clearanceMarginMm, value); }
        }

        public bool IsRectangular
        {
            get => _isRectangular;
            set { SetField(ref _isRectangular, value); }
        }

        public string SelectedAllowedBends
        {
            get => _selectedAllowedBends;
            set { SetField(ref _selectedAllowedBends, value); }
        }

        public string SelectedOptimization
        {
            get => _selectedOptimization;
            set { SetField(ref _selectedOptimization, value); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { SetField(ref _statusMessage, value); }
        }

        public bool IsPreviewReady
        {
            get => _isPreviewReady;
            set
            {
                if (SetField(ref _isPreviewReady, value))
                {
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        public bool CanPreview => IsStartPicked && IsEndPicked;

        public bool CanGenerate => CanPreview && IsPreviewReady;

        public void AppendStatus(string line)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            StatusMessage += $"\n{time}  {line}";
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
