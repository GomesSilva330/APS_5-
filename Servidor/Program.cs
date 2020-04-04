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
        private const int PORTA = 69;
        private const int MAX = 2048;
        private const string SERVIDOR = "J.A.R.V.I.S";
        private static readonly byte[] buffer = new byte[MAX];

        static void Main()
        {
            ConfigurarServidor();

            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd.ToLower() == "sair") {
                    Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes("\r\n[Servidor Fechado]")));
                    FecharServidor();
                    System.Threading.Thread.Sleep(2000);
                    Environment.Exit(0);
                }
                else
                Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes("\r\n[Broadcaster]: " + cmd)));
            }
        }


        private static void ConfigurarServidor()
        {
            Console.WriteLine("Começando...");
            var _host = Dns.GetHostName();
            var _ip = BuscarIp(_host);
            //var _ip = IPAddress.Any;
            Console.WriteLine($"Bem Vindo ao {SERVIDOR} , {_host}");

            var newip = IPAddress.Parse("25.4.212.0");

            Socket_Servidor.Bind(new IPEndPoint(_ip, PORTA));
            Socket_Servidor.Listen(20);
            Socket_Servidor.BeginAccept(AceitarConexao, null);

            Console.WriteLine("*----------------------*");
            Console.WriteLine($"O seu ip é: {_ip}");
            Console.WriteLine($"Sua porta é: {PORTA}");
            Console.WriteLine("Aguarando Clientes...");
            Console.WriteLine("*----------------------*");
        }

        private static void FecharServidor()
        {
            foreach (Socket socket in Clientes)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            Socket_Servidor.Close();
            Console.WriteLine("Fechando Servidor");
        }

        private static void AceitarConexao(IAsyncResult AR)
        {
            Socket _cliente;

            try { _cliente = Socket_Servidor.EndAccept(AR); }
            catch (ObjectDisposedException)
            {
                return;
            }

            Clientes.Add(_cliente);
            _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);

            Console.WriteLine("Novo Cliente se Conectou!");
            _cliente.Send(Encoding.UTF8.GetBytes($"\r\nBem Vindo ao {SERVIDOR}!\r\nPara sair, digite 'sair'\r\n*----------------------*\r\n\r\n"));

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

            string _msg = Encoding.UTF8.GetString(_rec_buffer);

            if (_msg.ToLower() == "sair")
            {
                // Always Shutdown before closing
                _cliente.Shutdown(SocketShutdown.Both);
                _cliente.Close();
                Clientes.Remove(_cliente);
                Console.WriteLine("Cliente se desconectou");
                return;
            }

            Console.WriteLine($"[{BuscarHost(_cliente)}] " + _msg);
            Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes(_msg)));

            _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);
        }
        private static string BuscarHost(Socket _cliente)
        {
            var _cliente_ip = ((IPEndPoint)_cliente.LocalEndPoint).Address.ToString();
            return Dns.GetHostEntry(_cliente_ip).HostName;
        }
        private static IPAddress BuscarIp(string host)
        {
            return Dns.GetHostEntry(host).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
        }
    }
}
