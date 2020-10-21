using Mathos.Parser;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Microsoft.Research.SEAL;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace server
{
    class Program
    {
        static TcpListener listener;


        static EncryptionParameters parms;

        static Evaluator evaluator;


        static void Main(string[] args)
        {
            listener = new TcpListener(IPAddress.Any, 2022);
            listener.Start();
            Console.WriteLine("Ожидание подключений...");

            TcpClient client = listener.AcceptTcpClient();

            while (true)
            {

                byte[] buffer = new byte[9377792];
                byte[] func_buffer = new byte[3];
                byte[] bufferParms = new byte[256];
                parms = new EncryptionParameters();
                try
                {
                    client.ReceiveTimeout = 10000;
                    Console.WriteLine("recieving");
                    int l = client.Client.Receive(bufferParms);
                    Console.WriteLine(l);

                    MemoryStream streamParms = new MemoryStream(bufferParms);
                    streamParms.Seek(0, SeekOrigin.Begin); 
                    parms.Load(streamParms);

                    SEALContext context = new SEALContext(parms);
                    evaluator = new Evaluator(context);

                    Console.WriteLine("recieving func");
                    l = client.Client.Receive(func_buffer);
                    Console.WriteLine(l);

                    string func = Encoding.ASCII.GetString(func_buffer);

                    Console.WriteLine("recieving count");
                    byte[] count_buff = new byte[3];
                    l = client.Client.Receive(count_buff);
                    string s = Encoding.ASCII.GetString(count_buff);
                    int count = Convert.ToInt32(s);
                    Console.WriteLine(l);

                    List<Ciphertext> data = new List<Ciphertext>();
                    for (int i = 0; i < count; i++)
                    {
                        Console.WriteLine(count);
                        Console.WriteLine(i);
                        Console.WriteLine("recieving values");
                        l = client.Client.Receive(buffer);
                        Console.WriteLine(l);

                        MemoryStream stream = new MemoryStream(buffer);
                        stream.Seek(0, SeekOrigin.Begin);

                        data.Add(new Ciphertext());
                        data[data.Count - 1].Load(context, stream);
                        stream.Close();

                    }

                    List<Ciphertext> res = new List<Ciphertext>();

                    for (int i = 0; i < data.Count; i++)
                    {
                        Ciphertext ciphertext = Func(data[i], func);
                        res.Add(ciphertext);
                    }


                    for (int i = 0; i < res.Count; i++)
                    {
                        MemoryStream streamRes = new MemoryStream();
                        res[i].Save(streamRes);
                        for (int j = 0; j < 100000000; j++)
                        {

                        }
                        client.Client.Send(streamRes.GetBuffer());
                        streamRes.Close();
                    }

                    
                    streamParms.Close();
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static Ciphertext Func(Ciphertext x, string func)
        {
            Ciphertext res = new Ciphertext();
            if (func.Contains('*'))
            {
                Plaintext plaintext = new Plaintext(func[0].ToString());
                evaluator.MultiplyPlain(x, plaintext, res);
                return res;
            }
            else
            {
                return new Ciphertext();
            }
        }

        public static byte[] Read(StreamReader srp, byte[] buffer)
        {
            int length = (int)srp.BaseStream.Length;
            int read, offset = 0;
            char[] c = Encoding.UTF8.GetString(buffer).ToCharArray();
            do
            {
                read = srp.Read(c, offset, length);
                offset += read;
                length -= read;
            } while (read > 0);
            buffer = Encoding.UTF8.GetBytes(c.ToString());

            return buffer;
        }
    }
}
