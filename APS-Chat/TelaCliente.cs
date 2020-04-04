using System;
using System.Collections;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace APS_Chat
{
    public partial class TelaCliente : Form
    {
        private static readonly Socket Socket_Cliente = new Socket
    (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private const int MAX = 2048;
        private static readonly byte[] buffer = new byte[MAX];
        private Hashtable emotions;
        public TelaCliente()
        {
            InitializeComponent();
            CreateEmotions();
            HabilitarFormulario();
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            txtResultado.Clear();
            ConectarAoServidor();
        }
        private void ConectarAoServidor()
        {
            try
            {
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

                Socket_Cliente.BeginConnect(_ip, _porta, Conectado, null);
                txtResultado.AppendText("Conectou!");
                DesabilitarFormulario();
            }
            catch (SocketException e)
            {
                txtResultado.AppendText("Não foi possível criar conexão, servidor offline");
                Socket_Cliente.Shutdown(SocketShutdown.Both);
                HabilitarFormulario();
            }
        }
        private void DesabilitarFormulario()
        {
            btnConectar.Visible = false;
            btnSair.Visible = true;
            txtIp.ReadOnly = true;
            txtPorta.ReadOnly = true;
            txtUsuario.ReadOnly = true;
            txtMensagem.ReadOnly = false;
            btnEnviar.Enabled = true;
        }
        private void HabilitarFormulario()
        {
            btnConectar.Visible = true;
            btnSair.Visible = false;
            txtIp.ReadOnly = false;
            txtPorta.ReadOnly = false;
            txtUsuario.ReadOnly = false;
            txtMensagem.ReadOnly = true;
            btnEnviar.Enabled = false;
        }
        private void btnEnviar_Click(object sender, EventArgs e)
        {
            EnviarMensagem(txtUsuario.Text + ": " + txtMensagem.Text);
            txtMensagem.Clear();
        }
        private void EnviarMensagem(string texto)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(texto);
            Socket_Cliente.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }
        private void Conectado(IAsyncResult AR)
        {
            try
            {
                Socket_Cliente.EndConnect(AR);
                Socket_Cliente.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceberResposta, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void ReceberResposta(IAsyncResult AR)
        {
            try
            {
                int _resposta = Socket_Cliente.EndReceive(AR);

                if (_resposta == 0) return;

                var data = new byte[_resposta];
                Array.Copy(buffer, data, _resposta);
                var resposta = Encoding.UTF8.GetString(data);
                txtResultado.Invoke((Action)delegate
                {
                    txtResultado.AppendText("\r\n"+resposta);
                });

                Emojies();
                Socket_Cliente.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceberResposta, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        private void btnSair_Click(object sender, EventArgs e)
        {
            try
            {
                Socket_Cliente.Close();
            }
            catch (Exception ex)
            { Console.WriteLine(ex); }
            finally
            {
                HabilitarFormulario();
                txtResultado.AppendText("\r\nDesconectou..");
            }
        }

        private void CreateEmotions()
        {
            emotions = new Hashtable(6);
            emotions.Add(":(", new Bitmap(Properties.Resources.cansado, new Size(20, 20)));
            emotions.Add("'-'", new Bitmap(Properties.Resources.luan, new Size(20, 20)));
            emotions.Add(";(", new Bitmap(Properties.Resources.preocupado, new Size(20, 20)));
            emotions.Add(":'(", new Bitmap(Properties.Resources.risada, new Size(20, 20)));
            emotions.Add(":)", new Bitmap(Properties.Resources.sorriso, new Size(20, 20)));
        }

        void Emojies()
        {
            txtResultado.Invoke((Action)delegate
            {
                foreach (string emote in emotions.Keys)
                {
                    while (txtResultado.Text.Contains(emote))
                    {
                        int ind = txtResultado.Text.IndexOf(emote);
                        txtResultado.ReadOnly = false;
                        txtResultado.Select(ind, emote.Length);
                        Clipboard.SetImage((Image)emotions[emote]);
                        if (Clipboard.ContainsImage())
                            txtResultado.Paste();

                        txtResultado.ReadOnly = true;
                        Clipboard.Clear();

                    }
                }
                //txtResultado.Paste();
            });
        }
    }
}
