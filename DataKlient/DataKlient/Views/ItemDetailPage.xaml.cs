using DataKlient.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace DataKlient.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        ItemDetailViewModel _viewModel= new ItemDetailViewModel();

        public ItemDetailPage()
        {
            InitializeComponent();
            

            _viewModel.OnStartLocalFileChceck();
            BindingContext = _viewModel;
            
         

        }

        public async void DownloadButton(object sender, EventArgs e)
        {
            _viewModel.OnDownloadClicked();
           
        }

        public async void OpenButton(object sender, EventArgs e)
        {
            _viewModel.OpenFileClicked();
        }

      public async void DeleteButton(object sender, EventArgs e)
        {
            _viewModel.DeleteButtonClickedAsync();
        }

    }
}