using DataKlient.Models;
using DataKlient.Services;
using DataKlient.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;



namespace DataKlient.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {

        private SessionLocalDetailsItem _usedSession;


        private FileItem _selectedItem;

        public ObservableCollection<FileItem> FileItems { get; }
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<FileItem> ItemTapped { get; }

        public ItemsViewModel()
        {
            Title = "Pliki";



            //tu zaczytanie z serwera
           //  GetDataListFromServerAsync();

            FileItems = new ObservableCollection<FileItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<FileItem>(OnItemSelected);

            AddItemCommand = new Command(OnAddItem);
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


            var actionSheet = new UIActionSheet("Wybierz opcję", null, "Anuluj", null, "Przeglądaj pliki", "Zrób zdjęcie");
            actionSheet.Clicked += (sender, args) =>
            {
                if (args.ButtonIndex == 0)
                {

                    // Wybrano "Przeglądaj pliki"
                    // Tutaj możesz uruchomić przeglądanie plików na iOS.
                }
                else if (args.ButtonIndex == 1)
                {
                    // Wybrano "Zrób zdjęcie"
                    // Tutaj możesz uruchomić aparat na iOS w celu zrobienia zdjęcia.
                }
            };
            actionSheet.ShowInView(UIApplication.SharedApplication.KeyWindow);
        }

        private async void OnAddItem_UWP()
        {

            try
            {
                var result = await FilePicker.PickAsync();
                if (result != null)
                {
                    // Wybrano plik. Możesz wykonać operacje na wybranym pliku, np. wyświetlić jego ścieżkę.
                    Console.WriteLine($"Wybrano plik: {result.FullPath}");
                }
                else
                {
                    // Anulowano wybór pliku.
                    Console.WriteLine("Anulowano wybór pliku.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd przeglądania plików: {ex.Message}");
            }


        }

        private void OnAddItem(object obj)
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                return    OnAddItem_IOS();
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
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
        }


        private async Task GetUsedSession()
        {
            SessionLocalDetailsService _activeSession = new SessionLocalDetailsService();
            var _session = await _activeSession.GetItems();

            if (_session != null && _session.Count != 0)
            {
                _usedSession = _session[0];
            }

        }

        

        private async Task GetDataListFromServerAsync()
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

                data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                _client.Close();

                string responseData = Encoding.ASCII.GetString(data, 0, bytes);


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
                //Przestrzeń na zrobienie zapisu błedu w pliku z logami
                Console.WriteLine(ex.ToString() + "\n");
            }
        }



    }
}
