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
            bool available = true;
            while (available)
            {
                int l;
                byte[] buffer = new byte[9377792];
                byte[] func_buffer = new byte[3];
                byte[] bufferParms = new byte[256];
                parms = new EncryptionParameters();

                try
                {
                    Console.WriteLine("recieving");
                    l = client.Client.Receive(bufferParms);
                    Console.WriteLine(l);

                    Console.WriteLine("recieving count");
                    byte[] count_buff = new byte[3];
                    l = client.Client.Receive(count_buff);
                    string s = Encoding.ASCII.GetString(count_buff);
                    int count = Convert.ToInt32(s);
                    Console.WriteLine(l);


                    Console.WriteLine("recieving func");
                    l = client.Client.Receive(func_buffer);
                    Console.WriteLine(l);
                    string func = Encoding.ASCII.GetString(func_buffer);

                    byte[] offset_buffer = new byte[4];
                    Console.WriteLine("recieving offset");
                    l = client.Client.Receive(offset_buffer);
                    Console.WriteLine(l);
                    string offset = Encoding.ASCII.GetString(offset_buffer);

                    for (int i = 0; i < count; i++)
                    {
                        MemoryStream streamParms = new MemoryStream(bufferParms);
                        streamParms.Seek(0, SeekOrigin.Begin);
                        parms.Load(streamParms);

                        SEALContext context = new SEALContext(parms);
                        evaluator = new Evaluator(context);

                        Ciphertext data = new Ciphertext();
                        Console.WriteLine(count);
                        Console.WriteLine(i);
                        Console.WriteLine("recieving values");
                        l = client.Client.Receive(buffer);
                        Console.WriteLine(l);

                        MemoryStream stream = new MemoryStream(buffer);
                        stream.Seek(0, SeekOrigin.Begin);

                        data = new Ciphertext();
                        data.Load(context, stream);
                        stream.Close();



                        Ciphertext res = new Ciphertext();

                        Ciphertext ciphertext = Func(data, func, new Plaintext(offset));
                        res = ciphertext;



                        MemoryStream streamRes = new MemoryStream();
                        res.Save(streamRes);

                        client.Client.Send(streamRes.GetBuffer());

                        streamRes.Close();
                        streamParms.Close();
                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    available = false;
                }
            }
            client.Client.Disconnect(true);
        }

        public static Ciphertext Func(Ciphertext x, string func, Plaintext offset)
        {
            Ciphertext res = new Ciphertext();
            if (func.Contains('*'))
            {
                Plaintext plaintext = new Plaintext(func[0].ToString());
                evaluator.SubPlainInplace(x, offset);
                evaluator.MultiplyPlain(x, plaintext, res);
                return res;
            }
            else
            {
                return new Ciphertext();
            }
        }
    }
}
