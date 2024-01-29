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

[assembly: Xamarin.Forms.Dependency(typeof(ItemsViewModel))]


namespace DataKlient.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {

        private SessionLocalDetailsItem _usedSession;


        private FileItem _selectedItem;
        private FileItem item;

        public ObservableCollection<FileItem> FileItems { get; set; }
       
        
        
        public Command LoadItemsCommand { get; }
       // public Command AddItemCommand { get; }
        public Command<FileItem> ItemTapped { get; }

        public ItemsViewModel()
        {
            Title = "Pliki";
            //tu zaczytanie z serwera
          

            FileItems = new ObservableCollection<FileItem>();
        
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<FileItem>(OnItemSelected);

            //   AddItemCommand = new Command(OnAddItem);
        }

   


        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;
          
            try
            {
                FileItems.Clear();
                var items = await DataStore.GetItemsAsync(true);
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
            
                var filePath = Path.Combine(FileSystem.AppDataDirectory);
                filePath = filePath + "\\Download";
                string new_filepath=await CoppyToAppDataDirectory(filePath);
                
                await UploadToServerAsync(new_filepath);
               // await   GetDataListFromServerAsync();
                //await ExecuteLoadItemsCommand();




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
          
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}&{nameof(ItemDetailViewModel.FileName)}={item.FileName}" )    ;
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
                _client = new TcpClient("185.230.225.4", 3333);
                NetworkStream stream = _client.GetStream();

                byte[] data = Encoding.ASCII.GetBytes("List "+ _usedSession.sessionID+ " "+_usedSession.userID);
                stream.Write(data, 0, data.Length);

                await Task.Delay(1000);

                //data = new byte[1024];
                //int bytes = stream.Read(data, 0, data.Length);

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
            try
            {
                FileItem fileItem = new FileItem();
                DataStore dataStore = new DataStore();
                FileInfo fileInfo = new FileInfo(filePath);
                long fileSizeInBytes = fileInfo.Length; // Rozmiar pliku w bajtach
                int fileSizeInMB = (int)fileSizeInBytes / (1024 * 1024); // Rozmiar pliku w megabajtach
               
                string extension = Path.GetExtension(filePath); // Rozszerzenie pliku
                string fileName = Path.GetFileName(filePath); // Nazwa pliku


                TcpClient client3 = new TcpClient("185.230.225.4", 3333);
                NetworkStream stream3 = client3.GetStream();
                byte[] data = Encoding.ASCII.GetBytes("Upload "+_usedSession.sessionID+" "+ _usedSession.userID+ " " + fileName + " " + extension + " " + fileSizeInMB);
                stream3.Write(data, 0, data.Length);

                await Task.Delay(1000);

                data = new byte[256];
                int bytes = stream3.Read(data, 0, data.Length);
                string responseData = Encoding.ASCII.GetString(data, 0, bytes);

                if (responseData.StartsWith("YourPath"))
                {
                    Console.WriteLine("Odposwiedź serwera: " + responseData);
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
                        await client.UploadFile(filePath, path);
                        await Task.Delay(1000);
                        await client.Disconnect();

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

                //int count = 1;
                //while (System.IO.File.Exists(fullPath))
                //{
                //    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                //    string extension = Path.GetExtension(fileName);
                //    fileName = $"{fileNameWithoutExtension}_{count}{extension}";
                //    fullPath = Path.Combine(filePath, fileName);
                //    count++;
                //}


                if(System.IO.File.Exists(fullPath))
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
    }
}
