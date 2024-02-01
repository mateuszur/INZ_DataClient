using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataKlient.Models;
namespace DataKlient.Services
{
    public  interface ISessionLocalDetailsService
    {
      
         event EventHandler<Models.SessionLocalDetailsService> OnSessionAdded;
         event EventHandler<Models.SessionLocalDetailsService> OnSessionUpdated;
         event EventHandler<Models.SessionLocalDetailsService> OnSessionGet;

        Task<List<Models.SessionLocalDetailsService>> GetItems();
    }
}
