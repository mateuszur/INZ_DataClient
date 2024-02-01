using DataKlient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace DataKlient.Services
{
    public class ReadConfig
    {

        public void ReadConfiguration(ClientConfig config)
        {
            try
            {
                var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configFilePath= Path.Combine(documentPath, "config.txt");
                var configLines = File.ReadAllLines(configFilePath);
                foreach (var line in configLines)
                {
                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        switch (keyValue[0])
                        {
                            case "ServerAddress":
                                config.ServerAddress = keyValue[1];
                                break;
                            case "DataServerPort":
                                config.DataServerPort = int.Parse(keyValue[1]);
                                break;

                            case "SFTPPort":
                                config.SFTPPort = int.Parse(keyValue[1]);
                                break;
                            case "Key":
                                config.Key = keyValue[1];
                                break;
                            case "IV":
                                config.IV = keyValue[1];
                                break;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("Błąd podczas odczytu pliku konfiguracyjnego!");
            }
        }


    }
}
