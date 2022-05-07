// Copyright (c) 2022 Lukin Aleksandr
// e-mail: lukin.a.g.spb@gmail.com
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Avalonia.Controls;


namespace SimpleLinuxTerminal
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {       
       
        public ObservableCollection<string> Ports { get; private set; } = new ObservableCollection<string>();
        public ObservableCollection<int>  BaudRateValues{get;private set;} = new ObservableCollection<int>()
        {
            300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 500600, 576000, 921600, 1000000, 1152000, 1500000, 2000000, 2500000, 3000000, 3500000, 4000000
        };

        List<Byte> _RxBytes = new List<byte>();

        SerialPort _SerialPort = new SerialPort();
        
        Mutex _Mx = new Mutex(false,"SLT_RxTextChargedMutex");       

        #region PortSelectedIndex
        int _PortSelectedIndex;        
        public int PortSelectedIndex
        {
            get => _PortSelectedIndex;

            set
            {
                _PortSelectedIndex = value;
                OnPropertyChanged("PortSelectedIndex");
            }
        }
        #endregion PortSelectedIndex
        #region IsConnected
        bool _IsConnected = false;
        public bool IsConnected
        {
            get => _IsConnected;
            set
            {
                _IsConnected = value;
                OnPropertyChanged("IsConnected");
            }
        }
        #endregion IsConnected
        #region IsStandartBaudRate
        bool _IsStandartBaudRate = true;
        public bool IsStandartBaudRate
        {
            get => _IsStandartBaudRate;
            set
            {
                _IsStandartBaudRate = value;
                OnPropertyChanged("IsStandartBaudRate");
            }
        }
        #endregion IsStandartBaudRate
        #region BaudRateSelectedIndex
        int _BaudRateSelectedIndex = 6;
        public int BaudRateSelectedIndex
        {
            get => _BaudRateSelectedIndex;
            set
            {
                _BaudRateSelectedIndex = value;
                OnPropertyChanged("BaudRateSelectedIndex");
            }
        }
        #endregion BaudRateSelectedIndex
        #region CustomBaudRate
        string _CustomBaudRate = "";
        public string CustomBaudRate
        {
            get => _CustomBaudRate;
            set
            {
                _CustomBaudRate = value;
                OnPropertyChanged("CustomBaudRate");
            }
        }
        #endregion CustomBaudRate
        #region RxValue
        string _RxValue = "";
        public string RxValue
        {
            get => _RxValue;
            set
            {
                _RxValue = value;
                OnPropertyChanged("RxValue");
            }
        }
        #endregion RxValue
        #region IsHexRxBytesFormat
        bool _IsHexRxBytesFormat = true;
        public bool IsHexRxBytesFormat
        {
            get => _IsHexRxBytesFormat;
            set
            {                
                _IsHexRxBytesFormat = value;
                OnPropertyChanged("IsHexRxBytesFormat");
            }
        }
        #endregion IsHexRxBytesFormat

        public MainWindow()
        {
            InitializeComponent();           
           
            DataContext = this;           
            RescanPorts();

            _SerialPort.DataReceived += new SerialDataReceivedEventHandler(OnRxSerialPortData);

            this.PropertyChanged += new PropertyChangedEventHandler((x,y)=>
            {
                if (y.PropertyName == "IsHexRxBytesFormat")
                {
                    _Mx.WaitOne();

                    if (_IsHexRxBytesFormat)                 
                        ConvertRxTextToHex();                  
                    else                  
                        ConvertRxTextToAscii();

                    _Mx.ReleaseMutex();
                }
            });
        }

        public void Connect()
        {           
            int n; 
            _SerialPort.PortName = GetPortName();    

            n = GetSelectedBaudRate(); 
            if (n == 0) return;
            _SerialPort.BaudRate = n; 

            _SerialPort.Open();                     
           
            IsConnected = _SerialPort.IsOpen;
        }

        public void Disconnect()
        {
            if (_SerialPort.IsOpen)
                _SerialPort.Close();
            IsConnected = false;
        }

        string GetPortName()
        {
            if (PortSelectedIndex < 0 || Ports?.Count == 0) return "";
            return Ports?[PortSelectedIndex]??string.Empty;
        }

        int GetSelectedBaudRate()
        {
            int result;
            if (IsStandartBaudRate)
            {
                return BaudRateValues[BaudRateSelectedIndex];
            }
            if (Int32.TryParse(_CustomBaudRate,out result))
                return result;
            return 0;
        }

        public void RescanPorts()
        {
            Ports.Clear();
            var ports = SerialPort.GetPortNames();
            int counter = -1;
            foreach (var item in ports)
            {
                Ports.Add(item);
                counter++;
            }
            PortSelectedIndex = counter;
        }

        public void ClearRxData()
        {
            _Mx.WaitOne();

            RxValue = string.Empty;
            _RxBytes.Clear();

            _Mx.ReleaseMutex();
        }

        void ConvertRxTextToHex()
        {
            string s = "";           
            foreach (var b in _RxBytes)
            {               
                s += (b > 16)? b.ToString("x") + ' ': "0"+ b.ToString("x") + ' '; 
            }
            RxValue = s;
        }

        void ConvertRxTextToAscii()
        {           
            ASCIIEncoding ascii = new ASCIIEncoding();
            RxValue = ascii.GetString(_RxBytes.ToArray(),0,_RxBytes.Count);
        }

        private async void OutputRxData(List<byte> bytes)
        {            
            string s = "";      
            _Mx.WaitOne();

            if (bytes.Count == 0) return;
            if (_IsHexRxBytesFormat)
            {
                foreach (byte b in bytes)
                {
                    s += (b > 16)? b.ToString("x") + ' ': "0"+ b.ToString("x") + ' ';              
                }
            }
            else
            {
                ASCIIEncoding ascii = new ASCIIEncoding();
                s = ascii.GetString(bytes.ToArray(),0,bytes.Count);
            }

            RxValue += s;

            _Mx.ReleaseMutex();
        }

        void OnRxSerialPortData(object sender, SerialDataReceivedEventArgs e)
        {            
            int count = 0;           
            
            _Mx.WaitOne();

            byte[] bytes = new byte[512];
            try
            {
                count = _SerialPort.Read(bytes, 0, 512);                
                var buffer = new List<byte>(); 
                for (int i = 0;i < count;i++)
                {
                    buffer.Add(bytes[i]);
                    _RxBytes.Add(bytes[i]);
                }
                OutputRxData(buffer);
            }
            catch { }   
            
            _Mx.ReleaseMutex();
            
        }

        #region PropertyChanged
        public new event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion PropertyChanged
    }
}