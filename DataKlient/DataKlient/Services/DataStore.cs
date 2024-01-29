using DataKlient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using System.IO;

namespace DataKlient.Services
{
    public class DataStore : IDataStore<FileItem>
    {
        private SQLiteAsyncConnection connection;
        public event EventHandler<FileItem> OnFileAdded;


        readonly List<FileItem> items;

        public DataStore()
        {
            //items = new List<FileItem>()
            //{
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "First item", Description="This is an item description." },
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "Second item", Description="This is an item description." },
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "Third item", Description="This is an item description." },
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "Fourth item", Description="This is an item description." },
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "Fifth item", Description="This is an item description." },
            //    new FileItem { Id = Guid.NewGuid().ToString(), Text = "Sixth item", Description="This is an item description." }
            //};
        }

        private async Task CreateConnection()
        {
            if (connection != null)
            {
                return;
            }


            var documentPath = Environment.GetFolderPath(
                               Environment.SpecialFolder.LocalApplicationData);

            var databasePath = Path.Combine(documentPath, "FileDB.db");

            connection = new SQLiteAsyncConnection(databasePath);
            await connection.CreateTableAsync<FileItem>();
        }

        public async Task DeleteDatabaseAsync()
        {
            await CreateConnection();
            await connection.DeleteAllAsync<FileItem>();
            await connection.ExecuteAsync("UPDATE sqlite_sequence SET seq = 1 WHERE name = 'FileItem'");
        }


        public async Task AddItemAsync(FileItem item)
        {
            await CreateConnection();
            await connection.InsertAsync(item);
            OnFileAdded?.Invoke(this, item);

       
        }

        public async Task<bool> UpdateItemAsync(FileItem item)
        {
            var oldItem = items.Where((FileItem arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task DeleteItemAsync(int id)
        {
            await CreateConnection();
            var oldItem = await connection.Table<FileItem>().Where( arg => arg.Id == id).FirstOrDefaultAsync(); 
                
            if (oldItem != null)
            {
                
                await connection.DeleteAsync(oldItem);
            }


            //items.Remove(oldItem);

            //  return await Task.FromResult(true);
        }

        public async Task<FileItem> GetItemAsyncByUser(int userID)
        {
            try
            {
                await CreateConnection();
                return await connection.Table<FileItem>().Where(x => x.UserID == userID).FirstOrDefaultAsync();
                // return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
            
        }

        public async Task<FileItem> GetItemByIdAsync(int id)
        {
            try
            {
                await CreateConnection();
                return await connection.Table<FileItem>().Where(x => x.Id == id).FirstOrDefaultAsync();
                // return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }

        }

        public async Task<IEnumerable<FileItem>> GetItemsAsync(bool forceRefresh = false)
        {
            await CreateConnection();
            return await connection.Table<FileItem>().ToArrayAsync();
           // return await Task.FromResult(items);
        }
    }
}