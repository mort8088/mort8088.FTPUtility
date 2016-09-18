using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace Mort8088.FTPUtility
{
    public static class FtpCommand
    {
        private static UriBuilder ftpUriBuilder;
        private static NetworkCredential ftpCredentials;

        public static string Host
        {
            get
            {
                return ftpUriBuilder.Host;
            }
            set
            {
                ftpUriBuilder.Host = value;
            }
        }

        /// <summary>
        /// Gets or sets the behavior of a client application's data transfer 
        /// process.
        /// </summary>
        public static bool UsePassive { get; set; }

        /// <summary>
        /// Gets or sets a System.Boolean value that specifies the data type 
        /// for file transfers.
        /// </summary>
        public static bool UseBinary { get; set; }

        /// <summary>
        ///  Gets or sets a System.Boolean value that specifies whether the 
        ///  control connection to the FTP server is closed after the request completes.
        /// </summary>
        public static bool KeepAlive { get; set; }

        public static string UserName
        {
            get
            {
                return ftpCredentials.UserName;
            }
            set
            {
                ftpCredentials.UserName = value;
            }
        }

        public static string Password
        {
            get
            {
                return ftpCredentials.Password;
            }
            set
            {
                ftpCredentials.Password = value;
            }
        }

        static FtpCommand()
        {
            ftpUriBuilder = new UriBuilder();
            ftpUriBuilder.Scheme = Uri.UriSchemeFtp;
            ftpUriBuilder.Port = 21;

            ftpCredentials = new NetworkCredential();

            UsePassive = true;

            UseBinary = true;

            KeepAlive = true;
        }

        private static FtpWebRequest getFtpWebRequest()
        {
            /* Create an FTP Request */
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpUriBuilder.Uri);

            /* Set Options */
            ftpRequest.UsePassive = UsePassive;

            ftpRequest.UseBinary = UseBinary;

            ftpRequest.KeepAlive = KeepAlive;

            /* Log in to the FTP Server with the User Name and Password Provided */
            ftpRequest.Credentials = ftpCredentials;

            return ftpRequest;
        }

        /// <summary>
        /// List files and folders in a given folder on the server
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static List<string> DirectoryListing(string directory)
        {
            return DirectoryListCall(directory, false);
        }

        /* List Directory Contents in Detail (Name, Size, Created, etc.) */
        public static List<string> directoryListDetailed(string directory)
        {
            return DirectoryListCall(directory, true);
        }

        private static List<string> DirectoryListCall(string directory, bool detailed)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");
            try
            {
                ftpUriBuilder.Path = directory;

                FtpWebRequest ftpRequest = getFtpWebRequest();

                /* Specify the Type of FTP Request */
                if (detailed)
                {
                    ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                }
                else
                {
                    ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                }

                /* Establish Return Communication with the FTP Server */
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                
                /* Establish Return Communication with the FTP Server */
                Stream ftpStream = ftpResponse.GetResponseStream();
                
                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);

                /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
                while (!ftpReader.EndOfStream)
                {
                    result.Add(ftpReader.ReadLine());
                }

                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
            }
            catch (Exception)
            {
                /* Return an Empty string Array if an Exception Occurs */
                result.Clear();
            }
            /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
            return result;
        }

        /// <summary>
        /// Download a file from the FTP server to the destination
        /// </summary>
        /// <param name="from">filename and path to the file, e.g. public_html/test.zip</param>
        /// <param name="to">The location to save the file, e.g. c:test.zip</param>
        public static void Download(string from, string to)
        {
            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            ftpUriBuilder.Path = from;

            FtpWebRequest ftpRequest = getFtpWebRequest();

            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            Stream responseStream = ftpResponse.GetResponseStream();

            StreamReader reader = new StreamReader(responseStream);

            StreamWriter writer = new StreamWriter(to);

            writer.Write(reader.ReadToEnd());

            writer.Close();
            reader.Close();
            ftpResponse.Close();
        }

        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="from">Full path to the source file e.g. c:test.zip</param>
        /// <param name="to">destination folder and filename e.g. public_html/test.zip</param>
        public static bool Upload(string from, string to)
        {
            bool result = false;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            ftpUriBuilder.Path = to;

            FtpWebRequest ftpRequest = getFtpWebRequest();

            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            StreamReader sourceStream = new StreamReader(from);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();

            ftpRequest.ContentLength = fileContents.Length;

            Stream requestStream = ftpRequest.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            if (ftpResponse.StatusCode == FtpStatusCode.CommandOK) result = true;

            ftpResponse.Close();

            return result;
        }

        /// <summary>
        /// Remove a file from the server.
        /// </summary>
        /// <param name="filename">filename and path to the file, e.g. public_html/test.zip</param>
        public static bool Delete(string filename)
        {
            bool result = false;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            ftpUriBuilder.Path = filename;

            FtpWebRequest ftpRequest = getFtpWebRequest();
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            if (ftpResponse.StatusCode == FtpStatusCode.CommandOK) result = true;

            ftpResponse.Close();

            return result;
        }

        public static bool rename(string from, string to)
        {
            bool result = false;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            try
            {
                ftpUriBuilder.Path = from;

                FtpWebRequest ftpRequest = getFtpWebRequest();

                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.Rename;

                /* Rename the File */
                ftpRequest.RenameTo = to;

                /* Establish Return Communication with the FTP Server */
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                if (ftpResponse.StatusCode == FtpStatusCode.CommandOK) result = true;

                /* Resource Cleanup */
                ftpResponse.Close();
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /* Create a New Directory on the FTP Server */
        public static bool createDirectory(string newDirectory)
        {
            bool result = false;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            try
            {
                ftpUriBuilder.Path = newDirectory;

                FtpWebRequest ftpRequest = getFtpWebRequest();

                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

                /* Establish Return Communication with the FTP Server */
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                if (ftpResponse.StatusCode == FtpStatusCode.CommandOK) result = true;

                /* Resource Cleanup */
                ftpResponse.Close();

                ftpRequest = null;
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /* Get the Date/Time a File was Created */
        public static DateTime getFileCreatedDateTime(string fileName)
        {
            DateTime result;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            try
            {
                ftpUriBuilder.Path = fileName;

                FtpWebRequest ftpRequest = getFtpWebRequest();

                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;

                /* Establish Return Communication with the FTP Server */
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                /* Establish Return Communication with the FTP Server */
                Stream ftpStream = ftpResponse.GetResponseStream();

                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);

                /* Store the Raw Response */
                string fileInfo = null;

                /* Read the Full Response Stream */
                try
                {
                    fileInfo = ftpReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;

                /* Return File Created Date Time */
                DateTime.TryParse(fileInfo, out result);
            }
            catch (Exception)
            {
                result = DateTime.MinValue;
            }

            return result;
        }

        /* Get the Size of a File */
        public static int getFileSize(string fileName)
        {
            int result;

            if (string.IsNullOrEmpty(ftpUriBuilder.Host))
                throw new ArgumentException("FTP host Not set.");

            try
            {
                ftpUriBuilder.Path = fileName;

                FtpWebRequest ftpRequest = getFtpWebRequest();

                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;

                /* Establish Return Communication with the FTP Server */
                FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                /* Establish Return Communication with the FTP Server */
                Stream ftpStream = ftpResponse.GetResponseStream();

                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);

                /* Store the Raw Response */
                string fileInfo = null;

                /* Read the Full Response Stream */
                try
                {
                    fileInfo = ftpReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;

                /* Return File Size */
                int.TryParse(fileInfo, out result);
            }
            catch (Exception)
            {
                result = -1;
            }

            /* Return an Empty string Array if an Exception Occurs */
            return result;
        }
    }
}
