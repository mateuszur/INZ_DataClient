using DataKlient.Models;
using DataKlient.Services;
using DataKlient.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DataKlient
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
      
           DependencyService.Register<DataStore>();
           
           MainPage = new AppShell();

           

           MainPage.Title = "Data Klient";
         
        }

        protected override async void OnStart()
        {
        

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
