using System;
using System.Collections.Generic;
using System.Text;
using SQLite;
using System;

namespace DataKlient.Models
{
    public class SessionLocalDetailsItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string sessionID { get; set; }
        public DateTime endSesionDate { get; set; }
    
        public string privileges { get; set; }

        public string userLogin { get; set; }
        public string userID { get; set; }

        public bool isValid { get; set; }
    }
}
