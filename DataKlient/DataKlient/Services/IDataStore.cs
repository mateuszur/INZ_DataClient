using DataKlient.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataKlient.Services
{
    public interface IDataStore<T>
    {


        event EventHandler<FileItem> OnFileAdded;
        //Task<bool> AddItemAsync(T item);
        Task AddItemAsync(T item);
        
        
        
        Task<bool> UpdateItemAsync(T item);
        Task DeleteItemAsync(int id);
        Task<T> GetItemAsyncByUser(int userid);
        Task<T> GetItemByIdAsync(int id);
        Task<IEnumerable<T>> GetItemsAsync(bool forceRefresh = false);
    }
}
