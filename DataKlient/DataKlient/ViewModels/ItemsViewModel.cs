using DataKlient.Models;
using DataKlient.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Essentials;
using Xamarin.Forms;



namespace DataKlient.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        private FileItem _selectedItem;

        public ObservableCollection<FileItem> Items { get; }
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<FileItem> ItemTapped { get; }

        public ItemsViewModel()
        {
            Title = "Pliki";
            Items = new ObservableCollection<FileItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<FileItem>(OnItemSelected);

            AddItemCommand = new Command(OnAddItem);
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await DataStore.GetItemsAsync(true);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        Items.Add(item);
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


        private async void OnAddItem_IOS()
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
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
        }


        }
    }

