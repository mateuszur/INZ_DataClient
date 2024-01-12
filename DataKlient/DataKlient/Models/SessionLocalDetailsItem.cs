using System;
using SQLite;

namespace DataKlient.Models
{
    public class SessionLocalDetailsItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string sessionID { get; set; }
        public string endSesionDate { get; set; }
    
        public string privileges { get; set; }

        public string userLogin { get; set; }
        public int userID { get; set; }

        public int isValid { get; set; }
    }
}
