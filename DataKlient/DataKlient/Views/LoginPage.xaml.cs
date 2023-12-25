using DataKlient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DataKlient.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {

       private LoginViewModel viewModel = new LoginViewModel();
       private string _usernaem;
       private string _password;

        public LoginPage()
        {
            InitializeComponent();
           
            this.BindingContext = viewModel;
           // this.BindingContext = new LoginViewModel();

            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
           
        }

        private async void Button_ClickedAsync(object sender, EventArgs e)
        {
            _usernaem = UserNameEntry.Text;
            _password= PasswordEntry.Text;

            bool result = await viewModel.OnLoginClickedAsync(_usernaem, _password);

            if (!result)
            {
                  await DisplayAlert("Błąd", "Podane dane logowania są nieprawidłowe", "OK");
            }
        }
    }

}