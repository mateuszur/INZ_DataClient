using DataKlient.Models;
using DataKlient.Services;
using FluentFTP;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
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
        private string localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory) + "\\Download";

        public ItemDetailViewModel()
        {
        
            
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
         //   string localPath= Path.Combine(FileSystem.AppDataDirectory)+"\\Download";
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
                    
                    if (!Directory.Exists(localPath))
                    {
                        Directory.CreateDirectory(localPath);
                        await client.DownloadFile(localPath + "\\" + fileName, path);
                    }
                    else
                    {
                        if ((await client.CompareFile(localPath + "\\" + fileName, path)) != FtpCompareResult.NotEqual)
                        {
                            await client.DownloadFile(localPath+"\\"+fileName, path);
                        }
                    }

                    await Task.Delay(1000);
                    await client.Disconnect();

                }
            }
            catch
            {
                _client?.Close();
                return;
            }

        }

      public bool OnStartLocalFileChceck()
        {
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


        public async Task OpenFileClickedAsync()
        {
            await Xamarin.Essentials.Launcher.OpenAsync(new Xamarin.Essentials.OpenFileRequest
            {
                File = new Xamarin.Essentials.ReadOnlyFile(localPath + "\\" + fileName)
            });
        }

    }
}
