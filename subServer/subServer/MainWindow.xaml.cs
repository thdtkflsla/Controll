using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Timers;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace subServer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpListener server = null;
        TcpClient clientSocket = null;
        static int counter = 0;

        public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();

        public MainWindow()
        {
            InitializeComponent();

            // socket start
            Thread t = new Thread(InitSocket);
            t.IsBackground = true;
            t.Start();
            //타이머를 사용하여 5초마다 한번식 실행
            Timer aTimer = new System.Timers.Timer(5000);

            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = true;


            /*
            Timer mTimer = new Timer(5000);
            mTimer.Elapsed += timedeevent;
            mTimer.Enabled = true;
            */


            // Console.WriteLine("Press the Enter key to exit the program... ");

            Console.ReadLine();

            // Console.WriteLine("Terminating the application...");
        }
        //모드 상태 불러오기
        /*private void timedeevent(object sender, ElapsedEventArgs e)
        {
            StringBuilder postmode = new StringBuilder();
            postmode.Append("serialnumber=test123");
            Encoding encoding = Encoding.UTF8;
            byte[] res = encoding.GetBytes(postmode.ToString());
            string URL2 = "http://210.123.254.243/android_login_api/controller_mode.php";

            HttpWebRequest wReqFirst2 = (HttpWebRequest)WebRequest.Create(URL2);
            wReqFirst2.Method = "POST";
            wReqFirst2.ContentType = "application/x-www-form-urlencoded";
            wReqFirst2.ContentLength = res.Length;
            //보낼 데이터를 Stream에 씀
            Stream postDataStream2 = wReqFirst2.GetRequestStream();
            postDataStream2.Write(res, 0, res.Length);
            postDataStream2.Close();

            HttpWebResponse wRespFirst2 = (HttpWebResponse)wReqFirst2.GetResponse();
            Stream respPostStream2 = wRespFirst2.GetResponseStream();
            StreamReader readerPost2 = new StreamReader(respPostStream2, Encoding.Default);
            string resultPost2 = readerPost2.ReadToEnd();
            JObject modesta = JObject.Parse(resultPost2);
            JObject sta = (JObject)modesta["mode"];
            string saving = (string)sta["saving_mode"];
            string security_mode = (string)sta["security_mode"];
            string alarm_mode = (string)sta["alarm_mode"];
            DisplayText(resultPost2);
            Console.WriteLine(resultPost2);
            //saving_mode(saving);
        }

        /*private void saving_mode(string saving)
        {
            MainWindow sta = new MainWindow();
            if (saving.Equals("on"))
            {
                
            }
        }*/

        private void InitSocket()
        {
            //로컬 포트 8181 을 Listen
            server = new TcpListener(IPAddress.Any, 53220);
            clientSocket = default(TcpClient);
            server.Start();
            DisplayText(">> Server Started");

            while (true)
            {
                try
                {
                    counter++;
                    //TcpClient Connection 요청을 받아들여 서버에서 새 TcpClient 객체를 생성하여 리턴
                    clientSocket = server.AcceptTcpClient();
                    //txtbox에 출력
                    DisplayText(">> 클라이언트 연결");
                    //NetworkStream을 얻어옴
                    NetworkStream stream = clientSocket.GetStream();
                    //데이터를 받을 버퍼 생성
                    byte[] buffer = new byte[1024];
                    //stream으로부터 바이트 데이타 읽기
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    //버퍼에 있는 데이터 인코딩
                    string user_name = Encoding.UTF8.GetString(buffer, 0, bytes);
                    user_name = user_name.Substring(0, user_name.IndexOf("$"));

                    clientList.Add(clientSocket, user_name);
                    //DisplayText(clientList);
                    

                    // send message all user
                    //SendMessageAll(user_name + " 님이 입장하셨습니다. ", "", false);
                    //SendMessageAll(user_name + " 님이 입장하셨습니다. ", "", false);
                    //handleClient 생성
                    handleClient h_client = new handleClient();
                    h_client.OnReceived += new handleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);
                }
                //소켓 오류시
                catch (SocketException se)
                {
                    //오류에 대한 메세지 호출
                    Trace.WriteLine(string.Format("InitSocket - SocketException : {0}", se.Message));
                    break;
                }
                //프로그램 오류시
                catch (Exception ex)
                {
                    //오류에 대한 메세지 호출
                    Trace.WriteLine(string.Format("InitSocket - Exception : {0}", ex.Message));
                    break;
                }

            }
            clientSocket.Close();
            server.Stop();
        }

        //클라이언트에서 연결 종료
        void h_client_OnDisconnected(TcpClient clientSocket)
        {
            //Dictionary에 있는 키값을 삭제
            if (clientList.ContainsKey(clientSocket))
            {
                clientList.Remove(clientSocket);
                
            }
               
        }
        //클라이언트에게 메세지를 받음
        private void OnReceived(string message, string user_name)
        {
            //txtbox에 유저아이디, 보낸 메세지 출력
            string displayMessage = message;
            
            DisplayText(displayMessage);
            SendMessageAll(message, true);
        }
        //받은 메세지를 연결된 클라이언트에 보내줌
        private void SendMessageAll(string message, bool flag)
        {
            //foreach로 clientList안에 있는 값을 추적
            foreach (var pair in clientList)
            {
                //연결되있는 클라이언트에 키값과 값을 받아온다 
                Trace.WriteLine(string.Format("tcpclient : {0} user_name : {1}", pair.Key, pair.Value));
                TcpClient client = pair.Key as TcpClient;
                //데이터를 읽어 Default 형식의 바이트 스트림으로 변환
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;
                //받은 메세지를 뿌려준다
                if (flag)
                {
                    //데이터 인코딩
                    buffer = Encoding.UTF8.GetBytes(message);
                }
                else
                {
                    buffer = Encoding.UTF8.GetBytes(message);
                }
                //받은 데이터를 Stream에 씀 
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
            }
        }
        //http
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            StringBuilder postmode = new StringBuilder();
            postmode.Append("serialnumber=test123");
            Encoding encoding = Encoding.UTF8;
            byte[] res = encoding.GetBytes(postmode.ToString());
            string URL2 = "http://210.123.254.243/android_login_api/controller_mode.php";

            HttpWebRequest wReqFirst2 = (HttpWebRequest)WebRequest.Create(URL2);
            wReqFirst2.Method = "POST";
            wReqFirst2.ContentType = "application/x-www-form-urlencoded";
            wReqFirst2.ContentLength = res.Length;
            //보낼 데이터를 Stream에 씀
            Stream postDataStream2 = wReqFirst2.GetRequestStream();
            postDataStream2.Write(res, 0, res.Length);
            postDataStream2.Close();

            HttpWebResponse wRespFirst2 = (HttpWebResponse)wReqFirst2.GetResponse();
            Stream respPostStream2 = wRespFirst2.GetResponseStream();
            StreamReader readerPost2 = new StreamReader(respPostStream2, Encoding.Default);
            string resultPost2 = readerPost2.ReadToEnd();
            JObject modesta = JObject.Parse(resultPost2);
            JObject sta = (JObject)modesta["mode"];
            string saving = (string)sta["saving_mode"];
            string security = (string)sta["security_mode"];
            string alarm = (string)sta["alarm_mode"];
            DisplayText(resultPost2);
            ////////////////////////////////////////////////////////////////////////////////////////////////////

            //post데이터 전송&받아오기
            StringBuilder postParams = new StringBuilder();
            postParams.Append("serialnumber=test123");

           


            //Encoding encoding = Encoding.UTF8;
            byte[] result = encoding.GetBytes(postParams.ToString());
           
            
            

            string Url = "http://210.123.254.243/android_login_api/controller.php";
            //HttpWebRequest로 통신
            HttpWebRequest wReqFirst = (HttpWebRequest)WebRequest.Create(Url);
            
            
           


           


            //통식 방식
            wReqFirst.Method = "POST";
            wReqFirst.ContentType = "application/x-www-form-urlencoded";
            wReqFirst.ContentLength = result.Length;
            //보낼 데이터를 Stream에 씀
            Stream postDataStream = wReqFirst.GetRequestStream();
            postDataStream.Write(result, 0, result.Length);
            postDataStream.Close();

            HttpWebResponse wRespFirst = (HttpWebResponse)wReqFirst.GetResponse();
            //데이터 받음
            Stream respPostStream = wRespFirst.GetResponseStream();
            //데이터 인코딩
            StreamReader readerPost = new StreamReader(respPostStream, Encoding.Default);
            //string으로 변환
            string resultPost = readerPost.ReadToEnd();
            //Console.WriteLine(resultPost);
            //Json파싱
            JObject onoff = JObject.Parse(resultPost);
            JObject ser = (JObject)onoff["serial_number"];
            //string ser2 = (string)onoff["serial_number"];
            string window = (string)ser["window"];
            string door = (string)ser["door"];
            string gas = (string)ser["gas"];
            string boiler = (string)ser["boiler"];
            //출력
            /*Console.WriteLine(resultPost);
            Console.WriteLine(window + ", " + door + ", " + gas + ", " + boiler + "", e.SignalTime);*/
            DisplayText(resultPost);
            Console.WriteLine(resultPost);

            //SendMessageAll(resultPost, true);
            //data 가공
            DataChanged(window, door, gas, boiler);

            //mode_management(saving, security, alarm, window, door, gas, boiler);
        }

       /* private void mode_management(string saving, string security, string alarm, string window, string door, string gas, string boiler)
        {
            if (saving.Equals("on"))
            {
                if()
            }
        }*/

        private void DataChanged(string window, string door, string gas, string boiler)
        {
            string win;
            string doo;
            string gass;
            string boil;

            //window
            if (window.Equals("on"))
            {
                win = "1";
            }
            else
            {
                win = "2";
            }
            //door
            if (door.Equals("on"))
            {
                doo = "1";
            }
            else
            {
                doo = "2";
            }
            //gas
            if (gas.Equals("on"))
            {
                gass = "1";
            }
            else
            {
                gass = "2";
            }
            //boiler
            if (boiler.Equals("on"))
            {
                boil = "1";
            }
            else
            {
                boil = "2";
            }

            SendMessageAll(win+doo+gass+boil, true);
        }






        //txtbox에 출력
        private void DisplayText(string text)
        {
            if (richTextBox1.Dispatcher.CheckAccess())
            {


                richTextBox1.AppendText(text + Environment.NewLine);
            }
            else
            {
                richTextBox1.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate
                {
                    richTextBox1.AppendText(text + Environment.NewLine);

                }));
            }

        }
    }
}
