using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Shell;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using Tobii.StreamEngine;
using System.Threading;
using System.Xaml;
using System.Xml.XPath;

namespace 検証用
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // フラグ等の初期化
        public static double Dpi_Factor = 1;
        //public string SingleOrDouble = "Single";
        OverLay overlay = new OverLay();
        public static Win32API.POINT gazingpoint = new Win32API.POINT();
        public const int GazingCircleAreaWidth_init = 100;
        public const int GazingCircleAreaHeight_init = 100;
        public int GazingCircleAreaWidth = 100;
        public int GazingCircleAreaHeight = 100;
        public int NeedTimeToGaze = 3000; // milliseconds
        public static int X;
        public static int Y;

        public void GetDpiFactorAndShowOverLay(object sender, RoutedEventArgs e)
        {
            // 起動時にDPI倍率を取得し、注視点表示用のオーバーレイを表示するための関数
            // 本来は起動中常に調べて、いつ倍率を変えても正常にマウス操作が出来るようにしたいが、
            // GetDpiFactor()をTaskに渡したOnGazePoint()から上手く呼び出せないため保留
            Window mainwindow = System.Windows.Application.Current.MainWindow;
            Dpi_Factor = PresentationSource.FromVisual(mainwindow).CompositionTarget.TransformFromDevice.M11;

            // オーバーレイ用のウィンドウをMainWIndowの子に設定
            overlay.Owner = this;
            overlay.Show();
        }

        public class Win32API
        {
            private object y_coordinate;
            private object x_coordinate;
            private object currentX;
            private object currentY;

            // Win32APIを.NETから使うために必要な構造体の定義

            // POINT構造体の定義
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }


            [DllImport("User32.Dll")]
            public static extern int GetDpiForWindow(
                in int hwnd
                );

            
        }
         private static void SetCursorPos(int v1, int v2)
        {
           // v1=mywindow.Top;
            // v2=mywindow.Left;
        }

        private void Button_Click_TrackingStart(object sender, RoutedEventArgs e)
        {
            
        
            Task task = new Task(StreamSample.StreamSampleMain);
            task.Start();
            Win32API.POINT currentpoint = new Win32API.POINT();
            //currentpoint.x=mywindow.Top;
            //currentpoint.y=mywindow.Left;
            // mywindow.Top=gazingpoint.x;
            //mywindow.Left=gazingpoint.y;
            mywindow.Top = 50;
            mywindow.Left = 200;
        }

        // 視線捕捉サンプル
        public static class StreamSample
        {
            private static void OnGazePoint(ref tobii_gaze_point_t gazePoint, IntPtr userData)
            {
                // Check that the data is valid before using it
                if (gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
                {
                    //Debug.WriteLine($"Gaze point: {gazePoint.position.x}, {gazePoint.position.y}");

                    // 画面解像度に合わせた座標に変換し、グローバル変数に記録
                    var w_height = System.Windows.SystemParameters.PrimaryScreenHeight;
                    var w_width = System.Windows.SystemParameters.PrimaryScreenWidth;
                    SetCursorPos((int)(w_width * gazePoint.position.x / Dpi_Factor), (int)(w_height * gazePoint.position.y / Dpi_Factor));
                    gazingpoint.x = (int)(w_width * gazePoint.position.x / Dpi_Factor);
                    gazingpoint.y = (int)(w_height * gazePoint.position.y / Dpi_Factor);
                }
            }

            public static void StreamSampleMain()
            {
                    // Create API context
                IntPtr apiContext;
                tobii_error_t result = Interop.tobii_api_create(out apiContext, null);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

                    // Enumerate devices to find connected eye trackers
                List<String> urls;
                result = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
                if (urls.Count == 0)
                {
                    Console.WriteLine("Error: No device found");
                    return;
                }

                    // Connect to the first tracker found
                IntPtr deviceContext;
                result = Interop.tobii_device_create(apiContext, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out deviceContext);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

                    // Subscribe to gaze data
                result = Interop.tobii_gaze_point_subscribe(deviceContext, OnGazePoint);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);

                    // This sample will collect 10000 gaze points
                for (int i = 0; i < 10000; i++)
                {
                        // Optionally block this thread until data is available. Especially useful if running in a separate thread.
                    Interop.tobii_wait_for_callbacks(new[] { deviceContext });
                    Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR || result == tobii_error_t.TOBII_ERROR_TIMED_OUT);

                    if (i % 10 == 0) //10回に1回だけ読み込む
                    {
                            // Process callbacks on this thread if data is available
                        Interop.tobii_device_process_callbacks(deviceContext);
                        Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
                    }
                }

                    // Cleanup
                result = Interop.tobii_gaze_point_unsubscribe(deviceContext);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
                result = Interop.tobii_device_destroy(deviceContext);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
                result = Interop.tobii_api_destroy(apiContext);
                Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
            }
        }
        
    }
}
