using DataKlient.Models;
using DataKlient.Services;
using DataKlient.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;
using FluentFTP;
using DataKlient.ViewModels;
using FluentFTP.Helpers;
using System.Security.Cryptography;
using System.Runtime.InteropServices.ComTypes;

[assembly: Xamarin.Forms.Dependency(typeof(ItemsViewModel))]


namespace DataKlient.ViewModels
{
  [QueryProperty(nameof(userId), nameof(userId))]
  
    public class ItemsViewModel : BaseViewModel
    {
        private int userId;
        private Models.SessionLocalDetailsService _usedSession;


        private FileItem _selectedItem;
        private FileItem item;
        private string filePath="";
        public ObservableCollection<FileItem> FileItems { get; set; }
       
        
        public Command LoadItemsCommand { get; }
       // public Command AddItemCommand { get; }
        public Command<FileItem> ItemTapped { get; }


        //Zaczytanie konfiguracji
        private string serverAddress;
        private int dataServerPort;
        private int sFTPPort;
        private byte[] key;
        private byte[] iv;
        private ClientConfig _clientConfig;
        private ReadConfig _readConfig;

        int itemID=0;
        string itemName="";

        public ItemsViewModel()
        {
            Title = "Pliki";
          
            this.filePath = LocalPathInit();

            FileItems = new ObservableCollection<FileItem>();
        
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<FileItem>(OnItemSelected);

            //inicjalizacja parametrów lokalnych konfiguracji 
            _clientConfig = new ClientConfig();
            _readConfig = new ReadConfig();
            _readConfig.ReadConfiguration(_clientConfig);
            
            this.serverAddress = _clientConfig.ServerAddress;
            this.dataServerPort = _clientConfig.DataServerPort;
            this.sFTPPort = _clientConfig.SFTPPort;
            this.key = StringToByteArray(_clientConfig.Key);
            this.iv = StringToByteArray(_clientConfig.IV);

        }

        


        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;
          
            try
            {
                FileItems.Clear();
                await GetUsedSession();
                var items = await DataStore.GetItemsAsync(true, _usedSession.userID);
               
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        FileItems.Add(item);
                    }
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
        }

