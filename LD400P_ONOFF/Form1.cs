using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace LD400P_ONOFF
{
    public partial class Form1 : Form
    {
        // LD400P Komponenten
        private ComboBox cmbPortsLD400P;
        private ComboBox cmbBaudRateLD400P;
        private Button btnConnectLD400P;
        private Button btnDisconnectLD400P;
        private Button btnLoadOn;
        private Button btnLoadOff;
        private Label lblPortLabelLD400P;
        private Label lblBaudLabelLD400P;
        private Label lblStatusLD400P;
        private GroupBox grpLD400P;
        private GroupBox grpControlLD400P;
        private SerialPort serialPortLD400P;
        private bool isConnectedLD400P = false;

        // CPX400DP Komponenten
        private ComboBox cmbPortsCPX400DP;
        private ComboBox cmbBaudRateCPX400DP;
        private Button btnConnectCPX400DP;
        private Button btnDisconnectCPX400DP;
        private Button btnOutputOn;
        private Button btnOutputOff;
        private Label lblPortLabelCPX400DP;
        private Label lblBaudLabelCPX400DP;
        private Label lblStatusCPX400DP;
        private GroupBox grpCPX400DP;
        private GroupBox grpControlCPX400DP;
        private SerialPort serialPortCPX400DP;
        private bool isConnectedCPX400DP = false;

        // Zyklus Komponenten
        private GroupBox grpCycle;
        private TextBox txtTime1;
        private TextBox txtTime2;
        private TextBox txtTime3;
        private TextBox txtTime4;
        private Label lblTime1;
        private Label lblTime2;
        private Label lblTime3;
        private Label lblTime4;
        private RadioButton rbSeconds;
        private RadioButton rbMinutes;
        private Button btnStartCycle;
        private Button btnStopCycle;
        private Label lblCycleStatus;
        private Label lblCycleCount;
        private System.Windows.Forms.Timer cycleTimer;
        private bool cycleRunning = false;
        private int cycleState = 0; // 0=warte, 1=CPX ein, 2=pause1, 3=LD ein, 4=pause2
        private int remainingTime = 0;
        private int cycleCounter = 0;

        // Start-Phase Auswahl
        private Label lblStartPhase;
        private ComboBox cmbStartPhase;

        // Messwerte / SOH / CSV Export
        private GroupBox grpMeasurements;
        private Label lblVoltage;
        private Label lblCurrent;
        private Label lblPassmAh;
        private Label lblTimeElapsedLabel;
        private Label lblTimeElapsedValue;
        private Label lblNominalCapacityLabel;
        private TextBox txtNominalCapacity;
        private Label lblSOHLabel;
        private Label lblSOHValue;
        private Button btnExportCSV;

        private double currentAmps = 0.0;
        private double voltageVolts = 0.0;
        private double currentPass_mAh = 0.0; // mAh consumed in the current pass
        private double total_mAh = 0.0; // total since app start (optional)
        private DateTime passStartTime = DateTime.MinValue;
        private TimeSpan passElapsed => passStartTime == DateTime.MinValue ? TimeSpan.Zero : (DateTime.Now - passStartTime);

        // Store only one CSV row per completed pass (summary)
        private List<CycleDataEntry> csvData = new List<CycleDataEntry>();

        // Gemeinsame Komponenten
        private TextBox txtLog;
        private Label lblLogLabel;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            LoadAvailablePorts();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "LD400P & CPX400DP Steuerung";
            // Wider window so log can be on the right
            this.Size = new Size(1000, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // ===== LD400P Gruppe =====
            grpLD400P = new GroupBox
            {
                Text = "LD400P (Elektronische Last)",
                Location = new Point(10, 10),
                Size = new Size(480, 150)
            };

            lblPortLabelLD400P = new Label
            {
                Text = "COM-Port:",
                Location = new Point(15, 25),
                Size = new Size(80, 20)
            };

            cmbPortsLD400P = new ComboBox
            {
                Location = new Point(100, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lblBaudLabelLD400P = new Label
            {
                Text = "Baudrate:",
                Location = new Point(230, 25),
                Size = new Size(80, 20)
            };

            cmbBaudRateLD400P = new ComboBox
            {
                Location = new Point(310, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBaudRateLD400P.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            cmbBaudRateLD400P.SelectedIndex = 0;

            btnConnectLD400P = new Button
            {
                Text = "Verbinden",
                Location = new Point(100, 55),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnConnectLD400P.Click += BtnConnectLD400P_Click;

            btnDisconnectLD400P = new Button
            {
                Text = "Trennen",
                Location = new Point(240, 55),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false,
                Visible = false
            };
            btnDisconnectLD400P.Click += BtnDisconnectLD400P_Click;

            lblStatusLD400P = new Label
            {
                Text = "Status: Nicht verbunden",
                Location = new Point(15, 95),
                Size = new Size(450, 20),
                ForeColor = Color.Red
            };

            grpControlLD400P = new GroupBox
            {
                Text = "Last Steuerung",
                Location = new Point(15, 115),
                Size = new Size(450, 28),
                Enabled = false
            };

            btnLoadOn = new Button
            {
                Text = "Last EIN",
                Location = new Point(10, 16),
                Size = new Size(200, 28),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnLoadOn.Click += BtnLoadOn_Click;

            btnLoadOff = new Button
            {
                Text = "Last AUS",
                Location = new Point(235, 16),
                Size = new Size(200, 28),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnLoadOff.Click += BtnLoadOff_Click;

            grpControlLD400P.Controls.Add(btnLoadOn);
            grpControlLD400P.Controls.Add(btnLoadOff);

            grpLD400P.Controls.Add(lblPortLabelLD400P);
            grpLD400P.Controls.Add(cmbPortsLD400P);
            grpLD400P.Controls.Add(lblBaudLabelLD400P);
            grpLD400P.Controls.Add(cmbBaudRateLD400P);
            grpLD400P.Controls.Add(btnConnectLD400P);
            grpLD400P.Controls.Add(btnDisconnectLD400P);
            grpLD400P.Controls.Add(lblStatusLD400P);
            grpLD400P.Controls.Add(grpControlLD400P);

            // ===== CPX400DP Gruppe =====
            grpCPX400DP = new GroupBox
            {
                Text = "CPX400DP (Netzteil)",
                Location = new Point(10, 170),
                Size = new Size(480, 150)
            };

            lblPortLabelCPX400DP = new Label
            {
                Text = "COM-Port:",
                Location = new Point(15, 25),
                Size = new Size(80, 20)
            };

            cmbPortsCPX400DP = new ComboBox
            {
                Location = new Point(100, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lblBaudLabelCPX400DP = new Label
            {
                Text = "Baudrate:",
                Location = new Point(230, 25),
                Size = new Size(80, 20)
            };

            cmbBaudRateCPX400DP = new ComboBox
            {
                Location = new Point(310, 23),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBaudRateCPX400DP.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            cmbBaudRateCPX400DP.SelectedIndex = 0;

            btnConnectCPX400DP = new Button
            {
                Text = "Verbinden",
                Location = new Point(100, 55),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnConnectCPX400DP.Click += BtnConnectCPX400DP_Click;

            btnDisconnectCPX400DP = new Button
            {
                Text = "Trennen",
                Location = new Point(240, 55),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false,
                Visible = false
            };
            btnDisconnectCPX400DP.Click += BtnDisconnectCPX400DP_Click;

            lblStatusCPX400DP = new Label
            {
                Text = "Status: Nicht verbunden",
                Location = new Point(15, 95),
                Size = new Size(450, 20),
                ForeColor = Color.Red
            };

            grpControlCPX400DP = new GroupBox
            {
                Text = "Ausgang 1 Steuerung",
                Location = new Point(15, 115),
                Size = new Size(450, 28),
                Enabled = false
            };

            btnOutputOn = new Button
            {
                Text = "Ausgang 1 EIN",
                Location = new Point(10, 16),
                Size = new Size(200, 28),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnOutputOn.Click += BtnOutputOn_Click;

            btnOutputOff = new Button
            {
                Text = "Ausgang 1 AUS",
                Location = new Point(235, 16),
                Size = new Size(200, 28),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnOutputOff.Click += BtnOutputOff_Click;

            grpControlCPX400DP.Controls.Add(btnOutputOn);
            grpControlCPX400DP.Controls.Add(btnOutputOff);

            grpCPX400DP.Controls.Add(lblPortLabelCPX400DP);
            grpCPX400DP.Controls.Add(cmbPortsCPX400DP);
            grpCPX400DP.Controls.Add(lblBaudLabelCPX400DP);
            grpCPX400DP.Controls.Add(cmbBaudRateCPX400DP);
            grpCPX400DP.Controls.Add(btnConnectCPX400DP);
            grpCPX400DP.Controls.Add(btnDisconnectCPX400DP);
            grpCPX400DP.Controls.Add(lblStatusCPX400DP);
            grpCPX400DP.Controls.Add(grpControlCPX400DP);

            // ===== Zyklus Gruppe =====
            grpCycle = new GroupBox
            {
                Text = "Automatischer Zyklus",
                Location = new Point(10, 330),
                Size = new Size(480, 240),
                Enabled = false
            };

            lblTime1 = new Label
            {
                Text = "Zeit 1 (CPX Ein):",
                Location = new Point(15, 25),
                Size = new Size(120, 20)
            };

            txtTime1 = new TextBox
            {
                Location = new Point(140, 23),
                Size = new Size(60, 25),
                Text = "10"
            };

            lblTime2 = new Label
            {
                Text = "Zeit 2 (Pause):",
                Location = new Point(220, 25),
                Size = new Size(120, 20)
            };

            txtTime2 = new TextBox
            {
                Location = new Point(340, 23),
                Size = new Size(60, 25),
                Text = "5"
            };

            lblTime3 = new Label
            {
                Text = "Zeit 3 (LD Ein):",
                Location = new Point(15, 55),
                Size = new Size(120, 20)
            };

            txtTime3 = new TextBox
            {
                Location = new Point(140, 53),
                Size = new Size(60, 25),
                Text = "15"
            };

            lblTime4 = new Label
            {
                Text = "Zeit 4 (Pause):",
                Location = new Point(220, 55),
                Size = new Size(120, 20)
            };

            txtTime4 = new TextBox
            {
                Location = new Point(340, 53),
                Size = new Size(60, 25),
                Text = "5"
            };

            rbSeconds = new RadioButton
            {
                Text = "Sekunden",
                Location = new Point(15, 90),
                Size = new Size(100, 25),
                Checked = true
            };

            rbMinutes = new RadioButton
            {
                Text = "Minuten",
                Location = new Point(120, 90),
                Size = new Size(100, 25)
            };

            lblStartPhase = new Label
            {
                Text = "Start-Phase:",
                Location = new Point(15, 125),
                Size = new Size(120, 20)
            };

            cmbStartPhase = new ComboBox
            {
                Location = new Point(140, 122),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStartPhase.Items.AddRange(new object[]
            {
                "Phase 1 (CPX EIN)",
                "Phase 2 (Pause)",
                "Phase 3 (LD EIN)",
                "Phase 4 (Pause)"
            });
            cmbStartPhase.SelectedIndex = 0;

            btnStartCycle = new Button
            {
                Text = "Starte Zyklus",
                Location = new Point(15, 160),
                Size = new Size(220, 40),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnStartCycle.Click += BtnStartCycle_Click;

            btnStopCycle = new Button
            {
                Text = "Stopp Zyklus",
                Location = new Point(255, 160),
                Size = new Size(220, 40),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Enabled = false
            };
            btnStopCycle.Click += BtnStopCycle_Click;

            lblCycleStatus = new Label
            {
                Text = "Zyklus bereit",
                Location = new Point(15, 210),
                Size = new Size(450, 20),
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            lblCycleCount = new Label
            {
                Text = "Durchläufe: 0",
                Location = new Point(15, 230),
                Size = new Size(450, 20),
                Font = new Font("Arial", 9),
                ForeColor = Color.Black
            };

            grpCycle.Controls.Add(lblTime1);
            grpCycle.Controls.Add(txtTime1);
            grpCycle.Controls.Add(lblTime2);
            grpCycle.Controls.Add(txtTime2);
            grpCycle.Controls.Add(lblTime3);
            grpCycle.Controls.Add(txtTime3);
            grpCycle.Controls.Add(lblTime4);
            grpCycle.Controls.Add(txtTime4);
            grpCycle.Controls.Add(rbSeconds);
            grpCycle.Controls.Add(rbMinutes);
            grpCycle.Controls.Add(lblStartPhase);
            grpCycle.Controls.Add(cmbStartPhase);
            grpCycle.Controls.Add(btnStartCycle);
            grpCycle.Controls.Add(btnStopCycle);
            grpCycle.Controls.Add(lblCycleStatus);
            grpCycle.Controls.Add(lblCycleCount);

            // ===== Messwerte / SOH Gruppe =====
            grpMeasurements = new GroupBox
            {
                Text = "Messwerte & SOH",
                Location = new Point(10, 580),
                Size = new Size(480, 140),
                Enabled = true
            };

            lblVoltage = new Label
            {
                Text = "Spannung (V): --",
                Location = new Point(15, 25),
                Size = new Size(200, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblCurrent = new Label
            {
                Text = "Strom (A): --",
                Location = new Point(250, 25),
                Size = new Size(200, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblPassmAh = new Label
            {
                Text = "Aktuell entnommene mAh (pass): 0.00",
                Location = new Point(15, 50),
                Size = new Size(450, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblTimeElapsedLabel = new Label
            {
                Text = "Verstrichene Zeit (pass):",
                Location = new Point(15, 75),
                Size = new Size(160, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblTimeElapsedValue = new Label
            {
                Text = "00:00:00",
                Location = new Point(180, 75),
                Size = new Size(120, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblNominalCapacityLabel = new Label
            {
                Text = "Nennkapazität (mAh):",
                Location = new Point(15, 100),
                Size = new Size(140, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            txtNominalCapacity = new TextBox
            {
                Text = "1000",
                Location = new Point(160, 98),
                Size = new Size(80, 22)
            };

            lblSOHLabel = new Label
            {
                Text = "SOH (%):",
                Location = new Point(260, 100),
                Size = new Size(60, 20),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            lblSOHValue = new Label
            {
                Text = "--",
                Location = new Point(320, 100),
                Size = new Size(100, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            // Export-Button unter Nennkapazität (sichtbar)
            btnExportCSV = new Button
            {
                Text = "Export CSV",
                Location = new Point(400, 96),
                Size = new Size(70, 26),
                BackColor = Color.LightSkyBlue
            };
            btnExportCSV.Click += BtnExportCSV_Click;

            grpMeasurements.Controls.Add(lblVoltage);
            grpMeasurements.Controls.Add(lblCurrent);
            grpMeasurements.Controls.Add(lblPassmAh);
            grpMeasurements.Controls.Add(lblTimeElapsedLabel);
            grpMeasurements.Controls.Add(lblTimeElapsedValue);
            grpMeasurements.Controls.Add(lblNominalCapacityLabel);
            grpMeasurements.Controls.Add(txtNominalCapacity);
            grpMeasurements.Controls.Add(lblSOHLabel);
            grpMeasurements.Controls.Add(lblSOHValue);
            grpMeasurements.Controls.Add(btnExportCSV);

            // ===== Log Bereich (rechts) =====
            lblLogLabel = new Label
            {
                Text = "Protokoll:",
                Location = new Point(500, 10),
                Size = new Size(100, 20),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            txtLog = new TextBox
            {
                Location = new Point(500, 35),
                Size = new Size(470, 660),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8)
            };

            // Komponenten zum Form hinzufügen (links column and right log)
            this.Controls.Add(grpLD400P);
            this.Controls.Add(grpCPX400DP);
            this.Controls.Add(grpCycle);
            this.Controls.Add(grpMeasurements);
            this.Controls.Add(lblLogLabel);
            this.Controls.Add(txtLog);

            // SerialPorts initialisieren
            serialPortLD400P = new SerialPort
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500,
                NewLine = "\r\n"
            };

            serialPortCPX400DP = new SerialPort
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
                NewLine = "\r\n"
            };

            // Timer initialisieren
            cycleTimer = new System.Windows.Forms.Timer();
            cycleTimer.Interval = 1000; // 1 Sekunde
            cycleTimer.Tick += CycleTimer_Tick;

            this.FormClosing += Form1_FormClosing;
        }

        private void LoadAvailablePorts()
        {
            string[] ports = SerialPort.GetPortNames();

            cmbPortsLD400P.Items.Clear();
            cmbPortsCPX400DP.Items.Clear();

            if (ports.Length > 0)
            {
                cmbPortsLD400P.Items.AddRange(ports);
                cmbPortsCPX400DP.Items.AddRange(ports);

                cmbPortsLD400P.SelectedIndex = 0;
                cmbPortsCPX400DP.SelectedIndex = ports.Length > 1 ? 1 : 0;
            }
            else
            {
                MessageBox.Show("Keine COM-Ports gefunden!", "Warnung",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CheckCycleAvailability()
        {
            grpCycle.Enabled = isConnectedLD400P && isConnectedCPX400DP;
        }

        // ===== LD400P Ereignisse =====
        private void BtnConnectLD400P_Click(object sender, EventArgs e)
        {
            if (cmbPortsLD400P.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie einen COM-Port aus!", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                serialPortLD400P.PortName = cmbPortsLD400P.SelectedItem.ToString();
                serialPortLD400P.BaudRate = int.Parse(cmbBaudRateLD400P.SelectedItem.ToString());
                serialPortLD400P.Open();

                isConnectedLD400P = true;
                UpdateConnectionStatusLD400P(true);
                LogMessage($"[LD400P] Verbunden mit {serialPortLD400P.PortName} @ {serialPortLD400P.BaudRate} Baud");

                // Device identification (may or may not respond)
                SendCommandLD400P("*IDN?");
                CheckCycleAvailability();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LD400P Verbindungsfehler: {ex.Message}", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"[LD400P] FEHLER: {ex.Message}");
            }
        }

        private void BtnDisconnectLD400P_Click(object sender, EventArgs e)
        {
            if (cycleRunning)
            {
                MessageBox.Show("Bitte stoppen Sie zuerst den Zyklus!", "Warnung",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DisconnectLD400P();
        }

        private void DisconnectLD400P()
        {
            if (serialPortLD400P != null && serialPortLD400P.IsOpen)
            {
                try
                {
                    SendCommandLD400P("INP 0");
                    Thread.Sleep(100);
                    serialPortLD400P.Close();
                    LogMessage("[LD400P] Verbindung getrennt");
                }
                catch (Exception ex)
                {
                    LogMessage($"[LD400P] Fehler beim Trennen: {ex.Message}");
                }
            }
            isConnectedLD400P = false;
            UpdateConnectionStatusLD400P(false);
            CheckCycleAvailability();
        }

        private void BtnLoadOn_Click(object sender, EventArgs e)
        {
            SendCommandLD400P("INP 1");
            LogMessage("[LD400P] Befehl gesendet: Last EIN");
        }

        private void BtnLoadOff_Click(object sender, EventArgs e)
        {
            SendCommandLD400P("INP 0");
            LogMessage("[LD400P] Befehl gesendet: Last AUS");
        }

        private void SendCommandLD400P(string command)
        {
            if (!isConnectedLD400P || !serialPortLD400P.IsOpen)
            {
                LogMessage("[LD400P] FEHLER: Keine Verbindung zum Gerät");
                return;
            }

            try
            {
                serialPortLD400P.WriteLine(command);
                LogMessage($"[LD400P] TX: {command}");

                Thread.Sleep(80);

                if (serialPortLD400P.BytesToRead > 0)
                {
                    try
                    {
                        string response = serialPortLD400P.ReadExisting();
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            LogMessage($"[LD400P] RX: {response.Trim()}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[LD400P] FEHLER beim Senden: {ex.Message}");
            }
        }

        /// <summary>
        /// Sendet ein Query an die LD400P und versucht die Antwort zurückzugeben.
        /// </summary>
        private string QueryLD400P(string query, int waitMs = 120)
        {
            if (!isConnectedLD400P || serialPortLD400P == null || !serialPortLD400P.IsOpen)
            {
                return null;
            }

            try
            {
                serialPortLD400P.DiscardInBuffer();
                serialPortLD400P.WriteLine(query);
                Thread.Sleep(waitMs);

                string resp = serialPortLD400P.ReadExisting();
                if (string.IsNullOrWhiteSpace(resp)) return null;
                return resp.Trim();
            }
            catch (Exception ex)
            {
                LogMessage($"[LD400P] QUERY Fehler: {ex.Message}");
                return null;
            }
        }

        private void UpdateConnectionStatusLD400P(bool connected)
        {
            if (connected)
            {
                lblStatusLD400P.Text = $"Status: Verbunden mit {serialPortLD400P.PortName}";
                lblStatusLD400P.ForeColor = Color.Green;
                btnConnectLD400P.Visible = false;
                btnDisconnectLD400P.Visible = true;
                btnDisconnectLD400P.Enabled = true;
                grpControlLD400P.Enabled = true;
                cmbPortsLD400P.Enabled = false;
                cmbBaudRateLD400P.Enabled = false;
            }
            else
            {
                lblStatusLD400P.Text = "Status: Nicht verbunden";
                lblStatusLD400P.ForeColor = Color.Red;
                btnConnectLD400P.Visible = true;
                btnDisconnectLD400P.Visible = false;
                grpControlLD400P.Enabled = false;
                cmbPortsLD400P.Enabled = true;
                cmbBaudRateLD400P.Enabled = true;
            }
        }

        // ===== CPX400DP Ereignisse =====
        private void BtnConnectCPX400DP_Click(object sender, EventArgs e)
        {
            if (cmbPortsCPX400DP.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie einen COM-Port aus!", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                serialPortCPX400DP.PortName = cmbPortsCPX400DP.SelectedItem.ToString();
                serialPortCPX400DP.BaudRate = int.Parse(cmbBaudRateCPX400DP.SelectedItem.ToString());
                serialPortCPX400DP.Open();

                isConnectedCPX400DP = true;
                UpdateConnectionStatusCPX400DP(true);
                LogMessage($"[CPX400DP] Verbunden mit {serialPortCPX400DP.PortName} @ {serialPortCPX400DP.BaudRate} Baud");

                SendCommandCPX400DP("*IDN?");
                CheckCycleAvailability();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CPX400DP Verbindungsfehler: {ex.Message}", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"[CPX400DP] FEHLER: {ex.Message}");
            }
        }

        private void BtnDisconnectCPX400DP_Click(object sender, EventArgs e)
        {
            if (cycleRunning)
            {
                MessageBox.Show("Bitte stoppen Sie zuerst den Zyklus!", "Warnung",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DisconnectCPX400DP();
        }

        private void DisconnectCPX400DP()
        {
            if (serialPortCPX400DP != null && serialPortCPX400DP.IsOpen)
            {
                try
                {
                    SendCommandCPX400DP("OP1 0");
                    Thread.Sleep(100);
                    serialPortCPX400DP.Close();
                    LogMessage("[CPX400DP] Verbindung getrennt");
                }
                catch (Exception ex)
                {
                    LogMessage($"[CPX400DP] Fehler beim Trennen: {ex.Message}");
                }
            }
            isConnectedCPX400DP = false;
            UpdateConnectionStatusCPX400DP(false);
            CheckCycleAvailability();
        }

        private void BtnOutputOn_Click(object sender, EventArgs e)
        {
            SendCommandCPX400DP("OP1 1");
            LogMessage("[CPX400DP] Befehl gesendet: Ausgang 1 EIN");
        }

        private void BtnOutputOff_Click(object sender, EventArgs e)
        {
            SendCommandCPX400DP("OP1 0");
            LogMessage("[CPX400DP] Befehl gesendet: Ausgang 1 AUS");
        }

        private void SendCommandCPX400DP(string command)
        {
            if (!isConnectedCPX400DP || !serialPortCPX400DP.IsOpen)
            {
                LogMessage("[CPX400DP] FEHLER: Keine Verbindung zum Gerät");
                return;
            }

            try
            {
                string commandWithTerminator = command + "\r\n";
                serialPortCPX400DP.WriteLine(commandWithTerminator.TrimEnd());
                LogMessage($"[CPX400DP] TX: {command}");

                Thread.Sleep(100);

                if (serialPortCPX400DP.BytesToRead > 0)
                {
                    try
                    {
                        string response = serialPortCPX400DP.ReadExisting();
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            LogMessage($"[CPX400DP] RX: {response.Trim()}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[CPX400DP] FEHLER beim Senden: {ex.Message}");
            }
        }

        private void UpdateConnectionStatusCPX400DP(bool connected)
        {
            if (connected)
            {
                lblStatusCPX400DP.Text = $"Status: Verbunden mit {serialPortCPX400DP.PortName}";
                lblStatusCPX400DP.ForeColor = Color.Green;
                btnConnectCPX400DP.Visible = false;
                btnDisconnectCPX400DP.Visible = true;
                btnDisconnectCPX400DP.Enabled = true;
                grpControlCPX400DP.Enabled = true;
                cmbPortsCPX400DP.Enabled = false;
                cmbBaudRateCPX400DP.Enabled = false;
            }
            else
            {
                lblStatusCPX400DP.Text = "Status: Nicht verbunden";
                lblStatusCPX400DP.ForeColor = Color.Red;
                btnConnectCPX400DP.Visible = true;
                btnDisconnectCPX400DP.Visible = false;
                grpControlCPX400DP.Enabled = false;
                cmbPortsCPX400DP.Enabled = true;
                cmbBaudRateCPX400DP.Enabled = true;
            }
        }

        // ===== Zyklus Ereignisse =====
        private void BtnStartCycle_Click(object sender, EventArgs e)
        {
            // Validierung der Eingaben
            int t1, t2, t3, t4;
            if (!int.TryParse(txtTime1.Text, out t1) || t1 <= 0 ||
                !int.TryParse(txtTime2.Text, out t2) || t2 <= 0 ||
                !int.TryParse(txtTime3.Text, out t3) || t3 <= 0 ||
                !int.TryParse(txtTime4.Text, out t4) || t4 <= 0)
            {
                MessageBox.Show("Bitte geben Sie gültige positive Zahlen für alle Zeiten ein!", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Zyklus starten
            cycleRunning = true;

            // determine start phase from dropdown
            int startPhaseIndex = cmbStartPhase.SelectedIndex; // 0..3
            int multiplier = rbMinutes.Checked ? 60 : 1;
            switch (startPhaseIndex)
            {
                case 0: // Phase 1
                    cycleState = 1;
                    remainingTime = t1 * multiplier;
                    // start with CPX on
                    SendCommandCPX400DP("OP1 1");
                    LogMessage("[ZYKLUS] Start-Phase: Phase 1 - CPX EIN");
                    break;
                case 1: // Phase 2
                    cycleState = 2;
                    remainingTime = t2 * multiplier;
                    LogMessage("[ZYKLUS] Start-Phase: Phase 2 - Pause");
                    break;
                case 2: // Phase 3
                    cycleState = 3;
                    remainingTime = t3 * multiplier;
                    // ensure LD is on
                    SendCommandLD400P("INP 1");
                    LogMessage("[ZYKLUS] Start-Phase: Phase 3 - LD EIN");
                    // mark pass start
                    passStartTime = DateTime.Now;
                    currentPass_mAh = 0.0;
                    break;
                case 3: // Phase 4
                    cycleState = 4;
                    remainingTime = t4 * multiplier;
                    LogMessage("[ZYKLUS] Start-Phase: Phase 4 - Pause");
                    break;
                default:
                    cycleState = 1;
                    remainingTime = t1 * multiplier;
                    SendCommandCPX400DP("OP1 1");
                    LogMessage("[ZYKLUS] Start-Phase default: Phase 1 - CPX EIN");
                    break;
            }

            cycleCounter = 1;
            csvData.Clear();

            // UI anpassen
            btnStartCycle.Enabled = false;
            btnStopCycle.Enabled = true;
            txtTime1.Enabled = false;
            txtTime2.Enabled = false;
            txtTime3.Enabled = false;
            txtTime4.Enabled = false;
            rbSeconds.Enabled = false;
            rbMinutes.Enabled = false;
            cmbStartPhase.Enabled = false;
            grpControlLD400P.Enabled = false;
            grpControlCPX400DP.Enabled = false;

            LogMessage("[ZYKLUS] Zyklus gestartet");
            LogMessage($"[ZYKLUS] Zeit 1: {t1}, Zeit 2: {t2}, Zeit 3: {t3}, Zeit 4: {t4} ({(rbMinutes.Checked ? "Minuten" : "Sekunden")})");

            lblCycleCount.Text = $"Durchläufe: {cycleCounter}";

            cycleTimer.Start();
        }

        private void BtnStopCycle_Click(object sender, EventArgs e)
        {
            StopCycle();
        }

        private void StopCycle()
        {
            cycleTimer.Stop();
            cycleRunning = false;

            // Alle Ausgänge ausschalten
            SendCommandCPX400DP("OP1 0");
            SendCommandLD400P("INP 0");

            // Finalize last pass if any (log once)
            if (currentPass_mAh > 0.0)
            {
                FinalizePassAndLog();
            }

            // UI zurücksetzen
            btnStartCycle.Enabled = true;
            btnStopCycle.Enabled = false;
            txtTime1.Enabled = true;
            txtTime2.Enabled = true;
            txtTime3.Enabled = true;
            txtTime4.Enabled = true;
            rbSeconds.Enabled = true;
            rbMinutes.Enabled = true;
            cmbStartPhase.Enabled = true;
            grpControlLD400P.Enabled = true;
            grpControlCPX400DP.Enabled = true;

            lblCycleStatus.Text = "Zyklus gestoppt";
            lblCycleStatus.ForeColor = Color.Red;

            LogMessage("[ZYKLUS] Zyklus gestoppt");
            LogMessage("[ZYKLUS] Alle Ausgänge ausgeschaltet");
        }

        private void CycleTimer_Tick(object sender, EventArgs e)
        {
            remainingTime--;

            // Read latest measurements from LD400P each tick (if connected)
            ReadMeasurements();

            // If LD is ON (phase 3) accumulate mAh consumed
            if (cycleState == 3)
            {
                // Timer tick is 1 second, so delta seconds = 1
                // mAh increment = current(A) * (seconds / 3600) * 1000 = current * (1/3.6)
                double increment_mAh = currentAmps / 3.6;
                if (increment_mAh > 0 && !double.IsNaN(increment_mAh) && !double.IsInfinity(increment_mAh))
                {
                    currentPass_mAh += increment_mAh;
                    total_mAh += increment_mAh;
                }
                // ensure passStartTime set when LD phase running
                if (passStartTime == DateTime.MinValue) passStartTime = DateTime.Now;
            }

            // Zeiteinheit für Anzeige
            string unit = rbMinutes.Checked ? "Minuten" : "Sekunden";
            int displayTime = rbMinutes.Checked ? (remainingTime / 60) : remainingTime;

            // Update measurement UI
            UpdateMeasurementsUI();

            // Status aktualisieren
            string statusText = "";
            switch (cycleState)
            {
                case 1:
                    statusText = $"Phase 1: CPX Ausgang EIN - Verbleibend: {displayTime} {unit}";
                    lblCycleStatus.ForeColor = Color.Green;
                    break;
                case 2:
                    statusText = $"Phase 2: Pause - Verbleibend: {displayTime} {unit}";
                    lblCycleStatus.ForeColor = Color.Orange;
                    break;
                case 3:
                    statusText = $"Phase 3: LD Last EIN - Verbleibend: {displayTime} {unit}";
                    lblCycleStatus.ForeColor = Color.Blue;
                    break;
                case 4:
                    statusText = $"Phase 4: Pause - Verbleibend: {displayTime} {unit}";
                    lblCycleStatus.ForeColor = Color.Orange;
                    break;
            }
            lblCycleStatus.Text = statusText;

            // Prüfen ob Zeit abgelaufen
            if (remainingTime <= 0)
            {
                // Nächste Phase
                int multiplier = rbMinutes.Checked ? 60 : 1;
                int t1, t2, t3, t4;
                int.TryParse(txtTime1.Text, out t1);
                int.TryParse(txtTime2.Text, out t2);
                int.TryParse(txtTime3.Text, out t3);
                int.TryParse(txtTime4.Text, out t4);

                switch (cycleState)
                {
                    case 1: // CPX war Ein, jetzt Aus und Pause
                        SendCommandCPX400DP("OP1 0");
                        LogMessage("[ZYKLUS] Phase 1 beendet: CPX400DP Ausgang AUS");
                        cycleState = 2;
                        remainingTime = t2 * multiplier;
                        LogMessage("[ZYKLUS] Phase 2: Pause");
                        break;

                    case 2: // Pause vorbei, LD Ein
                        SendCommandLD400P("INP 1");
                        LogMessage("[ZYKLUS] Phase 2 beendet");
                        cycleState = 3;
                        remainingTime = t3 * multiplier;
                        LogMessage("[ZYKLUS] Phase 3: LD400P Last EIN");
                        // passStartTime will be set on first tick where cycleState==3
                        break;

                    case 3: // LD war Ein, jetzt Aus und Pause -> here we finalize pass once
                        SendCommandLD400P("INP 0");
                        LogMessage("[ZYKLUS] Phase 3 beendet: LD400P Last AUS");
                        // finalize pass: log mAh consumed this pass and compute SOH (once)
                        FinalizePassAndLog();

                        cycleState = 4;
                        remainingTime = t4 * multiplier;
                        LogMessage("[ZYKLUS] Phase 4: Pause");
                        break;

                    case 4: // Pause vorbei, Zyklus neu starten
                        LogMessage("[ZYKLUS] Phase 4 beendet");
                        cycleState = 1;
                        remainingTime = t1 * multiplier;
                        SendCommandCPX400DP("OP1 1");
                        LogMessage("[ZYKLUS] Phase 1: CPX400DP Ausgang EIN (Neuer Durchlauf)");
                        cycleCounter++;
                        lblCycleCount.Text = $"Durchläufe: {cycleCounter}";
                        // reset for next pass accumulation (will set passStartTime when LD turns on)
                        currentPass_mAh = 0.0;
                        passStartTime = DateTime.MinValue;
                        break;
                }
            }
        }

        private void FinalizePassAndLog()
        {
            double nominal = 0.0;
            double.TryParse(txtNominalCapacity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out nominal);

            double sohPercent = double.NaN;
            if (nominal > 0)
            {
                sohPercent = (currentPass_mAh / nominal) * 100.0;
            }

            LogMessage($"[SOH] Durchlauf {cycleCounter}: Entnommene Kapazität = {currentPass_mAh:F3} mAh" + (nominal > 0 ? $", SOH = {sohPercent:F2} %" : ""));
            lblSOHValue.Text = (double.IsNaN(sohPercent) ? "--" : $"{sohPercent:F2} %");

            // Add exactly one summary row per pass to csv data (no Phase, no Current_A, no Elapsed_s column)
            csvData.Add(new CycleDataEntry
            {
                Timestamp = DateTime.Now,
                CycleNumber = cycleCounter,
                Voltage_V = voltageVolts,
                Pass_mAh = currentPass_mAh,
                SOHPercent = double.IsNaN(sohPercent) ? (double?)null : sohPercent
            });

            // Reset pass timer marker
            passStartTime = DateTime.MinValue;
        }

        /// <summary>
        /// Liest Spannung und Strom vom LD400P.
        /// Laut Handbuch: "V?" -> liefert Spannung als "<value>V", "I?" -> Strom als "<value>A".
        /// Diese Methode verwendet Regex, um die erste Zahl in der Antwort zu extrahieren.
        /// </summary>
        private void ReadMeasurements()
        {
            if (!isConnectedLD400P) return;

            try
            {
                // Laut Handbuch: V? liefert Spannung mit Suffix "V"
                string vresp = QueryLD400P("V?");
                if (!string.IsNullOrWhiteSpace(vresp))
                {
                    double? v = ParseNumericFromResponse(vresp);
                    if (v.HasValue) voltageVolts = v.Value;
                    else LogMessage($"[LD400P] Konnte Spannung nicht parsen: '{vresp}'");
                }

                // Laut Handbuch: I? liefert Strom mit Suffix "A"
                string cresp = QueryLD400P("I?");
                if (!string.IsNullOrWhiteSpace(cresp))
                {
                    double? a = ParseNumericFromResponse(cresp);
                    if (a.HasValue) currentAmps = a.Value;
                    else LogMessage($"[LD400P] Konnte Strom nicht parsen: '{cresp}'");
                }
            }
            catch (Exception ex)
            {
                // keep previous values if reading fails
                LogMessage($"[LD400P] Messwerte lesen fehlgeschlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Extrahiert die erste Gleitkommazahl aus der Antwort (z.B. "12.34V" -> 12.34).
        /// Gibt null zurück, falls keine Zahl gefunden wird.
        /// </summary>
        private double? ParseNumericFromResponse(string resp)
        {
            if (string.IsNullOrWhiteSpace(resp)) return null;

            // Manche Antworten können Komma als Dezimaltrennzeichen verwenden oder hinzufügen wie "46247,00".
            // Wir versuchen, mit Regex die Zahl zu extrahieren und dann mit InvariantCulture oder (falls nötig) lokaler Komma-Notation zu parsen.
            var m = Regex.Match(resp, @"-?\d+[.,]?\d*");
            if (m.Success)
            {
                string token = m.Value.Replace(',', '.'); // unify decimal separator
                double val;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    return val;
            }
            return null;
        }

        private void UpdateMeasurementsUI()
        {
            lblVoltage.Text = $"Spannung (V): {voltageVolts:F3}";
            lblCurrent.Text = $"Strom (A): {currentAmps:F3}";
            lblPassmAh.Text = $"Aktuell entnommene mAh (pass): {currentPass_mAh:F3}";
            lblTimeElapsedValue.Text = passElapsed.ToString(@"hh\:mm\:ss");

            // Update SOH display live if nominal provided
            double nominal;
            if (double.TryParse(txtNominalCapacity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out nominal) && nominal > 0)
            {
                double soh = (currentPass_mAh / nominal) * 100.0;
                lblSOHValue.Text = $"{soh:F2} %";
            }
            else
            {
                lblSOHValue.Text = "--";
            }
        }

        // ===== CSV Export =====
        private void BtnExportCSV_Click(object sender, EventArgs e)
        {
            if (csvData.Count == 0)
            {
                MessageBox.Show("Keine Messdaten vorhanden zum Exportieren.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";
                sfd.FileName = $"cycle_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            // CSV Header (no Phase, no Current_A, no Elapsed_s) + SOH
                            sw.WriteLine("Timestamp;CycleNumber;Voltage_V;Pass_mAh;SOH_percent");

                            // Format numeric values with fixed decimal places to avoid long repeating fractions
                            foreach (var row in csvData)
                            {
                                string ts = row.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                                string cycle = row.CycleNumber.ToString(CultureInfo.InvariantCulture);
                                string voltage = row.Voltage_V.ToString("F3", CultureInfo.InvariantCulture); // 3 decimal places
                                string passmAh = row.Pass_mAh.ToString("F3", CultureInfo.InvariantCulture);   // 3 decimal places
                                string soh = row.SOHPercent.HasValue ? row.SOHPercent.Value.ToString("F2", CultureInfo.InvariantCulture) : "";

                                string line = $"{ts};{cycle};{voltage};{passmAh};{soh}";
                                sw.WriteLine(line);
                            }
                        }
                        MessageBox.Show($"Daten erfolgreich nach {sfd.FileName} exportiert.", "Export abgeschlossen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LogMessage($"[CSV] Exportiert: {sfd.FileName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Export: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogMessage($"[CSV] FEHLER beim Export: {ex.Message}");
                    }
                }
            }
        }

        // ===== Gemeinsame Funktionen =====
        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cycleRunning)
            {
                StopCycle();
            }
            DisconnectLD400P();
            DisconnectCPX400DP();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // Helper struct for CSV rows (only pass-summary fields)
        private class CycleDataEntry
        {
            public DateTime Timestamp { get; set; }
            public int CycleNumber { get; set; }
            public double Voltage_V { get; set; }
            public double Pass_mAh { get; set; }
            // Neuer Eintrag für SOH (in Prozent). Nullable, falls kein Nominalwert angegeben wurde.
            public double? SOHPercent { get; set; }
        }
    }
}