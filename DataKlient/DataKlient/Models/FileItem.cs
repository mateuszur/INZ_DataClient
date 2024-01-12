using SQLite;
using System;

namespace DataKlient.Models
{
    public class FileItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserID { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string DateOfTransfer { get; set; }
    }
}