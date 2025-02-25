using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        public int port = 31416;
        IPEndPoint ie;

        public Form1()
        {
            InitializeComponent();
            ie = new IPEndPoint(IPAddress.Parse(ipServer), port);
            if (txtUsuario.Text != "admin")
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
                lblinfo.Text += sr.ReadToEnd();


            }
            Console.WriteLine("Ending connection");
            server.Close();
        }
    }
}
