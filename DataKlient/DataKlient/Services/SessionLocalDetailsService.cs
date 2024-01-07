using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataKlient.Models;
using SQLite;

namespace DataKlient.Services
{
    public class SessionLocalDetailsService : ISessionLocalDetailsService
    {
        private SQLiteAsyncConnection connection;
        public event EventHandler<SessionLocalDetailsItem> OnSessionAdded;
        public event EventHandler<SessionLocalDetailsItem> OnSessionUpdated;
        public event EventHandler<SessionLocalDetailsItem> OnSessionGet;

        private async Task CreateConnection()
        {
            if (connection != null)
            {
                return;
            }


            var documentPath = Environment.GetFolderPath(
                               Environment.SpecialFolder.LocalApplicationData);
           
            var databasePath = Path.Combine(documentPath, "SessionDB.db");

            connection = new SQLiteAsyncConnection(databasePath);
            await connection.CreateTableAsync<SessionLocalDetailsItem>();

        }

        //Trzeba dodać wyszukiwanie sesji po userID
        public async Task<SessionLocalDetailsItem> GetItems()
        {
            await CreateConnection();
            return await connection.Table<SessionLocalDetailsItem>().Where(x => x.isValid == true).FirstOrDefaultAsync();
        }



        public async Task AddItem(SessionLocalDetailsItem item)
        {
            await CreateConnection();
            await connection.InsertAsync(item);
            OnSessionAdded?.Invoke(this, item);
        }

        public async Task UpdateItem(SessionLocalDetailsItem item)
        {
            await CreateConnection();
            await connection.UpdateAsync(item);
            OnSessionUpdated?.Invoke(this, item);
        }

        public async Task AddOrUpdate(SessionLocalDetailsItem item)
        {
            if (item.Id == 0)
            {
                await AddItem(item);
            }
            else
            {
                await UpdateItem(item);
            }
        }



    }
}
