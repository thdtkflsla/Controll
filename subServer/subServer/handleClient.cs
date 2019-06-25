using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace subServer
{
    class handleClient
    {
        TcpClient clientSocket = null;
        public Dictionary<TcpClient, string> clientList = null;

        //클라이언트 연결
        public void startClient(TcpClient clientSocket, Dictionary<TcpClient, string> clientList)
        {
            this.clientSocket = clientSocket;
            this.clientList = clientList;

            //스레드 생성
            Thread t_hanlder = new Thread(doChat);
            //배경 스레드로 설정
            t_hanlder.IsBackground = true;
            t_hanlder.Start();
        }

        //받을 메세지
        public delegate void MessageDisplayHandler(string message, string user_name);
        public event MessageDisplayHandler OnReceived;
        //연결 종료시 발생
        public delegate void DisconnectedHandler(TcpClient clientSocket);
        public event DisconnectedHandler OnDisconnected;

        private void doChat()
        {
            NetworkStream stream = null;
            try
            {
                //데이터를 받을 버퍼 생성
                byte[] buffer = new byte[1024];
                //빈 변수 생성
                string msg = string.Empty;
                //초기값 설정
                int bytes = 0;
                int MessageCount = 0;

                while (true)
                {
                    MessageCount++;
                    //보낼 데이터를 읽어 Default 형식의 바이트 스트림으로 변환
                    stream = clientSocket.GetStream();
                    //stream으로부터 바이트 데이타 읽기
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    //데이타 인코딩
                    msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                    msg = msg.Substring(0, msg.IndexOf("$"));
                    //대리자 호출
                    if (OnReceived != null)
                        OnReceived(msg, clientList[clientSocket].ToString());
                }
            }

            catch (SocketException se)
            {
                //소켓 오류 발생시 메세지 출력
                Trace.WriteLine(string.Format("doChat - SocketException : {0}", se.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);
                    //Stream과 Tcpclient 객체 닫기
                    clientSocket.Close();
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                //프로그램 오류 발생시 메세지 출력
                Trace.WriteLine(string.Format("doChat - Exception : {0}", ex.Message));

                if (clientSocket != null)
                {
                    if (OnDisconnected != null)
                        OnDisconnected(clientSocket);
                    //Stream과 Tcpclient 객체 닫기
                    clientSocket.Close();
                    stream.Close();
                }
            }
        }
    }
    
}
