using DataKlient.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DataKlient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
       public Command LoginCommand { get; }
       

        private bool isCheckBoxChecked=false;
        private bool isPasswordEnabled=true;

        private Color serverStatusColor=Color.Red;
        private readonly Timer _timer;

        /// na potrzeby testów ping-pong
        
        
        private TcpClient _client;
        private NetworkStream _stream;

        /// na potrzeby testów ping-pong

        public LoginViewModel()
        {
           LoginCommand = new Command(OnLoginClicked);
            
           _timer= new Timer (CheckServerAvailability, null, 0, 20000 );

        }

        private  void OnLoginClicked()
        {

          
             Shell.Current.GoToAsync("//ItemsPage");

        }


        public bool IsCheckBoxChecked
        {
            get { return isCheckBoxChecked; }
            set
            {
                if (isCheckBoxChecked != value)
                {
                    isCheckBoxChecked = value;
                    ShowHidePassword();
                    OnPropertyChanged(nameof(IsCheckBoxChecked));
                }
            }

        }


        public bool IsPasswordEnabled
        {
            get { return isPasswordEnabled; }
            set
            {
                if (isPasswordEnabled != value)
                {
                    isPasswordEnabled = value;
                    OnPropertyChanged(nameof(IsPasswordEnabled));
                }

            }
        }

        private void ShowHidePassword()
        {
            if (IsCheckBoxChecked)
            {

                IsPasswordEnabled = false;
            }
            else
            {
                IsPasswordEnabled = true;
            }

        }

        // Metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor

        public Color ServerStatusColor
        {
            get { return serverStatusColor; }
            set
            {
                if (serverStatusColor != value)
                {
                    serverStatusColor = value;
                    //CheckServerAvailability();
                    OnPropertyChanged(nameof(ServerStatusColor));
                }
            }
        }
        
        private async void CheckServerAvailability(object state)
        {
            // Logika sprawdzająca dostępność serwera
            bool isServerAvailable = CheckServer(); // Możesz użyć odpowiedniej logiki do sprawdzenia stanu serwera
           
            ServerStatusColor = isServerAvailable ? Color.Green : Color.Red;
        }


        private bool CheckServer()
        {
            try
            {
                _client = new TcpClient("192.168.1.90", 3333);
                _stream = _client.GetStream();

                if (SendRequest("PING") == "PONG")
                {
                    _client.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }catch (Exception ex) {

               Console.WriteLine(ex.ToString());
                return false;
            }
          


        }


        public string SendRequest(string request)
        {
            byte[] requestData = Encoding.ASCII.GetBytes(request);
            _stream.Write(requestData, 0, requestData.Length);

            byte[] buffer = new byte[1024];
            int byteCount = _stream.Read(buffer, 0, buffer.Length);

            string response = Encoding.ASCII.GetString(buffer, 0, byteCount);
            //Console.WriteLine("Odpowiedź od serwera: " + response);
            return request;
        }





        // Metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor
    }
}
