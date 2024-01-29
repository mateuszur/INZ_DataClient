using DataKlient.ViewModels;
using DataKlient.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DataKlient
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
          
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
           
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
            Routing.RegisterRoute(nameof(ItemsPage), typeof(ItemsPage));

        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");

        }
       
    }
}