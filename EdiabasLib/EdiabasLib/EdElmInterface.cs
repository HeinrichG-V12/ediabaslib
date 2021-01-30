﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
// ReSharper disable UseNullPropagation

namespace EdiabasLib
{
    public class EdElmInterface : IDisposable
    {
        public class ElmInitEntry
        {
            public ElmInitEntry(string command, int version = -1, bool okResponse = true)
            {
                Command = command;
                OkResponse = okResponse;
                Version = version;
            }

            public string Command { get; }
            public bool OkResponse { get; }
            public int Version { get; }
        }

        public static ElmInitEntry[] Elm327InitCommands =
        {
            new ElmInitEntry("ATD"),
            new ElmInitEntry("ATE0"),
            //new ElmInitEntry("ATPP2COFF"),      // reject fake elms (disables also WGSoft adapters)
            new ElmInitEntry("ATSH6F1"),
            new ElmInitEntry("ATCF600"),
            new ElmInitEntry("ATCM700"),
            new ElmInitEntry("ATPBC001"),
            new ElmInitEntry("ATSPB"),
            new ElmInitEntry("ATAT0"),
            new ElmInitEntry("ATSTFF"),
            new ElmInitEntry("ATAL"),
            new ElmInitEntry("ATH1"),
            new ElmInitEntry("ATS0"),
            new ElmInitEntry("ATL0"),
            new ElmInitEntry("ATCSM0", 210),    // disable silent monitoring
            new ElmInitEntry("ATCTM5", 210),    // timer multiplier 5
            new ElmInitEntry("ATJE", 130),      // ELM data format, used for fake ELM detection
            //new ElmInitEntry("ATPPS", -1, false),     // some BT chips have a short buffer, so this test will fail
        };

        public static ElmInitEntry[] Elm327InitFullTransport =
        {
            new ElmInitEntry("ATSH6F1"),
            new ElmInitEntry("ATFCSH6F1"),
            new ElmInitEntry("ATPBC101"),   // set Parameter for CAN B Custom Protocol 11/500 with var. DLC
            new ElmInitEntry("ATBI"),       // bypass init sequence
        };

        public static ElmInitEntry[] Elm327InitCarlyTransport =
        {
            new ElmInitEntry("ATGB1"),      // switch to binary mode
            new ElmInitEntry("ATSH6F1"),
            new ElmInitEntry("ATFCSH6F1"),
            new ElmInitEntry("ATPBC101"),   // set Parameter for CAN B Custom Protocol 11/500 with var. DLC
            new ElmInitEntry("ATBI"),       // bypass init sequence
        };

        public static byte[] EcuAddrListE89 =
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0D, 0x0E, 0x0F, 0x12, 0x13,
            0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x20, 0x21, 0x22, 0x23, 0x24,
            0x26, 0x27, 0x28, 0x29, 0x2A, 0x30, 0x31, 0x32, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B,
            0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B,
            0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D,
            0x5E, 0x5F, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D,
            0x6E, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x77, 0x78, 0x79, 0x7A, 0x7D, 0x8B, 0x90, 0x91, 0x92,
            0x93, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0xA0, 0xA1, 0xA2, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8,
            0xA9, 0xAB, 0xAD, 0xAE
        };

        public static byte[] EcuAddrListF01 =
        {
            0x00, 0x01, 0x02, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0D, 0x0E, 0x0F, 0x10, 0x11,
            0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x20, 0x21, 0x22,
            0x23, 0x24, 0x25, 0x26, 0x27, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x30, 0x31, 0x32, 0x34, 0x35,
            0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45,
            0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4D, 0x4E, 0x4F, 0x50, 0x53, 0x54, 0x55, 0x56, 0x57, 0x59,
            0x5A, 0x5B, 0x5D, 0x5E, 0x5F, 0x60, 0x61, 0x63, 0x64, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6D, 0x6E,
            0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0xA0, 0xA5, 0xA6, 0xA7, 0xA8,
            0xA9, 0xAB
        };

        private static readonly long TickResolMs = Stopwatch.Frequency / 1000;
        private const int Elm327ReadTimeoutOffset = 1000;
        private const int Elm327CommandTimeout = 1500;
        private const int Elm327DataTimeout = 2000;
        private const int Elm327DataCarlyTimeout = 500;
        private const int Elm327CanBlockSize = 8;
        private const int Elm327CanSepTime = 0;
        private const int Elm327TimeoutBaseMultiplier = 4;
        private bool _disposed;
        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private long _elm327ReceiveStartTime;
        private bool _elm327DataMode;
        private bool _elm327FullTransport;
        private bool _elm327CarlyTransport;
        private volatile bool _elm327ReceiverBusy;
        private int _elm327TimeoutMultiplier = 1;
        private bool _elm327BinaryData;
        private int _elm327CanHeader;
        private int _elm327Timeout;
        private Thread _elm327Thread;
        private bool _elm327TerminateThread;
        private readonly AutoResetEvent _elm327RequEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _elm327RespEvent = new AutoResetEvent(false);
        private volatile byte[] _elm327RequBuffer;
        private readonly Queue<byte[]> _elm327RequQueue = new Queue<byte[]>();
        private readonly Queue<byte> _elm327RespQueue = new Queue<byte>();
        private volatile List<string> _elm327CarlyRespList;
        private readonly Object _elm327BufferLock = new Object();

