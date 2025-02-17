﻿using HashLib.Checksum;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PKGTool
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("PKG Tool");
            Console.WriteLine("-----------------------");
            Console.WriteLine();
            Console.WriteLine("Extract : PKGTool -x <input path> [-o <output path>");
            Console.WriteLine("Create : PKGTool -c <input path> [-o <output path>");
        }

        static String GenerateFileName(UInt64 ID, MemoryStream File)
        {
            String fn = String.Empty;
            BinaryReader reader = new BinaryReader(File);
            String magic = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            switch(magic)
            {
                case "\x1BLua":
                    fn = $"{ID:X16}.lc";
                    break;
                case "\x6F\x7F\xF3\x73":
                    fn = $"{ID:X16}.bapd";
                    break;
                case "\xFB\x42\x9B\x06":
                    fn = $"{ID:X16}.bmscu";
                    break;
                case "BTXT":
                    fn = $"{ID:X16}.txt";
                    break;
                case "CWAV":
                    fn = $"{ID:X16}.bcwav";
                    break;
                case "FGRP":
                    fn = $"{ID:X16}.bfgrp";
                    break;
                case "FSAR":
                    fn = $"{ID:X16}.bfsar";
                    break;
                case "LSND":
                    fn = $"{ID:X16}.blsnd";
                    break;
                case "LUT\x01":
                    fn = $"{ID:X16}.blut";
                    break;
                case "MANM":
                    fn = $"{ID:X16}.bcskla";
                    break;
                case "MFNT":
                    fn = $"{ID:X16}.bfont";
                    break;
                case "MMDL":
                    fn = $"{ID:X16}.bcmdl";
                    break;
                case "MNAV":
                    fn = $"{ID:X16}.bmnav";
                    break;
                case "MPSI":
                    fn = $"{ID:X16}.bpsi";
                    break;
                case "MPSY":
                    fn = $"{ID:X16}.bcptl";
                    break;
                case "MSAD":
                    fn = $"{ID:X16}.bmsad";
                    break;
                case "MSAS":
                    fn = $"{ID:X16}.bmsas";
                    break;
                case "MSCD":
                    fn = $"{ID:X16}.bmscd";
                    break;
                case "MSHD":
                    fn = $"{ID:X16}.bshdat";
                    break;
                case "MSUR":
                    fn = $"{ID:X16}.bsmat";
                    break;
                case "MTXT":
                    fn = $"{ID:X16}.bctex";
                    break;
                case "MUCT":
                    fn = $"{ID:X16}.buct";
                    break;
                default:
                    fn = $"{ID:X16}.bin";
                    break;
            }
            File.Position = 0;
            return fn;
        }

        static void Main(string[] args)
        {
            CRC64 crc = new CRC64();
            FileStream tmp = null;
            String fn = String.Empty;
            String filePath = String.Empty;
            String outPath = String.Empty;
            Dread.FileFormats.PKG pkg = new Dread.FileFormats.PKG();
            Dictionary<String, UInt64> AssetIDByFilePath = JObject.Parse(Encoding.UTF8.GetString(Properties.Resources.resource_infos)).ToObject<Dictionary<String, UInt64>>();
            Dictionary<UInt64, String> AssetFilePathByID = AssetIDByFilePath.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            try
            {
                if (args.Length >= 2)
                {
                    if (args[0] == "-x")
                    {
                        if (!File.Exists(args[1]))
                            throw new FileNotFoundException($"Couldn't find the file {args[1]}");

                        pkg.import(File.OpenRead(args[1]));

                        if (args.Length == 4)
                        {
                            if (args[2] == "-o")
                                outPath = args[3];
                            else
                            {
                                Usage();
                                return;
                            }
                        }
                        else if (args.Length == 2)
                            outPath = String.Join(Path.DirectorySeparatorChar, Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(args[1]));
                        else
                        {
                            Usage();
                            return;
                        }

                        if (!Directory.Exists(outPath))
                            Directory.CreateDirectory(outPath);

                        using(var list = new StreamWriter(String.Join(Path.DirectorySeparatorChar, outPath, "files.list")))
                        {
                            foreach (var file in pkg.Files)
                            {
                                if (AssetFilePathByID.ContainsKey(file.Key))
                                {
                                    fn = AssetFilePathByID[file.Key];
                                    filePath = String.Join(Path.DirectorySeparatorChar, outPath, fn.Replace('/', Path.DirectorySeparatorChar));
                                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                                }
                                else
                                    fn = GenerateFileName(file.Key, file.Value);
                                Console.WriteLine($"Extracting {fn}...");
                                tmp = File.Open(String.Join(Path.DirectorySeparatorChar, outPath, fn), FileMode.Create, FileAccess.Write);
                                file.Value.CopyTo(tmp);
                                file.Value.Position = 0L;
                                tmp.Close();
                                list.WriteLine(fn);
                            }
                        }

                        pkg.Close();
                    }
                    else if (args[0] == "-c")
                    {
                        if (!Directory.Exists(args[1]))
                            throw new FileNotFoundException($"Couldn't find the folder {args[1]}");

                        if (!File.Exists(String.Join(Path.DirectorySeparatorChar, args[1], "files.list")))
                            throw new FileNotFoundException($"Couldn't find the file files.list");

                        if (args.Length == 4)
                        {
                            if (args[2] == "-o")
                                outPath = args[3];
                            else
                            {
                                Usage();
                                return;
                            }
                        }
                        else if (args.Length == 2)
                            outPath = String.Join(Path.DirectorySeparatorChar, Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(args[1]));
                        else
                        {
                            Usage();
                            return;
                        }

                        if (Path.GetExtension(outPath) == String.Empty)
                            outPath += ".pkg";
                        else if (Path.GetExtension(outPath).ToLower() != ".pkg")
                            outPath = Path.ChangeExtension(outPath, ".pkg");

                        using (var list = new StreamReader(String.Join(Path.DirectorySeparatorChar, args[1], "files.list")))
                        {
                            while (!list.EndOfStream)
                            {
                                fn = list.ReadLine().TrimEnd('\r', '\n');
                                try {
                                    if (Path.GetFileNameWithoutExtension(fn).Length == 16)
                                    {
                                        pkg.Files.Add(new KeyValuePair<UInt64, MemoryStream>(Convert.ToUInt64(Path.GetFileNameWithoutExtension(fn), 16), new MemoryStream(File.ReadAllBytes(String.Join(Path.DirectorySeparatorChar, args[1], fn)))));
                                    }
                                    else
                                    {
                                        pkg.Files.Add(new KeyValuePair<UInt64, MemoryStream>(crc.ComputeAsValue(fn), new MemoryStream(File.ReadAllBytes(String.Join(Path.DirectorySeparatorChar, args[1], fn)))));
                                    }
                                } catch {
                                    pkg.Files.Add(new KeyValuePair<UInt64, MemoryStream>(crc.ComputeAsValue(fn), new MemoryStream(File.ReadAllBytes(String.Join(Path.DirectorySeparatorChar, args[1], fn)))));
                                }
                                Console.WriteLine($"Adding {fn}...");
                            }
                        }

                        tmp = File.Open(outPath, FileMode.Create, FileAccess.Write);
                        pkg.export(tmp);
                        tmp.Close();
                        pkg.Close();
                    }
                }
                else
                    Usage();
            } catch (Exception ex) {
                Console.WriteLine("An error occured!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }
}
