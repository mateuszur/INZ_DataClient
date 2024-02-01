using DataKlient.Models;
using DataKlient.Services;
using MobileCoreServices;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;
using static CoreFoundation.DispatchSource;
using static Org.BouncyCastle.Math.EC.ECCurve;


namespace DataKlient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {

        public Command LoginCommand { get; }


        private bool isCheckBoxChecked = false;
        private bool isPasswordEnabled = true;

        private Color serverStatusColor = Color.Red;
        private System.Threading.Timer _timer = null;

        private string __sessionID;
        private int __userID;

        private string localPath;

        //Zaczytanie konfiguracji
        private string serverAddress;
        private int dataServerPort;
        private int sFTPPort;
        private byte[] key;
        private byte[] iv;
        private ClientConfig _clientConfig;
        private ReadConfig _readConfig;
        private Services.SessionLocalDetailsService _session = new Services.SessionLocalDetailsService();
        private Models.SessionLocalDetailsService _newSession = new Models.SessionLocalDetailsService();

        public LoginViewModel()
        {
            this.localPath = LocalPathInit();
            //inicjalizacja parametrów lokalnych
            _clientConfig = new ClientConfig();
            _readConfig = new ReadConfig();
            _readConfig.ReadConfiguration(_clientConfig);

            this.serverAddress=_clientConfig.ServerAddress;
            this.dataServerPort = _clientConfig.DataServerPort;
            this.sFTPPort = _clientConfig.SFTPPort;
            this.key = StringToByteArray(_clientConfig.Key);
            this.iv = StringToByteArray(_clientConfig.IV);


            _timer = new System.Threading.Timer(CheckServerAvailability, null, 0, 10000);

        }



        public async Task<bool> OnLoginClickedAsync(string username, string password)
        {
            TcpClient _client = null;

            try
            {
                _client = new TcpClient(serverAddress, dataServerPort);
                NetworkStream stream = _client.GetStream();

                //Obsługa żądania
                string plaintext = "Login " + username + " " + password;
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                stream.Write(data, 0, data.Length);

             
                await Task.Delay(1500);

                //Obsługa odp string respon = sessionDetails.SessionID + " " + sessionDetails.DataTimeEnd + " "+userDetails.Privileges + " " + userDetails.Login + " " + userDetails.ID;
                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                string[] parts = responseData.Split(',');


                if (parts[0] == "LoginSuccessful" && (parts[3] == "1" || parts[3] == "2"))
                {
                   

                    _newSession.sessionID = parts[1];
                    _newSession.endSesionDate = parts[2];
                    _newSession.privileges = parts[3];
                    _newSession.userLogin = parts[4];
                    _newSession.userID = int.Parse(parts[5]);
                    _newSession.isValid = 1;

                    await _session.AddItem(_newSession);

                    _client.Close();

                    await GetDataListFromServerAsync();
                    _ = Shell.Current.GoToAsync("//ItemsPage");
                    _timer.Dispose();
                    return true;

                }
                else
                {
                    _client.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                _client?.Close();
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                Console.WriteLine(ex.ToString() + "\n");
                return false;
               

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

        public void CheckServerAvailability(object state)
        {
            // Logika sprawdzająca dostępność serwera
            Task<bool> temp = CheckServerAsync();
            bool isServerAvailable = temp.Result;
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
                _client = new TcpClient(serverAddress, dataServerPort);
                NetworkStream _stream = _client.GetStream();
               
                string plaintext = "Ping";
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                _stream.Write(data, 0, data.Length);

                await Task.Delay(1750);
               
                data = new byte[256];
                int bytes = _stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                responseData= DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                if (responseData == "Pong")
                {
                    _client.Close();
                    return true;
                }
                else
                {
                    _client.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return false;
            }
            finally
            {
                _client?.Close();

            }
        }

        //Koniec metody sprawdzająca dostępność serwera i ustawiająca odpowiedni kolor


        //Metody sprawdzajace czy są loklane sesje- sesja na serwerze 24h
        public async Task<bool> CheckLocalSessionAsync()
        {

            TcpClient _client = null;
            try
            {
                Services.SessionLocalDetailsService _activeSession = new Services.SessionLocalDetailsService();
                var _Session = await _activeSession.GetItems();

                if (_Session != null && _Session.Count != 0)
                {
                    _client = new TcpClient(serverAddress, dataServerPort);
                    NetworkStream stream = _client.GetStream();

                    string plaintext = "IsSessionValid " + _Session[0].sessionID + " " + _Session[0].userID;
                    string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                    byte[] dataSessio = Encoding.ASCII.GetBytes(encryptedMessage);
                  
                    stream.Write(dataSessio, 0, dataSessio.Length);
                    await Task.Delay(1000);

                    dataSessio = new byte[256];
                    int bytes = stream.Read(dataSessio, 0, dataSessio.Length);
                    string responseData = Encoding.ASCII.GetString(dataSessio, 0, bytes);
                    responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                    if (responseData == "Sesion is valid")
                    {

                        _ = Shell.Current.GoToAsync("//ItemsPage");
                       
                        _timer.Dispose();
                        _client.Close();
                        return true;
                    }
                    else
                    {
                            Services.SessionLocalDetailsService _session = new Services.SessionLocalDetailsService();
                            Models.SessionLocalDetailsService _newSession = _Session[0];

                        _newSession.isValid = 0;

                        await _session.UpdateSelectedItem(_newSession);

                        _client.Close();
                        return false;
                    }


                }
                else
                {
                    _client?.Close();
                    return false;
                }

            }
            catch (Exception ex)
            {
                _client?.Close();
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                Console.WriteLine(ex.ToString() + "\n");
                return false;
            }
        }

        private async Task CheckCert()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:

                    var filePath = Path.Combine(localPath, "..", "klucz_prywatny.pem");
                    var filePath2 = Path.Combine(localPath, "..", "klucz_publiczny.pem");

                    if (!File.Exists(filePath) || !File.Exists(filePath2))
                    {
                        Console.Write("Certy nie istnieją- generuje nowe\n");
                        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                        rsa.KeySize = 2048;
                        AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetRsaKeyPair(rsa);

                        // Zapisz klucz prywatny do pliku PEM
                        using (TextWriter textWriter = new StreamWriter(filePath))
                        {
                            PemWriter pemWriter = new PemWriter(textWriter);
                            pemWriter.WriteObject(keyPair.Private);
                        }

                        // Zapisz klucz publiczny do pliku PEM
                        using (TextWriter textWriter = new StreamWriter(filePath2))
                        {
                            PemWriter pemWriter = new PemWriter(textWriter);
                            pemWriter.WriteObject(keyPair.Public);
                        }

                    }
                    //else
                    //{

                    //    using (StreamReader reader = new StreamReader(filePath2))
                    //    {
                    //        // Odczytaj plik jako tekst
                    //        string pemFileContent = reader.ReadToEnd();

                    //        // Wyświetl zawartość pliku na ekranie
                    //        Console.WriteLine(pemFileContent);
                    //    }
                    //}
            break;

                case Device.UWP:
                    if (!File.Exists(localPath + "\\klucz_prywatny.pem") || !File.Exists(localPath + "\\klucz_publiczny.pem"))
                    {
                        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                        rsa.KeySize = 2048;
                        AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetRsaKeyPair(rsa);

                        // Zapisz klucz prywatny do pliku PEM
                        using (TextWriter textWriter = new StreamWriter(localPath + "\\klucz_prywatny.pem"))
                        {
                            PemWriter pemWriter = new PemWriter(textWriter);
                            pemWriter.WriteObject(keyPair.Private);
                        }

                        // Zapisz klucz publiczny do pliku PEM
                        using (TextWriter textWriter = new StreamWriter(localPath + "\\klucz_publiczny.pem"))
                        {
                            PemWriter pemWriter = new PemWriter(textWriter);
                            pemWriter.WriteObject(keyPair.Public);
                        }


                    }
                    break;
            }




        }


        public async Task<bool> CheckServerConfig()
        {
            try
            {
                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                        if (!File.Exists(localPath + "/config.txt"))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                        break;

                    case Device.UWP:
                        if (!File.Exists(localPath + "\\config.txt"))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                        break;

                    default:
                        return false;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }


        public async Task SetSettings()
        {

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    SetSettingsIOS();
                    break;

                case Device.UWP:
                    SetSettingsUWP();
                    break;
            }
        }


        public async Task SetSettingsUWP()
        {
            try
            {

                var result = await FilePicker.PickAsync();

                if (result == null)
                {
                    return;
                }

                var stream = await result.OpenReadAsync();

                string fileName = result.FileName;

                if (result.FileName == "config.txt")
                {

                    string fullPath = Path.Combine(localPath, fileName);



                    if (System.IO.File.Exists(fullPath))
                    {

                        System.IO.File.Delete(fullPath);
                    }

                    using (var fileStream = System.IO.File.Create(fullPath))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(fileStream);
                    }

                    return;
                }
            }
            catch (Exception e)
            {
                //miejsce na log
                return;
            }
        }

