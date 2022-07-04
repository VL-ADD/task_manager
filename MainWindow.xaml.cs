using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Collections.ObjectModel;
using System.ComponentModel;




namespace TestTask
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public class ListParamForListView
    {
        public bool Del { get; set; }
        public ImageSource ProcessIconFile { get; set; }
        public string ProcessShortName { get; set; }
        public string ProcessId { get; set; }
        public string ProcessFullName { get; set; }
        public string ProcessBits { get; set; }
        public string ProcessUserName { get; set; }
        public string ProcessComandLine { get; set; }
        public string ProcessIsSign { get; set; }
        public string ProcessIsElevated { get; set; }
    }

    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool lpSystemInfo);

        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        static ManagementObjectCollection processList; //// список процессов 
        
        static bool IsArdateFlag = false; /// происходит ли в данный момент обновление списка
        static double ProgressPersent = 0;/// переменная для хранения прогресса в процентах
        static List<ListParamForListView> buff = new List<ListParamForListView>();

        public static ObservableCollection<ListParamForListView> Processes = new ObservableCollection<ListParamForListView>();

        //Thread myThread = new Thread(new ThreadStart(Writecollection)); /// поток
        
        BackgroundWorker BW = new BackgroundWorker() { WorkerSupportsCancellation = true };
        //public static List<ListParamForListView> Processes = new List<ListParamForListView>();
        

        public MainWindow()
        {
            InitializeComponent();
            foreach (ListParamForListView x in CreateProcessList())
            {
                Processes.Add(x);
            }
            ProcList.ItemsSource = Processes;
        }


        private void Writecollection(object sender, DoWorkEventArgs e) ///////// заполняем начальную коллекцию
        {
            
            string[] argList = { string.Empty, string.Empty };

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process");
            processList = searcher.Get();
            int eeee = processList.Count;
            double one_persent = 100 / (double)eeee;
            double i = 0;
            
            foreach (ManagementObject obj in processList)
            {

                ListParamForListView ListItem = new ListParamForListView();
                ListItem.ProcessId = obj["ProcessId"].ToString();
                ListItem.ProcessShortName = obj["Name"].ToString();
                ListItem.ProcessFullName = Convert.ToString(obj["ExecutablePath"]);
                ListItem.ProcessComandLine = Convert.ToString(obj["CommandLine"]);

                try
                {
                    int ee = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                }
                catch (Exception)
                {
                    argList[0] = "";
                    argList[1] = "";
                }
                ListItem.ProcessUserName = argList[0];

                IntPtr hProcess = OpenProcess(1040, false, Convert.ToUInt32(obj["ProcessId"])); /// HANDLE PROCESS


                ////////////////////////////////////////////////// проверка битности процесса
                bool flag64;
                string type_proces = "";
                IsWow64Process(hProcess, out flag64);
                CloseHandle(hProcess);
                if (flag64)
                {
                    type_proces = "86x";
                }
                else
                {
                    type_proces = "64x";
                }
                ListItem.ProcessBits = type_proces;

                ////////////////////////////////////////////////// проверка наличия цифровой подписи файла

                string filePath = "";
                filePath = Convert.ToString(obj["ExecutablePath"]);

                if (!File.Exists(filePath))
                {
                    ListItem.ProcessIsSign = "File not found";
                }
                X509Certificate2 theCertificate;
                try
                {
                    X509Certificate theSigner = X509Certificate.CreateFromSignedFile(filePath);
                    theCertificate = new X509Certificate2(theSigner);
                    ListItem.ProcessIsSign = "File signed";
                }
                catch (Exception)
                {
                    ListItem.ProcessIsSign = "No signed";
                }
                //////////////////////////////////////////////////  получаем путь к иконке файла
                ///

                if (Convert.ToString(obj["ExecutablePath"]) != "")
                {
                    ImageSource ico_so;
                    if (true)
                    {
                        try
                        {
                            var icon = System.Drawing.Icon.ExtractAssociatedIcon(Convert.ToString(obj["ExecutablePath"])   /*dlg.FileName*/);
                            var bmp = icon.ToBitmap();
                            ico_so = loadBitmap(bmp);
                            ListItem.ProcessIconFile = ico_so;
                        }
                        catch
                        {

                        }
                    }
                }
                
                ListItem.ProcessIsElevated = " **** ";
                i++;
                buff.Add(ListItem);
                ProgressPersent = (one_persent * i);
                if (IsArdateFlag == true)
                    BW.ReportProgress((int)ProgressPersent);
                if (BW.CancellationPending == true)
                {
                    break;
                }
                    
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ReWriteList(buff);
            IsArdateFlag = false;
            OnlyButton.Content = "Обновить";
           
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            persent.Text = "    " + e.ProgressPercentage.ToString() + "%";
        }


        public void ReWriteList(List<ListParamForListView> Xnew)
        {
            List<ListParamForListView> NewX = Xnew;

            foreach (ListParamForListView Xold in Processes)
                foreach(ListParamForListView Xn in NewX)
                {
                    if(Xold.ProcessId == Xn.ProcessId)
                    {
                        Xold.Del = false;
                        Xn.Del = true;
                    }
                    else
                    {
                        Xold.Del = true;
                        Xn.Del = false;
                    }
                }

            for(int i = 0; i < Processes.Count; i ++)
            {
                if (Processes[i].Del == true)
                {
                    Processes.RemoveAt(i);
                }
            }
            foreach (ListParamForListView Xn in NewX)
            {
                if (Xn.Del == false)
                {
                    Processes.Add(Xn);
                }
            }
        }

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                IntPtr.Zero, Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }


         public List<ListParamForListView> CreateProcessList()
        {
            List<ListParamForListView> ListP = new List<ListParamForListView>();
            string[] argList = { string.Empty, string.Empty };

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process");
            processList = searcher.Get();
            int eeee = processList.Count;
            double one_persent =  100 / (double)eeee;
            double i = 0;
            


            foreach (ManagementObject obj in processList)
            {
                
                ListParamForListView ListItem = new ListParamForListView();
                ListItem.ProcessId = obj["ProcessId"].ToString();
                ListItem.ProcessShortName = obj["Name"].ToString();
                ListItem.ProcessFullName = Convert.ToString( obj["ExecutablePath"]);
                ListItem.ProcessComandLine = Convert.ToString(obj["CommandLine"]);
                try
                {
                    int ee = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                }
                catch(Exception)
                {
                    argList[0] = "";
                    argList[1] = "";
                }

                ListItem.ProcessUserName = argList[0];

                IntPtr hProcess = OpenProcess(1040, false, Convert.ToUInt32(obj["ProcessId"])); /// HANDLE PROCESS


                ////////////////////////////////////////////////// проверка битности процесса
                bool flag64;
                string type_proces = "";
                IsWow64Process(hProcess, out flag64);
                CloseHandle(hProcess);
                if (flag64)
                {
                    type_proces = "86x";
                }
                else
                {
                    type_proces = "64x";
                }
                ListItem.ProcessBits = type_proces;

                ////////////////////////////////////////////////// проверка наличия цифровой подписи файла

                string filePath = "";
                filePath = Convert.ToString(obj["ExecutablePath"]);

                if (!File.Exists(filePath))
                {
                    ListItem.ProcessIsSign = "File not found";
                }
                X509Certificate2 theCertificate;
                try
                {
                    X509Certificate theSigner = X509Certificate.CreateFromSignedFile(filePath);
                    theCertificate = new X509Certificate2(theSigner);
                    ListItem.ProcessIsSign = "File signed";
                }
                catch (Exception ex)
                {
                    ListItem.ProcessIsSign = "No signed";
                }
                //////////////////////////////////////////////////  получаем путь к иконке файла
                ///

                if (Convert.ToString(obj["ExecutablePath"]) != "")
                {
                    ImageSource ico_so;
                    if ( true)
                    {
                        try
                        {
                            var icon = System.Drawing.Icon.ExtractAssociatedIcon(Convert.ToString(obj["ExecutablePath"])   /*dlg.FileName*/);
                            var bmp = icon.ToBitmap();
                            ico_so = loadBitmap(bmp);
                            ListItem.ProcessIconFile = ico_so;
                        }
                        catch
                        {

                        }
                    }
                }
                
                ////////////////////////////////////////////////////// 
                //string OwnerSID = String.Empty;
                //string[] sid = new String[1];
                //obj.InvokeMethod("GetOwnerSid", (object[])sid);
                //OwnerSID = sid[0];
                //Console.WriteLine("uname = {0}", OwnerSID);
                ///////////////////////////////////////////////////////////добавляем элемент в список
                
                ListItem.ProcessIsElevated = " **** ";
                i++;
                ListP.Add(ListItem);
                ProgressPersent =( one_persent * i);
                //if (IsArdateFlag == true)
                //    BW.ReportProgress((int)ProgressPersent);
               
            }
            return ListP;
        }

       

        private void Only_button_click(object sender, EventArgs e)
        {
            if (IsArdateFlag == true)
            {
                OnlyButton.IsEnabled = false;
                OnlyButton.Content = "Прерывание";
                BW.CancelAsync();
                OnlyButton.IsEnabled = true; 
            }


            if (IsArdateFlag == false)
            {
                OnlyButton.Content = "Прервать";
                IsArdateFlag = true;
                BW.WorkerReportsProgress = true;
                BW.DoWork += Writecollection;
                BW.ProgressChanged += worker_ProgressChanged;
                BW.RunWorkerCompleted += worker_RunWorkerCompleted;
                BW.RunWorkerAsync();
            }
            
        }
    }
}
