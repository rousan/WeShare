using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace WeShare
{
    public partial class HomeForm : Form
    {

        public static TcpClient tracker;
        public static String ERROR_CODE = Guid.NewGuid().ToString();
        public static TcpClient current_server_client = null;
        public static Thread current_server_client_thread = null;
        public static List<String> selected_files = new List<string>();
        public static String cache_folder_file_dialog = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static Dictionary<String, bool> all_threads_state = new Dictionary<string, bool>();

        public HomeForm()
        {
            InitializeComponent();
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            
        }

        private void HomeForm_Load(object sender, EventArgs e)
        {
            try
            {
                Utils.setupPath();
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                new Thread(new ThreadStart(HoldServer)).Start();
                this.FormClosed += HomeForm_Closed;
                textBox2.Text = Utils.readip();
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }

        private void HomeForm_Closed(object sender, EventArgs e)
        {
            try
            {
                Utils.saveip(textBox2.Text);
                Environment.Exit(0);
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }


        public void HoldServer()
        {
            try
            {
                TcpListener listener = new TcpListener(Utils.SERVER_PORT);
                listener.Start();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    tracker = client;
                    new Thread(new ThreadStart(HandleClientRequest)).Start();
                }
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }

        public static String writetext(String text, TcpClient target)
        {
            String outs = ERROR_CODE;
            try
            {
                NetworkStream stream = target.GetStream();
                byte[] data = Encoding.UTF8.GetBytes(text);
                stream.WriteByte((byte)Encoding.UTF8.GetBytes(data.Length + "").Length);
                stream.Write(Encoding.UTF8.GetBytes(data.Length + ""), 0, Encoding.UTF8.GetBytes(data.Length + "").Length);
                stream.Write(data, 0, data.Length);
                outs = "1";
            }
            catch (Exception exp)
            {
                outs = ERROR_CODE;
                Utils.printTestError(exp);
            }
            return outs;
        }

        public delegate void ProccessingCallback(double rate);

        public static bool writefile(String file_path, TcpClient client, ProccessingCallback callback)
        {
            bool outs = false;
            try
            {
                if(!File.Exists(file_path) || client == null) {
                    return false;
                }
                String r1 = writetext(Path.GetFileName(file_path), client);
                if(r1.Equals(ERROR_CODE)) {
                    return false;
                }
                String r2 = writetext(new FileInfo(file_path).Length + "", client);
                if (r2.Equals(ERROR_CODE))
                {
                    return false;
                }
                long file_size = new FileInfo(file_path).Length;
                FileStream fs = new FileStream(file_path, FileMode.Open, FileAccess.Read);
                NetworkStream stream = client.GetStream();
                byte[] buff = new byte[Utils.getOptimizedBuffSize(file_size)];
                int r = 0;
                long total_sent = 0;
                callback(0.0);
                while ((r = fs.Read(buff, 0, buff.Length)) != 0)
                {
                    stream.Write(buff, 0, r);
                    total_sent += r;
                    double percent = (((double)total_sent * (double)100)/ (double)file_size);
                    if (percent > 1)
                        percent = percent - 1;
                    callback(percent);
                }
                fs.Close();
                try
                {
                    int r3 = stream.ReadByte();
                }
                catch (Exception exp)
                {
                }
                callback(100.0);
                return true;
            }
            catch (Exception exp)
            {
                outs = false;
                Utils.printTestError(exp);
            }
            return outs;
        }

        public static bool readfile(String folder_holder, String use_file_name, TcpClient client, ProccessingCallback callback)
        {
            bool outs = false;
            try
            {
                if (!Directory.Exists(folder_holder))
                {
                    Directory.CreateDirectory(folder_holder);
                }
                if(client == null) {
                    return false;
                }

                String file_name = readText(client);
                if (file_name.Equals(ERROR_CODE))
                {
                    return false;
                }
                if(!String.IsNullOrEmpty(use_file_name)) {
                    file_name = Path.GetFileName(use_file_name);
                }
                String file_size = readText(client);
                if (file_size.Equals(ERROR_CODE))
                {
                    return false;
                }
                String output_file = Path.Combine(folder_holder, file_name);
                FileStream fs = new FileStream(output_file, FileMode.OpenOrCreate, FileAccess.Write);
                long size = Convert.ToInt64(file_size);
                NetworkStream stream = client.GetStream();
                byte[] buff = new byte[Utils.getOptimizedBuffSize(size)];
                long total_read = 0;
                callback(0.0);
                while (total_read < size)
                {
                    int read = stream.Read(buff, 0, buff.Length);
                    if (read <= 0)
                    {
                        return false;
                    }
                    total_read += read;
                    fs.Write(buff, 0, read);
                    double percent = (((double)total_read * (double)100) / (double)size);
                    if (percent > 1)
                    {
                        percent = percent - 1.0;
                    }
                    callback(percent);
                }
                try
                {
                    fs.Flush();
                    fs.Close();
                }
                catch (Exception)
                {
                }
                try
                {
                    stream.WriteByte(100);
                }
                catch (Exception)
                {
                }
                callback(100.0);
                return true;
            }
            catch (Exception exp)
            {
                outs = false;
                Utils.printTestError(exp);
            }
            return outs;
        }

        public static String readText(TcpClient target)
        {
            String outs = ERROR_CODE;
            try
            {
                NetworkStream stream = target.GetStream();
                int byt1 = stream.ReadByte();
                if (byt1 < 0)
                {
                    throw new Exception();
                }
                String size_str = "";
                for (int i = 0; i < byt1; i++)
                {
                    int bt = stream.ReadByte();
                    if(bt < 0) {
                        throw new Exception();
                    }
                    size_str += (char)bt;
                }
                int size = Convert.ToInt32(size_str);
                byte[] data = new byte[size];
                byte[] buff = new byte[size];
                int pos = 0;
                int total_read = 0;
                while (total_read < size)
                {
                    int read = stream.Read(buff, 0, buff.Length - total_read);
                    if (read < 0)
                    {
                        throw new Exception();
                    }
                    total_read += read;
                    for (int i = 0; i < read; i++)
                    {
                        data[pos] = buff[i];
                        pos++;
                    }
                }
                outs = Encoding.UTF8.GetString(data);
            }
            catch (Exception exp)
            {
                outs = ERROR_CODE;
                Utils.printTestError(exp);
            }
            return outs;
        }


        public void HandleClientRequest()
        {
            TcpClient client = tracker;
            Label status_lbl = null;
            try
            {
                Utils.setupPath();

                String device_name = readText(client);
                if(device_name.Equals(ERROR_CODE)) {
                    throw new Exception();
                }

                Label device_name_lbl = new Label();
                device_name_lbl.Location = new System.Drawing.Point(3, 0);
                device_name_lbl.AutoSize = true;
                device_name_lbl.TabIndex = 0;
                device_name_lbl.Text = device_name;

                status_lbl = new Label();
                status_lbl.AutoSize = true;
                status_lbl.TabIndex = 0;
                status_lbl.Location = new System.Drawing.Point(188, 0);
                status_lbl.Text = "Connected";

                Label ip_lbl = new Label();
                ip_lbl.AutoSize = true;
                ip_lbl.TabIndex = 0;
                ip_lbl.Location = new System.Drawing.Point(366, 0);
                ip_lbl.Text = client.Client.RemoteEndPoint.ToString().Split(':')[0];

                TableLayoutPanel tlp = new TableLayoutPanel();
                tlp.ColumnCount = 3;
                tlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.96419F));
                tlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 49.03581F));
                tlp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 137F));
                tlp.Controls.Add(device_name_lbl, 0, 0);
                tlp.Controls.Add(status_lbl, 1, 0);
                tlp.Controls.Add(ip_lbl, 2, 0);
                tlp.Location = new System.Drawing.Point(3, 3);
                tlp.RowCount = 1;
                tlp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
                tlp.Size = new System.Drawing.Size(490, 24);
                tlp.TabIndex = 0;

                BeginInvoke(new MethodInvoker(() =>
                {
                    flowLayoutPanel1.Controls.Add(tlp);
                    flowLayoutPanel1.ScrollControlIntoView(tlp);
                }));

                BeginInvoke(new MethodInvoker(() =>
                {
                    status_lbl.Text = "Processing...";
                }));

                String root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WeShare", "WeShare-1.0.1");
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }
                String received_folder = Path.Combine(root, "Received");
                String sent_folder = Path.Combine(root, "Sent");
                if (!Directory.Exists(received_folder))
                {
                    Directory.CreateDirectory(received_folder);
                }
                if (!Directory.Exists(sent_folder))
                {
                    Directory.CreateDirectory(sent_folder);
                }
                DateTime dt = DateTime.Now;
                String date_folder_name = dt.Day + "-" + dt.Month + "-" + dt.Year + " " + dt.Hour + "." + dt.Minute + "." + dt.Second + "." + dt.Millisecond;
                String target_folder = Path.Combine(received_folder, device_name, date_folder_name);
                if (!Directory.Exists(target_folder))
                {
                    Directory.CreateDirectory(target_folder);
                }

                String file_count = readText(client);
                if (file_count.Equals(ERROR_CODE))
                {
                    throw new Exception();
                }
                int file_count_num = Convert.ToInt32(file_count);
                int total_file_received = 0;
                BeginInvoke(new MethodInvoker(() =>
                {
                    status_lbl.Text = "Received " + total_file_received + "/" + file_count_num + "(0.0%)";
                }));
                for (int i = 0; i < file_count_num; i++ )
                {
                    bool received = readfile(target_folder, null, client, (percent) =>
                    {
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            status_lbl.Text = "Received " + total_file_received + "/" + file_count_num + "(" + percent.ToString("0.0") + "%)";
                        }));
                    });
                    if(!received) {
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            status_lbl.Text = "Total received " + total_file_received + " out of " + file_count_num;
                        }));
                        try
                        {
                            client.Close();
                        }
                        catch (Exception)
                        {
                        }
                        return;
                    }
                    total_file_received++;
                    BeginInvoke(new MethodInvoker(() =>
                    {
                        status_lbl.Text = "Received " + total_file_received + "/" + file_count_num + "(100.0%)";
                    }));
                }

                try
                {
                    client.GetStream().WriteByte(100);
                    client.GetStream().Close();
                }
                catch (Exception)
                {
                }
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                }

                BeginInvoke(new MethodInvoker(() =>
                {
                    status_lbl.Text = "All file(s) received : " + file_count_num;
                }));
            }
            catch (Exception exp)
            {
                try
                {
                    client.Close();
                    
                }
                catch (Exception)
                {
                }
                if (status_lbl != null)
                {
                    BeginInvoke(new MethodInvoker(() =>
                    {
                        status_lbl.Text = "Disconnected";
                    }));
                }
                Utils.printTestError(exp);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }


        public void showNotif(String txt)
        {
            try
            {
                Utils.printLine(txt);
            }
            catch (Exception)
            {
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
            try
            {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
                button1.Enabled = false;
                button4.Enabled = true;
                button3.Enabled = false;
                button3.Text = "Proccessing...";

                String ip = textBox2.Text;
                if(String.IsNullOrEmpty(ip)) {
                    resetSending();
                    showNotif("Please enter device ip");
                    return;
                }
                List<String> all_valid_files = new List<String>();
                foreach(String file_path in selected_files) {
                    if(!File.Exists(file_path)) {
                        continue;
                    }
                    all_valid_files.Add(file_path);
                }
                if(all_valid_files.Count == 0) {
                    selected_files.Clear();
                    textBox1.Text = "";
                    resetSending();
                    showNotif("Please choose file(s) to send");
                    return;
                }

                Thread th = null;
                int total_file_sent = 0;
                th = new Thread(new ThreadStart(() => {

                    TcpClient cleint = null;
                    try
                    {
                        cleint = new TcpClient();
                        current_server_client = cleint;
                        current_server_client_thread = Thread.CurrentThread;
                        try
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                button3.Text = "Connecting...";
                            }));
                            cleint.Connect(ip, Utils.SERVER_PORT);
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                button3.Text = "Connected";
                            }));
                        }
                        catch (Exception exp)
                        {
                            try
                            {
                                cleint.Close();
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                
                                if (all_threads_state[Thread.CurrentThread.Name])
                                {
                                    showNotif("Could not connect with the specified device");
                                }
                            }
                            catch (Exception)
                            {
                            }
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                resetSending();
                            }));
                            return;
                        }
                        
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            button3.Text = "Processing...";
                        }));
                        String result1 = writetext(Environment.MachineName, cleint);
                        if(result1.Equals(ERROR_CODE)) {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                resetSending();
                            }));
                            try
                            {
                                cleint.Close();
                            }
                            catch (Exception exp)
                            {
                            }
                            showNotif("Connection is closed, please try again");
                            return;
                        }
                        String result2 = writetext(all_valid_files.Count + "", cleint);
                        if (result2.Equals(ERROR_CODE))
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                resetSending();
                            }));
                            try
                            {
                                cleint.Close();
                            }
                            catch (Exception exp)
                            {

                            }
                            showNotif("Connection is closed, please try again");
                            return;
                        }
                        total_file_sent = 0;
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            button3.Text = "Sending..." + total_file_sent + "/" + all_valid_files.Count;
                            progressBar1.Visible = true;
                            progressBar1.Value = 0;
                        }));
                        foreach(String file_path in all_valid_files) {
                            try
                            {
                                bool sent = writefile(file_path, cleint, (percent) => {
                                    BeginInvoke(new MethodInvoker(() =>
                                    {
                                        progressBar1.Value = (int)percent;
                                    }));
                                });
                                if(!sent) {
                                    BeginInvoke(new MethodInvoker(() =>
                                    {
                                        resetSending();
                                    }));
                                    try
                                    {
                                        cleint.Close();
                                    }
                                    catch (Exception exp)
                                    {
                                    }
                                    showNotif(String.Format("Total {0} file(s) can not be sent due to connection closing, please try again", all_valid_files.Count - total_file_sent));
                                    return;
                                }
                                total_file_sent++;
                                BeginInvoke(new MethodInvoker(() =>
                                {
                                    button3.Text = "Sending..." + total_file_sent + "/" + all_valid_files.Count;
                                }));
                            }
                            catch (Exception exp)
                            {
                                Utils.printTestError(exp);
                            }
                        }

                        try
                        { 
                            int res = cleint.GetStream().ReadByte();
                        }
                        catch (Exception)
                        {
                        }
                        
                        try
                        {
                            cleint.GetStream().Close();
                            cleint.Close();
                        }
                        catch (Exception)
                        {
                        }
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            progressBar1.Value = 100;
                        }));
                        showNotif("All file(s) sent successfully");
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            resetSending();
                        }));
                    }
                    catch (Exception exp)
                    {
                        Utils.printTestError("abort");
                        BeginInvoke(new MethodInvoker(() =>
                        {
                            resetSending();
                        }));
                        try
                        {
                            if(cleint != null) {
                                cleint.Close();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        Utils.printTestError(exp);
                        if (all_valid_files.Count - total_file_sent == 0)
                        {
                            showNotif(String.Format("All file(s) sent successfully", all_valid_files.Count - total_file_sent));
                        }
                        else
                        {
                            showNotif(String.Format("Total {0} file(s) can not be sent, please try again", all_valid_files.Count - total_file_sent));
                        }
                    }
                }));
                th.Name = Guid.NewGuid().ToString();
                all_threads_state.Add(th.Name, true);
                th.Start();

            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }

        public void resetSending()
        {
            try
            {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
                button1.Enabled = true;
                button4.Enabled = false;
                button3.Enabled = true;
                button3.Text = "Send";
            }
            catch (Exception)
            {
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.InitialDirectory = cache_folder_file_dialog;
                ofd.Filter = "All Files (*.*)|*.*";
                ofd.FilterIndex = 0;
                ofd.SupportMultiDottedExtensions = true;
                ofd.Multiselect = true;
                DialogResult dr = ofd.ShowDialog();
                if(dr.Equals(DialogResult.OK)) {
                    selected_files.Clear();
                    String txt = "";
                    foreach (String file_path in ofd.FileNames)
                    {
                        if (!File.Exists(file_path))
                        {
                            continue;
                        }
                        txt += "\"" + Path.GetFileName(file_path) + "\", ";
                        selected_files.Add(file_path);
                    }
                    txt = txt.TrimEnd(new char[] { ' ', ',' });
                    if (String.IsNullOrEmpty(txt))
                    {
                        selected_files.Clear();
                    }
                    if(selected_files.Count > 0) {
                        cache_folder_file_dialog = Path.GetDirectoryName(selected_files[0]);
                    }
                    textBox1.Text = txt;
                }
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                
                if(current_server_client_thread != null) {

                    try
                    {
                        if(current_server_client_thread.Name != null) {
                            all_threads_state[current_server_client_thread.Name] = false;
                         }
                    }
                    catch (Exception)
                    {
                    }
 
                    try
                    {
                        current_server_client_thread.Interrupt();
                        current_server_client_thread.Abort();
                    }
                    catch (Exception)
                    {
                      
                    }
                }

                if(current_server_client != null) {
                    try
                    {
                        current_server_client.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
                resetSending();
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void progressBar1_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if(progressBar1.Visible) {
                    groupBox2.Height -= progressBar1.Height;
                    flowLayoutPanel1.Height -= progressBar1.Height;
                    groupBox2.Location = new Point(groupBox2.Location.X, groupBox2.Location.Y + progressBar1.Height);
                }
                else
                {
                    groupBox2.Height += progressBar1.Height;
                    flowLayoutPanel1.Height += progressBar1.Height;
                    groupBox2.Location = new Point(groupBox2.Location.X, groupBox2.Location.Y - progressBar1.Height);
                }
            }
            catch (Exception exp)
            {
            }
        }

        

    }
}