        public FileItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }


        private  void OnAddItem_IOS()
        {
            //await Shell.Current.GoToAsync(nameof(NewItemPage));

            try
            {
                var actionSheet = new UIActionSheet("Wybierz opcję", null, "Anuluj", null, "Przeglądaj pliki", "Przeglądaj zdjęcia");
                actionSheet.Clicked += async (sender, args) =>
                {
                    if (args.ButtonIndex == 0)
                    {
                        var filePath = Path.Combine(FileSystem.AppDataDirectory);
                    

                        string new_filepath = await CoppyToAppDataDirectory(filePath);

                        await UploadToServerAsync(new_filepath);
                        // Wybrano "Przeglądaj pliki"
                        // Tutaj możesz uruchomić przeglądanie plików na iOS.
                    }
                    else if (args.ButtonIndex == 1)
                    {
                        var photo = await MediaPicker.CapturePhotoAsync();
                        
                    }
                };
                actionSheet.ShowInView(UIApplication.SharedApplication.KeyWindow);
            }catch(Exception ex)
            {
                //przestrzeń na logi apliakcji 
                Console.WriteLine($"Błąd przeglądania plików: {ex.Message}");
            }
        }

        private async void OnAddItem_UWP()
        {
            try
            {
                string new_filepath=await CoppyToAppDataDirectory(filePath);
                
                await UploadToServerAsync(new_filepath);

            }
            catch (Exception ex)
            {
                //przestrzeń na logi apliakcji 
                Console.WriteLine($"Błąd przeglądania plików: {ex.Message}");
            }


        }

        public void OnAddItem()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                 OnAddItem_IOS();
                    break;

                case Device.UWP:
                    OnAddItem_UWP();
                    break;
            }
        }

        async void OnItemSelected(FileItem item)
        {

            if (item == null)
           
                return;

            // This will push the ItemDetailPage onto the navigation stack
            this.itemID = item.Id;
            this.itemName = item.FileName;
           // await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}&{nameof(ItemDetailViewModel.FileName)}={item.FileName}" )    ;
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={this.itemID}&{nameof(ItemDetailViewModel.FileName)}={this.itemName}" )    ;
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
            }catch(Exception ex)
            {
                return;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                
            }
        }

        

        public async Task GetDataListFromServerAsync()
        { 

                TcpClient _client = null;
                DataStore dataStore = new DataStore();
                FileItem fileItem = new FileItem();
            try
            {
                await GetUsedSession();
                await dataStore.DeleteDatabaseAsync();
                _client = new TcpClient(serverAddress, dataServerPort);
                NetworkStream stream = _client.GetStream();

                string plaintext = "List " + _usedSession.sessionID + " " + _usedSession.userID;
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
                 

                    for(int i=1; i<parts.Length-1;i++)
                    {
                        string[] parts2 = parts[i].Split(' ');
                        
                            
                            fileItem.FileName = parts2[0].ToString();
                            fileItem.FileSize = parts2[1].ToString();
                            fileItem.FileType= parts2[2].ToString();
                            fileItem.UserID = _usedSession.userID;
                            fileItem.DateOfTransfer = parts2[3].ToString()+parts2[4].ToString();

                        
                     await   dataStore.AddItemAsync(fileItem);
                    }
                }

            }
            catch(Exception ex)
            {
                _client?.Close();
                return;
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
           
            }
        }

        async Task UploadToServerAsync(string filePath)
        {
            TcpClient client=null;
            try
            {
                FileItem fileItem = new FileItem();
                DataStore dataStore = new DataStore();
                FileInfo fileInfo = new FileInfo(filePath);
                long fileSizeInBytes = fileInfo.Length; // Rozmiar pliku w bajtach
                double fileSizeInMB = Math.Round((double)fileSizeInBytes / (1024 * 1024),3); // Rozmiar pliku w megabajtach
               
                string extension = Path.GetExtension(filePath); // Rozszerzenie pliku
                string fileName = Path.GetFileName(filePath); // Nazwa pliku


                client= new TcpClient(serverAddress, dataServerPort);
                NetworkStream stream = client.GetStream();

                string plaintext = "Upload " + _usedSession.sessionID + " " + _usedSession.userID + " " + fileName + " " + extension + " " + fileSizeInMB;
                string encryptedMessage = Convert.ToBase64String(EncryptStringToBytes_Aes(plaintext, key, iv));
                byte[] data = Encoding.ASCII.GetBytes(encryptedMessage);
                stream.Write(data, 0, data.Length);


                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);
                responseData = DecryptStringFromBytes_Aes(Convert.FromBase64String(responseData), key, iv);

                client.Close();


                if (responseData.StartsWith("YourPath"))
                {
                    Console.WriteLine("Odposwiedź serwera: " + responseData);
                    string[] parts = responseData.Split(' ');


                    string host = serverAddress; //IP serwera FTP ten sam co DataSerwer
                    int portFTP = sFTPPort;
                    string path = parts[1];
                    string username = parts[2]; //ze stringa1
                    string passwd = parts[3]; //ze stringa 


                  
                        var _client = new AsyncFtpClient(host, username, passwd);
                        _client.Port = portFTP;

                        _client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                        _client.Config.ValidateAnyCertificate = true;
                      
                    //nawiazanie połaczenia i przesłanie pliku
                        await _client.Connect();
                        await _client.UploadFile(filePath, path);
                        await Task.Delay(1000);
                        await _client.Disconnect();

                    fileItem.FileName = fileName;
                    fileItem.FileSize = fileSizeInMB.ToString();
                    fileItem.FileType = extension;
                    fileItem.UserID = _usedSession.userID;
                    fileItem.DateOfTransfer = DateTime.Now.ToString("dd.MM.yyyyHH:mm:ss");


                    await dataStore.AddItemAsync(fileItem);
                    FileItems.Add(fileItem);
                }
                else
                {
                    return;
                }
            }
            catch (Exception e)
            {
                client?.Close();
                Console.WriteLine(e.ToString());
            }
        }

        async Task<string> CoppyToAppDataDirectory(string filePath)
        {
            try
            {

                var result = await FilePicker.PickAsync();
              
                if (result == null)
                {
                    return null;
                }
                
                var stream = await result.OpenReadAsync();

                string fileName = result.FileName.Replace(' ', '_');
                
                string fullPath = Path.Combine(filePath, fileName);

                int count = 1;
                while (System.IO.File.Exists(fullPath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExtension}_{count}{extension}";
                    fullPath = Path.Combine(filePath, fileName);
                    count++;
                }


                if (System.IO.File.Exists(fullPath))
                {
                   
                    System.IO.File.Delete(fullPath);
                }
               
                using (var fileStream = System.IO.File.Create(fullPath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
             
                return fullPath;
            }catch(Exception e)
            {
                //miejsce na log
                return null;
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

        private static byte[] StringToByteArray(string hex)
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
