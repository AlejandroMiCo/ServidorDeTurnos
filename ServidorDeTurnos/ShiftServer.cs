﻿using System;
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
        private static readonly object l = new object();

        public string[] users;
        public List<string> waitQueue; //Tambien estan esperando por silksong Ç_Ç  

        static bool isServerRunning = true;
        static Socket s;
        static int port = 31416;
        bool firstTime = true;
        static IPEndPoint ie;
        public List<string> clientsName = new List<string>();



        public void Init()
        {

            ReadNames(Environment.GetEnvironmentVariable("userprofile") + "\\usuarios.txt");
            waitQueue = new List<string>();
            leerLista();

            bool isPortSet = false;

            //try
            //{
            //    string directory = Environment.GetEnvironmentVariable("userprofile");
            //    using (BinaryWriter bw = new BinaryWriter(new FileStream(directory + "\\pin.bin", FileMode.Create)))
            //    {
            //        bw.Write(1111);
            //    }
            //}
            //catch (Exception ex) when (ex is IOException || ex is IOException)
            //{
            //    throw;
            //}

            do
            {
                try
                {
                    ie = new IPEndPoint(IPAddress.Any, port);
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
                    if (firstTime)
                    {
                        firstTime = false;
                        port = 1024;
                    }
                    else
                    {
                        port++;
                    }

                }
            } while (!isPortSet);

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
            int defaultPass = 1234;
            int pass;
            string passRoute = Environment.GetEnvironmentVariable("userprofile") + "\\pin.bin";
            Socket cliente = (Socket)socket;
            IPEndPoint ieCliente = (IPEndPoint)cliente.RemoteEndPoint;
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
                    sw.WriteLine("Welcome to the Silksong Waiting Queue server, Indique su nombre:\n");
                    sw.Flush();
                    newUserName = sr.ReadLine();

                    bool isAdmin = newUserName == "admin";


                    if (!users.Contains(newUserName) && !isAdmin)
                    {
                        sw.WriteLine("Usuario desconocido");
                        sw.Flush();
                        cliente.Close();
                    }



                    if (isAdmin)
                    {
                        lock (l)
                        {

                            defaultPass = ReadPin(passRoute) != -1 ? ReadPin(passRoute) : defaultPass;
                            sw.WriteLine("Por favor, indique la contraseña de administrador");
                            sw.Flush();
                            int.TryParse(sr.ReadLine(), out pass);
                        }

                        if (defaultPass != pass)
                        {
                            cliente.Close();
                        }
                    }

                    do
                    {
                        switch (sr.ReadLine())
                        {
                            case string comand when comand == "list":
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
                            case string comand when comand == "add":
                                List<string> namesList = new List<string>();
                                lock (l)
                                {
                                    foreach (var item in waitQueue)
                                    {
                                        namesList.Add(item.Split(':')[0]);
                                    }

                                    if (!namesList.Contains(newUserName))
                                    {
                                        waitQueue.Add(newUserName + ":" + DateTime.Now.ToString());
                                    }
                                }
                                sw.WriteLine("OK");
                                sw.Flush();
                                break;

                            case string comand when comand.Split(' ')[0] == "del" && isAdmin:
                                bool error = true;

                                if (comand.Split(' ').Length == 2 && int.TryParse(comand.Split(' ')[1], out int pos))
                                {
                                    lock (l)
                                    {
                                        if (pos <= waitQueue.Count - 1 && waitQueue.Count > 0 && pos > 0)      
                                        {
                                            error = false;
                                            waitQueue.RemoveAt(pos);
                                        }
                                    }
                                }

                                if (error)
                                {
                                    sw.WriteLine("delete error");
                                    sw.Flush();
                                }

                                break;

                            case string comand when comand.Split(' ')[0] == "chpin" && isAdmin:
                                if (comand.Split(' ').Length == 2 && Int32.TryParse(comand.Split(' ')[1], out Int32 result))
                                {
                                    if (result > 999 && result < 9999)
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
                                }

                                break;
                            case string comand when comand == "exit" && isAdmin:
                                cliente.Close();

                                break;
                            case string comand when comand == "shutdown" && isAdmin:
                                guardarLista();
                                isServerRunning = false;
                                s.Close();
                                break;

                            default:
                                sw.WriteLine("Comando no reconocido");
                                sw.Flush();
                                break;

                        }
                    } while (isAdmin && isServerRunning);
                    cliente.Close();

                }
                catch (Exception ex) when (ex is IOException)
                {
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

                    users = listedNames;
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
                    int pin = br.ReadInt32();
                    Console.WriteLine(pin);

                    if (999 < pin && pin <= 9999)
                    {
                        return pin;
                    }

                    return -1;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                return -1;
            }
        }

        public void guardarLista()
        {
            try
            {
                string directory = Environment.GetEnvironmentVariable("userprofile");
                using (StreamWriter sw = new StreamWriter(directory + "\\queue.txt"))
                {
                    lock (l)
                    {
                        foreach (string alumnoEnCola in waitQueue)
                        {
                            sw.WriteLine(alumnoEnCola);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException | ex is ArgumentException)
            {

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
