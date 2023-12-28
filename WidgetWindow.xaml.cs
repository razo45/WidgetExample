using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;


namespace WidgetExampleNS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class WidgetWindow : Window
    {
        public string Temp = string.Empty;
        // Константы для работы с окнами
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x80;

        // Импорт функции SetWindowLong
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
        public WidgetWindow()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


            string a = string.Empty;
            a = Task.Run(() => MakeRequest()).Result;
            double currentTemperature  = GetTemperatureForCurrentTime(a);
            int roundedValue = (int)Math.Round(currentTemperature + 1);
            lbMain1.Content = "Сейчас: " + (roundedValue ) + "C°";
            var timer2 = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
            timer2.Tick += (o, e) =>
            {

                lbMain.Content = DateTime.Now.ToString("HH:mm:ss");
              
            };
            timer2.Start();





            this.Left = Properties.Settings.Default.PozX;
            this.Top = Properties.Settings.Default.PozY;

            this.lmAbs = Properties.Settings.Default.Point;


        }

        public static double GetTemperatureForCurrentTime(string jsonString)
        {
            try
            {
                // Преобразование строки JSON в объект dynamic
                dynamic jsonObject = JsonConvert.DeserializeObject(jsonString);

                // Получение текущего времени
                DateTime currentTime = DateTime.Now;

                // Преобразование JArray в список строк
                List<string> timeList = jsonObject.hourly.time.ToObject<List<string>>();

                // Использование LINQ для получения ближайшего времени
                string nearestTime = timeList
                    .Select(time => DateTime.Parse(time))
                    .OrderBy(time => Math.Abs((time - currentTime).TotalSeconds))
                    .First()
                    .ToString("yyyy-MM-ddTHH:mm");

                // Использование методов JArray для получения индекса
                int index = timeList.IndexOf(nearestTime);

                // Получение температуры для найденного времени
                double currentTemperature = jsonObject.hourly.temperature_2m[index];


                return currentTemperature;
                Console.WriteLine($"Текущая температура: {currentTemperature}°C");
            }
            catch (Exception ex)
            {
                // Обработка ошибок при разборе JSON
                Console.WriteLine("Ошибка при разборе JSON: " + ex.Message);
                return double.NaN; // или выбросить исключение
            }
        }


        static async Task<string> MakeRequest()
        {
            using (HttpClient httpClient = new HttpClient())
            {


                try
                {
                    // Отправляем GET-запрос и получаем ответ
                    HttpResponseMessage response = await httpClient.GetAsync("https://api.open-meteo.com/v1/forecast?latitude=55.7522&longitude=37.6156&hourly=temperature_2m&past_days=1&forecast_days=1");

                    // Проверяем успешность запроса
                    if (response.IsSuccessStatusCode)
                    {
                        // Получаем содержимое ответа
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Обрабатываем полученные данные
                        Console.WriteLine("Ответ от API:");
                        Console.WriteLine(responseBody);
                        string responseData = await response.Content.ReadAsStringAsync();
                        return responseData;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: {response.StatusCode}");
                        return response.StatusCode.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    return ex.Message.ToString();
                }






            }
        }

        #region Bottommost

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        private void ToBack()
        {
            var handle = new WindowInteropHelper(this).Handle;
            SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ToBack();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            ToBack();
        }

        #endregion

        #region Move

        private bool winDragged = false;
        private Point lmAbs = new Point();

        void Window_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            winDragged = true;
            this.lmAbs = e.GetPosition(this);
            this.lmAbs.Y = Convert.ToInt16(this.Top) + this.lmAbs.Y;
            this.lmAbs.X = Convert.ToInt16(this.Left) + this.lmAbs.X;
            Mouse.Capture(this);
        }

        void Window_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            winDragged = false;
            Mouse.Capture(null);
        }

        void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (winDragged)
            {
                Point MousePosition = e.GetPosition(this);
                Point MousePositionAbs = new Point();
                MousePositionAbs.X = Convert.ToInt16(this.Left) + MousePosition.X;
                MousePositionAbs.Y = Convert.ToInt16(this.Top) + MousePosition.Y;
                this.Left = this.Left + (MousePositionAbs.X - this.lmAbs.X);
                this.Top = this.Top + (MousePositionAbs.Y - this.lmAbs.Y);





                this.lmAbs = MousePositionAbs;

                Properties.Settings.Default.PozX = this.Left;
                Properties.Settings.Default.PozY = this.Top;
                Properties.Settings.Default.Point = this.lmAbs;
                Properties.Settings.Default.Save();
            }
        }

        #endregion

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Получаем дескриптор окна
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            // Получаем текущий стиль окна
            int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Устанавливаем новый стиль, скрывающий окно из Alt+Tab
            SetWindowLong(hwnd, GWL_EXSTYLE, currentStyle | WS_EX_TOOLWINDOW);
        }

        // Импорт функции GetWindowLong
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            this.Close();
        }

        private void RichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}

