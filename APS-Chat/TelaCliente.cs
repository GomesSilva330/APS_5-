using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        private const int MAX = 5000;
        private static readonly byte[] buffer = new byte[MAX];
        private Hashtable emotions;
        private List<bool> Logado = new List<bool>();

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
                Login();
                DesabilitarFormulario();
            }
            catch (SocketException e)
            {
                txtResultado.AppendText("Não foi possível criar conexão, servidor offline");
                Socket_Cliente.Shutdown(SocketShutdown.Both);
                HabilitarFormulario();
            }
        }
        private bool Login()
        {
            try
            {
                var User = new { username = txtUsuario.Text, password = txtSenha.Text };
                EnviarMensagem("-LOGIN-" + JsonConvert.SerializeObject(User));
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        private void DesabilitarFormulario()
        {
            btnConectar.Visible = false;
            btnSair.Visible = true;
            btnLogar.Visible = true;
            txtIp.ReadOnly = true;
            txtPorta.ReadOnly = true;
            txtUsuario.ReadOnly = true;
            txtSenha.ReadOnly = true;
            txtMensagem.ReadOnly = false;
            btnEnviar.Enabled = true;
            btnAnexar.Enabled = true;
        }
        private void HabilitarFormulario()
        {
            btnConectar.Visible = true;
            btnSair.Visible = false;
            btnLogar.Visible = false;
            txtIp.ReadOnly = false;
            txtPorta.ReadOnly = false;
            txtUsuario.ReadOnly = false;
            txtMensagem.ReadOnly = true;
            btnEnviar.Enabled = false;
            btnAnexar.Enabled = false;
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

                Logado.Add(resposta.Contains($"Usuário {txtUsuario.Text} se conectou!"));
                if (Logado.Any(x => x == true))
                {

                    txtResultado.Invoke((Action)delegate
                     {
                         if (resposta.Contains("-IMG-") && resposta.Contains("-EIMG-"))
                             VerificarMensagem(txtUsuario.Text, resposta.Replace("-IMG-", "").Replace("-EIMG-", ""));
                         else
                             txtResultado.AppendText("\r\n" + resposta);
                     });
                    txtUsuario.Invoke((Action)delegate
                    {
                        txtUsuario.ReadOnly = true;
                    });
                    txtSenha.Invoke((Action)delegate
                    {
                        txtSenha.ReadOnly = true;
                    });
                    btnLogar.Invoke((Action)delegate
                    {
                        btnLogar.Visible = false;
                    });
                    Emojies();
                }
                else
                {
                    txtResultado.Invoke((Action)delegate
                    {
                        txtResultado.AppendText(resposta + "\n");
                    });
                    txtUsuario.Invoke((Action)delegate
                    {
                        txtUsuario.ReadOnly = false;
                    });
                    txtSenha.Invoke((Action)delegate
                    {
                        txtSenha.ReadOnly = false;
                    });
                    btnLogar.Invoke((Action)delegate
                    {
                        btnLogar.Visible = true;
                    });
                }

                Socket_Cliente.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceberResposta, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        private void btnSair_Click(object sender, EventArgs e)
        {

            EnviarMensagem($"-DISCONNECTED-O usuário {txtUsuario.Text} se desconectou...");
            Socket_Cliente.Disconnect(false);
            Socket_Cliente.Shutdown(SocketShutdown.Both);
            Socket_Cliente.Close();
            Environment.Exit(0);
        }


        private void CreateEmotions()
        {
            emotions = new Hashtable(6);
            emotions.Add(":(", new Bitmap(Properties.Resources.cansado, new Size(20, 20)));
            emotions.Add("'-'", new Bitmap(Properties.Resources.luan, new Size(20, 20)));
            emotions.Add(";(", new Bitmap(Properties.Resources.preocupado, new Size(20, 20)));
            emotions.Add(":')", new Bitmap(Properties.Resources.risada, new Size(20, 20)));
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

        private void btnLogar_Click(object sender, EventArgs e)
        {
            Login();
        }

        private void btnAnexar_Click(object sender, EventArgs e)
        {
            FileDialog Menssagem = new OpenFileDialog();
            Menssagem.CheckFileExists = true;
            Menssagem.CheckPathExists = true;
            Menssagem.RestoreDirectory = true;
            Menssagem.Title = "Escolha o arquivo de imagem";
            if (Menssagem.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string arquivo = Menssagem.FileName;
                    Bitmap _bitmap = new Bitmap(arquivo);
                    _bitmap = new Bitmap(_bitmap, new Size(150, 100));
                    System.IO.MemoryStream mss = new System.IO.MemoryStream();
                    _bitmap.Save(mss, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] byteImage = mss.ToArray();

                    string base64String = Convert.ToBase64String(byteImage);

                    EnviarMensagem($"{txtUsuario.Text} enviou uma imagem: \n");
                    EnviarMensagem("-IMG-" + base64String + "-EIMG-");

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Por favor, envie um arquivo de imagem");
                    Console.WriteLine(ex);
                }
            }
        }
        public void VerificarMensagem(string user, string base64)
        {
            byte[] img_bytes = Convert.FromBase64String(base64);
            using (var ms = new System.IO.MemoryStream(img_bytes, 0, img_bytes.Length))
            {
                var _img = Image.FromStream(ms);
                Image img = new Bitmap(_img, new Size(150, 100));

                if (Clipboard.ContainsImage()) { 
                    Clipboard.Clear();
                    txtResultado.Invoke((Action)delegate
                    {
                        txtResultado.AppendText($"Clipboard cheio, não foi possível anexar imagem");
                    });
                    return;
                }
                Clipboard.SetImage(img);
                txtResultado.Invoke((Action)delegate
                {
                    txtResultado.ReadOnly = false;
                    txtResultado.Paste(DataFormats.GetFormat(DataFormats.Bitmap));
                    txtResultado.AppendText($"\n\r");
                    txtResultado.ReadOnly = true;
                });
            }
        }
    }
}
