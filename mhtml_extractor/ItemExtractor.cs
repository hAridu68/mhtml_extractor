using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
namespace mhtml_extractor
{
    public class ItemExtractor
    {
        public delegate void OutputCallback(String FileName, long filesize, string filetype);
        public delegate void HeaderMessage(String boundery);
        public OutputCallback _OutputCallback { get; set; }
        public HeaderMessage _HeaderMsg { get; set; }
        private FileInfo IFile;
        private FromBase64Transform base64;
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
            long current_pos = sr.BaseStream.Position;
            long end_content = current_pos;
            while (!sr.EndOfStream)
            {
                str0 = sr.ReadLine();
                if (Regex.IsMatch(str0, Boundery))
                {
                    end_content = sr.BaseStream.Position - (str0.Length - 2);
                    break;
                }
            }
            sr.BaseStream.Position = current_pos;
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
        private byte Hex2Ascii(byte[] buff, int pos, int count)
        {
            int val = 0;
            byte tmp;
            for (int i = pos; i < (pos + count); i++)
            {
                tmp = (buff[i]);
                val <<= 4;
                val += (tmp - ((tmp >= 65 && tmp <= 70) ? 55 : 48));
            }
            return (byte)val;
        }
        public void Extruct()
        {
            StreamReaderEx sr = IFile.CreateStreamReaderEx(FileMode.Open, FileAccess.Read, FileShare.Read);
            string str0;
            string boundery = "";
            bool hasBoundery = false;
            string FileName;
            string encoding = string.Empty;
            while (!sr.EndOfStream)
            {
                str0 = sr.ReadLine();
                Match bd = Regex.Match(str0, @"(boundary)=\x22([\S]*)\x22");
                if (bd.Success)
                {
                    boundery = bd.Groups[2].Value;
                    hasBoundery = true;
                    sr.BaseStream.Position = GetEndContent(sr, boundery);
                    sr.ReadLine();
                    break;
                }
            }
            if (!hasBoundery) return;
            _HeaderMsg.Invoke(boundery);
            while (!sr.EndOfStream)
            {
                long end_content = GetEndContent(sr, boundery);
                byte[] buff = new byte[512], buff2 = new byte[512], b64BOut = new byte[3];
                string filetype = "";
                do
                {
                    string strx = sr.ReadLine();
                    if (strx == "\r\n")
                    {
                        break;
                    }
                    Match mc = Regex.Match(strx, @"^Content-Type:\s*([\w]*)/([\w]*)");
                    if (mc.Success)
                    {
                        filetype = "." + mc.Groups[2];
                        continue;
                    }
                    mc = Regex.Match(strx, @"^Content-Transfer-Encoding:\s*([\S]*)");
                    if (mc.Success)
                    {
                        encoding = mc.Groups[1].Value;
                        continue;
                    }
                } while (!sr.EndOfStream);

                long fsize = end_content - sr.BaseStream.Position;
                FileName = GetRandomFileName(filetype);
                using (FileStream fs = new FileStream(FileName, FileMode.CreateNew))
                {
                    _OutputCallback.Invoke(FileName, fsize, filetype);
                    switch (encoding)
                    {
                        case "quoted-printable":
                            StringBuilder sb = new StringBuilder();
                            while (true)
                            {
                                string txt = sr.ReadLine();
                                if (!(sr.BaseStream.Position < end_content))
                                    break;
                                int ln = txt.ToBytes(buff, 0) - 2;
                                int rLn = 0, wLn = 0;
                                byte tmp = 0;
                                while (rLn < ln)
                                {
                                    switch (buff[rLn])
                                    {
                                        case 61:
                                            if ((rLn + 1) == ln)
                                            {
                                                tmp = buff2[--wLn];
                                                break;
                                            }
                                            tmp = Hex2Ascii(buff, rLn + 1, 2);
                                            rLn += 2;
                                            break;
                                        default:
                                            tmp = buff[rLn];
                                            break;
                                    }
                                    buff2[wLn] = tmp;
                                    wLn++; rLn++;
                                }
                                fs.Write(buff2, 0, wLn);
                            }
                            fs.Flush();
                            sr.BaseStream.Position = end_content;
                            break;
                        case "base64":
                            base64 = new FromBase64Transform(FromBase64TransformMode.IgnoreWhiteSpaces);
                            int inputbytes, byteoffset, iBlockSize = 4;
                            while (sr.BaseStream.Position < end_content)
                            {
                                string txt = sr.ReadLine();
                                if (txt == "\r\n")
                                    break;
                                byteoffset = 0;
                                inputbytes = txt.Substring(0, txt.Length - 2).ToBytes(buff, 0);
                                while (inputbytes - byteoffset > iBlockSize)
                                {
                                    base64.TransformBlock(buff, byteoffset, 4, b64BOut, 0);
                                    byteoffset += 4;
                                    fs.Write(b64BOut, 0, base64.OutputBlockSize);
                                }
                                b64BOut = base64.TransformFinalBlock(buff, byteoffset, inputbytes - byteoffset);
                                fs.Write(b64BOut, 0, b64BOut.Length);
                                fs.Flush();
                            }
                            base64.Clear();
                            break;
                        default:
                            while (sr.BaseStream.Position < end_content)
                            {
                                int wz = (int)(end_content - sr.BaseStream.Position);
                                wz = wz < buff.Length ? wz : buff.Length;
                                sr.BaseStream.Read(buff, 0, wz);
                                fs.Write(buff, 0, wz);
                                fs.Flush();
                            }
                            break;
                    }
                }
                sr.BaseStream.Position = end_content;
                sr.ReadLine();
            }

        }
    }
}
