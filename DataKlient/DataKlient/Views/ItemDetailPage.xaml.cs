using DataKlient.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace DataKlient.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}