        public async Task SetSettingsIOS()
        {

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var library = Path.Combine(documents, "config.txt");

            Directory.CreateDirectory(localPath + "TEST" + DateTime.Now);

            string path = Path.Combine(documents, "config.txt");

            var content = File.ReadAllText(path);

            Console.Write("Błąd przy odczycie pliku!");
        }


        private async Task GetDataListFromServerAsync()
        {

            TcpClient _client = null;
            DataStore dataStore = new DataStore();
            FileItem fileItem = new FileItem();
            try
            {
                
                await dataStore.DeleteDatabaseAsync();
                _client = new TcpClient(serverAddress, dataServerPort);
                NetworkStream stream = _client.GetStream();

                string plaintext = "List " + _newSession.sessionID + " " + _newSession.userID;
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);


                byte[] buffer = new byte[1024];
                var memoryStream = new MemoryStream();

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                data = memoryStream.ToArray();

                _client.Close();

                //string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                string responseData = Encoding.ASCII.GetString(data, 0, data.Length);
                responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                string[] parts = responseData.Split(',');

                if (parts.Length > 0)
                {


                    for (int i = 1; i < parts.Length - 1; i++)
                    {
                        string[] parts2 = parts[i].Split(' ');


                        fileItem.FileName = parts2[0].ToString();
                        fileItem.FileSize = parts2[1].ToString();
                        fileItem.FileType = parts2[2].ToString();
                        fileItem.UserID = _newSession.userID;
                        fileItem.DateOfTransfer = parts2[3].ToString() + parts2[4].ToString();


                        await dataStore.AddItemAsync(fileItem);
                    }
                }

            }
            catch (Exception ex)
            {
                _client?.Close();
                return;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami

            }
        }

        private string LocalPathInit()
        {
            string path;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    return path;
                    break;

                case Device.UWP:
                    path = System.IO.Path.Combine(FileSystem.AppDataDirectory) ;
                    return path;
                    break;
                default:
                    return null;
                    break;
            }
        }

        static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            string plaintext = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

    }
}
