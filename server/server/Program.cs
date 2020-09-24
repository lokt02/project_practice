using Mathos.Parser;
using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace server
{
    class Program
    {
        static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static void Main(string[] args)
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 2022));
            socket.Listen(50);

            while (true)
            {
                Socket s = socket.Accept();
                Console.WriteLine("Принято подключение");

                //byte[] number1_buffer = new byte[512];
                //byte[] operator_buffer = new byte[16];
                //byte[] number2_buffer = new byte[512];
                byte[] buffer = new byte[8388608];
                try
                {
                    //s.Connect(s.AddressFamily.ToString(), s.);
                    s.Receive(buffer);
                    string[] str = Encoding.ASCII.GetString(buffer).Split(' ');
                    Array.Resize(ref str, str.Length-1);
                    string res = "";
                    for (int i = 1; i < str.Length; i++)
                    {
                        //Console.WriteLine(i);
                        //Console.WriteLine(str[i]);
                        //if(i > str.Length - 2)
                        //{
                        //    Console.WriteLine(str[i]);
                        //}
                        double func;
                        try
                        {
                            func = Func(Convert.ToDouble(str[i]), str[0]);
                            if (i < str.Length - 1)
                            {
                                res += Math.Round(func, 6) + " ";
                            }
                            else
                            {
                                res += Math.Round(func, 6);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    
                    byte[] res_buffer = Encoding.ASCII.GetBytes(res);
                    s.Send(res_buffer);

                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        public static double Func(double x, string func)
        {
            Console.WriteLine(Math.Round(x, 4).ToString().Replace(',', '.'));
            var res = func.Replace("x", "(" + Math.Round(x, 4).ToString().Replace(',', '.') + ")");
            MathParser parser = new MathParser();
            double p = parser.Parse(res);
            if (p < int.MaxValue)
            {
                Console.WriteLine(p);
                return p;
            }
            else
            {
                return int.MaxValue;
            }
        }
    }
}
