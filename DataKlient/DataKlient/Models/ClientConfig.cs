using System;
using System.Collections.Generic;
using System.Text;

namespace DataKlient.Models
{
    public class ClientConfig
    {

        public string ServerAddress { get; set; }
        public int DataServerPort { get; set; } = 3333;
        public int SFTPPort { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }



    }
}
