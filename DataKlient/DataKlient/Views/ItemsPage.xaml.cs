using DataKlient.Models;
using DataKlient.ViewModels;
using DataKlient.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DataKlient.Views
{
    public partial class ItemsPage : ContentPage
    {
         ItemsViewModel _viewModel= new ItemsViewModel();
    

        private string _item;

        public ItemsPage()
        {
            InitializeComponent();
       
            //BindingContext = _viewModel = new ItemsViewModel();
            BindingContext = _viewModel ;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }

      


    }
}