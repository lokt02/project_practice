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
using System.Threading;

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
        public static string func;
        public static int scale;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Button1_click(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread thread = new Thread(new ThreadStart(StartBuild));
                thread.Start();

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

        public void StartBuild()
        {
            string ip = "", port = "";
            Dispatcher.BeginInvoke(new ThreadStart(delegate { ip = ServerIP.Text; port = ServerPort.Text; }));
            Thread.Sleep(100);
            TcpClient client = new TcpClient(ip, Convert.ToInt32(port));

            Dispatcher.BeginInvoke(new ThreadStart(delegate { func = TextBox1.Text; scale = (int)scale_bar.Maximum; }));
            Thread.Sleep(100);
            Calculation(1, scale, func, client);
        }

        public void Calculation(double step, int m, string func, TcpClient client)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate {
                Canvas.Children.Clear();
                AxisInit(m);
            }));

            points = new List<List<Point>>();

            EncryptionParameters parms = new EncryptionParameters(SchemeType.BFV);
            parms.PolyModulusDegree = 4096;
            parms.CoeffModulus = CoeffModulus.BFVDefault(4096);
            parms.PlainModulus = new Modulus(4096);


            SEALContext context = new SEALContext(new EncryptionParameters(parms));
            KeyGenerator keygen = new KeyGenerator(context);
            PublicKey publicKey = keygen.PublicKey;
            SecretKey secretKey = keygen.SecretKey;

            Encryptor encryptor = new Encryptor(context, publicKey);

            Decryptor decryptor = new Decryptor(context, secretKey);

            MemoryStream streamParms = new MemoryStream();
            MemoryStream streamSk = new MemoryStream();

            secretKey.Save(streamSk);
            parms.Save(streamParms); 
            
            byte[] spbuff = streamParms.GetBuffer();

            client.Client.Send(spbuff);

            byte[] count = Encoding.ASCII.GetBytes(Convert.ToInt32(Canvas.ActualWidth + step).ToString());
            client.Client.Send(count);

            byte[] func_buffer = Encoding.ASCII.GetBytes(func);
            client.Client.Send(func_buffer);

            byte[] offset_buffer = Encoding.ASCII.GetBytes((0).ToString());
            client.Client.Send(offset_buffer);

            int x = -(int)Canvas.ActualWidth / 2;
            for (int i = 0; i <= (int)(Canvas.ActualWidth/step); i += 1)
            {
                Plaintext data = new Plaintext();
                string temp = (i).ToString();
                bool sign;
                if(x >= 0)
                {
                    data = new Plaintext(x.ToString());
                    sign = false;
                }
                else
                {
                    data = new Plaintext(Math.Abs(x).ToString());
                    sign = true;
                }


                Ciphertext dataEncrypted = new Ciphertext();

                encryptor.Encrypt(data, dataEncrypted);


                byte[] sbuff;

                MemoryStream streamEncrypted = new MemoryStream();
                dataEncrypted.Save(streamEncrypted);
                sbuff = streamEncrypted.GetBuffer();

                client.Client.Send(sbuff);
                streamEncrypted.Close();


                byte[] bufferRecieved = new byte[9377792 * 2];
                dataEncrypted = new Ciphertext();

                client.Client.Receive(bufferRecieved);
                MemoryStream streamRecieved = new MemoryStream(bufferRecieved);
                Ciphertext c = new Ciphertext();
                c.Load(context, streamRecieved);
                dataEncrypted = c;


                data = new Plaintext();
                decryptor.Decrypt(dataEncrypted, data);



                streamParms.Close();
                streamSk.Close();

                double f;
                string t = data.ToString();
                f = Convert.ToInt32(data.ToString(), 16);

                List<Point> p = new List<Point>();
                int ep = 0;
                Dispatcher.BeginInvoke(new ThreadStart(delegate { ep = (int)scale_bar.Maximum+1; }));
                Thread.Sleep(10);

                if (sign)
                {
                    x = -Math.Abs(x);
                    //f = -Math.Abs(f);
                }

                for (int k = 1; k < ep; k++)
                {
                    //p.Add(new Point((i - (int)Canvas.ActualWidth / 2) * k, (f + Canvas.ActualHeight / 2) * k));
                    p.Add(new Point((x) * k + Canvas.ActualWidth/2, f * k + Canvas.ActualHeight / 2));
                }

                points.Add(p);

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (i > 1 && points.Count > 1 && i < points.Count)
                    {
                        Lines(points[i - 1][(int)(scale_bar.Value - 1)], points[i][(int)(scale_bar.Value - 1)]);
                    }
                }));

                if (x < Canvas.ActualWidth / 2)
                {
                    x += (int)step;
                }
            }

            client.Client.Disconnect(true);

            /////
            /////
            /////
            //for (int i = 0; i < points.Count - 1; i++)
            //{
            //    Lines(points[i][(int)(scale_bar.Value - 1)], points[i + 1][(int)(scale_bar.Value - 1)]);
            //}
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
            for (int i = 1; i < points.Count - 1; i++)
            {
                Lines(points[i - 1][(int)(scale_bar.Value - 1)], points[i][(int)(scale_bar.Value - 1)]);
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
