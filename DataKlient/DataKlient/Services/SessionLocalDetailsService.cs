using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task<List<SessionLocalDetailsItem>> GetItems()
        {
            try
            {
                await CreateConnection();
                var items = await connection.Table<SessionLocalDetailsItem>()
                             .Where(x => x.isValid == 1 )
                             .ToListAsync();

                return items;
                // return await connection.Table<SessionLocalDetailsItem>().ToListAsync();
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
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

        public async Task UpdateSelectedItem(SessionLocalDetailsItem item)
        {
            await CreateConnection();
            var itemToEdit = await connection.Table<SessionLocalDetailsItem>()
                         .Where(x => x.sessionID == item.sessionID) // Zastąp "id" identyfikatorem rekordu, który chcesz edytować
                         .FirstOrDefaultAsync();

            if (itemToEdit != null)
            {
                itemToEdit.isValid = 0;
                await UpdateItem(itemToEdit);
            }

        }

        //public async Task AddOrUpdate(SessionLocalDetailsItem item)
        //{
        //    if (item.Id == 0)
        //    {
        //        await AddItem(item);
        //    }
        //    else
        //    {
        //        await UpdateItem(item);
         }
        }



  

