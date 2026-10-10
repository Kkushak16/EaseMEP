using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartMepRouter.Core.Connectors;
using SmartMepRouter.UI.Settings;

namespace SmartMepRouter.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the main modeless Smart MEP Router dialog window.
    /// Exposes connector bindings, obstacle filter checklist, geometry options, and status.
    /// Implements the state table of design.md section 7.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public const string OptimizationFewerBends = "Low Fitting Count (Fewer Bends)";
        public const string OptimizationShortest = "Shortest Length";
        public const string Bends90Only = "90° Orthogonal Only";
        public const string Bends45And90 = "45° and 90° (FR-08)";

        private string _startConnectorText = "Not selected";
        private string _endConnectorText = "Not selected";
        private bool _isStartPicked;
        private bool _isEndPicked;

        private bool _includeColumns = true;
        private bool _includeFraming = true;
        private bool _includeWalls = true;
        private bool _includeCeilings = false;
        private double _clearanceMarginMm = 100.0;
        private string _clearanceError = string.Empty;

        private bool _isRectangular = true;
        private string _selectedAllowedBends = Bends90Only;
        private string _selectedOptimization = OptimizationFewerBends;
        private MepDiscipline _selectedDiscipline = MepDiscipline.Duct;

        private string _statusMessage = "Ready. Select start and end connectors in the Revit view.";
        private string _banner = string.Empty;
        private bool _isPreviewReady;
        private bool _isBusy;
        private bool _isPicking;
        private bool _isDarkTheme = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised when a setting changes after a preview exists, so the host can discard preview graphics.</summary>
        public event EventHandler? PreviewInvalidated;

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
                    InvalidatePreview();
                    RaiseStateChanged();
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
                    InvalidatePreview();
                    RaiseStateChanged();
                }
            }
        }

        public bool IncludeColumns
        {
            get => _includeColumns;
            set { if (SetField(ref _includeColumns, value)) InvalidatePreview(); }
        }

        public bool IncludeFraming
        {
            get => _includeFraming;
            set { if (SetField(ref _includeFraming, value)) InvalidatePreview(); }
        }

        public bool IncludeWalls
        {
            get => _includeWalls;
            set { if (SetField(ref _includeWalls, value)) InvalidatePreview(); }
        }

        public bool IncludeCeilings
        {
            get => _includeCeilings;
            set { if (SetField(ref _includeCeilings, value)) InvalidatePreview(); }
        }

        /// <summary>Wall/column clearance margin in millimetres. Must be strictly positive.</summary>
        public double ClearanceMarginMm
        {
            get => _clearanceMarginMm;
            set
            {
                if (SetField(ref _clearanceMarginMm, value))
                {
                    ClearanceError = value > 0.0 && !double.IsNaN(value) && !double.IsInfinity(value)
                        ? string.Empty
                        : "Enter a positive number of millimetres.";
                    InvalidatePreview();
                    RaiseStateChanged();
                }
            }
        }

        public string ClearanceError
        {
            get => _clearanceError;
            private set { if (SetField(ref _clearanceError, value)) OnPropertyChanged(nameof(HasClearanceError)); }
        }

        public bool HasClearanceError => !string.IsNullOrEmpty(_clearanceError);

        public bool IsRectangular
        {
            get => _isRectangular;
            set
            {
                if (SetField(ref _isRectangular, value))
                {
                    OnPropertyChanged(nameof(IsRound));
                    InvalidatePreview();
                }
            }
        }

        public bool IsRound
        {
            get => !_isRectangular;
            set => IsRectangular = !value;
        }

        public string SelectedAllowedBends
        {
            get => _selectedAllowedBends;
            set { if (SetField(ref _selectedAllowedBends, value)) InvalidatePreview(); }
        }

        public bool Allow45DegreeBends => _selectedAllowedBends != null && _selectedAllowedBends.Contains("45");

        public string SelectedOptimization
        {
            get => _selectedOptimization;
            set { if (SetField(ref _selectedOptimization, value)) InvalidatePreview(); }
        }

        public MepDiscipline SelectedDiscipline
        {
            get => _selectedDiscipline;
            set
            {
                if (SetField(ref _selectedDiscipline, value))
                {
                    OnPropertyChanged(nameof(IsDuctDiscipline));
                    OnPropertyChanged(nameof(IsPipeDiscipline));
                    OnPropertyChanged(nameof(IsCableTrayDiscipline));
                    OnPropertyChanged(nameof(GenerateButtonText));
                    InvalidatePreview();
                }
            }
        }

        public bool IsDuctDiscipline
        {
            get => _selectedDiscipline == MepDiscipline.Duct;
            set { if (value) SelectedDiscipline = MepDiscipline.Duct; }
        }

        public bool IsPipeDiscipline
        {
            get => _selectedDiscipline == MepDiscipline.Pipe;
            set { if (value) SelectedDiscipline = MepDiscipline.Pipe; }
        }

        public bool IsCableTrayDiscipline
        {
            get => _selectedDiscipline == MepDiscipline.CableTray;
            set { if (value) SelectedDiscipline = MepDiscipline.CableTray; }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { SetField(ref _statusMessage, value); }
        }

        /// <summary>Banner shown while the user must act in the Revit view (design.md section 7). Empty hides it.</summary>
        public string Banner
        {
            get => _banner;
            set { if (SetField(ref _banner, value)) OnPropertyChanged(nameof(HasBanner)); }
        }

        public bool HasBanner => !string.IsNullOrEmpty(_banner);

        public bool IsPreviewReady
        {
            get => _isPreviewReady;
            set { if (SetField(ref _isPreviewReady, value)) RaiseStateChanged(); }
        }

        /// <summary>True while a search or generation is running; disables pick/generate and turns Preview into Cancel.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { if (SetField(ref _isBusy, value)) RaiseStateChanged(); }
        }

        /// <summary>True while the user is picking in the Revit view.</summary>
        public bool IsPicking
        {
            get => _isPicking;
            set { if (SetField(ref _isPicking, value)) RaiseStateChanged(); }
        }

        public bool CanPick => !IsBusy && !IsPicking;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set { if (SetField(ref _isDarkTheme, value)) OnPropertyChanged(nameof(ThemeButtonText)); }
        }

        /// <summary>Label of the toggle: names the theme the click switches to.</summary>
        public string ThemeButtonText => IsDarkTheme ? "Light" : "Dark";

        public bool CanPreview => IsBusy || (IsStartPicked && IsEndPicked && !HasClearanceError && !IsPicking);

        public bool CanGenerate => !IsBusy && !IsPicking && IsStartPicked && IsEndPicked && !HasClearanceError && IsPreviewReady;

        public string PreviewButtonText => IsBusy ? "Cancel" : "Preview Path";

        public string GenerateButtonText => _selectedDiscipline switch
        {
            MepDiscipline.Pipe => "✓ Generate Pipes",
            MepDiscipline.CableTray => "✓ Generate Cable Trays",
            _ => "✓ Generate Ducts"
        };

        public void AppendStatus(string line)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"\n{time}  {line}";
            string current = StatusMessage ?? string.Empty;

            if (current.Length > 20000)
            {
                int midNewline = current.IndexOf('\n', current.Length / 2);
                if (midNewline >= 0)
                {
                    current = current.Substring(midNewline);
                }
            }

            StatusMessage = current + entry;
        }

        /// <summary>Copies persisted values into this view model (FR-15). Connectors are never persisted.</summary>
        public void ApplySettings(UserSettings s)
        {
            IncludeColumns = s.IncludeColumns;
            IncludeFraming = s.IncludeFraming;
            IncludeWalls = s.IncludeWalls;
            IncludeCeilings = s.IncludeCeilings;
            IsRectangular = s.IsRectangular;
            if (s.ClearanceMarginMm > 0.0) ClearanceMarginMm = s.ClearanceMarginMm;
            IsDarkTheme = s.Theme != "Light";
            if (s.Optimization == OptimizationFewerBends || s.Optimization == OptimizationShortest)
                SelectedOptimization = s.Optimization;
            if (!string.IsNullOrEmpty(s.Discipline) && Enum.TryParse(s.Discipline, out MepDiscipline disc))
                SelectedDiscipline = disc;
        }

        public UserSettings ToSettings() => new UserSettings
        {
            IncludeColumns = IncludeColumns,
            IncludeFraming = IncludeFraming,
            IncludeWalls = IncludeWalls,
            IncludeCeilings = IncludeCeilings,
            IsRectangular = IsRectangular,
            ClearanceMarginMm = ClearanceMarginMm,
            Optimization = SelectedOptimization,
            Theme = IsDarkTheme ? "Dark" : "Light",
            Discipline = SelectedDiscipline.ToString()
        };

        private void InvalidatePreview()
        {
            if (_isPreviewReady)
            {
                IsPreviewReady = false;
                PreviewInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RaiseStateChanged()
        {
            OnPropertyChanged(nameof(CanPick));
            OnPropertyChanged(nameof(CanPreview));
            OnPropertyChanged(nameof(CanGenerate));
            OnPropertyChanged(nameof(PreviewButtonText));
            OnPropertyChanged(nameof(GenerateButtonText));
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
