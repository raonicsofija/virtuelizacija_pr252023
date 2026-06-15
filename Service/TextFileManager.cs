using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class TextFileManager : IDisposable
    {
        private readonly string path;
        private TextWriter writer;
        private TextReader reader;
        private bool disposed = false;

        public string Path
        {
            get { return path; }
        }

        public TextFileManager(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is not defined.");
            }

            this.path = path;

            string directoryPath = System.IO.Path.GetDirectoryName(this.path);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(this.path))
            {
                File.Create(this.path).Close();
            }
        }

        public void AppendText(string text)
        {
            CheckIfDisposed();

            CloseWriter();

            writer = new StreamWriter(path, true);
            writer.WriteLine(DateTime.Now + " - " + text);
            writer.Flush();
        }

        public void AppendLine(string text)
        {
            CheckIfDisposed();

            CloseWriter();

            writer = new StreamWriter(path, true);
            writer.WriteLine(text);
            writer.Flush();
        }

        public void ClearAndWriteLine(string text)
        {
            CheckIfDisposed();

            CloseWriter();

            writer = new StreamWriter(path, false);
            writer.WriteLine(text);
            writer.Flush();
        }

        public string ReadAllText()
        {
            CheckIfDisposed();

            CloseReader();

            reader = new StreamReader(path);
            return reader.ReadToEnd();
        }

        public void ClearText()
        {
            CheckIfDisposed();

            CloseWriter();

            writer = new StreamWriter(path, false);
            writer.Write(string.Empty);
            writer.Flush();
        }

        private void CloseWriter()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        private void CloseReader()
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("TextFileManager");
            }
        }

        ~TextFileManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CloseWriter();
                    CloseReader();
                }

                disposed = true;
            }
        }
    }
}
