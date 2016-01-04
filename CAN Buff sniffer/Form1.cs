using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.IO;

namespace CAN_Buff_sniffer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public class Ramec
        {
            /*
            * Class for store CAN Bus packet
            */
            public UInt16 ID; // ID of CAN Bus packet
            byte CTL; // len od Data
            public byte[] Data; // Data
            byte CRC; // CRC, not used
            DateTime Cas; // Time of arrive packet

            public Ramec(string Radek) // parse string from format "0x0000 01 02 03 04\r\n"
            {
                this.Cas = DateTime.Now; // Now arrived
                this.ID = Convert.ToUInt16(Radek.Substring(Radek.IndexOf("0x") + 2, 4), 16); // get ID, from hex
                Radek = Radek.Substring(7);
                byte i = 0;
                this.CTL = (byte)(Regex.Matches(Radek, " ").Count + 1); // get num of Data bytes
                this.Data = new byte[this.CTL];
                for (; i < this.CTL; i++) // parse data Bytes from hex
                {
                    this.Data[i] = Convert.ToByte(Radek.Substring(i * 3, 2), 16);
                }
            }

            public override string ToString() // override ToString to format "0x0000 XX ..."
            {
                string Retezec = "0x" + this.ID.ToString("X4") + "\t"; // ID of CAN Bus packet
                foreach (byte dato in this.Data) Retezec += dato.ToString("X2") + "\t"; // bin array to Hex (format "0A 0B 00")
                return Retezec.Substring(0, Retezec.Length - 1); // remove last space (tab) from foreach
            }

            public string ToStringTime() // the same as ToString but with time "03.01.2016 18:24:43	0x0571	6F  41  00  01  01  00"
            {
                string Retezec = this.Cas.ToShortDateString() + " " + this.Cas.ToLongTimeString() + "\t0x" + this.ID.ToString("X4") + "\t";
                foreach (byte dato in this.Data) Retezec += dato.ToString("X2") + "\t"; // bin array to Hex (format "0A 0B 00")
                return Retezec.Substring(0, Retezec.Length - 1); // remove last space (tab) from foreach
            }

            public string ToStringTimeMs() // the same as ToString but with time with ms "03.01.2016 18:24:43:122	0x0571	6F  41  00  01  01  00"
            {
                string Retezec = this.Cas.ToShortDateString() + " " + this.Cas.ToLongTimeString() + ":" + this.Cas.Millisecond + "\t0x" + this.ID.ToString("X4") + "\t";
                foreach (byte dato in this.Data) Retezec += dato.ToString("X2") + "\t"; // bin array to Hex (format "0A 0B 00")
                return Retezec.Substring(0, Retezec.Length - 1); // remove last space (tab) from foreach
            }
        }

        Thread vlakno;
        bool MaBezet = false; // should thread run?
        //List<Ramec> Ramce = new List<Ramec>(); // list for store CAN Bus packets, not used

        UInt16[] Whitelist = new UInt16[] {  }; // array of whitelisted CAN Bus packets
        UInt16[] Blacklist = new UInt16[] { 0x065F }; // array of blacklisted CAN Bus packets

        private void button1_Click(object sender, EventArgs e)
        {
            if (!MaBezet) // is running?
            {
                Properties.Settings.Default.Port = textBox1.Text;
                Properties.Settings.Default.UseWhiteList = UseWhiteList.Checked;
                Properties.Settings.Default.UseBlackList = UseBlackList.Checked;
                Properties.Settings.Default.LogWithTime = LogWithTime.Checked;
                Properties.Settings.Default.LogWithMs = LogWithMs.Checked;
                Properties.Settings.Default.Save(); // save all options to Settings
                MaBezet = true; // yes, it should run
                vlakno = new Thread(Vlakno);
                vlakno.Start(); // start thread
                OpenClose.Text = "Close"; // change state of button to "Close"
            }
            else
            {
                vlakno.Abort(); // kill thread
                OpenClose.Text = "Open"; // change state of button to "Close"
                MaBezet = false; // no, it should'n run
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // after start restore all optiong from Settings
            textBox1.Text = Properties.Settings.Default.Port;
            UseWhiteList.Checked = Properties.Settings.Default.UseWhiteList;
            UseBlackList.Checked = Properties.Settings.Default.UseBlackList;
            LogWithTime.Checked = Properties.Settings.Default.LogWithTime;
            LogWithMs.Checked = Properties.Settings.Default.LogWithMs;
        }

        string VIN = ""; // status string, now for VIN only

        bool FilterInsert(String Radek) // insert with filtering by WhiteList of BlackList if is enabled
        {
            Ramec ramec = new Ramec(Radek); // make new CAN Bus frame
            String retezec = "";
            if (ramec.ID == 0x065F) // mh to bude VIN (oh, it's VIN, it need's special work)
            {
                byte[] pole;
                switch (ramec.Data[0]) // first part of VIN
                {
                    case 0:
                        pole = ramec.Data.Skip(5).Take(3).ToArray(); // take 3 bytes from 6st byte
                        VIN = System.Text.Encoding.Default.GetString(pole);
                        break;
                    case 1:
                        pole = ramec.Data.Skip(1).Take(7).ToArray(); // take 7 bytes from firts byte
                        VIN = VIN.Substring(0, 3) + System.Text.Encoding.Default.GetString(pole); // replace midle part of VIN
                        break;
                    case 2:
                        pole = ramec.Data.Skip(1).Take(7).ToArray(); // the same
                        VIN = VIN.Substring(0, 3 + 7) + System.Text.Encoding.Default.GetString(pole); // replace last part of VIN

                        Status.Invoke((MethodInvoker)delegate () // show VIN on GUI
                        {
                            Status.Text = "VIN: " + VIN;
                        });
                        break;
                    default:
                        break;
                }
            }
            if (SouborRaw != null) // is file avaliable for writing?
            {
                // log all arrived CAN Bus data to file xxxRaw
                if (LogWithTime.Checked) retezec = (LogWithMs.Checked ? ramec.ToStringTimeMs() : ramec.ToStringTime()); // log to file, with time of without
                else retezec = Radek.Replace(" ", "\t");
                SouborRaw.WriteLine(retezec);
                SouborRaw.Flush();
            }
            if ((UseWhiteList.Checked && Whitelist.Contains(ramec.ID)) || // filter by WhiteList
                (UseBlackList.Checked && !Blacklist.Contains(ramec.ID)) || // filter by BlackList
                (!UseWhiteList.Checked && !UseBlackList.Checked)) // no filter selected
            {
                listBox1.Items.Add(ramec); // add item to listBox
                listBox1.SelectedIndex = listBox1.Items.Count - 1; // scroll to last item
                if (SouborFiltered != null) // is file avaliable for writing?
                {
                    // write filter data only
                    if (LogWithTime.Checked) retezec = (LogWithMs.Checked ? ramec.ToStringTimeMs() : ramec.ToStringTime());
                    else retezec = Radek.Replace(" ", "\t");
                    SouborFiltered.WriteLine(retezec);
                    SouborFiltered.Flush();
                }
            }
            return false;
        }

        SerialPort COM;
        StreamWriter SouborRaw;
        StreamWriter SouborFiltered;

        void OpenFiles(String Name) // open Files with user defined name (one for Raw data and second for Filtered data)
        {
            try
            {
                SouborRaw = new StreamWriter(Name + "Raw.csv", true); // append is enabled
            }
            catch (Exception ex)
            { }
            try
            {
                SouborFiltered = new StreamWriter(Name + ".csv", true); // append is enabled
            }
            catch (Exception ex) // ops, something is wrong
            { }
        }

        void OpenFiles() // open Files with predefined name Data.csv and DataRaw.csv 
        {
            OpenFiles("Data");
        }

        void Vlakno()
        {
            // main work thread
            while (MaBezet)
            {
                try
                {
                    COM = new SerialPort(textBox1.Text); // get user defined COM port name
                    COM.NewLine = "\r\n"; // define string of end of line (\r and \n)
                    try
                    {
                        COM.Open(); // try to open serial port
                    }
                    catch (Exception ex) // problem with opening serial port
                    {
                        MessageBox.Show("Problém s otevřením sériového portu. " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    if (FileName.Text == "") OpenFiles(); // open output log files
                    else OpenFiles(FileName.Text);

                    while (MaBezet)
                    {
                        try
                        {
                            string Radek = COM.ReadLine();
                            //Ramce.Add(new Ramec(Radek));
                            listBox1.Invoke((MethodInvoker)delegate () // insert arrived data to GUI from thread
                            {
                                FilterInsert(Radek);
                            });
                        }
                        catch (TimeoutException ex) // no ne line, wait for next time
                        { }
                        catch (Exception ex) // another exception
                        { }
                    }
                }
                catch (ThreadAbortException) // end of thread
                { }
                //catch ()
                catch (Exception ex) // other error
                {
                    string Err = ex.Message + "\r\n\r\n" + ex.StackTrace; // show message with StackTrace
                }
            }
            OpenClose.Invoke((MethodInvoker)delegate () // change state of button
            {
                OpenClose.Text = "Open";
            });
            if ((COM != null) || COM.IsOpen) COM.Close(); // if is serial port open, than close it
            if (SouborRaw !=null) SouborRaw.Close(); // if is file used, than close it
            if (SouborFiltered != null) SouborFiltered.Close(); // if is file used, than close it
            MaBezet = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Few test CAN Bus packets
            String[] StrRamcu = new string[]
            {
                "0x02C3 11",
                "0x060E 08 01",
                "0xFFFF 00 01 02",
                "0x042B 19 01 00 00 00 00",
                "0x0000 00 01 02 03 04 05 06 07",
                "0x065F 00 00 00 00 00 54 4D 42",  // VIN, first part
                "0x065F 01 42 00 00 00 00 00 35", // next part of VIN
                "0x065F 02 32 00 00 00 00 00 00" // and last part of VIN

            };
            if (FileName.Text == "") OpenFiles(); // open files
            else OpenFiles(FileName.Text);
            Whitelist = new UInt16[] { 0xFFFF }; // fill WhiteList by 0xFFFF (only 0xFFFF will by listed)
            Blacklist = new UInt16[] { 0x2C3 }; // fill BlackList by 0x2C3 (only 0x2C3 will be ignored)
            for (int i = 0; i < 10; i++) // test fill of data
            {
                foreach (string Radek in StrRamcu)
                {
                    FilterInsert(Radek);
                    Ramec ramec = new Ramec(Radek);
                    string debug = ramec.ToStringTime();
                    Thread.Sleep(100); // slower, for GUI test
                }
            }//*/

            //listBox1.Items.Add(new Ramec(Radek));//Ramce.Add(new Ramec(Str));
        }
    }
}
