using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace APS_Chat
{
    public partial class TelaCliente : Form
    {
        public TelaCliente()
        {
            InitializeComponent();
        }
        private static readonly Socket Socket_Cliente = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int MAX = 2048;

        private void btnConectar_Click(object sender, EventArgs e)
        {
            txtResultado.Clear();
            ConectarAoServidor();
            ReceberResposta();
        }
        private void ConectarAoServidor()
        {
            while (!Socket_Cliente.Connected)
            {
                try
                {
                    Console.WriteLine("Tentando se conectar, aguarde...");
                    IPAddress _ip;
                    int _porta;

                    if (!IPAddress.TryParse(txtIp.Text, out _ip))
                    {
                        txtResultado.AppendText("IP inválido");
                        return;
                    }

                    if (!int.TryParse(txtPorta.Text, out _porta))
                    {
                        txtResultado.AppendText("Porta inválida");
                        return;
                    }

                    Socket_Cliente.Connect(_ip, _porta);
                }
                catch (SocketException e)
                {
                    txtResultado.AppendText("Não foi possível criar conexão");
                    Console.WriteLine(e);
                }
            }

            txtResultado.Clear();
            MessageBox.Show(this, "Conexão criada com sucesso!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DesabilitarFormulario();
        }

        private void DesabilitarFormulario()
        {
            btnConectar.Enabled = false;
            txtIp.ReadOnly = true;
            txtPorta.ReadOnly = true;
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            EnviarMensagem(txtMensagem.Text);
            txtMensagem.Clear();
        }

        private void EnviarMensagem(string texto)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(texto);
            Socket_Cliente.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private void ReceberResposta()
        {
            var buffer = new byte[MAX];
            int _resposta = Socket_Cliente.Receive(buffer, SocketFlags.None);
            if (_resposta == 0) return;
            var data = new byte[_resposta];
            Array.Copy(buffer, data, _resposta);
            string resposta = Encoding.ASCII.GetString(data);
            
            txtResultado.AppendText(resposta);
        }
    }
}
