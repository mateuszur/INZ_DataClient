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
     
       private string _usernaem;
       private string _password;


        private Timer _timer = null;

        public LoginPage()
        {

            InitializeComponent();
             CheckLocalSessionAsync();
            this.BindingContext = viewModel;

        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            _usernaem = "mateuszur"; //UserNameEntry.Text;
            _password = "Pa$$w0rd";//PasswordEntry.Text;

            bool result = await viewModel.OnLoginClickedAsync(_usernaem, _password);

            if (!result)
            {
                  await DisplayAlert("Błąd", "Podane dane logowania są nieprawidłowe", "OK");
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
    }

}