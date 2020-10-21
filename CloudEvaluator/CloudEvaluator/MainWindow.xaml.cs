using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Research.SEAL;
using System.Net.Sockets;
using System.Net;
using Mathos.Parser;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;

namespace CloudEvaluator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int j;
        public string[] str;
        public List<List<Point>> points = new List<List<Point>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Button1_click(object sender, RoutedEventArgs e)
        {
            //Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //s.Connect(ServerIP.Text, Convert.ToInt32(ServerPort.Text));
                TcpClient client = new TcpClient(ServerIP.Text, Convert.ToInt32(ServerPort.Text));

                string m = TextBox1.Text;
                int scale = (int)scale_bar.Maximum;
                Graf_Building(1, scale, m, client);
                MessageBox.Show("loading");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void OnCanvasClick(object sender, RoutedEventArgs e)
        {
            //string m = TextBox1.Text;
            //Graf_Building(0.01, 20, m);
        }

        public void Graf_Building(double step, int m, string func, TcpClient client)
        {
            //Canvas.Children.Clear();
            //AxisInit(m);

            Calculation(step, m, func, client);
        }

        public void Calculation(double step, int m, string func, TcpClient client)
        {
            points = new List<List<Point>>();
            //string val = func;


            EncryptionParameters parms = new EncryptionParameters(SchemeType.BFV);
            parms.PolyModulusDegree = 4096;
            parms.CoeffModulus = CoeffModulus.BFVDefault(4096);
            parms.PlainModulus = new Modulus(4096 * 2 * 2);


            SEALContext context = new SEALContext(new EncryptionParameters(parms));
            KeyGenerator keygen = new KeyGenerator(context);
            PublicKey publicKey = keygen.PublicKey;
            SecretKey secretKey = keygen.SecretKey;

            Encryptor encryptor = new Encryptor(context, publicKey);

            //Evaluator evaluator = new Evaluator(context);

            Decryptor decryptor = new Decryptor(context, secretKey);

            //NetworkStream networkStream = new NetworkStream(s);

            //MemoryStream streamEncrypted = new MemoryStream();
            MemoryStream streamParms = new MemoryStream();
            MemoryStream streamSk = new MemoryStream();

            secretKey.Save(streamSk);
            parms.Save(streamParms);

            List<Plaintext> data = new List<Plaintext>();

            for (int i = 0; i <= (int)(Canvas.ActualWidth); i += (int)step)
            {
                string temp = i.ToString();
                data.Add(new Plaintext(temp));
            }

            List<Ciphertext> dataEncrypted = new List<Ciphertext>();

            for(int i = 0; i < data.Count; i++)
            {
                dataEncrypted.Add(new Ciphertext());
                encryptor.Encrypt(data[i], dataEncrypted[i]);
            }

            //foreach (Ciphertext d in dataEncrypted)
            //{
            //    d.Save(streamEncrypted);
            //}

            byte[] spbuff = streamParms.GetBuffer();
            byte[] sbuff;

            client.Client.Send(spbuff);

            byte[] func_buffer = Encoding.ASCII.GetBytes(func);
            client.Client.Send(func_buffer);

            byte[] count = Encoding.ASCII.GetBytes(dataEncrypted.Count.ToString());
            client.Client.Send(count);

            for (int i = 0; i < dataEncrypted.Count; i++)
            {
                MemoryStream streamEncrypted = new MemoryStream();
                dataEncrypted[i].Save(streamEncrypted);
                sbuff = streamEncrypted.GetBuffer();
                for (int j = 0; j < 100000000; j++)
                {

                }
                client.Client.Send(sbuff);
                streamEncrypted.Close();
            }

            //client.Client.Send(sbuff);

            byte[] bufferRecieved = new byte[9377792 * 2];
            dataEncrypted = new List<Ciphertext>();
            for (int i = 0; i < data.Count; i++)
            {
                client.Client.Receive(bufferRecieved);
                MemoryStream streamRecieved = new MemoryStream(bufferRecieved);
                Ciphertext c = new Ciphertext();
                c.Load(context, streamRecieved);
                dataEncrypted.Add(c);

            }
            //client.Client.Receive(bufferRecieved);
            //MemoryStream streamRecieved = new MemoryStream(bufferRecieved);
            //dataEncrypted = new List<Ciphertext>();
            //foreach (var d in dataEncrypted)
            //{
            //    Ciphertext c = new Ciphertext();
            //    c.Load(context, streamRecieved);
            //    dataEncrypted.Add(c);
            //}
            data = new List<Plaintext>();
            for (int i = 0; i < dataEncrypted.Count; i++)
            {
                data.Add(new Plaintext());
                decryptor.Decrypt(dataEncrypted[i], data[i]);
            }

            
            streamParms.Close();
            streamSk.Close();

            j = 2;

            for (double i = 0; i <= Canvas.ActualWidth; i += step)
            {
                double f;
                double f1;
                try
                {
                    f = -Math.Round(Convert.ToDouble(data[j]), 4);
                    f1 = -Math.Round(Convert.ToDouble(data[j - 1]), 4);
                }
                catch (Exception e)
                {
                    f = 0;
                    f1 = 0;
                }
                j++;
                double error = 0.2;

                List<Point> p = new List<Point>();
                for (int k = 0; k < m; k++)
                {
                    p.Add(new Point((i + error) * k - Canvas.ActualWidth, f * k - Canvas.ActualHeight));
                }

                points.Add(p);
            }


            /////
            /////
            /////
            Canvas.Children.Clear();
            AxisInit((int)scale_bar.Value);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Lines(points[i][(int)(scale_bar.Value - 1)], points[i + 1][(int)(scale_bar.Value - 1)]);
            }
            /////
            /////
            /////
            
        }

        public byte[] Read(StreamReader srp, byte[] buffer)
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

        public void ScrollScaler(object sender, RoutedEventArgs e)
        {
            Canvas.Children.Clear();
            AxisInit((int)scale_bar.Value);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Lines(points[i][(int)(scale_bar.Value - 1)], points[i + 1][(int)(scale_bar.Value - 1)]);
            }

        }

        public void AxisInit(int m)
        {
            Line YAxis = new Line
            {
                Stroke = Brushes.Red,
                X1 = Canvas.ActualWidth / 2,
                X2 = Canvas.ActualWidth / 2,
                Y1 = 0,
                Y2 = Canvas.ActualHeight
            };
            Canvas.Children.Add(YAxis);

            Point zPoint = new Point(Canvas.ActualWidth / 2, Canvas.ActualHeight / 2);

            for (int i = (int)(Canvas.ActualHeight / 2 + m); i < Canvas.ActualHeight; i+=m)
            {
                Line y = new Line
                {
                    Stroke = Brushes.Red,
                    X1 = Canvas.ActualWidth / 2 - 4,
                    X2 = Canvas.ActualWidth / 2 + 4,
                    Y1 = i,
                    Y2 = i
                };
                Canvas.Children.Add(y);
            }

            for (int i = (int)(Canvas.ActualHeight / 2 - m); i > 0; i -= m)
            {
                Line y = new Line
                {
                    Stroke = Brushes.Red,
                    X1 = Canvas.ActualWidth / 2 - 4,
                    X2 = Canvas.ActualWidth / 2 + 4,
                    Y1 = i,
                    Y2 = i
                };
                Canvas.Children.Add(y);
            }

            Line XAxis = new Line
            {
                Stroke = Brushes.Green,
                X1 = 0,
                X2 = Canvas.ActualWidth,
                Y1 = Canvas.ActualHeight / 2,
                Y2 = Canvas.ActualHeight / 2
            };
            Canvas.Children.Add(XAxis);

            for (int i = (int)(Canvas.ActualWidth / 2 + m); i < Canvas.ActualWidth; i += m)
            {
                Line x = new Line
                {
                    Stroke = Brushes.Green,
                    X1 = i,
                    X2 = i,
                    Y1 = Canvas.ActualHeight / 2 - 4,
                    Y2 = Canvas.ActualHeight / 2 + 4
                };
                Canvas.Children.Add(x);
            }

            for (int i = (int)(Canvas.ActualWidth / 2 - m); i > 0; i -= m)
            {
                Line x = new Line
                {
                    Stroke = Brushes.Green,
                    X1 = i,
                    X2 = i,
                    Y1 = Canvas.ActualHeight / 2 - 4,
                    Y2 = Canvas.ActualHeight / 2 + 4
                };
                Canvas.Children.Add(x);
            }
        }

        private void Lines(Point p1, Point p2)
        {
            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.Black
            };
            Canvas.Children.Add(line);
        }

        private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
