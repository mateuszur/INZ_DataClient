using DataKlient.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DataKlient.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class ItemDetailViewModel : BaseViewModel
    {
        private int itemId;
        private string fileName;
        private string fileExtension;
        private string fileSize;
        private string dateOfTransfer;

        public string Id { get; set; }

        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public string FileExtension
        {
            get => fileExtension;
            set => SetProperty(ref fileExtension, value);
        }


        public string FileSize
        {
            get => fileSize;
            set => SetProperty(ref fileSize, value);
        }

        public string DateOfTransfer
        {

            get => dateOfTransfer;
            
            set=> SetProperty(ref dateOfTransfer, value);
        }

        public int ItemId
        {
            get
            {
                return itemId;
            }
            set
            {
                itemId = value;
                LoadItemId(value);
            }
        }

        public async void LoadItemId(int itemId)
        {
            try
            {
                var item = await DataStore.GetItemByIdAsync(itemId);
             
                FileName = item.FileName;
                FileExtension = item.FileType;
                FileSize= item.FileSize;

                string date = item.DateOfTransfer;
                date = date.Insert(10, " ");

                DateOfTransfer = date;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }
    }
}
