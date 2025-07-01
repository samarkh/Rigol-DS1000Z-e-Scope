using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DS1000Z_E_USB_Control.Mathematics
{
    /// <summary>
    /// Settings for basic mathematical operations (ADD, SUB, MUL, DIV)
    /// </summary>
    public class BasicOperationsSettings : INotifyPropertyChanged
    {
        #region Private Fields
        private string _source1 = "CHANnel1";
        private string _source2 = "CHANnel2";
        private string _operation = "ADD";
        #endregion

        #region Properties
        /// <summary>
        /// First source channel for basic operations
        /// </summary>
        [JsonPropertyName("source1")]
        public string Source1
        {
            get => _source1;
            set => SetProperty(ref _source1, value);
        }

        /// <summary>
        /// Second source channel for basic operations
        /// </summary>
        [JsonPropertyName("source2")]
        public string Source2
        {
            get => _source2;
            set => SetProperty(ref _source2, value);
        }

        /// <summary>
        /// Mathematical operation (ADD, SUBtract, MULtiply, DIVide)
        /// </summary>
        [JsonPropertyName("operation")]
        public string Operation
        {
            get => _operation;
            set => SetProperty(ref _operation, value);
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Settings for FFT analysis operations
    /// </summary>
    public class FFTAnalysisSettings : INotifyPropertyChanged
    {
        #region Private Fields
        private string _source = "CHANnel1";
        private string _window = "RECTangular";
        private string _split = "FULL";
        private string _unit = "VRMS";
        #endregion

        #region Properties
        /// <summary>
        /// Source channel for FFT analysis
        /// </summary>
        [JsonPropertyName("source")]
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        /// <summary>
        /// FFT window function (RECTangular, HANNing, BLACkman)
        /// </summary>
        [JsonPropertyName("window")]
        public string Window
        {
            get => _window;
            set => SetProperty(ref _window, value);
        }

        /// <summary>
        /// FFT display split mode (FULL, LEFT, RIGHt)
        /// </summary>
        [JsonPropertyName("split")]
        public string Split
        {
            get => _split;
            set => SetProperty(ref _split, value);
        }

        /// <summary>
        /// FFT measurement unit (VRMS, DB, DBM)
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Settings for digital filter operations
    /// </summary>
    public class DigitalFiltersSettings : INotifyPropertyChanged
    {
        #region Private Fields
        private string _filterType = "LPASs";
        private double _w1Frequency = 1000.0;
        private double _w2Frequency = 10000.0;
        #endregion

        #region Properties
        /// <summary>
        /// Digital filter type (LPASs, HPASs, BPASs, BSTop)
        /// </summary>
        [JsonPropertyName("filterType")]
        public string FilterType
        {
            get => _filterType;
            set => SetProperty(ref _filterType, value);
        }

        /// <summary>
        /// Filter frequency W1 parameter (Hz)
        /// </summary>
        [JsonPropertyName("w1Frequency")]
        public double W1Frequency
        {
            get => _w1Frequency;
            set => SetProperty(ref _w1Frequency, value);
        }

        /// <summary>
        /// Filter frequency W2 parameter (Hz)
        /// </summary>
        [JsonPropertyName("w2Frequency")]
        public double W2Frequency
        {
            get => _w2Frequency;
            set => SetProperty(ref _w2Frequency, value);
        }

        /// <summary>
        /// Filter frequency W1 parameter as string (for backward compatibility)
        /// </summary>
        [JsonPropertyName("w1")]
        public string W1
        {
            get => _w1Frequency.ToString();
            set
            {
                if (double.TryParse(value, out double result))
                    W1Frequency = result;
            }
        }

        /// <summary>
        /// Filter frequency W2 parameter as string (for backward compatibility)
        /// </summary>
        [JsonPropertyName("w2")]
        public string W2
        {
            get => _w2Frequency.ToString();
            set
            {
                if (double.TryParse(value, out double result))
                    W2Frequency = result;
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Settings for advanced mathematical functions
    /// </summary>
    public class AdvancedMathSettings : INotifyPropertyChanged
    {
        #region Private Fields
        private string _function = "INTG";
        private string _startPoint = "0";
        private string _endPoint = "100";
        #endregion

        #region Properties
        /// <summary>
        /// Advanced math function (INTG, DIFF, ROOT, LOG, LN, EXP, ABS, MAG)
        /// </summary>
        [JsonPropertyName("function")]
        public string Function
        {
            get => _function;
            set => SetProperty(ref _function, value);
        }

        /// <summary>
        /// Start point for advanced math calculation
        /// </summary>
        [JsonPropertyName("startPoint")]
        public string StartPoint
        {
            get => _startPoint;
            set => SetProperty(ref _startPoint, value);
        }

        /// <summary>
        /// End point for advanced math calculation
        /// </summary>
        [JsonPropertyName("endPoint")]
        public string EndPoint
        {
            get => _endPoint;
            set => SetProperty(ref _endPoint, value);
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}