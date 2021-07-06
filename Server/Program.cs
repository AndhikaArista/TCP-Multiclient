using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiServer
{
    class Program
    {
        // untuk membuat socket server baru dengan protokol TCP
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // untuk menyimpan socket dari client yang terhubung ke server
        private static readonly List<Socket> clientSockets = new List<Socket>();
        // size maksimal data yang bisa diterima oleh server
        private const int BUFFER_SIZE = 2048;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        // port yang digunakan
        private const int PORT = 8050;
        // ip address yang digunakan
        private const string IPADDRESS = "127.0.0.1";
        // list untuk menyimpan chat
        private static List<string> chats = new List<string>();

        static void Main()
        {
            Console.Title = "Server";

            // Memulai server
            SetupServer();

            // jika user menginput ke console server
            // server akan di stop
            Console.ReadLine();
            CloseAllSockets();
        }

        // SetupServer berguna untuk menginisialisasi dan memulai server
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            // menghubungkan socket server dengan ipaddress dan port yang sudah ditentukan
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(IPADDRESS), PORT));

            // mulai server, dan jalankan fungsi callback untuk menerima client
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server Ready");
        }

        // CloseAllSocket untuk memberhentikan server
        private static void CloseAllSockets()
        {
            // putus koneksi dengan semua client yang terhubung
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            // simpan data
            SaveData();

            // tutup socket server
            serverSocket.Close();
        }

        // AcceptCallback untuk menunggu dan menerima client yang ingin terhubung
        private static void AcceptCallback(IAsyncResult AR)
        {
            // buat socket baru untuk menampung client
            Socket socket;

            try
            {
                // tunggu sampai ada client yang terhubung
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            // tambahkan client tersebut ke dalam list 
            clientSockets.Add(socket);

            // jalankan fungsi callback untuk menghandle client tersebut
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            
            // ulangi fungsi accepcallback untuk menerima client baru
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        // ReceiveCallback untuk menghandle dan menerima kiriman data dari client yang dihandle
        private static void ReceiveCallback(IAsyncResult AR)
        {
            // socket client yang dihandle
            Socket client = (Socket)AR.AsyncState;
            // untuk menampung besar byte data kiriman dari client
            int received;

            try
            {
                // terima data dari client
                received = client.EndReceive(AR);
            }
            catch (SocketException)
            {
                // tutup client jika client yang dihandle tersebut terputus dari server
                Console.WriteLine("Client forcefully disconnected");
                client.Close();

                // remove client dari list client yang terhubung
                clientSockets.Remove(client);
                return;
            }

            // convert data yang diterima dari byte menjadi string
            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string dataReceived = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine(dataReceived);

            // jika client menginput "exit", maka client disconnect
            // jika tidak, maka broadcast pesan tersebut
            if (dataReceived.ToLower() == "exit")
            {
                // disconnect client
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                clientSockets.Remove(client);
                Console.WriteLine("Client disconnected");
                return;
            }
            else
            {
                // simpan chat kedalam list
                chats.Add(dataReceived);

                // simpan data
                SaveData();

                // broadcast pesan
                Broadcast(client, dataReceived);
            }

            // panggil kembali fungsi ReceiveCallback untuk menghandle client ini
            client.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, client);
        }

        // Broadcast untuk mengirim chat ke seluruh client kecuali client pengirim
        private static void Broadcast(Socket sender, string message)
        {
            // convert data pesan dari string ke byte
            byte[] byteMessage = Encoding.ASCII.GetBytes(message);

            // lakukan pengulangan untuk setiap client yang terhubung
            foreach (Socket client in clientSockets)
            {
                // jika client tersebut bukan pengirim, maka kirim data pesan tersebut
                if (client != sender)
                {
                    client.Send(byteMessage, 0, byteMessage.Length, SocketFlags.None);
                }
            }
        }

        private static void SaveData()
        {
            // simpan kedalam file txt
            File.WriteAllLines(@"D:\Kuliah\Semester 4\Workshop Arsitektur Jaringan dan Komputer\FP C#\Server\ServerChatData.txt", chats);
        }
    }
}