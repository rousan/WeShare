using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;

namespace WeShare
{
    class Utils
    {

        public static int SERVER_PORT = 8999;

        public static void setupPath()
        {
            try
            {
                String root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WeShare", "WeShare-1.0.1");
                if(!Directory.Exists(root)) {
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
            }
            catch (Exception exp)
            {
                Utils.printTestError(exp);
            }
        }


        public static void saveip(String ip)
        {
            try
            {
                String rootlocalappdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeShare");
                if(!Directory.Exists(rootlocalappdata)) {
                    Directory.CreateDirectory(rootlocalappdata);
                }
                String ip_file = Path.Combine(rootlocalappdata, "ip.txt");
                WriteToFile(ip_file, ip, false);
            }
            catch (Exception)
            {
            }
        }

        public static int getOptimizedBuffSize(long max)
        {
            long max__ = 1024*1024*5;
            if(max < max__) {
                return (int)(max + 1);
            }
            else
            {
                return (int)max__;
            }
        }

        public static String readip()
        {
            String outs = "";
            try
            {
                String rootlocalappdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeShare");
                if (!Directory.Exists(rootlocalappdata))
                {
                    Directory.CreateDirectory(rootlocalappdata);
                }
                String ip_file = Path.Combine(rootlocalappdata, "ip.txt");
                if(!File.Exists(ip_file)) {
                    WriteToFile(ip_file, "", false);
                }
                outs = ReadFile(ip_file);
            }
            catch (Exception)
            {
            }
            return outs;
        }

        public static void savefilepath(String path)
        {
            try
            {
                String rootlocalappdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeShare");
                if (!Directory.Exists(rootlocalappdata))
                {
                    Directory.CreateDirectory(rootlocalappdata);
                }
                String path_file = Path.Combine(rootlocalappdata, "path.txt");
                WriteToFile(path_file, path, false);
            }
            catch (Exception)
            {
            }
        }

        public static void CompressFileFolders(ArrayList fileFolders, String output_zip_file)
        {
            try
            {
                if (File.Exists(output_zip_file))
                {
                    try
                    {
                        File.Delete(output_zip_file);
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }

                ZipArchive za = ZipFile.Open(output_zip_file, ZipArchiveMode.Create);
                foreach (Object file in fileFolders)
                {
                    if (file == null)
                        continue;
                    String file_current = file.ToString().Trim();
                    if (File.Exists(file_current))
                    {
                        za.CreateEntryFromFile(file_current, Path.GetFileName(file_current));
                    }
                    else if (Directory.Exists(file_current))
                    {
                        PushToZipArchiveIterative(file_current, za, "");
                    }
                }
                za.Dispose();
            }
            catch (Exception e)
            {
                printTestError(e);
            }
        }

        public static void WriteToFile(String file, String text, Boolean append)
        {
            try
            {
                StreamWriter sw = new StreamWriter(file, append);
                sw.Write(text);
                sw.Flush();
                sw.Close();
            }
            catch (Exception e)
            {
                printTestError(e);
            }
        }

        public static void WriteFile(String file, String text, Boolean append)
        {
            try
            {
                StreamWriter sw = new StreamWriter(file, append);
                sw.Write(text);
                sw.Flush();
                sw.Close();
            }
            catch (Exception e)
            {
                printTestError(e);
            }
        }

        public static String ReadFile(String file)
        {
            String outs = "";

            try
            {
                StreamReader sr = new StreamReader(file);
                String line;
                while( (line=sr.ReadLine()) != null ) {
                    outs += line + "\n";
                }
                outs = outs.Substring(0, outs.Length - 1);
                sr.Close();
            }
            catch (Exception e)
            {
                printTestError(e);
            }
            return outs;
        }


        private static void PushToZipArchiveIterative(String folder_path, ZipArchive za, String base_entry)
        {
            try
            {
                if (!Directory.Exists(folder_path) || za == null || base_entry == null)
                {
                    return;
                }
                bool empty1 = true;
                bool empty2 = true;
                foreach (String file in Directory.GetFiles(folder_path))
                {
                    empty1 = false;
                    za.CreateEntryFromFile(file, (String.IsNullOrEmpty(base_entry)) ? (Path.GetFileName(folder_path) + "/" + Path.GetFileName(file)) : (base_entry + "/" + Path.GetFileName(folder_path) + "/" + Path.GetFileName(file)));
                }
                foreach (String file in Directory.GetDirectories(folder_path))
                {
                    empty2 = false;
                    PushToZipArchiveIterative(file, za, (String.IsNullOrEmpty(base_entry)) ? (Path.GetFileName(folder_path)) : (base_entry + "/" + Path.GetFileName(folder_path)));
                }
                if (empty1 & empty2)
                {
                    za.CreateEntry((String.IsNullOrEmpty(base_entry)) ? (Path.GetFileName(folder_path)) : (base_entry + "/" + Path.GetFileName(folder_path)) + "/");
                }
            }
            catch (Exception e)
            {
                //nothing
            }
        }

        public static bool DecompressZip(String zip_file_path, String output_folder)
        {
            bool outs = false;
            try
            {
                if (!File.Exists(zip_file_path))
                {
                    return false;
                }
                if (!Directory.Exists(output_folder))
                {
                    Directory.CreateDirectory(output_folder);
                }
                ZipFile.ExtractToDirectory(zip_file_path, output_folder);
                outs = true;
                return outs;
            }
            catch (Exception e)
            {
                outs = false;
                printTestError(e);
            }
            return outs;
        }

        


        public static void printTestError(Object obj)
        {
            obj = (obj == null) ? ("null") : (obj);
            //MessageBox.Show(obj.ToString());
        }

        public static void printLine(Object obj)
        {
            obj = (obj == null) ? ("null") : (obj);
            MessageBox.Show(obj.ToString());
        }

        public static String getUniqueTempFile(String extention)
        {
            if (extention == null || String.IsNullOrEmpty(extention.Trim()))
            {
                extention = "tmp";
            }
            String tmp = Path.GetTempFileName();
            String file = Path.Combine(Path.GetDirectoryName(tmp), Path.GetFileNameWithoutExtension(tmp) + "." + extention);
            try
            {
                file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "." + extention);
            }
            catch (Exception e)
            {
                file = Path.Combine(Path.GetDirectoryName(tmp), Path.GetFileNameWithoutExtension(tmp) + "." + extention);
            }
            return file;
        }

    }
}
