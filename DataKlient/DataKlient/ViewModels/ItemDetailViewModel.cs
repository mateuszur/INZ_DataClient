using DataKlient.Models;
using DataKlient.Services;
using FluentFTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;

namespace DataKlient.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    [QueryProperty(nameof(FileName), nameof(FileName))]

    public class ItemDetailViewModel : BaseViewModel
    {
        private int itemId;
        private string fileName;
        private string fileExtension;
        private string fileSize;
        private string dateOfTransfer;

        private string localPath;

        private bool _isFileLocal;
        private Models.SessionLocalDetailsService _usedSession;

        //Zaczytanie konfiguracji
        private string serverAddress;
        private int dataServerPort;
        private int sFTPPort;
        private byte[] key;
        private byte[] iv;
        private ClientConfig _clientConfig;
        private ReadConfig _readConfig;


        public ItemDetailViewModel()
        {
            //inicjalizacja parametrów lokalnych
            _clientConfig = new ClientConfig();
            _readConfig = new ReadConfig();
            _readConfig.ReadConfiguration(_clientConfig);

            this.serverAddress = _clientConfig.ServerAddress;
            this.dataServerPort = _clientConfig.DataServerPort;
            this.sFTPPort = _clientConfig.SFTPPort;
            this.key = StringToByteArray(_clientConfig.Key);
            this.iv = StringToByteArray(_clientConfig.IV);
            this.localPath=   LocalPathInit();


        }

        public string Id { get; set; }

        public string FileName
        {
            get
            {
             return   fileName;
            }
            set {
                SetProperty(ref fileName, value);
                fileName= value;
            }
        }

        public string FileExtension
        {
            get => fileExtension;
            set => SetProperty(ref fileExtension, value);
        }


        public string FileSize
        {
            get => fileSize;
            set => SetProperty(ref fileSize, value);
        }

        public string DateOfTransfer
        {

            get => dateOfTransfer;
            
            set=> SetProperty(ref dateOfTransfer, value);
        }

        public int ItemId
        {
            get
            {
                return itemId;
            }
            set
            {
                itemId = value;
                LoadItemId(value);
            }
        }

        public async void LoadItemId(int itemId)
        {
            try
            {
                var item = await DataStore.GetItemByIdAsync(itemId);
             
                FileName = item.FileName;
                FileExtension = item.FileType;
                FileSize= item.FileSize;

                string date = item.DateOfTransfer;
                date = date.Insert(10, " ");

                DateOfTransfer = date;
                OnStartLocalFileChceck();
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }
        private async Task GetUsedSession()
        {
            try
            {
                Services.SessionLocalDetailsService _activeSession = new Services.SessionLocalDetailsService();
                var _session = await _activeSession.GetItems();

                if (_session != null && _session.Count != 0)
                {
                    _usedSession = _session[0];
                    return;
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                return;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami

            }
        }

        public async Task OnDownloadClicked()
        {
            TcpClient _client = null;
            DataStore dataStore = new DataStore();
          // string localPath= Path.Combine(FileSystem.AppDataDirectory)+"\\Download";
            try
            {
                await GetUsedSession();
               _client = new TcpClient(serverAddress, dataServerPort);
               
                NetworkStream stream = _client.GetStream();

                string plaintext = "Download " + _usedSession.sessionID + " " + _usedSession.userID + " " + fileName;
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                if (responseData.StartsWith("YourPathToDownload"))
                {
                    string[] parts = responseData.Split(' ');

                    string host = serverAddress; //IP serwera FTP ten sam co DataSerwer
                    int portFTP = sFTPPort;
                    string path = parts[1];
                    string username = parts[2]; //ze stringa1
                    string passwd = parts[3]; //ze stringa 

                    var client = new AsyncFtpClient(host, username, passwd);
                    client.Port = portFTP;

                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;

                    //nawiazanie połaczenia i przesłanie pliku
                    await client.Connect();

                    

                    switch (Device.RuntimePlatform) {
                        case Device.iOS:
                            if (!Directory.Exists(localPath))
                            {
                                Directory.CreateDirectory(localPath);

                                await client.DownloadFile(localPath + "/" + fileName, path);
                                Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                            }
                            else
                            {
                                if ((await client.CompareFile(localPath + "/" + fileName, path)) != FtpCompareResult.NotEqual)
                                {
                                    await client.DownloadFile(localPath + "/" + fileName, path);
                                    Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                                }

                            }
                            break;
                        case Device.UWP:
                            if (!Directory.Exists(localPath))
                            {
                                Directory.CreateDirectory(localPath);

                                await client.DownloadFile(localPath + "\\" + fileName, path);
                                Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                            }
                            else
                            {
                                if ((await client.CompareFile(localPath + "\\" + fileName, path)) != FtpCompareResult.NotEqual)
                                {
                                    await client.DownloadFile(localPath + "\\" + fileName, path);
                                    Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                                }

                            }
                            break;
                    }

                    await Task.Delay(1000);
                    await client.Disconnect();
                    OnStartLocalFileChceck();


                }
            }
            catch
            {
                Console.WriteLine("Błąd podczas pobierania pliku!");
                _client?.Close();
                return;
            }

        }

      public bool OnStartLocalFileChceck()
        {

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    if (File.Exists(localPath + "/" + fileName))
                    {
                        IsFileLocal = true;
                        return true;

                    }
                    else
                    {
                        IsFileLocal = false;
                        return false;
                    }

                    break;


                    case Device.UWP:
                    if (File.Exists(localPath + "\\" + fileName))
                    {
                        IsFileLocal = true;
                        return true;

                    }
                    else
                    {
                        IsFileLocal = false;
                        return false;
                    }

                    break;
                default:
                    return false;
                    break;
            }
            
            
        }


        public bool IsFileLocal
        {
            get { return _isFileLocal; }
            set
            {
                if (_isFileLocal != value)
                {
                    _isFileLocal = value;
                    OnPropertyChanged(nameof(IsFileLocal));
                }

            }
        }


        public async Task OpenFileClicked()
        {//zmian mac
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                  await  OpenFileClickedAsyncIOS();
                    break;

                case Device.UWP:
                  await  OpenFileClickedAsyncUWP();
                    break;
            } 
        }


        public async Task OpenFileClickedAsyncUWP()
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(localPath + "\\" + fileName)
            });
        }

        private async Task OpenFileClickedAsyncIOS()
        {
            try {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(localPath + "/" + fileName)

                });

                var directories =  Directory.EnumerateDirectories(localPath);
                foreach (var directory in directories)
                {
                    Console.WriteLine("File: "+directory);
                }

               // Console.WriteLine("OK:"+localPath+ "/" + fileName);
                
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd otwierania: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Błąd otwierania Koniec. ");
            }
        }

        private string LocalPathInit()
        {


            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var outPath = System.IO.Path.Combine(path, "Download");
                    return outPath;
                    break;

                case Device.UWP:
                    var path2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var outPath2 = System.IO.Path.Combine(path2, "Download");

                    if (!Directory.Exists(outPath2))
                    {
                        Directory.CreateDirectory(outPath2);
                    }

                    return outPath2;
                    break;
                default:
                    return null;
                    break;
            }
        }


        public async Task DeleteButtonClickedAsync()
        {
            TcpClient _client = null;
            try
            {
                await GetUsedSession();
                _client = new TcpClient(serverAddress, dataServerPort);
                NetworkStream stream = _client.GetStream();


                string plaintext = "Delete " + _usedSession.sessionID + " " + _usedSession.userID + " " + fileName;
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);
                _client.Close();

                if (responseData.StartsWith("FileDeletedSuccessfully"))
                {
                    var filePath =System.IO.Path.Combine(localPath, fileName);
                       
                    await DataStore.DeleteItemAsync(itemId);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _ = Shell.Current.GoToAsync("//ItemsPage");

                    }


                }
                else
                {
                    await DataStore.DeleteItemAsync(itemId);
                    _ = Shell.Current.GoToAsync("//ItemsPage");

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _client?.Close();
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
