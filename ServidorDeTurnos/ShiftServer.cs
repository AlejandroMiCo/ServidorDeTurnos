using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorDeTurnos
{
    internal class ShiftServer
    {
        public string[] users;
        public List<string> waitQueue; //Tambien estan esperando por silksong Ç_Ç  



        public void ReadNames(string route)
        {
            try
            {
                using (StreamReader sr = new StreamReader(route))
                {
                    string list = sr.ReadToEnd();
                    string[] listedNames = list.Split(';');

                    users = new string[listedNames.Length];

                    for (int i = 0; i < users.Length; i++)
                    {
                        users[i] = listedNames[i];
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            {
            }
        }


        public int ReadPin(string route)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(new FileStream(route, FileMode.Open)))
                {
                    return br.ReadInt32();
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                return -1;
            }
        }

        public void Init()
        {
            ReadNames("C:\\Users\\Alejandro\\Desktop\\usuarios.txt");
        }
    }
}
