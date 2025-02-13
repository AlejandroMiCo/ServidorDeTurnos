using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServidorDeTurnos
{
    internal class ShiftServer
    {
        public string[] users;
        public List<string> waitQueue; //Tambien estan esperando por silksong Ç_Ç  

        private static readonly object l = new object();
        static bool isServerRunning = true;
        static Socket s;
        static int[] ports = { 31416, 1024, 1025, 1026, 1027, 1028, 1029, 1030 };
        static IPEndPoint ie;
        public List<StreamWriter> clientes = new List<StreamWriter>();
        public List<string> clientsName = new List<string>();


        public void Init()
        {
            //ReadNames("C:\\Users\\Alejandro\\Desktop\\usuarios.txt");

            //string route = Environment.GetEnvironmentVariable("userprofile") + "\\pin.bin";

            //Console.WriteLine(ReadPin(route));


            leerLista();

            bool isPortSet = false;
            int cont = 0;

            do
            {
                if (cont < ports.Length)
                {
                    try
                    {
                        ie = new IPEndPoint(IPAddress.Any, ports[cont]);
                        s = new Socket(
                            AddressFamily.InterNetwork,
                            SocketType.Stream,
                            ProtocolType.Tcp
                        );
                        s.Bind(ie);
                        isPortSet = true;
                    }
                    catch (Exception ex)
                        when (ex is SocketException || ex is ObjectDisposedException)
                    {
                        isPortSet = false;
                    }
                }
                cont++;
            } while (!isPortSet && cont < ports.Length);

            if (!isPortSet)
            {
                s.Close();
                isServerRunning = false;
            }
            else
            {
                s.Listen(10);
            }

            Console.WriteLine("Server waiting at port {0}", ie.Port);

            ReadNames(Environment.GetEnvironmentVariable("userprofile") + "\\usuarios.txt");

            while (isServerRunning)
            {
                try
                {
                    Socket cliente = s.Accept();
                    Thread hilo = new Thread(hiloCliente);
                    hilo.IsBackground = true;
                    hilo.Start(cliente);

                }
                catch (SocketException e) when (e.ErrorCode == (int)SocketError.Interrupted)
                {
                    isServerRunning = false;
                }
            }
        }


        private void hiloCliente(object socket)
        {
            string newUserName = "";
            string mensaje;
            int defaultPass = 1234;
            int pass;
            string passRoute = Environment.GetEnvironmentVariable("userprofile") + "\\pin.bin";
            Socket cliente = (Socket)socket;
            IPEndPoint ieCliente = (IPEndPoint)cliente.RemoteEndPoint;
            bool isConnected = true;
            Console.WriteLine(
                "Connected with client {0} at port {1}",
                ieCliente.Address,
                ieCliente.Port
            );
            using (NetworkStream ns = new NetworkStream(cliente))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                try
                {
                    sw.WriteLine("Welcome to the Silksong Waiting Queue server");
                    sw.WriteLine("Indique su nombre:");
                    sw.Flush();
                    newUserName = sr.ReadLine();

                    bool isAdmin = newUserName == "admin";

                    if (!users.Contains(newUserName) && !isAdmin)
                    {
                        sw.WriteLine("Usuario desconocido");
                        sw.Flush();
                    }
                    else
                    {
                        if (isAdmin)
                        {
                            defaultPass = ReadPin(passRoute) != -1 ? ReadPin(passRoute) : defaultPass;
                            sw.WriteLine("Por favor, indique la contraseña de administrador");
                            int.TryParse(sr.ReadLine(), out pass);

                            if (defaultPass != pass)
                            {
                                cliente.Close();
                            }
                        }

                        switch (sr.ReadLine())
                        {
                            case string comand when comand is "list":
                                foreach (string alumnos in waitQueue)
                                {
                                    sw.WriteLine($"->{alumnos}");
                                    sw.Flush();
                                }
                                if (!isAdmin)
                                {
                                    cliente.Close();
                                }

                                break;
                            case string comand when comand is "add":
                                List<string> namesList = new List<string>();
                                foreach (var item in waitQueue)
                                {
                                    namesList.Add(item.Split(':')[0]);
                                }

                                if (!namesList.Contains(newUserName))
                                {
                                    waitQueue.Add(newUserName + ":" + DateTime.Now.ToString());
                                }
                                sw.WriteLine("OK");
                                sw.Flush();
                                if (isAdmin)
                                {
                                    cliente.Close();
                                }
                                break;
                            case string comand when comand.Split(' ')[0] is "del" && isAdmin:
                                bool error = true;

                                if (comand.Split(':').Length == 2 && int.TryParse(comand.Split(':')[1], out int pos))
                                {
                                    if (pos <= waitQueue.Count - 1)
                                    {
                                        error = false;
                                        waitQueue.RemoveAt(pos);
                                    }
                                }

                                if (error)
                                {
                                    sw.WriteLine("delete error");
                                    sw.Flush();
                                }

                                break;

                            case string comand when comand.Split(' ')[0] is "chpin" && isAdmin:
                                if (Int32.TryParse(comand.Split(' ')[1], out Int32 result))
                                {
                                    try
                                    {
                                        string directory = Environment.GetEnvironmentVariable("userprofile");
                                        using (BinaryWriter bw = new BinaryWriter(new FileStream(directory + "\\pin.bin", FileMode.Create)))
                                        {
                                            bw.Write(result);
                                        }
                                    }
                                    catch (Exception ex) when (ex is IOException || ex is IOException)
                                    {

                                        throw;
                                    }
                                }

                                break;
                            case string comand when comand is "exit" && isAdmin:
                                cliente.Close();

                                break;
                            case string comand when comand is "shutdown" && isAdmin:
                                guardarLista();
                                isServerRunning = false;
                                cliente.Close();            // igual sobra
                                break;
                            default:
                                sw.WriteLine("Comando no reconocido");
                                sw.Flush();
                                break;
                        }
                    }
                    cliente.Close();
                }
                catch (IOException)
                {
                    //Salta al acceder al socket
                    //y no estar permitido
                }
                Console.WriteLine(
                    "Finished connection with {0}:{1}",
                    ieCliente.Address,
                    ieCliente.Port
                );
            }
        }


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

        public void guardarLista()
        {
            string directory = Environment.GetEnvironmentVariable("userprofile");
            using (StreamWriter sw = new StreamWriter(directory + "\\queue.txt"))
            {
                foreach (string alumnoEnCola in waitQueue)
                {
                    sw.WriteLine(alumnoEnCola);
                }
            }
        }

        public void leerLista()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("userprofile") + "\\queue.txt"))
                {
                    while (!sr.EndOfStream)
                    {
                        waitQueue.Add(sr.ReadLine());
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException) 
            {

            }
        }
    }
}
