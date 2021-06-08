using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Timers;

namespace USBCommTest
{
    public class SensorControlUnit
    {
        private static SerialPort wSensorControlUnitPort;
        public string SensorControlUnitSerial { get; private set; }
        public int WaitingDataReceived { get; set; }

        public SensorControlUnit()
        {
            SensorControlUnitPort = SensorControlUnitPort ?? new SerialPort();
            WaitingDataReceived = 0;
            waitingToClose = false;
        }

        private SerialPort SensorControlUnitPort
        {
            get
            {
                wSensorControlUnitPort = wSensorControlUnitPort ?? new SerialPort();
                return wSensorControlUnitPort;
            }
            set => wSensorControlUnitPort = value;
        }
        public string LastResp { get; private set; } = string.Empty;

        public bool OpenSensorControlUnit()
        {
            try
            {
                if (SensorControlUnitPort.IsOpen)
                {
                    SensorControlUnitPort.DiscardInBuffer();
                    SensorControlUnitPort.DiscardOutBuffer();
                    return true;
                }

                SensorControlUnitPort.PortName = PortName;
                SensorControlUnitPort.BaudRate = 9600;
                SensorControlUnitPort.DataBits = 8;
                SensorControlUnitPort.StopBits = StopBits.One;
                SensorControlUnitPort.Parity = Parity.None;
                SensorControlUnitPort.ReadTimeout = 2000;
                SensorControlUnitPort.WriteTimeout = 2000;

                SensorControlUnitPort.Open();
            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(500);
                return OpenSensorControlUnit();
            }
            return true;
        }

        private bool timeout { get; set; }
        private bool waitingToClose { get; set; }
        public string PortName { get; set; }

        public bool CloseSensorControlUnit()
        {
            if (waitingToClose) return false;
            waitingToClose = true;
            var timer = new Timer(SensorControlUnitPort.ReadTimeout);
            timer.Elapsed += new ElapsedEventHandler(IsReadTimeout_Elapsed);
            timer.Start();
            timeout = false;
            while (WaitingDataReceived > 0 && !timeout)
            {
                //Do the thread wait for the process to finish or timeout.
                //So we don't close the reader while still in use.
            }
            waitingToClose = false;
            timer.Stop();
            timer.Dispose();
            try
            {
                if (SensorControlUnitPort.PortName == PortName)
                {
                    if (SensorControlUnitPort.IsOpen)
                    {
                        SensorControlUnitPort.Close();
                        SensorControlUnitPort.Dispose();
                        WaitingDataReceived = 0;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                SensorControlUnitPort.Dispose();
                WaitingDataReceived = 0;
                return true;
                //TODO Show the user a error message or stop the process.
            }
        }
        public void ForceCloseSensorControlUnit()
        {
            if (SensorControlUnitPort.IsOpen)
            {
                SensorControlUnitPort.Close();
            }
            System.Threading.Thread.Sleep(500);
        }
        private void IsReadTimeout_Elapsed(object sender, ElapsedEventArgs e)
        {
            timeout = true;
        }

        public enum BeepLEDColorCode : byte
        {
            None = 0x00,
            Beep = 0x01,
            Red = 0x02,
            Green = 0x04,
            Yellow = 0x08,
            Blue = 0x10,
            Ready = 0x20
        }

        public enum BeepLength : byte
        {
            Short = 0x19,
            Normal = 0x32,
            Long = 0x64
        }

        public void Beep()
        {
            Beep((byte)BeepLEDColorCode.Green ^ (byte)BeepLEDColorCode.Beep, (byte)BeepLength.Short);
        }

        private void Beep(byte LEDColor, byte beepLength)
        {

            try
            {
                if (!SensorControlUnitPort.IsOpen || SensorControlUnitPort == null)
                {
                    OpenSensorControlUnit();
                }
                var bytes = new byte[] { 0x02, 0x91, (byte)LEDColor, (byte)beepLength };

                SensorControlUnitPort.DataReceived += Beep_DataReceived;
                WaitingDataReceived++;
                SensorControlUnitPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                LastResp = ex.Message;
                throw;
            }
        }

        private void Beep_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            WaitingDataReceived--;
            var buffer = ReadSerialPortRespose();
            if (buffer != null)
            {
                LastResp = string.Join("", buffer.Select(x => x.ToString("X"))) == "2910" ? "BEEEEP!" : "Error on Beep";
            }
            SensorControlUnitPort.DataReceived -= Beep_DataReceived;
            CloseSensorControlUnit();
        }

        private byte[] ReadSerialPortRespose()
        {
            if (SensorControlUnitPort.IsOpen)
            {
                var bufferSize = SensorControlUnitPort.BytesToRead;
                var buffer = new byte[bufferSize];
                SensorControlUnitPort.Read(buffer, 0, bufferSize);
                return buffer;
            }
            return null;
        }
        public void SendReadySignal()
        {
            SendWithResponse(new byte[] { (byte)BeepLEDColorCode.Ready });
        }
        public void SendErrorSignal()
        {
            SendWithResponse(new byte[] { (byte)BeepLEDColorCode.Red });
        }
        public void SendSuccessSignal()
        {
            SendWithResponse(new byte[] { (byte)BeepLEDColorCode.Green });
        }

        public void SendWithResponse(byte[] bytesToSend)
        {
            try
            {
                if (!SensorControlUnitPort.IsOpen || SensorControlUnitPort == null)
                {
                    OpenSensorControlUnit();
                }
                WaitingDataReceived++;
                SensorControlUnitSerial = "";
                SensorControlUnitPort.DataReceived += SensorControlUnitSerial_DataReceived;
                SensorControlUnitPort.Write(bytesToSend, 0, bytesToSend.Length);
                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                LastResp = ex.Message;
            }
        }
        private void SensorControlUnitSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!SensorControlUnitPort.IsOpen || SensorControlUnitPort == null)
                {
                    OpenSensorControlUnit();
                }

                var buffer = ReadSerialPortRespose();
                if (buffer != null)
                {
                    var readerSerial = string.Join("", buffer.Select(x => char.ConvertFromUtf32(x)));
                    LastResp = readerSerial;
                    SensorControlUnitSerial = readerSerial;
                }
                SensorControlUnitPort.DataReceived -= SensorControlUnitSerial_DataReceived;
                WaitingDataReceived--;
                CloseSensorControlUnit();
            }
            catch (Exception ex)
            {
                LastResp = ex.Message;
            }
        }

        public string SendCmd(string cmd)
        {
            cmd += Convert.ToChar(13);
            if (!SensorControlUnitPort.IsOpen)
            {
                LastResp = "Port Not Open";
                return LastResp;
            }
            try
            {
                SensorControlUnitPort.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                LastResp = ex.Message;
                return LastResp;
            }
            try
            {
                LastResp = SensorControlUnitPort.ReadLine();
                LastResp += SensorControlUnitPort.ReadLine();
                LastResp = LastResp.Replace("\r", string.Empty);
                return LastResp;
            }
            catch (Exception ex)
            {
                LastResp = ex.Message;
                return LastResp;
            }
        }

    }
}