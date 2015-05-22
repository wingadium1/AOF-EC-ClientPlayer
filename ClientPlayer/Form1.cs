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
using System.Threading;
using System.Windows.Forms;

namespace ClientPlayer
{
    public partial class formpresent : Form
    {
        #region Variable

        //defaut address
        string IP = null;
        int PORT = 2505;

        //Threads
        System.Threading.Thread thConnecttoServer;

        //Biến dùng để gửi, nhận dữ liệu
        byte[] outputData = new byte[1024];
        byte[] inputData = new byte[1024];

        TcpClient _client;
        public TcpClient Client
        {
            get { return _client; }
            set { _client = value; }
        }
        // register flag
        private bool registed = false;
        private int timeLeft;
        string _name;
        private bool sendted = false;
        #endregion


        public formpresent()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            labelQuestion.Text = "";
            labelTimer.Text = "";
            labelAnswer.Text = "";
            labelStatus.Text = "";
            button1.Enabled = false;
        }

        private void formpresent_Load(object sender, EventArgs e)
        {
            inputIP();
        }


        #region Function, Method and Procedure
        private void inputIP()
        {
            InputBoxResult test = InputBox.Show("Input server IP" + "\n" 
                  , "Input server IP", "Default", 100, 0);

            if (test.ReturnCode == DialogResult.OK)
            {
                
                IP = test.Text;
               // MessageBox.Show(IP);

                thConnecttoServer = new Thread(Connect);
                thConnecttoServer.IsBackground = true;
                thConnecttoServer.Start();
                

            }
            else
            {
                Application.Exit();
            }
        }

        private delegate void dlgStartTheQuestion();
        private void Connect()
        {
            try
            {
                //Thông báo đã kết nối đến máy chủ
                Client = new TcpClient();
                Client.Connect(IPAddress.Parse(IP), PORT);

                while (true)//Trong khi vẫn còn kết nối
                {
                    //Nhận dữ liệu từ máy chủ
                    try
                    {
                        labelStatus.Text = "Connected to server IP:  "+ IP;
                        if (!registed)
                        {
                            register();
                            registed = true;
                        }
                        else
                        {
                            byte[] dlNhan = new byte[1048576];
                            Client.GetStream().Read(dlNhan, 0, 1048576);

                            // tmp = Encoding.Unicode.GetString(dlNhan);
                            Utility.Message reciveMessage = ByteArrayToMessage(dlNhan);
                            switch (reciveMessage.type)
                            {
                                case (Utility.Message.Type.Quest):
                                    sendted = false;
                                    button1.Enabled = true;
                                    labelQuestion.Text = reciveMessage.x.question;
                                    labelAnswer.Text = "The true answer is : "+ reciveMessage.x.ans;
                                    labelAnswer.Visible = false;
                                    if (null != reciveMessage.image)
                                    {
                                        MemoryStream ms = new MemoryStream(reciveMessage.image);
                                        Image returnImage = Image.FromStream(ms);
                                        Bitmap image = null;
                                        image = new Bitmap(returnImage);
                                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                        pictureBox1.ClientSize = new Size(600, 400);
                                        pictureBox1.Image = (Image)image;
                                    }
                                    {
                                        timeLeft = reciveMessage.x.questionTime*10;
                                        StartTheQuestion();
                                    }
                                    break;
                                case (Utility.Message.Type.ShowAns):
                                    labelAnswer.Visible = true; 
                                    break;
                                case (Utility.Message.Type.Ans):
                                    if (!sendted)
                                    {
                                        timerCountDown.Stop();
                                        button1.Enabled = false;
                                    }
                                    break;
                                case (Utility.Message.Type.Cnt):
                                    if (!sendted)
                                    {
                                        button1.Enabled = true;
                                    }
                                    break;
                            }
                        }
                    }

                    catch
                    {
                        MessageBox.Show("Lost Connect!!!! + IP");
                        registed = false;
                        Connect();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Lost Connect!!!!");
                registed = false;
                Connect();
            }

        }
        #endregion

        private void formpresent_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (null != Client)
            {
                Client.Close();
            }
            if (null != thConnecttoServer)
            {
                thConnecttoServer.Abort();
            }
            
            Application.Exit();
        }

        private void register()
        {
            InputBoxResult name = InputBox.Show("Enter your name" + "\n" 
                  , "Register", "Default", 100, 0);

            if (name.ReturnCode == DialogResult.OK)
            {
                _name = name.Text;
                InputBoxResult slot = InputBox.Show("Hey "+ _name +"! Enter your slot" + "\n"
                  , "Register", "Default", 100, 0);
                if (name.ReturnCode == DialogResult.OK)
                {
                    Utility.Message registerMess = 
                        new Utility.Message(Utility.Message.Type.JoinSlot, null, slot.Text, LocalIPAddress(), _name);
                    SendData(registerMess);

                    this.TopMost = true;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;

                }
                else
                {
                    Application.Exit();
                }
            }
            else
            {
                Application.Exit();
            }
        }

        private void SendData(Utility.Message data)
        {
            try
                {
                    System.IO.MemoryStream fs = new System.IO.MemoryStream();
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    formatter.Serialize(fs, data);
                    byte[] buffer = fs.ToArray();
                    Client.GetStream().Write(buffer, 0, buffer.Length);
                    
                }
                catch (Exception er)
                {
                    
                }
            
        }

        private string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private byte[] MessageToByteArray(Utility.Message message)
        {
            System.IO.MemoryStream fs = new System.IO.MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(fs, message);
            return fs.ToArray();
        }

        private static Utility.Message ByteArrayToMessage(byte[] arrBytes)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ms.Write(arrBytes, 0, arrBytes.Length);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            Utility.Message reciveMessage = (Utility.Message)formatter.Deserialize(ms);
            return reciveMessage;
        }

        delegate void StartQuestionDelegate();
        private void StartTheQuestion()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new StartQuestionDelegate(StartTheQuestion));
            }
            else
            {
                
                labelTimer.Text = "10''00";
                timerCountDown.Interval = 100;
                timerCountDown.Start();
            }
        }

        


        

        private void sendAnswers()
        {
            Utility.Message newMess = new Utility.Message(Utility.Message.Type.Ans, null, "",IP, _name);
            SendData(newMess);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            sendAnswers();
            sendted = true;
            button1.Enabled = false;
            timerCountDown.Stop();
            labelTimer.Text = String.Format("{0}''{1}", timeLeft / 10, (timeLeft % 10));
        }

        delegate void SetTextCallback(string text);
        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.labelTimer.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.labelTimer.Text = text;
            }
        }

        private void timerCountDown_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                // Display the new time left 
                // by updating the Time Left label.
                timeLeft -= 1;
                labelTimer.Text = String.Format("{0}''{1}", timeLeft / 10, (timeLeft % 10));
            }
            else
            {
                // If the user ran out of time, stop the timer, show 
                // a MessageBox, and fill in the answers.
                timerCountDown.Stop();
                labelTimer.Text = "Time's up!";
                // MessageBox.Show("Time's up");
                button1.Enabled = false;
                sendAnswers();
            }
        }

        private void labelAnswer_Click(object sender, EventArgs e)
        {

        }


    }

    

    
}
