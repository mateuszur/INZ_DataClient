using DataKlient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DataKlient.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
       //inicjalizacja modelu logowania
       private LoginViewModel viewModel = new LoginViewModel();
       private ItemDetailPage viewModel2 = new ItemDetailPage();
       private string _usernaem;
       private string _password;


        private Timer _timer = null;

        public LoginPage()
        {

            InitializeComponent();
            CheckServerConfig();
            
            this.BindingContext = viewModel;

        }

        private async Task CheckServerConfig()
        {
            bool result = await viewModel.CheckServerConfig();
            if (!result)
            {
                await DisplayAlert("Błąd", "Brak konfiguracji! Wybierz -> Ustawienia", "OK");
            }else
            {
               await CheckLocalSessionAsync();
               
            }
        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            _usernaem = UserNameEntry.Text;
            _password = PasswordEntry.Text;

            bool result = await viewModel.OnLoginClickedAsync(_usernaem, _password);
            bool result2= await viewModel.CheckServerConfig();
            if (!result )
            {
                  await DisplayAlert("Błąd", "Podane dane logowania są nieprawidłowe", "OK");
            }
            if (!result2)
            {
                await DisplayAlert("Błąd", "Brak konfiguracji! Wybierz -> Ustawienia", "OK");
            }
        }
        private async Task<bool> CheckLocalSessionAsync()
        {
            bool result= await viewModel.CheckLocalSessionAsync();
            if(!result)
            {
                await DisplayAlert("Błąd", "Brak ważnej sesji! Zaloguję się ponownie!", "OK");
                return false;
            }
            else
            {
                return true;
            }
        }

        private async void SettingsButton(object sender, EventArgs e)
        {
            await viewModel.SetSettings();
        }
    }

}