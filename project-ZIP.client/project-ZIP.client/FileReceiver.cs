﻿using System;
using System.Net.Sockets;
using System.Windows.Forms;

namespace project_ZIP.client
{
    public class FileReceiver
    {

        public enum FileReceiveStatus
        {
            Ok, Error
        }

        public static FileReceiveStatus FileReceive(Socket socketFd)
        {
            //receive File size
            var sizeBytes = new byte[sizeof(int)];

            try
            {
                socketFd.Receive(sizeBytes, sizeBytes.Length, 0);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception:\t\n" + exc.Message);
                var window = (ProjectZip)Application.OpenForms[0];
                window.SetControls(true);
            }

            var size = BitConverter.ToInt32(sizeBytes, 0);

            //receive File
            var fas = new FileAndSize
            {
                SizeRemaining = size,
                SocketFd = socketFd
            };

            socketFd.BeginReceive(fas.Buffer, 0, FileAndSize.BUF_SIZE, 0,  FileReceiveCallback, fas);

            return FileReceiveStatus.Ok;
        }

        private static void FileReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var fileAndSize = (FileAndSize)ar.AsyncState;
                var socketFd = fileAndSize.SocketFd;

                var bytesReceived = fileAndSize.SocketFd.EndReceive(ar);

                fileAndSize.SizeRemaining -= bytesReceived;

                fileAndSize.File = Combine(fileAndSize.File, fileAndSize.Buffer);

                if (fileAndSize.SizeRemaining > 0)
                {
                    socketFd.BeginReceive(fileAndSize.Buffer, 0, FileAndSize.BUF_SIZE, 0,
                        FileReceiveCallback, fileAndSize);
                }
                else
                {
                    var window = (ProjectZip)Application.OpenForms[0];
                    window.SetControls(true);
                    window.SetFileSelectTextBox("");

                    //send file back to form
                    window.DownloadFile(fileAndSize.File);

                    socketFd.Shutdown(SocketShutdown.Both);
                    socketFd.Close();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception:\t\n" + exc.Message);
                var window = (ProjectZip)Application.OpenForms[0];
                window.SetControls(true);
            }
        }

        //method for combining two byte arrays
        private static byte[] Combine(byte[] first, byte[] second)
        {
            var ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
