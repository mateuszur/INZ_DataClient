using DataKlient.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DataKlient.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
       public Command LoginCommand { get; }

     
        private bool isCheckBoxChecked=false;
        private bool isPasswordEnabled= true;


        public LoginViewModel()
        {
           LoginCommand = new Command(OnLoginClicked);
        
        }

        private  void OnLoginClicked()
        {

          
             Shell.Current.GoToAsync("//AboutPage");

        }


        public bool IsCheckBoxChecked
        {
            get { return isCheckBoxChecked; }
            set
            {
                if (isCheckBoxChecked != value)
                {
                    isCheckBoxChecked = value;
                    ShowHidePassword();
                    OnPropertyChanged(nameof(IsCheckBoxChecked));
                }
            }

        }


        public bool IsPasswordEnabled
        {
            get { return isPasswordEnabled; }
            set
            {
                if (isPasswordEnabled != value)
                {
                    isPasswordEnabled = value;
                    OnPropertyChanged(nameof(IsPasswordEnabled));
                }

            }
        }

        private void ShowHidePassword()
        {
            if (IsCheckBoxChecked)
            {

                IsPasswordEnabled = false;
            }
            else
            {
                IsPasswordEnabled = true;
            }

        }


         
        


    }
}
