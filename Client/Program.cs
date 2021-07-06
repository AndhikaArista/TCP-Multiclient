using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

namespace MultiClient
{
    class Program
    {
        // buat socket baru
        private static readonly Socket ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // ip address yang digunakan
        private const string IPADDRESS = "127.0.0.1";
        // port yang digunakan
        private const int PORT = 8050;
        // username
        private static string username;

        static void Main()
        {
            Console.Title = "Client";

            // user menginput username
            Console.Write("Masukkan username: ");
            username = Console.ReadLine();

            // connect client ke server
            ConnectToServer();
        }

        private static void ConnectToServer()
        {
            try
            {
                // connect client ke server dengan ipaddress dan port yang telah ditentukan
                ClientSocket.Connect(IPAddress.Parse(IPADDRESS), PORT);
                Console.WriteLine("Connection success");

                // buat thread untuk mengirim data ke server
                Thread send = new Thread(SendRequest);
                send.Start();

                // buat thread untuk membaca data dari server
                Thread read = new Thread(ReceiveResponse);
                read.Start();
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection failed");
            }
        }

        // untuk exit dan disconnect client
        private static void Exit()
        {
            SendString("exit");
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
            Environment.Exit(0);
        }

        // SendRequest untuk user menginput data
        private static void SendRequest()
        {
            // lakukan pengulangan selama client tersambung
            while (ClientSocket.Connected)
            {
                // user menginput pesan
                byte[] inputBuffer = new byte[1024];
                Stream inputStream = Console.OpenStandardInput(inputBuffer.Length);
                Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, inputBuffer.Length));
                string input = Console.ReadLine();

                // jika pesan yang diinput "exit", maka client akan menjalankan fungsi Exit()
                // jika tidak, maka kirim pesan dengan ditambahkan username
                if (input.ToLower() == "exit")
                {
                    Exit();
                }
                else
                {
                    string request = $"{username}: {input}";
                    SendString(request);
                }
            }
        }

        // SendString untuk mengirimkan data ke server
        private static void SendString(string data)
        {
            if (ClientSocket.Connected)
            {
                // convert data dari string menjadi byte
                byte[] dataBuffer = Encoding.ASCII.GetBytes(data);
                // kirim data ke server
                ClientSocket.Send(dataBuffer, 0, dataBuffer.Length, SocketFlags.None);
            }
        }

        // ReceiveResponse untuk client mengambil data kiriman dari server
        private static void ReceiveResponse()
        {
            while (ClientSocket.Connected)
            {
                // besar data maksimal yang bisa diterima
                var buffer = new byte[2048];
                // client menunggu kiriman data
                int received = ClientSocket.Receive(buffer, SocketFlags.None);
                // convert data dari byte ke string
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.ASCII.GetString(data);
                // tulis data
                Console.WriteLine(text);
            }
        }
    }
}