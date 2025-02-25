using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClienteEjercicio2
{
    public partial class Form1 : Form
    {
        public string ipServer = IPAddress.Loopback.ToString();
        public ushort port = 31416;
        IPEndPoint ie;
        IPAddress ip;
        String user;

        public Form1()
        {
            InitializeComponent();
            leerDatos();

            ie = new IPEndPoint(ip, port);

            txtUsuario.Text = user.Trim();
            txtPuerto.Text = ie.Port.ToString();
            txtIp.Text = ie.Address.ToString();
        }

        private void btn_Click(object sender, EventArgs e)
        {
            string comand;
            Socket server = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            try
            {
                ie = new IPEndPoint(ip, port);
                server.Connect(ie);
            }
            catch (SocketException ex)
            {
                lblinfo.Text = String.Format(
                    "Error connection: {0}\nError code: {1}({2})",
                    ex.Message,
                    (SocketError)ex.ErrorCode,
                    ex.ErrorCode)
                ;
                return;
            }
            IPEndPoint ieServer = (IPEndPoint)server.RemoteEndPoint;
            Console.WriteLine("Server on IP:{0} at port {1}", ieServer.Address, ieServer.Port);
            using (NetworkStream ns = new NetworkStream(server))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {

                sw.WriteLine(txtUsuario.Text);
                sw.Flush();

                comand = ((Button)sender).Tag.ToString();
                sw.WriteLine(comand);
                sw.Flush();
                lblinfo.Text = "";
                user = txtUsuario.Text;
                lblinfo.Text += sr.ReadToEnd();


            }
            Console.WriteLine("Ending connection");
            guardarDatos();
            server.Close();
        }

        private void txtUsuario_TextChanged(object sender, EventArgs e)
        {
            if (txtUsuario.Text != "admin" && (ushort.TryParse(txtPuerto.Text, out port) && IPAddress.TryParse(txtIp.Text, out ip)))
            {
                btnList.Enabled = true;
                btnAdd.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnList.Enabled = false;
            }
        }


        public void guardarDatos()
        {
            try
            {
                string directory = Environment.GetEnvironmentVariable("userprofile");
                using (StreamWriter sw = new StreamWriter(directory + "\\datos.txt"))
                {
                    sw.WriteLine($"{port}_{ip}_{user}");
                }
            }
            catch (Exception ex) when (ex is IOException | ex is ArgumentException)
            {
            }
        }

        public void leerDatos()
        {
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("userprofile") + "\\datos.txt"))
                {
                    String datos = sr.ReadToEnd();

                    ushort.TryParse(datos.Split('_')[0],out port);
                    IPAddress.TryParse(datos.Split('_')[1], out ip);
                    user = datos.Split('_')[2];
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            {
                port = 31416;
                ip = IPAddress.Loopback;
                user = "";
            }
        }
    }
}
