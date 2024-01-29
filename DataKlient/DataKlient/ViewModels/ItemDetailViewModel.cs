using DataKlient.Models;
using DataKlient.Services;
using FluentFTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        private bool _isFileLocal;
        private SessionLocalDetailsItem _usedSession;
      // private string localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory) + "\\Download";

        private string localPath;

        public ItemDetailViewModel()
        {

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
                SessionLocalDetailsService _activeSession = new SessionLocalDetailsService();
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
               _client = new TcpClient("185.230.225.4", 3333);
               
                NetworkStream stream = _client.GetStream();

                byte[] data = Encoding.ASCII.GetBytes("Download " + _usedSession.sessionID + " " + _usedSession.userID + " " + fileName);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);

                if (responseData.StartsWith("YourPathToDownload"))
                {
                    string[] parts = responseData.Split(' ');

                    string host = "185.230.225.4"; //IP serwera FTP
                    int portFTP = 3334;
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
                   
                }
            }
            catch
            {
                Console.WriteLine("Błąd podczas pobierania pliku!");
                _client?.Close();
                return;
            }

        }

        //do usunięcia
        private async Task DownloadAsync(string localPath,string path, AsyncFtpClient client)
        {
            try
            {
                await client.Connect();
                if (!Directory.Exists(localPath))
                {
                    Directory.CreateDirectory(localPath);

                    await client.DownloadFile(localPath , path);
                    Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                }
                else
                {
                    if ((await client.CompareFile(localPath, path)) != FtpCompareResult.NotEqual)
                    {
                        await client.DownloadFile(localPath, path);
                        Console.WriteLine("Ścieżka do plików lokalnych: " + localPath);
                    }

                }
                await client.Disconnect();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString()); 
            }
        }

      public bool OnStartLocalFileChceck()
        {
            //zmiany na macu
            if (File.Exists(localPath + "\\" + fileName))
            {
                IsFileLocal= true;
                return true;

            }
            else
            {
                IsFileLocal = false;
                return false;
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

        private string  LocalPathInit()
        {
            string path;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    path = System.IO.Path.Combine(FileSystem.AppDataDirectory) + "/Download";
                    return path;
                    break;

                case Device.UWP:
                    path = System.IO.Path.Combine(FileSystem.AppDataDirectory) + "\\Download";
                    return path;
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
                _client = new TcpClient("185.230.225.4", 3333);
                NetworkStream stream = _client.GetStream();

                byte[] data = Encoding.ASCII.GetBytes("Delete " + _usedSession.sessionID + " " + _usedSession.userID + " " + fileName);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);

                _client.Close();

                if (responseData.StartsWith("FileDeletedSuccessfully"))
                {
                    string filePath = localPath + "\\" + fileName;
                    await DataStore.DeleteItemAsync(itemId);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _ = Shell.Current.GoToAsync("//ItemsPage");

                    }
                    else
                    {
                        _ = Shell.Current.GoToAsync("//ItemsPage");

                    }
                    
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _client?.Close();
            }
        }

    }
}
