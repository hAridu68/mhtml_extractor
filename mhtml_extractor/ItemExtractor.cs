﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
namespace mhtml_extractor
{
    public class ItemExtractor
    {
        public delegate void OutputCallback(String FileName, long filesize, string filetype);
        public delegate void HeaderMessage(String boundery);
        public OutputCallback _OutputCallback { get; set; }
        public HeaderMessage _HeaderMsg { get; set; }
        private FileInfo IFile;
        public ItemExtractor(string FileName)
        {
            IFile = new FileInfo(FileName);
            _OutputCallback = Output;
            _HeaderMsg = hMsg;
        }

        private void hMsg(string boundery)
        {
            Console.WriteLine("");
        }

        private void Output(string FileName, long filesize, string filetype)
        {
            Console.WriteLine(FileName + "; " + filesize + "; " + filetype);
        }

        private long GetEndContent(StreamReaderEx sr, string Boundery)
        {            
            string str0;
            long end_content = sr.BaseStream.Position;
            while (!sr.EndOfStream)
            {
                str0 = sr.ReadLine();
                if (Regex.IsMatch(str0, Boundery))
                {
                    end_content = sr.BaseStream.Position - (str0.Length - 1) -1;
                    break;
                }
            }            
            return end_content;
        }
        private string GetRandomFileName(string perfix)
        {
            Random rnd = new Random((int)DateTime.Now.Ticks);
            FileInfo fi;
            do
            {
                fi = new FileInfo("./" + rnd.Next().ToString("x4") + rnd.Next().ToString("x4") + rnd.Next().ToString("x4") + perfix);
            } while (fi.Exists);
            return fi.FullName;
        }
        public void Extruct()
        {
            StreamReaderEx sr = IFile.CreateStreamReaderEx(FileMode.Open, FileAccess.Read, FileShare.Read);
            string str0;
            string boundery = "";
            bool hasBoundery = false;
            string FileName;            
            while (!sr.EndOfStream)
            {
                str0 = sr.ReadLine();
                Match bd = Regex.Match(str0, @"(boundary)=\x22([\S]*)\x22");
                if (bd.Success)
                {
                    boundery = bd.Groups[2].Value;
                    hasBoundery = true;
                    break;
                }
            }
            if (!hasBoundery) return;
            _HeaderMsg.Invoke(boundery);
            while (!sr.EndOfStream)
            {
                str0 = sr.ReadLine();
                if (Regex.IsMatch(str0, boundery))
                {                    
                    long current_pos = sr.current_pos;
                    long end_content = GetEndContent(sr, boundery);
                    sr.BaseStream.Position = current_pos;
                    byte[] buff = new byte[512];
                    string filetype = "";
                    while (!sr.EndOfStream)
                    {
                        string strx = sr.ReadLine();
                        if (strx == "\r\n")
                            break;
                        Match mc = Regex.Match(strx, @"^Content-Type:\s([\w]*)/([\w]*)");
                        if (mc.Success)
                        {
                            filetype = "." + mc.Groups[2];
                        }
                    }

                    long fsize = end_content - sr.BaseStream.Position;
                    FileName = GetRandomFileName(filetype);
                    using (FileStream fs = new FileStream(FileName, FileMode.CreateNew))
                    {
                        _OutputCallback.Invoke(FileName, fsize, filetype);
                        while (sr.BaseStream.Position < end_content)
                        {
                            int wz = (int)(end_content - sr.BaseStream.Position);
                            wz = wz < buff.Length ? wz : buff.Length;
                            sr.BaseStream.Read(buff, 0, wz);
                            fs.Write(buff, 0, wz);
                            fs.Flush();
                        }
                    }
                }
            }
            
        }

    }
}
