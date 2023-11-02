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
     

        public LoginPage()
        {
            InitializeComponent();

            this.BindingContext = new LoginViewModel();
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
           // Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//AboutPage");
        }
    }
}