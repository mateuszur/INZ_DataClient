using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataKlient.Models;
namespace DataKlient.Services
{
    public  interface ISessionLocalDetailsService
    {
      
         event EventHandler<SessionLocalDetailsItem> OnSessionAdded;
         event EventHandler<SessionLocalDetailsItem> OnSessionUpdated;
         event EventHandler<SessionLocalDetailsItem> OnSessionGet;

        Task<List<SessionLocalDetailsItem>> GetItems();
    }
}