        public bool StreamFailure { get; set; }
        public EdiabasNet Ediabas { get; set; }

        public EdElmInterface(EdiabasNet ediabas, Stream inStream, Stream outStream)
        {
            Ediabas = ediabas;
            _inStream = inStream;
            _outStream = outStream;
        }

        public bool InterfaceDisconnect()
        {
            Elm327StopThread();
            Elm327Exit();
            StreamFailure = false;

            return true;
        }

        public bool InterfacePurgeInBuffer()
        {
            lock (_elm327BufferLock)
            {
                _elm327RespQueue.Clear();
            }
            return true;
        }

        public bool InterfaceSendData(byte[] sendData, int length, bool setDtr, double dtrTimeCorr)
        {
            lock (_elm327BufferLock)
            {
                if (_elm327RequBuffer != null)
                {
                    return false;
                }
            }
            byte[] data = new byte[length];
            Array.Copy(sendData, data, length);
            lock (_elm327BufferLock)
            {
                _elm327RequBuffer = data;
            }
            _elm327RequEvent.Set();

            return true;
        }

        public bool InterfaceReceiveData(byte[] receiveData, int offset, int length, int timeout, int timeoutTelEnd, EdiabasNet ediabasLog)
        {
            timeout += Elm327ReadTimeoutOffset;
            _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
            for (;;)
            {
                lock (_elm327BufferLock)
                {
                    if (_elm327RespQueue.Count >= length)
                    {
                        break;
                    }
                }

                if (ReceiverBusy())
                {
                    _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
                }
                else
                {
                    if ((Stopwatch.GetTimestamp() - _elm327ReceiveStartTime) > timeout * TickResolMs)
                    {
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "*** Receive timeout");
                        return false;
                    }
                }

                _elm327RespEvent.WaitOne(timeout, false);
            }
            lock (_elm327BufferLock)
            {
                for (int i = 0; i < length; i++)
                {
                    receiveData[i + offset] = _elm327RespQueue.Dequeue();
                }
            }
            return true;
        }

        public bool Elm327Init()
        {
            _elm327DataMode = false;
            _elm327ReceiverBusy = false;
            _elm327TimeoutMultiplier = 1;
            _elm327BinaryData = false;
            lock (_elm327BufferLock)
            {
                _elm327RequBuffer = null;
                _elm327RequQueue.Clear();
                _elm327RespQueue.Clear();
                _elm327CarlyRespList = null;
            }
            bool firstCommand = true;
            foreach (ElmInitEntry elmInitEntry in Elm327InitCommands)
            {
                bool optional = elmInitEntry.Version >= 0;
                if (!Elm327SendCommand(elmInitEntry.Command, elmInitEntry.OkResponse))
                {
                    if (!firstCommand)
                    {
                        if (!optional)
                        {
                            return false;
                        }
                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM optional command {0} failed", elmInitEntry.Command);
                    }
                    if (firstCommand && !optional)
                    {
                        if (!Elm327SendCommand(elmInitEntry.Command, elmInitEntry.OkResponse))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (string.Compare(elmInitEntry.Command, "ATCTM5", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        _elm327TimeoutMultiplier = 5;
                    }
                }
                if (!elmInitEntry.OkResponse)
                {
                    string answer = Elm327ReceiveAnswer(Elm327CommandTimeout);
                    if (string.IsNullOrEmpty(answer))
                    {
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM no answer");
                    }
                }
                firstCommand = false;
            }

            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM timeout multiplier: {0}", _elm327TimeoutMultiplier);

            if (!Elm327SendCommand("AT@1", false))
            {
                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Sending @1 failed");
                return false;
            }
            string elmDevDesc = Elm327ReceiveAnswer(Elm327CommandTimeout);
            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM ID: {0}", elmDevDesc);
            if (elmDevDesc.ToUpperInvariant().Contains("CARLY-UNIVERSAL"))
            {
                _elm327CarlyTransport = true;
            }

            if (!Elm327SendCommand("AT#1", false))
            {
                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Sending #1 failed");
                return false;
            }
            string elmManufact = Elm327ReceiveAnswer(Elm327CommandTimeout);
            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM Manufacturer: {0}", elmManufact);

            if (!_elm327CarlyTransport && elmManufact.ToUpperInvariant().Contains("WGSOFT"))
            {
                _elm327FullTransport = true;
                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "WGSOFT adapter not supported");
                return false;
            }
            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM full transport: {0}", _elm327FullTransport);

            if (_elm327CarlyTransport)
            {
                foreach (ElmInitEntry elmInitEntry in Elm327InitCarlyTransport)
                {
                    if (!Elm327SendCommand(elmInitEntry.Command))
                    {
                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM carly transport command {0} failed", elmInitEntry.Command);
                        return false;
                    }
                    else
                    {
                        if (string.Compare(elmInitEntry.Command, "ATGB1", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            _elm327BinaryData = true;
                        }
                    }
                }
            }

            if (_elm327FullTransport)
            {
                foreach (ElmInitEntry elmInitEntry in Elm327InitFullTransport)
                {
                    if (!Elm327SendCommand(elmInitEntry.Command))
                    {
                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM full transport command {0} failed", elmInitEntry.Command);
                        return false;
                    }
                }
            }

            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM binary data: {0}", _elm327BinaryData);

            _elm327CanHeader = 0x6F1;
            _elm327Timeout = -1;
            StreamFailure = false;
            Elm327StartThread();
            return true;
        }

        public void Elm327Exit()
        {
            try
            {
                Elm327LeaveDataMode(Elm327CommandTimeout);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Elm327StartThread()
        {
            if (_elm327Thread != null)
            {
                return;
            }
            _elm327TerminateThread = false;
            _elm327RequEvent.Reset();
            _elm327RespEvent.Reset();
            _elm327Thread = new Thread(Elm327ThreadFunc)
            {
                Priority = ThreadPriority.Highest
            };
            _elm327Thread.Start();
        }

        private void Elm327StopThread()
        {
            if (_elm327Thread != null)
            {
                _elm327TerminateThread = true;
                _elm327RequEvent.Set();
                _elm327Thread.Join();
                _elm327Thread = null;
                _elm327RequBuffer = null;
                _elm327RequQueue.Clear();
                _elm327RespQueue.Clear();
            }
        }

        private void Elm327ThreadFunc()
        {
            while (!_elm327TerminateThread)
            {
                if (_elm327FullTransport || _elm327CarlyTransport)
                {
                    Elm327CanSenderFull();
                }
                else
                {
                    Elm327CanSender();
                }

                Elm327CanReceiver();
                _elm327RequEvent.WaitOne(10, false);
            }
        }

        private void Elm327CanSenderFull()
        {
            byte[] reqBuffer = null;

            lock (_elm327BufferLock)
            {
                if (_elm327RequQueue.Count == 0)
                {
                    byte[] tempBuffer = _elm327RequBuffer;
                    _elm327RequBuffer = null;

                    if (tempBuffer != null && tempBuffer.Length >= 4)
                    {
                        bool funcAddress = (tempBuffer[0] & 0xC0) == 0xC0;     // functional address
                        if (funcAddress)
                        {
                            int dataLength = tempBuffer[0] & 0x3F;
                            bool longRequ = dataLength == 0 || dataLength > 2;
                            byte[] ecuAddrList = longRequ ? EcuAddrListF01 : EcuAddrListE89;
                            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Replacing functional address with address list. Long request={0}", longRequ);

                            foreach (byte addr in ecuAddrList)
                            {
                                byte[] queueBuffer = new byte[tempBuffer.Length];
                                Array.Copy(tempBuffer, queueBuffer, tempBuffer.Length);
                                queueBuffer[0] = (byte)((queueBuffer[0] & ~0xC0) | 0x80);
                                queueBuffer[1] = addr;
                                _elm327RequQueue.Enqueue(queueBuffer);
                            }
                        }
                        else
                        {
                            _elm327RequQueue.Enqueue(tempBuffer);
                        }
                    }
                }

                if (_elm327RequQueue.Count > 0)
                {
                    reqBuffer = _elm327RequQueue.Dequeue();
                }
            }

            if (reqBuffer != null && reqBuffer.Length >= 4)
            {
                bool funcAddress = (reqBuffer[0] & 0xC0) == 0xC0;     // functional address
                byte targetAddr = reqBuffer[1];
                byte sourceAddr = reqBuffer[2];
                int dataOffset = 3;
                int dataLength = reqBuffer[0] & 0x3F;
                if (dataLength == 0)
                {
                    // with length byte
                    if (reqBuffer[3] == 0x00)
                    {
                        dataLength = (reqBuffer[4] << 8) + reqBuffer[5];
                        dataOffset = 6;
                    }
                    else
                    {
                        dataLength = reqBuffer[3];
                        dataOffset = 4;
                    }
                }

                if (reqBuffer.Length < (dataOffset + dataLength))
                {
                    return;
                }

                int canHeader = 0x600 | sourceAddr;
                if (_elm327CanHeader != canHeader)
                {
                    if (!Elm327SendCommand("ATSH" + string.Format("{0:X03}", canHeader)))
                    {
                        _elm327CanHeader = -1;
                        return;
                    }
                    if (!Elm327SendCommand("ATFCSH" + string.Format("{0:X03}", canHeader)))
                    {
                        _elm327CanHeader = -1;
                        return;
                    }
                    _elm327CanHeader = canHeader;
                }

                int blockSize = 0x00;
                int sepTime = _elm327CarlyTransport ? 0x02 : 0x00;
                if (!Elm327SendCommand("ATFCSD" + string.Format("{0:X02}30{1:X02}{2:X02}", targetAddr, blockSize, sepTime)))
                {
                    return;
                }
                if (!Elm327SendCommand("ATCEA" + string.Format("{0:X02}", targetAddr)))
                {
                    return;
                }
                if (!Elm327SendCommand("ATFCSM1"))
                {
                    return;
                }

                byte[] canSendBuffer = new byte[dataLength];
                Array.Copy(reqBuffer, dataOffset, canSendBuffer, 0, dataLength);
                Elm327SendCanTelegram(canSendBuffer, true, funcAddress);
            }
        }

        private void Elm327CanSender()
        {
            byte[] requBuffer;
            lock (_elm327BufferLock)
            {
                requBuffer = _elm327RequBuffer;
                _elm327RequBuffer = null;
            }
            if (requBuffer != null && requBuffer.Length >= 4)
            {
                byte targetAddr = requBuffer[1];
                byte sourceAddr = requBuffer[2];
                int dataOffset = 3;
                int dataLength = requBuffer[0] & 0x3F;
                if (dataLength == 0)
                {
                    // with length byte
                    if (requBuffer[3] == 0x00)
                    {
                        dataLength = (requBuffer[4] << 8) + requBuffer[5];
                        dataOffset = 6;
                    }
                    else
                    {
                        dataLength = requBuffer[3];
                        dataOffset = 4;
                    }
                }
                if (requBuffer.Length < (dataOffset + dataLength))
                {
                    return;
                }

                int canHeader = 0x600 | sourceAddr;
                if (_elm327CanHeader != canHeader)
                {
                    if (!Elm327SendCommand("ATSH" + string.Format("{0:X03}", canHeader)))
                    {
                        _elm327CanHeader = -1;
                        return;
                    }
                    _elm327CanHeader = canHeader;
                }
                byte[] canSendBuffer = new byte[8];
                if (dataLength <= 6)
                {
                    // single frame
                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Send SF");
                    canSendBuffer[0] = targetAddr;
                    canSendBuffer[1] = (byte)(0x00 | dataLength); // SF
                    Array.Copy(requBuffer, dataOffset, canSendBuffer, 2, dataLength);
                    Elm327SendCanTelegram(canSendBuffer);
                }
                else
                {
                    // first frame
                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Send FF");
                    canSendBuffer[0] = targetAddr;
                    canSendBuffer[1] = (byte)(0x10 | ((dataLength >> 8) & 0x0F)); // FF
                    canSendBuffer[2] = (byte)dataLength;
                    int telLen = 5;
                    Array.Copy(requBuffer, dataOffset, canSendBuffer, 3, telLen);
                    dataLength -= telLen;
                    dataOffset += telLen;
                    if (!Elm327SendCanTelegram(canSendBuffer))
                    {
                        return;
                    }
                    byte blockSize = 0;
                    byte sepTime = 0;
                    bool waitForFc = true;
                    byte blockCount = 1;
                    for (;;)
                    {
                        if (waitForFc)
                        {
                            Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Wait for FC");
                            bool wait = false;
                            do
                            {
                                int[] canRecData = Elm327ReceiveCanTelegram(Elm327DataTimeout);
                                if (canRecData == null)
                                {
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "*** FC timeout");
                                    return;
                                }
                                if (canRecData.Length >= 5 &&
                                    ((canRecData[0] & 0xFF00) == 0x0600) &&
                                    ((canRecData[0] & 0xFF) == targetAddr) && (canRecData[1 + 0] == sourceAddr) &&
                                    ((canRecData[1 + 1] & 0xF0) == 0x30)
                                    )
                                {
                                    byte frameControl = (byte)(canRecData[1 + 1] & 0x0F);
                                    switch (frameControl)
                                    {
                                        case 0: // CTS
                                            wait = false;
                                            break;

                                        case 1: // Wait
                                            Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Wait for next FC");
                                            wait = true;
                                            break;

                                        default:
                                            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** Invalid FC: {0:X01}", frameControl);
                                            return;
                                    }
                                    blockSize = (byte)canRecData[1 + 2];
                                    sepTime = (byte)canRecData[1 + 3];
                                    _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
                                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "BS={0} ST={1}", blockSize, sepTime);
                                }
                                if (_elm327TerminateThread)
                                {
                                    return;
                                }
                            }
                            while (wait);
                        }

                        waitForFc = false;
                        if (blockSize > 0)
                        {
                            if (blockSize == 1)
                            {
                                waitForFc = true;
                            }
                            blockSize--;
                        }
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Send CF");
                        bool expectResponse = (waitForFc || (dataLength <= 6));
                        // consecutive frame
                        Array.Clear(canSendBuffer, 0, canSendBuffer.Length);
                        canSendBuffer[0] = targetAddr;
                        canSendBuffer[1] = (byte)(0x20 | (blockCount & 0x0F)); // CF
                        telLen = dataLength;
                        if (telLen > 6)
                        {
                            telLen = 6;
                        }
                        Array.Copy(requBuffer, dataOffset, canSendBuffer, 2, telLen);
                        dataLength -= telLen;
                        dataOffset += telLen;
                        blockCount++;
                        if (!Elm327SendCanTelegram(canSendBuffer, expectResponse))
                        {
                            return;
                        }
                        if (dataLength <= 0)
                        {
                            break;
                        }

                        if (!waitForFc)
                        {   // we have to wait here, otherwise thread requires too much computation time
                            Thread.Sleep(sepTime < 50 ? 50 : sepTime);
                        }
                        if (_elm327TerminateThread)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void Elm327CanReceiver()
        {
            byte blockCount = 0;
            byte sourceAddr = 0;
            byte targetAddr = 0;
            byte fcCount = 0;
            int recLen = 0;
            byte[] recDataBuffer = null;
            for (;;)
            {
                bool dataAvailable = _elm327CarlyTransport || DataAvailable();
                if (recLen == 0 && !dataAvailable)
                {
                    return;
                }

                int[] canRecData = Elm327ReceiveCanTelegram(Elm327DataTimeout);
                if (canRecData != null && canRecData.Length >= (1 + 2))
                {
                    byte frameType = (byte)((canRecData[1 + 1] >> 4) & 0x0F);
                    int telLen;
                    if (recLen == 0)
                    {
                        // first telegram
                        sourceAddr = (byte)(canRecData[0] & 0xFF);
                        targetAddr = (byte)canRecData[1 + 0];
                        switch (frameType)
                        {
                            case 0: // single frame
                                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Rec SF");
                                telLen = canRecData[2] & 0x0F;
                                if (telLen > (canRecData.Length - 1 - 2))
                                {
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Invalid length");
                                    continue;
                                }
                                recDataBuffer = new byte[telLen];
                                for (int i = 0; i < telLen; i++)
                                {
                                    recDataBuffer[i] = (byte)canRecData[1 + 2 + i];
                                }
                                recLen = telLen;
                                _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
                                break;

                            case 1: // first frame
                                {
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Rec FF");
                                    if (canRecData.Length < (1 + 8))
                                    {
                                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Invalid length");
                                        continue;
                                    }
                                    telLen = ((canRecData[1 + 1] & 0x0F) << 8) + canRecData[1 + 2];
                                    recDataBuffer = new byte[telLen];
                                    recLen = 5;
                                    for (int i = 0; i < recLen; i++)
                                    {
                                        recDataBuffer[i] = (byte)canRecData[1 + 3 + i];
                                    }
                                    blockCount = 1;

                                    if (_elm327CarlyTransport)
                                    {
                                        bool respListEmpty;
                                        lock (_elm327BufferLock)
                                        {
                                            respListEmpty = _elm327CarlyRespList == null || _elm327CarlyRespList.Count == 0;
                                        }

                                        if (respListEmpty)
                                        {
                                            Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Carly aborted transmission, creating dummy response");
                                            recLen = recDataBuffer.Length;
                                            // send dummy byte to abort transmission
                                            byte[] canSendBuffer = new byte[1];
                                            canSendBuffer[0] = 0x00;
                                            if (!Elm327SendCanTelegram(canSendBuffer))
                                            {
                                                return;
                                            }
                                            break;
                                        }
                                    }

                                    if (!_elm327FullTransport && !_elm327CarlyTransport)
                                    {
                                        byte[] canSendBuffer = new byte[8];
                                        canSendBuffer[0] = sourceAddr;
                                        canSendBuffer[1] = 0x30; // FC
                                        canSendBuffer[2] = Elm327CanBlockSize;
                                        canSendBuffer[3] = Elm327CanSepTime;
                                        fcCount = Elm327CanBlockSize;
                                        if (!Elm327SendCanTelegram(canSendBuffer))
                                        {
                                            return;
                                        }
                                    }
                                    _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
                                    break;
                                }

                            default:
                                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** Rec invalid frame {0:X01}", frameType);
                                continue;
                        }
                    }
                    else
                    {
                        // next frame
                        if (frameType == 2 && recDataBuffer != null &&
                            (sourceAddr == (canRecData[0] & 0xFF)) && (targetAddr == canRecData[1 + 0]))
                        {
                            int blockCount1 = canRecData[1 + 1] & 0x0F;
                            int blockCount2 = blockCount & 0x0F;
                            if (blockCount1 != blockCount2)
                            {
                                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Invalid block count: {0} {1}", blockCount1, blockCount2);
                                continue;
                            }
                            Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Rec CF");
                            telLen = recDataBuffer.Length - recLen;
                            if (telLen > 6)
                            {
                                telLen = 6;
                            }
                            if (telLen > (canRecData.Length - 1 - 2))
                            {
                                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Invalid length");
                                continue;
                            }
                            for (int i = 0; i < telLen; i++)
                            {
                                recDataBuffer[recLen + i] = (byte)canRecData[1 + 2 + i];
                            }
                            recLen += telLen;
                            blockCount++;
                            if (!_elm327FullTransport && !_elm327CarlyTransport && fcCount > 0 && recLen < recDataBuffer.Length)
                            {
                                fcCount--;
                                if (fcCount == 0)
                                {   // send FC
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "(Rec) Send FC");
                                    byte[] canSendBuffer = new byte[8];
                                    canSendBuffer[0] = sourceAddr;
                                    canSendBuffer[1] = 0x30; // FC
                                    canSendBuffer[2] = Elm327CanBlockSize;
                                    canSendBuffer[3] = Elm327CanSepTime;
                                    fcCount = Elm327CanBlockSize;
                                    if (!Elm327SendCanTelegram(canSendBuffer))
                                    {
                                        return;
                                    }
                                }
                            }
                            _elm327ReceiveStartTime = Stopwatch.GetTimestamp();
                        }
                    }
                    if (recDataBuffer != null && recLen >= recDataBuffer.Length)
                    {
                        break;
                    }
                }
                else
                {
                    if (canRecData == null)
                    {   // nothing received
                        return;
                    }
                }
                if (_elm327TerminateThread)
                {
                    return;
                }
            }

            if (recLen >= recDataBuffer.Length)
            {
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "Received length: {0}", recLen);
                byte[] responseTel;
                // create BMW-FAST telegram
                if (recDataBuffer.Length > 0xFF)
                {
                    responseTel = new byte[recDataBuffer.Length + 6];
                    responseTel[0] = 0x80;
                    responseTel[1] = targetAddr;
                    responseTel[2] = sourceAddr;
                    responseTel[3] = 0x00;
                    responseTel[4] = (byte)(recDataBuffer.Length >> 8);
                    responseTel[5] = (byte)recDataBuffer.Length;
                    Array.Copy(recDataBuffer, 0, responseTel, 6, recDataBuffer.Length);
                }
                else if (recDataBuffer.Length > 0x3F)
                {
                    responseTel = new byte[recDataBuffer.Length + 4];
                    responseTel[0] = 0x80;
                    responseTel[1] = targetAddr;
                    responseTel[2] = sourceAddr;
                    responseTel[3] = (byte)recDataBuffer.Length;
                    Array.Copy(recDataBuffer, 0, responseTel, 4, recDataBuffer.Length);
                }
                else
                {
                    responseTel = new byte[recDataBuffer.Length + 3];
                    responseTel[0] = (byte)(0x80 | recDataBuffer.Length);
                    responseTel[1] = targetAddr;
                    responseTel[2] = sourceAddr;
                    Array.Copy(recDataBuffer, 0, responseTel, 3, recDataBuffer.Length);
                }
                byte checkSum = CalcChecksumBmwFast(responseTel, 0, responseTel.Length);
                lock (_elm327BufferLock)
                {
                    foreach (byte data in responseTel)
                    {
                        _elm327RespQueue.Enqueue(data);
                    }
                    _elm327RespQueue.Enqueue(checkSum);
                }
                _elm327RespEvent.Set();
            }
        }

        private bool Elm327SendCommand(string command, bool readAnswer = true)
        {
            try
            {
                if (!Elm327LeaveDataMode(Elm327CommandTimeout))
                {
                    _elm327DataMode = false;
                    return false;
                }
                FlushReceiveBuffer();
                byte[] sendData = Encoding.UTF8.GetBytes(command + "\r");
                _outStream.Write(sendData, 0, sendData.Length);
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CMD send: {0}", command);
                if (readAnswer)
                {
                    string answer = Elm327ReceiveAnswer(Elm327CommandTimeout);
                    // check for OK
                    if (!answer.Contains("OK\r"))
                    {
                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** ELM invalid response: {0}", answer);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** ELM stream failure: {0}", EdiabasNet.GetExceptionText(ex));
                StreamFailure = true;
                return false;
            }
            return true;
        }

        private bool Elm327SendCanTelegram(byte[] canTelegram, bool expectResponse = true, bool funcAddress = false)
        {
            try
            {
                int timeout = expectResponse? 0xFF : 0x00;
                if (_elm327CarlyTransport)
                {
                    if (funcAddress)
                    {
                        timeout = 0xFF;
                    }
                    else
                    {
                        timeout = Elm327DataCarlyTimeout / Elm327TimeoutBaseMultiplier / _elm327TimeoutMultiplier;
                    }
                }

                if ((timeout == 0x00) || (timeout != _elm327Timeout))
                {
                    if (!Elm327SendCommand(string.Format("ATST{0:X02}", timeout), false))
                    {
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "Setting timeout failed");
                        _elm327Timeout = -1;
                        return false;
                    }
                    string answer = Elm327ReceiveAnswer(Elm327CommandTimeout);
                    // check for OK
                    if (!answer.Contains("OK\r") && !answer.Contains("STOPPED\r") && !answer.Contains("NO DATA\r") && !answer.Contains("DATA ERROR\r"))
                    {
                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** ELM set timeout invalid response: {0}", answer);
                        _elm327Timeout = -1;
                        return false;
                    }
                }
                _elm327Timeout = timeout;

                if (!Elm327LeaveDataMode(Elm327CommandTimeout))
                {
                    _elm327DataMode = false;
                    _elm327Timeout = -1;
                    return false;
                }
                FlushReceiveBuffer();
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte data in canTelegram)
                {
                    stringBuilder.Append((string.Format("{0:X02}", data)));
                }
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN send: {0}", stringBuilder.ToString());
                stringBuilder.Append("\r");
                byte[] sendData = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                _outStream.Write(sendData, 0, sendData.Length);
                _elm327DataMode = expectResponse;
            }
            catch (Exception ex)
            {
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "*** ELM stream failure: {0}", EdiabasNet.GetExceptionText(ex));
                StreamFailure = true;
                return false;
            }
            return true;
        }

        private int[] Elm327ReceiveCanTelegram(int timeout)
        {
            List<int> resultList = new List<int>();
            try
            {
                if (!_elm327DataMode)
                {
                    return null;
                }

                string answer;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (_elm327CarlyTransport)
                {
                    answer = Elm327DataCarlyAnswer(timeout);
                }
                else
                {
                    answer = Elm327ReceiveAnswer(timeout, true);
                    if (!_elm327DataMode)
                    {   // switch to monitor mode
#if false
                    // Monitor mode disables CAN ack,
                    // for testing a second CAN node is required.
                    // With this hack this can be avoided
                    if (!Elm327SendCanTelegram(new byte[] { 0x00 }))
#else
                        if (!Elm327SendCommand("ATMA", false))
#endif
                        {
                            return null;
                        }
                        _elm327DataMode = true;
                    }
                }

                if (string.IsNullOrEmpty(answer))
                {
                    return null;
                }
                // remove all spaces
                answer = answer.Replace(" ", string.Empty);
                if ((answer.Length & 0x01) == 0)
                {   // must be odd because of can header
                    return null;
                }
                if (!Regex.IsMatch(answer, @"\A[0-9a-fA-F]{3,19}\Z"))
                {
                    return null;
                }
                resultList.Add(Convert.ToInt32(answer.Substring(0, 3), 16));
                for (int i = 3; i < answer.Length; i += 2)
                {
                    resultList.Add(Convert.ToInt32(answer.Substring(i, 2), 16));
                }
            }
            catch (Exception)
            {
                return null;
            }
            return resultList.ToArray();
        }

        private bool Elm327LeaveDataMode(int timeout)
        {
            if (_elm327CarlyTransport)
            {
                _elm327DataMode = false;
                _elm327ReceiverBusy = false;
                lock (_elm327BufferLock)
                {
                    _elm327CarlyRespList = null;
                }
                return true;
            }

            if (!_elm327DataMode)
            {
                return true;
            }

            bool elmThread = _elm327Thread != null && Thread.CurrentThread == _elm327Thread;
            StringBuilder stringBuilder = new StringBuilder();
            while (DataAvailable())
            {
                int data = _inStream.ReadByteAsync();
                if (data >= 0)
                {
                    stringBuilder.Append(Convert.ToChar(data));
                    if (data == 0x3E)
                    {
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM data mode already terminated: " + stringBuilder);
                        _elm327DataMode = false;
                        return true;
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                _outStream.WriteByte(0x20);    // space
            }
            Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM send SPACE");

            long startTime = Stopwatch.GetTimestamp();
            for (;;)
            {
                while (DataAvailable())
                {
                    int data = _inStream.ReadByteAsync();
                    if (data >= 0)
                    {
                        stringBuilder.Append(Convert.ToChar(data));
                        if (data == 0x3E)
                        {
                            if (Ediabas != null)
                            {
                                string response = stringBuilder.ToString();
                                if (!response.Contains("STOPPED\r"))
                                {
                                    Ediabas.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM data mode not stopped: " + stringBuilder);
                                }
                                else
                                {
                                    Ediabas.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM data mode terminated");
                                }
                            }
                            _elm327DataMode = false;
                            return true;
                        }
                    }
                }
                if ((Stopwatch.GetTimestamp() - startTime) > timeout * TickResolMs)
                {
                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "*** ELM leave data mode timeout");
                    return false;
                }
                if (elmThread)
                {
                    if (_elm327TerminateThread)
                    {
                        return false;
                    }
                    _elm327RequEvent.WaitOne(10, false);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private string Elm327ReceiveAnswer(int timeout, bool canData = false)
        {
            bool elmThread = _elm327Thread != null && Thread.CurrentThread == _elm327Thread;
            StringBuilder stringBuilder = new StringBuilder();
            long startTime = Stopwatch.GetTimestamp();
            for (;;)
            {
                while (DataAvailable())
                {
                    int data = _inStream.ReadByteAsync();
                    if (data >= 0 && data != 0x00)
                    {   // remove 0x00
                        if (canData)
                        {
                            if (data == '\r')
                            {
                                string answer = stringBuilder.ToString();
                                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN rec: {0}", answer);
                                return answer;
                            }
                            stringBuilder.Append(Convert.ToChar(data));
                        }
                        else
                        {
                            stringBuilder.Append(Convert.ToChar(data));
                        }
                        if (data == 0x3E)
                        {
                            _elm327DataMode = false;
                            if (canData)
                            {
                                Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM Data mode aborted");
                                return string.Empty;
                            }
                            string answer = stringBuilder.ToString();
                            Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CMD rec: {0}", answer);
                            return answer;
                        }
                    }
                }

                if ((Stopwatch.GetTimestamp() - startTime) > timeout * TickResolMs)
                {
                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM rec timeout");
                    return string.Empty;
                }
                if (elmThread)
                {
                    if (_elm327TerminateThread)
                    {
                        return string.Empty;
                    }
                    _elm327RequEvent.WaitOne(10, false);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private string Elm327DataCarlyAnswer(int timeout)
        {
            lock (_elm327BufferLock)
            {
                if (_elm327CarlyRespList != null && _elm327CarlyRespList.Count > 0)
                {
                    string answer = _elm327CarlyRespList[0];
                    _elm327CarlyRespList.RemoveAt(0);
                    if (_elm327CarlyRespList.Count == 0)
                    {
                        _elm327DataMode = false;
                        _elm327CarlyRespList = null;
                    }

                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN rec cached: {0}", answer);
                    return answer;
                }

                _elm327CarlyRespList = null;
            }

            try
            {
                bool elmThread = _elm327Thread != null && Thread.CurrentThread == _elm327Thread;
                int recTimeout = timeout;
                if (_elm327Timeout > 0)
                {
                    recTimeout = (_elm327Timeout * 4 * _elm327TimeoutMultiplier) + 200;
                }
                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM receive timeout: {0}", recTimeout);

                List<byte> recData = new List<byte>();
                StringBuilder recString = new StringBuilder();
                byte[] buffer = new byte[100];
                long startTime = Stopwatch.GetTimestamp();
                _elm327ReceiverBusy = true;

                for (; ; )
                {
                    while (DataAvailable())
                    {
                        int length = _inStream.Read(buffer, 0, buffer.Length);
                        if (length > 0)
                        {
                            startTime = Stopwatch.GetTimestamp();
                            if (!_elm327BinaryData)
                            {
                                bool finished = false;
                                for (int pos = 0; pos < length; pos++)
                                {
                                    switch (buffer[pos])
                                    {
                                        case (byte)'\n':
                                            break;

                                        case 0x3E:
                                            finished = true;
                                            break;

                                        default:
                                            recString.Append(Convert.ToChar(buffer[pos]));
                                            break;
                                    }
                                }

                                if (finished)
                                {
                                    string[] recArray = recString.ToString().Split('\r');
                                    List<string> recList = new List<string>();
                                    foreach (string line in recArray)
                                    {
                                        string trimmedLine = line.Trim();
                                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                                        {
                                            recList.Add(trimmedLine);
                                        }
                                    }

                                    if (recList.Count == 0)
                                    {
                                        _elm327DataMode = false;
                                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM CAN receive list empty");
                                        return string.Empty;
                                    }

                                    string answer = recList[0];
                                    recList.RemoveAt(0);
                                    if (recList.Count == 0)
                                    {
                                        _elm327DataMode = false;
                                    }
                                    else
                                    {
                                        lock (_elm327BufferLock)
                                        {
                                            _elm327CarlyRespList = recList;
                                        }
                                    }

                                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN rec: {0}", answer);
                                    return answer;
                                }

                                break;
                            }

                            if (length < 2)
                            {
                                break;
                            }

                            bool lastBlock = false;
                            switch (buffer[0])
                            {
                                case 0xBB:
                                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM rec bin length: {0}", length - 1);
                                    break;

                                case 0xBE:
                                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM rec bin last length: {0}", length - 1);
                                    lastBlock = true;
                                    break;

                                default:
                                    _elm327DataMode = false;
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM rec no binary data");
                                    return string.Empty;
                            }

                            recData.AddRange(buffer.ToList().GetRange(1, length - 1));

                            if (lastBlock)
                            {
                                List<string> recList = new List<string>();
                                int telLength = 10;
                                if (recData.Count % telLength != 0)
                                {
                                    _elm327DataMode = false;
                                    Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN data length invalid: {0}", recData.Count);
                                    return string.Empty;
                                }

                                int pos = 0;
                                for (; ; )
                                {
                                    if (pos >= recData.Count)
                                    {
                                        break;
                                    }

                                    if (recData[pos] != 0x06)
                                    {   // invalid CAN high byte
                                        _elm327DataMode = false;
                                        Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN data high byte invalid: {0:X02}", recData[pos]);
                                        return string.Empty;
                                    }

                                    if (pos + telLength > recData.Count)
                                    {
                                        break;
                                    }

                                    StringBuilder stringBuilder = new StringBuilder();
                                    int source = (recData[pos + 0] << 8) + recData[pos + 1];
                                    stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:X03}", source));

                                    for (int i = 2; i < telLength; i++)
                                    {
                                        stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0:X02}", recData[pos + i]));
                                    }

                                    recList.Add(stringBuilder.ToString());

                                    pos += telLength;
                                }

                                if (recList.Count == 0)
                                {
                                    _elm327DataMode = false;
                                    Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM CAN receive list empty");
                                    return string.Empty;
                                }

                                string answer = recList[0];
                                recList.RemoveAt(0);
                                if (recList.Count == 0)
                                {
                                    _elm327DataMode = false;
                                }
                                else
                                {
                                    lock (_elm327BufferLock)
                                    {
                                        _elm327CarlyRespList = recList;
                                    }
                                }

                                Ediabas?.LogFormat(EdiabasNet.EdLogLevel.Ifh, "ELM CAN rec: {0}", answer);
                                return answer;
                            }
                        }
                    }

                    if ((Stopwatch.GetTimestamp() - startTime) > recTimeout * TickResolMs)
                    {
                        _elm327DataMode = false;
                        Ediabas?.LogString(EdiabasNet.EdLogLevel.Ifh, "ELM rec timeout");
                        return string.Empty;
                    }

                    if (elmThread)
                    {
                        if (_elm327TerminateThread)
                        {
                            return string.Empty;
                        }
                        _elm327RequEvent.WaitOne(10, false);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            finally
            {
                _elm327ReceiverBusy = false;
            }
        }

        private void FlushReceiveBuffer()
        {
            _inStream.Flush();
            while (DataAvailable())
            {
                _inStream.ReadByteAsync();
            }
        }

        private bool DataAvailable()
        {
#if Android
            return _inStream.HasData();
#else
            if (!(_inStream is System.Net.Sockets.NetworkStream networkStream))
            {
                return false;
            }
            return networkStream.DataAvailable;
#endif
        }

        bool ReceiverBusy()
        {
            if (_elm327CarlyTransport)
            {
                if (_elm327ReceiverBusy)
                {
                    return true;
                }

                lock (_elm327BufferLock)
                {
                    if (_elm327RequQueue.Count > 0)
                    {
                        return true;
                    }

                    if (_elm327CarlyRespList != null && _elm327CarlyRespList.Count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        public static byte CalcChecksumBmwFast(byte[] data, int offset, int length)
        {
            byte sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += data[i + offset];
            }
            return sum;
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                InterfaceDisconnect();
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }
}
