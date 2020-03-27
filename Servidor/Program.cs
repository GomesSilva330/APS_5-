using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Servidor
{
    class Program
    {
        private static readonly Socket Socket_Servidor = new Socket
           (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> Clientes = new List<Socket>();
        private const int PORTA = 27015;
        private const int MAX = 2048;
        private const string SERVIDOR = "J.A.R.V.I.S";
        private static readonly byte[] buffer = new byte[MAX];

        static void Main()
        {
            ConfigurarServidor();

            Console.ReadLine();
            FecharServidor();
        }

        private static IPAddress BuscarIp(string host)
        {
            return Dns.GetHostEntry(host).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
        }
        private static string BuscarHost(string ip)
        {
            return Dns.GetHostEntry(ip).HostName;
        }
        private static void ConfigurarServidor()
        {
            Console.WriteLine("Começando...");
            var _host = Dns.GetHostName();
            //var _ip = BuscarIp(_host);
            var _ip = IPAddress.Any;
            Console.WriteLine($"Bem Vindo ao {SERVIDOR} , {_host}");

            Socket_Servidor.Bind(new IPEndPoint(_ip, PORTA));
            Socket_Servidor.Listen(20);
            Socket_Servidor.BeginAccept(AceitarConexao, null);

            Console.WriteLine("*----------------------*");
            Console.WriteLine($"O seu ip é: {_ip}");
            Console.WriteLine($"Sua porta é: {PORTA}");
            Console.WriteLine("Aguarando Clientes...");
            Console.WriteLine("*----------------------*");
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void FecharServidor()
        {
            foreach (Socket socket in Clientes)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            Console.WriteLine("Desligando em 3...");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Desligando em 2...");
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Desligando em 1...");
            System.Threading.Thread.Sleep(1000);
            Socket_Servidor.Close();
        }

        private static void AceitarConexao(IAsyncResult AR)
        {
            Socket socket;

            try { socket = Socket_Servidor.EndAccept(AR); }
            catch (ObjectDisposedException)
            {
                return;
            }

            Clientes.Add(socket);
            socket.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, socket);

            Console.WriteLine("Novo Cliente se Conectou!");
            socket.Send(Encoding.ASCII.GetBytes($"Bem Vindo ao {SERVIDOR}!"));
            socket.Send(Encoding.ASCII.GetBytes($"\r\nPara sair, digite \"sair\""));

            Socket_Servidor.BeginAccept(AceitarConexao, null);
        }

        private static void ReceberConexao(IAsyncResult AR)
        {
            Socket _cliente = (Socket)AR.AsyncState;
            int _receptor;

            try
            {
                _receptor = _cliente.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("O Cliente foi desconectado :( ");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                _cliente.Close();
                Clientes.Remove(_cliente);
                return;
            }

            byte[] _rec_buffer = new byte[_receptor];
            Array.Copy(buffer, _rec_buffer, _receptor);

            string _msg = Encoding.ASCII.GetString(_rec_buffer);

            var _cliente_ip = ((IPEndPoint)_cliente.LocalEndPoint).Address.ToString();
            var _cliente_nome = BuscarHost(_cliente_ip);

            Console.WriteLine($"({_cliente_nome}) " + _msg);

            if (_msg.ToLower() == "sair")
            {
                // Always Shutdown before closing
                _cliente.Shutdown(SocketShutdown.Both);
                _cliente.Close();
                Clientes.Remove(_cliente);
                Console.WriteLine("Cliente se desconectou");
                return;
            }
            _cliente.Send(Encoding.ASCII.GetBytes("\r\n Recebido"));
            _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);
        }
    }
}
