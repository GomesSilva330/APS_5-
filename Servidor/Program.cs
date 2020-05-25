using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Servidor
{
    public static class Provider
    {
        public static string DATABASE = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=" + Path.GetDirectoryName(Environment.CurrentDirectory).Replace(@"\bin\Debug", "") + "\\Database.mdf;Integrated Security=True";
    }
    class Program
    {
        private static readonly Socket Socket_Servidor = new Socket
           (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> Clientes = new List<Socket>();
        private const int PORTA = 69;
        private const int MAX = 5000;
        private const string SERVIDOR = "J.A.R.V.I.S";
        private const string _diretorio = "J.A.R.V.I.S";
        private static readonly byte[] buffer = new byte[MAX];

        static void Main()
        {
            ConfigurarServidor();

            while (true)
            {
                string cmd = Console.ReadLine();
                if (cmd.ToLower() == "sair")
                {
                    Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes("\r\n[Servidor Fechado]")));
                    FecharServidor();
                    System.Threading.Thread.Sleep(2000);
                    Environment.Exit(0);
                }
                else if (cmd.ToLower() == "/criar")
                {
                    CriarUsuario();
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


            Socket_Servidor.Bind(new IPEndPoint(_ip, PORTA));
            Socket_Servidor.Listen(20);
            Socket_Servidor.BeginAccept(AceitarConexao, null);

            Console.WriteLine("*----------------------*");
            Console.WriteLine($"O seu ip é: {_ip}");
            Console.WriteLine($"Sua porta é: {PORTA}");
            Console.WriteLine($"Digite '/Criar' para criar um novo usuário'");
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

            _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);

            Console.WriteLine("Novo Cliente se Conectou!");

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

            if (_msg.Contains("-DISCONNECTED-"))
            {
                Console.WriteLine("O Cliente foi desconectado :( ");
                _cliente.Close();
                Clientes.Remove(_cliente);
                Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes(_msg.Replace("-DISCONNECTED-", ""))));
                return;
            }
            else if (_msg.Contains("-LOGIN-"))
            {
                var _string = _msg.Replace("-LOGIN-", "");
                dynamic obj = JsonConvert.DeserializeObject(_string);
                if (VerificarLogin(obj))
                {
                    Clientes.Add(_cliente);
                    Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes($"Usuário {obj.username} se conectou!")));
                    ArmazenarMensagem($"Usuário {obj.username} se conectou!");
                }
                else
                    _cliente.Send(Encoding.UTF8.GetBytes("\r\nUsuário ou senha incorretos."));

                _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);
            }
            else
            {
                Clientes.ForEach(x => x.Send(Encoding.UTF8.GetBytes(_msg)));
                if (!_msg.Contains("-IMG-") && !_msg.Contains("-EIMG-"))
                    ArmazenarMensagem(_msg);
                _cliente.BeginReceive(buffer, 0, MAX, SocketFlags.None, ReceberConexao, _cliente);
            }

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
        private static bool VerificarLogin(dynamic obj)
        {
            SqlConnection conn = new SqlConnection(Provider.DATABASE);
            conn.Open();

            string query = $"select * from Usuarios where Usuario = '{obj.username}' and Senha = '{obj.password}'";
            SqlDataAdapter adp = new SqlDataAdapter(query, conn);

            DataTable dt = new DataTable();
            adp.Fill(dt);

            conn.Close();

            if (dt.Rows.Count > 0) return true;
            else return false;
        }
        private static bool ArmazenarMensagem(string msg)
        {
            SqlConnection conn = new SqlConnection(Provider.DATABASE);
            conn.Open();

            string query = $"Insert into Mensagens (Mensagem,Criado) values (@msg,@data)";
            SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@msg", msg);
            cmd.Parameters.AddWithValue("@data", DateTime.Now);

            var rows = cmd.ExecuteNonQuery();
            conn.Close();

            if (rows > 0) return true;
            else return false;
        }
        private static void CriarUsuario()
        {
            Console.WriteLine("Digite o Usuário:");
            var username = Console.ReadLine();
            Console.WriteLine("Digite a Senha:");
            var password = Console.ReadLine();

            try
            {
                SqlConnection conn = new SqlConnection(Provider.DATABASE);
                conn.Open();

                string query = $"Insert into Usuarios (Usuario,Senha) values (@user,@pass)";
                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", password);

                var rows = cmd.ExecuteNonQuery();
                conn.Close();

                Console.WriteLine($"Usuário {username} criado com sucesso!");
            }
            catch (Exception)
            {
                Console.WriteLine($"Falha ao criar Usuário.");
                throw;
            }
        }
    }
}
