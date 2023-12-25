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

       

        public LoginViewModel()
        {
          // LoginCommand = new Command(OnLoginClicked);
          
           _timer= new Timer (CheckServerAvailability, null, 0, 20000 );

        }

        public async Task<bool> OnLoginClickedAsync(string username, string password)
        {
            try
            {
                TcpClient client = new TcpClient("185.230.225.4", 3333);
                NetworkStream stream = client.GetStream();

                //Obsługa rządania
                byte[] dataUser = Encoding.ASCII.GetBytes("Login " + username + " " + password);
                stream.Write(dataUser, 0, dataUser.Length);
                await Task.Delay(1500);

                //Obsługa odp string respon = sessionDetails.SessionID + " " + sessionDetails.DataTimeEnd + " "+userDetails.Privileges + " " + userDetails.Login + " " + userDetails.ID;
                dataUser = new byte[256];

                int bytes = stream.Read(dataUser, 0, dataUser.Length);
                string responseData = Encoding.ASCII.GetString(dataUser, 0, bytes);
                string[] parts = responseData.Split(' ');


                if (parts[0] == "LoginSuccessful" && (parts[4] == "1" || parts[4]=="2"))
                {

                    _ = Shell.Current.GoToAsync("//ItemsPage");
                    
                    client.Close();
                    return true;

                }
                else
                {
                    client.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                return false;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                Console.WriteLine(ex.ToString() + "\n");

            }


            

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
                    CheckServerAvailability(DateTime.Now);

                    OnPropertyChanged(nameof(ServerStatusColor));
                }
            }
        }

        private void CheckServerAvailability(object state)
        {
            // Logika sprawdzająca dostępność serwera
           Task<bool> temp = CheckServerAsync();
            bool isServerAvailable= temp.Result;
            if (isServerAvailable)
            {
                ServerStatusColor = Color.Green;
            }
            else
            {
                ServerStatusColor = Color.Red;
            }
        }


        private async Task<bool> CheckServerAsync()
        {
            TcpClient _client = null;
            try
            {
             _client= new TcpClient("185.230.225.4", 3333);
               NetworkStream _stream = _client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes("Ping");
               await _stream.WriteAsync(data, 0, data.Length);

                await Task.Delay(15000);
                data = new byte[256];

                int bytes = await _stream.ReadAsync(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);

                if (responseData == "Pong")
                { 
                    _client.Close();
                    return true;
                }else
                {
                _client.Close();
                return false;
                }

            }catch (Exception ex) {
                Console.WriteLine(ex.ToString());
               
                return false;}
            finally
            {
                _client?.Close();
             
            }
        }


        //    public string SendRequest(string request)
        //{
        //    byte[] requestData = Encoding.ASCII.GetBytes(request);
        //    _stream.Write(requestData, 0, requestData.Length);

        //    byte[] buffer = new byte[1024];
        //    int byteCount = _stream.Read(buffer, 0, buffer.Length);

        //    string response = Encoding.ASCII.GetString(buffer, 0, byteCount);
        //    //Console.WriteLine("Odpowiedź od serwera: " + response);
        //    return request;
        //}





        // Metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor
    }
}
