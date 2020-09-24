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

namespace CloudEvaluator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int j;
        public string[] str;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Button1_click(object sender, RoutedEventArgs e)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                s.Connect(ServerIP.Text, Convert.ToInt32(ServerPort.Text));


                string m = TextBox1.Text;
                int scale = (int)scale_bar.Value;
                Graf_Building(0.1, scale, m, s);
                MessageBox.Show("clicked");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //try
            //{

            //    byte[] res_buff = new byte[1024];
            //    s.Receive(res_buff);

            //    MessageBox.Show(Encoding.ASCII.GetString(res_buff));
            //}
            //catch(Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}

        }

        public void OnCanvasClick(object sender, RoutedEventArgs e)
        {
            //string m = TextBox1.Text;
            //Graf_Building(0.01, 20, m);
        }

        public void Graf_Building(double step, int m, string func, Socket s)
        {
            Canvas.Children.Clear();
            AxisInit(m);

            Calculation(step, m, func, s);
        }

        public void Calculation(double step, int m, string func, Socket s)
        {
            string val = func + " ";
            for (double i = -Canvas.ActualWidth / 2; i <= Canvas.ActualWidth / 2; i += step)
            {
                if (i + step <= Canvas.ActualWidth)
                {
                    val += Math.Round(i, 6).ToString() + " ";
                }
                else
                {
                    val += Math.Round(i, 6).ToString();
                }
            }
            byte[] buffer = Encoding.ASCII.GetBytes(val);
            s.Send(buffer);

            byte[] buffer1 = new byte[8388608 * 2];
            s.Receive(buffer1);

            str = Encoding.ASCII.GetString(buffer1).Split(' ');
            j = 2;
            Array.Resize<string>(ref str, str.Length - 1);

            for (double i = -Canvas.ActualWidth / 2; i <= Canvas.ActualWidth / 2; i += step)
            {
                double f;
                double f1;
                try
                {
                    f = -Math.Round(Convert.ToDouble(str[j]), 4);
                    f1 = -Math.Round(Convert.ToDouble(str[j - 1]), 4);
                }
                catch (Exception e)
                {
                    f = 0;
                    f1 = 0;
                }
                j++;
                double error = 0.2;
                Point p1 = new Point((i + error) * m + Canvas.ActualWidth / 2, f * m + Canvas.ActualHeight / 2);
                Point p2 = new Point((i - step + error) * m + Canvas.ActualWidth / 2, f1 * m + Canvas.ActualHeight / 2);

                Lines(p2, p1);
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
                    X1 = Canvas.ActualWidth / 2 - m/5,
                    X2 = Canvas.ActualWidth / 2 + m/5,
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
                    X1 = Canvas.ActualWidth / 2 - m / 5,
                    X2 = Canvas.ActualWidth / 2 + m / 5,
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
                    Y1 = Canvas.ActualHeight / 2 - m / 5,
                    Y2 = Canvas.ActualHeight / 2 + m / 5
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
                    Y1 = Canvas.ActualHeight / 2 - m / 5,
                    Y2 = Canvas.ActualHeight / 2 + m / 5
                };
                Canvas.Children.Add(x);
            }
        }

        //public double Func(double x, string func)
        //{
        //    var res = func.Replace("x", "(" + (Math.Round(x, 4)).ToString().Replace(',','.') + ")");
        //    return new MathParser().Parse(res);
        //}

        private void Lines(Point p1, Point p2)
        {
            try
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
            catch (Exception e)
            {
                //Line line = new Line
                //{
                //    X1 = p1.X,
                //    Y1 = -Canvas.ActualHeight,
                //    X2 = p2.X,
                //    Y2 = p2.Y,
                //    Stroke = Brushes.Black
                //};
                //Canvas.Children.Add(line);
            }
        }
    }
}
