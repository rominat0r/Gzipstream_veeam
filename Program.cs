using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace microsoft.botsay
{
    class Program
    {

        static string mergeFolder;
        static List<string> Packets = new List<string>();
        public static string directoryPath = @".";
        public static int Main(string[] argv)
        {
            if (argv.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()
                                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                        .InformationalVersion
                                        .ToString();

                Console.WriteLine($"GZipTest.exe v{versionString}");
                Console.WriteLine("-------------");
                Console.WriteLine("Usage: GZipTest.exe compress <file_to_compress> <directory_to_save> \n       GZipTest.exe decompress <directory_to_decompress> <directory_to_save> ");

                return 1;
            }

            if (argv.Length != 3)
            {
                Console.WriteLine("Usage: GZipTest.exe <compress/decompress> <in_dir compressed_file> | <compressed_file out_dir>");
                return 1;
            }

            string sDir;
            string sCompressedFile;
            string sCommand = argv[0];

            try
            {
                if (sCommand == "compress")
                {
                    sDir = argv[1];
                    sCompressedFile = argv[2];
                    SplitFile(directoryPath + @"\" + sDir, Convert.ToInt32(5));
                    DirectoryInfo di = Directory.CreateDirectory(directoryPath + @"\" + sCompressedFile);
                    foreach (var packet in Packets)
                    {
                        FileInfo directorySelected = new FileInfo(directoryPath + @"\" + packet.ToString());
                        Compress(directorySelected, sCompressedFile);
                        File.Move(directoryPath + @"\" + packet.ToString() + ".gz", directoryPath + @"\" + sCompressedFile + @"\" + packet.ToString() + ".gz");
                        File.Delete(directoryPath + @"\" + packet.ToString());
                    }
                }
                else if (sCommand == "decompress")
                {
                    sCompressedFile = argv[1];
                    sDir = argv[2];
                    DirectoryInfo di = Directory.CreateDirectory(directoryPath + @"\" + sDir);
                    DirectoryInfo directorySelected = new DirectoryInfo(directoryPath + @"\" + sCompressedFile);

                    foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
                    {

                        Decompress(fileToDecompress);
                        string currentFileName = fileToDecompress.Name;
                        string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                        File.Move(directoryPath + @"\" + sCompressedFile + @"\" + newFileName, directoryPath + @"\" + sDir + @"\" + newFileName);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Wrong arguments");
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
        delegate void ProgressDelegate(string sMessage);
        public static void Compress(FileInfo directorySelected, string filename)
        {
            using (FileStream originalFileStream = directorySelected.OpenRead())
            {
                if ((File.GetAttributes(directorySelected.FullName) &
                   FileAttributes.Hidden) != FileAttributes.Hidden & directorySelected.Extension != ".gz")
                {

                    using (FileStream compressedFileStream = File.Create(directorySelected + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                           CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);

                        }
                    }
                    FileInfo info = new FileInfo(directoryPath + Path.DirectorySeparatorChar + directorySelected + ".gz");
                    Console.WriteLine($"Compressed {directorySelected.Name} from {directorySelected.Length.ToString()} to {info.Length.ToString()} bytes.");
                }
            }
        }

        public static void Decompress(FileInfo fileToDecompress)
        {

            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);

                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
        }
        public static bool SplitFile(string SourceFile, int nNoofFiles)
        {
            bool Split = false;
            try
            {
                FileStream fs = new FileStream(SourceFile, FileMode.Open, FileAccess.Read);
                int SizeofEachFile = (int)Math.Ceiling((double)fs.Length / nNoofFiles);

                for (int i = 0; i < nNoofFiles; i++)
                {
                    string baseFileName = Path.GetFileNameWithoutExtension(SourceFile);
                    string Extension = Path.GetExtension(SourceFile);

                    FileStream outputFile = new FileStream(Path.GetDirectoryName(SourceFile) + "\\" + baseFileName + "." +
                        i.ToString().PadLeft(5, Convert.ToChar("0")) + Extension + ".tmp", FileMode.Create, FileAccess.Write);

                    mergeFolder = Path.GetDirectoryName(SourceFile);

                    int bytesRead = 0;
                    byte[] buffer = new byte[SizeofEachFile];

                    if ((bytesRead = fs.Read(buffer, 0, SizeofEachFile)) > 0)
                    {
                        outputFile.Write(buffer, 0, bytesRead);


                        string packet = baseFileName + "." + i.ToString().PadLeft(5, Convert.ToChar("0")) + Extension.ToString() + ".tmp";
                        Packets.Add(packet);
                    }

                    outputFile.Close();

                }
                fs.Close();
            }
            catch (Exception Ex)
            {
                throw new ArgumentException(Ex.Message);
            }

            return Split;
        }
    }
}
