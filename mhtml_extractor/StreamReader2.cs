using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace mhtml_extractor
{
    public static class ExReader {
        public static StreamReaderEx CreateStreamReaderEx(this FileInfo finfo, FileMode fmode, FileAccess facc, FileShare fshr)
        {
            if (finfo.Exists)
            {
                return new StreamReaderEx(finfo.FullName, fmode, facc, fshr);
            }
            return null;
        }
        public static void AppendBytes(this StringBuilder sb,byte[] bytes, int offset, int count)
        {            
            sb.Append(Encoding.ASCII.GetString(bytes, offset, count));
        }
    }
    public class StreamReaderEx : FileStream
    {
        public long current_pos { get; private set; }
        public long pervious_pos { get; private set; }
        public bool EndOfStream
        { 
            get
            {
                return Position == Length;
            }
        }
        public Stream BaseStream { get { return this; } }
        public StreamReaderEx(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
            current_pos = Position;
        }

        public string ReadLine()
        {
            StringBuilder sb = new StringBuilder();
            byte[] buff = new byte[512];
            int redn;
            bool newline =false;
            pervious_pos = current_pos;
            do
            {
                redn = Read(buff, 0, buff.Length);
                int nlen = redn;
                if (EndOfStream && (redn == 0)) 
                    break;
                for (int i = 0; i < (redn - 1); i++)
                {
                    if (buff[i] == 0xD || buff[i+1] == 0xA) {
                        int CRLF = (buff[i] ^ 0xD) + ((buff[i + 1] ^ 0xA) >> 8);

                        switch (CRLF)
                        {
                            case 0:
                                newline = true;
                                i+=2;
                                break;
                            default:

                                if ((CRLF << 8) == 0)
                                {
                                    newline = true;
                                    i+=2;
                                    break;
                                }
                                newline = ((byte)(CRLF >> 8) == 0);
                                i++;
                                break;
                        }
                    }
                    if (newline)
                    {
                        nlen = (redn - (redn - i));
                        current_pos = Seek(-(redn - i), SeekOrigin.Current);
                        break;
                    }
                }
                sb.AppendBytes(buff, 0, nlen);
            } while (!newline);
            return sb.ToString();
        }
    }
}
