using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Z80.Properties;
using static Z80.AssemblerZ80;

namespace Z80
{
    public partial class MainForm : Form
    {
        #region Members

        // Assembler object
        private AssemblerZ80 assemblerZ80;
        
        // Rows of memory panel   
        private Label[] memoryAddressLabels = new Label[0x10];

        // Columns of memory panel
        private Label[] memoryAddressIndexLabels = new Label[0x10];

        // Contents of memory panel table
        private Label[,] memoryTableLabels = new Label[0x10, 0x10];

        // Rows of ports panel   
        private Label[] portAddressLabels = new Label[0x10];

        // Columns of ports panel
        private Label[] portAddressIndexLabels = new Label[0x10];

        // Contents of ports panel table
        private Label[,] portTableLabels = new Label[0x10, 0x10];

        // File selected for loading/saving 
        private string sourceFile = "";

        // Next instruction address
        private UInt16 nextInstrAddress = 0;

        // Line on which a breakpoint has been set
        private int lineBreakPoint = -1;

        // Tooltip for button/menu items
        private ToolTip toolTip;

        // Delay for running program
        private Timer timer = new Timer();

        // SDk-85 interface
        private FormMPF1 formMPF1 = null;

        // Indicate number of activations of soundbit
        private int soundActiveCounter = 10000;

        // Macro definitions found
        private Dictionary<string, MACRO> macros;

        // Macro structure
        private struct MACRO
        {
            public string name;
            public List<string> args;
            public List<string> locals;
            public List<string> lines;
        }

        // Terminal (debug) interface 
        private FormTerminal formTerminal = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            toolStripButtonRun.Enabled = false;
            toolStripButtonFast.Enabled = false;
            toolStripButtonStep.Enabled = false;
            toolStripButtonStop.Enabled = false;

            pbBreakPoint.Image = new Bitmap(pbBreakPoint.Height, pbBreakPoint.Width);
            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);

            // Scroll memory panel with mousewheel
            this.panelMemory.MouseWheel += PanelMemory_MouseWheel;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// MainForm loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set location of mainform
            this.Location = new Point(10, 10);

            // Tooltip with line (address) info
            toolTip = new ToolTip();
            toolTip.OwnerDraw = true;
            toolTip.IsBalloon = false;
            toolTip.BackColor = Color.Azure;
            toolTip.Draw += ToolTip_Draw;
            toolTip.Popup += ToolTip_Popup;

            // Create font for header text
            Font font = new Font("Tahoma", 9.75F, FontStyle.Bold);

            // We can view 256 bytes of memory at a time, it will be in form of 16 X 16
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "memoryAddressLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X").PadLeft(4, '0');
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(44, 15);
                label.Location = new Point(10, 20 + 20 * i);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelMemoryInfo.Controls.Add(label);

                memoryAddressLabels[i] = label;
            }

            // MemoryAddressIndexLabels, display the top row required for the memory table
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "memoryAddressIndexLabel" + i.ToString("X");
                label.Font = font;
                label.Text = i.ToString("X");
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(20, 15);
                label.Location = new Point(60 + 30 * i, 0);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelMemoryInfo.Controls.Add(label);

                memoryAddressIndexLabels[i] = label;
            }

            // MemoryTableLabels, display the memory contents
            for (int i = 0; i < 0x10; i++)
            {
                for (int j = 0; j < 0x10; j++)
                {
                    Label label = new Label();
                    int address = 16 * i + j;
                    label.Name = "memoryTableLabel" + address.ToString("X").PadLeft(2, '0');
                    label.Text = null;
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    label.Visible = true;
                    label.Size = new System.Drawing.Size(24, 15);
                    label.Location = new Point(60 + 30 * j, 20 + 20 * i);
                    panelMemoryInfo.Controls.Add(label);

                    memoryTableLabels[i, j] = label;
                }
            }

            // PortAddressLabels, display initial labels from 0x00 to 0x10 
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "portAddressLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X").PadLeft(2, '0');
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(40, 15);
                label.Location = new Point(10, 20 + 20 * i);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelPortInfo.Controls.Add(label);

                portAddressLabels[i] = label;
            }

            // portAddressIndexLabels, display the top row required for the port table
            for (int i = 0; i < 0x10; i++)
            {
                Label label = new Label();
                label.Name = "portAddressIndexLabel" + i.ToString("X");
                label.Font = font;
                label.Text = (i * 16).ToString("X");
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Visible = true;
                label.Size = new System.Drawing.Size(20, 15);
                label.Location = new Point(60 + 30 * i, 0);
                label.BackColor = SystemColors.GradientInactiveCaption;
                panelPortInfo.Controls.Add(label);

                portAddressIndexLabels[i] = label;
            }

            // portTableLabels, display the port contents
            for (int i = 0; i < 0x10; i++)
            {
                for (int j = 0; j < 0x10; j++)
                {
                    Label label = new Label();
                    int address = 16 * i + j;
                    label.Name = "portTableLabel" + address.ToString("X");
                    label.Text = null;
                    label.Visible = true;
                    label.Size = new System.Drawing.Size(30, 15);
                    label.Location = new Point(60 + 30 * j, 20 + 20 * i);
                    panelPortInfo.Controls.Add(label);

                    portTableLabels[i, j] = label;
                }
            }

            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
            timer.Tick += new EventHandler(TimerEventProcessor);

            // Initialize the buttons (add a tag with info)
            InitButtons();
        }

        /// <summary>
        /// Timer event handler
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void TimerEventProcessor(Object obj, EventArgs myEventArgs)
        {
            Timer timer = (Timer)obj;

            UInt16 currentInstrAddress = nextInstrAddress;

            string error = assemblerZ80.RunInstruction(currentInstrAddress, ref nextInstrAddress);

            UInt16 startViewAddress = Convert.ToUInt16(memoryAddressLabels[0].Text, 16);

            if (!chkLock.Checked)
            {
                if (nextInstrAddress > startViewAddress + 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
                if (nextInstrAddress < startViewAddress)         startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
            }

            UpdateMemoryPanel(startViewAddress, nextInstrAddress);
            UpdateDisplay();
            UpdateKeyboard();
            UpdatePortPanel();
            UpdateTerminal();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();

            if (error == "")
            {
                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], false);

                if ((lineBreakPoint >= 0) && (assemblerZ80.RAMprogramLine[nextInstrAddress] == lineBreakPoint))
                {
                    timer.Enabled = false;

                    // Enable event handler for updating row/column 
                    richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                    ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);
                    if (chkLock.Checked)
                    {
                        UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
                    } else
                    {
                        UpdateMemoryPanel(currentInstrAddress, nextInstrAddress);
                    }

                    UpdatePortPanel();
                    UpdateTerminal();
                    UpdateRegisters();
                    UpdateFlags();
                    UpdateInterrupts();

                    toolStripButtonRun.Enabled = true;
                    toolStripButtonFast.Enabled = true;
                    toolStripButtonStep.Enabled = true;
                    toolStripButtonStop.Enabled = false;
                }

                if ((lineBreakPoint == -1) && (assemblerZ80.RAMprogramLine[nextInstrAddress] == lineBreakPoint))
                {
                    timer.Enabled = false;

                    // Enable event handler for updating row/column 
                    richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                    UpdateMemoryPanel(nextInstrAddress, nextInstrAddress);
                    UpdatePortPanel();
                    UpdateTerminal();
                    UpdateRegisters();
                    UpdateFlags();
                    UpdateInterrupts();

                    resetSimulatorToolStripMenuItem.Enabled = true;
                    toolStripButtonReset.Enabled = true;
                    toolStripButtonNew.Enabled = true;
                    toolStripButtonRun.Enabled = false;
                    toolStripButtonFast.Enabled = false;
                    toolStripButtonStep.Enabled = false;
                    toolStripButtonStop.Enabled = false;

                    MessageBox.Show("No valid instruction at the next address (0x" + nextInstrAddress.ToString("X").PadLeft(2, '0') + ")", "SYSTEM HALTED", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            } else
            {
                timer.Enabled = false;

                resetSimulatorToolStripMenuItem.Enabled = true;
                toolStripButtonReset.Enabled = true;
                toolStripButtonNew.Enabled = true;
                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                if (error == "System Halted")
                {
                    ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], false);
                    MessageBox.Show(error, "SYSTEM HALTED", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else
                {
                    ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], true);
                    MessageBox.Show(error, "RUNTIME ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Skip delay loops when simulating
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkSkipLoops_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                assemblerZ80.skipDelay = chkSkipLoops.Checked;
            }
        }

        /// <summary>
        /// Show/Hide MPF-1 interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkMPF1_CheckedChanged(object sender, EventArgs e)
        {
            if (formMPF1 == null)
            {
                int x = this.Location.X + this.Width + 10;
                int y = this.Location.Y;

                if (x > Screen.PrimaryScreen.WorkingArea.Width)
                {
                    x = this.Width / 2;
                    y = this.Height / 2;
                }

                formMPF1 = new FormMPF1(x, y);
                formMPF1.Show();

                // Port 0 (keyboard data) is high by default
                if (assemblerZ80 != null)
                {
                    assemblerZ80.PORT[0] = 0xFF;
                }
            } else
            {
                formMPF1.Close();
                formMPF1 = null;

                if (assemblerZ80 != null)
                {
                    assemblerZ80.PORT[0] = 0x00;
                }
            }

            UpdatePortPanel();
        }

        /// <summary>
        /// Memory startaddress changing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMemoryStartAddress_TextChanged(object sender, EventArgs e)
        {
            string hexdigits = "1234567890ABCDEFabcdef";
            bool noHex = false;
            foreach (char c in tbMemoryStartAddress.Text)
            {
                if (hexdigits.IndexOf(c) < 0)
                {
                    MessageBox.Show("Only hexadecimal values", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    noHex = true;
                }
            }

            if (noHex) tbMemoryStartAddress.Text = "0000";
        }

        /// <summary>
        /// View memory from this address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbMemoryStartAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
            {
                UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
            }
        }

        /// <summary>
        /// Startaddress changing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbSetProgramCounter_TextChanged(object sender, EventArgs e)
        {
            string hexdigits = "1234567890ABCDEFabcdef";
            bool noHex = false;
            foreach (char c in tbSetProgramCounter.Text)
            {
                if (hexdigits.IndexOf(c) < 0)
                {
                    MessageBox.Show("Only hexadecimal values", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    noHex = true;
                }
            }

            if (noHex) tbSetProgramCounter.Text = "0000";
        }

        /// <summary>
        /// Startaddress changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbSetProgramCounter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter) && (assemblerZ80 != null))
            {
                nextInstrAddress = Convert.ToUInt16(tbSetProgramCounter.Text, 16);
                labelPCRegister.Text = tbSetProgramCounter.Text;
                
                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);

                if (!chkLock.Checked) UpdateMemoryPanel(nextInstrAddress, nextInstrAddress);
            }
        }

        /// <summary>
        /// Timer delay while running
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDownDelay_ValueChanged(object sender, EventArgs e)
        {
            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
        }

        /// <summary>
        /// Add/Change breakpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void pbBreakPoint_MouseClick(object sender, MouseEventArgs e)
        {
            // Get character index of mouse Y position in current program
            int index = richTextBoxProgram.GetCharIndexFromPosition(new Point(0, e.Y));

            // Get line number
            lineBreakPoint = richTextBoxProgram.GetLineFromCharIndex(index);

            // Set (update) breakpoint on screen
            UpdateBreakPoint(lineBreakPoint);
        }

        /// <summary>
        /// Main form resized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            // Set (update) breakpoint on screen
            UpdateBreakPoint(lineBreakPoint);
        }

        /// <summary>
        /// Draw tooltip with specific font
        /// </summary>
        private void ToolTip_Draw(object sender, DrawToolTipEventArgs e)
        {
            Font font = new Font(FontFamily.GenericMonospace, 12.0f);
            e.DrawBackground();
            e.DrawBorder();
            e.Graphics.DrawString(e.ToolTipText, font, Brushes.Black, new Point(2, 2));
        }

        /// <summary>
        /// Set font for the tooltip popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            Font font = new Font(FontFamily.GenericMonospace, 12.0f);
            Size size = TextRenderer.MeasureText(toolTip.GetToolTip(e.AssociatedControl), font);
            e.ToolTipSize = new Size(size.Width + 3, size.Height + 3);
        }

        /// <summary>
        /// Change memory view range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PanelMemory_MouseWheel(object sender, MouseEventArgs e)
        {
            if (assemblerZ80 != null)
            {
                if (e.Delta < 0)
                {
                    if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) < 0xFFF0)
                    {
                        UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) + 0x0010);

                        tbMemoryStartAddress.Text = n.ToString("X4");
                        UpdateMemoryPanel(n, nextInstrAddress);
                    }
                }

                if (e.Delta > 0)
                {
                    if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) >= 0x0010)
                    {
                        UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) - 0x0010);

                        tbMemoryStartAddress.Text = n.ToString("X4");
                        UpdateMemoryPanel(n, nextInstrAddress);
                    }
                }
            }
        }

        /// <summary>
        /// Change sign flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagS_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null) assemblerZ80.flagS = chkFlagS.Checked;
        }

        /// <summary>
        /// Change zero flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagZ_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null) assemblerZ80.flagZ = chkFlagZ.Checked;
        }

        /// <summary>
        /// Change auxiliary carry flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagH_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null) assemblerZ80.flagH = chkFlagH.Checked;
        }

        /// <summary>
        /// Change parity/overflow flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagPV_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null) assemblerZ80.flagPV = chkFlagPV.Checked;
        }

        /// <summary>
        /// Change carry flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFlagC_CheckedChanged(object sender, EventArgs e)
        {
            if (assemblerZ80 != null) assemblerZ80.flagC = chkFlagC.Checked;
        }

        /// <summary>
        /// Show tooltiptext
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_MouseHover(object sender, EventArgs e)
        {
            Control control = (Control)sender;
            Instruction instruction = (Instruction)control.Tag;

            toolTip.SetToolTip(control, instruction.Description);
            toolTip.Active = true;
        }

        #endregion

        #region EventHandlers (Menu)

        private void open_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select Assembly File";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "Z80 assembly|*.asm;*.z80;*.Z80|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                System.IO.StreamReader asmProgramReader;
                asmProgramReader = new System.IO.StreamReader(sourceFile);
                richTextBoxProgram.Text = asmProgramReader.ReadToEnd();
                asmProgramReader.Close();
            }
        }

        private void save_Click(object sender, EventArgs e)
        {
            if (sourceFile == "")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "Save File As";
                fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                fileDialog.FileName = "";
                fileDialog.Filter = "Z80 assembly|*.asm;*.z80;*.Z80|All Files|*.*";

                if (fileDialog.ShowDialog() != DialogResult.Cancel)
                {
                    sourceFile = fileDialog.FileName;
                    System.IO.StreamWriter asmProgramWriter;
                    asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                    asmProgramWriter.Write(richTextBoxProgram.Text);
                    asmProgramWriter.Close();
                }
            } else
            {
                System.IO.StreamWriter asmProgramWriter;
                asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                asmProgramWriter.Write(richTextBoxProgram.Text);
                asmProgramWriter.Close();
            }
        }

        private void saveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "Save File As";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "Z80 assembly|*.asm;*.z80;*.Z80|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                System.IO.StreamWriter asmProgramWriter;
                asmProgramWriter = new System.IO.StreamWriter(sourceFile);
                asmProgramWriter.Write(richTextBoxProgram.Text);
                asmProgramWriter.Close();
            }
        }

        private void saveBinary_Click(object sender, EventArgs e)
        {
            if (assemblerZ80.programRun == null)
            {
                MessageBox.Show("Nothing yet to save", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "Save Binary File As";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "Binary|*.bin|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                int start = -1;
                int end = -1;

                // Find start address of code
                for (int i = 0; (i < assemblerZ80.RAM.Length) && (start == -1); i++)
                {
                    if (assemblerZ80.RAMprogramLine[i] != -1) start = i;
                }

                // Find end address of code
                for (int i = assemblerZ80.RAM.Length - 1; (i >= 0) && (end == -1); i--)
                {
                    if (assemblerZ80.RAMprogramLine[i] != -1) end = i;
                }

                if ((start == -1) || (end == -1))
                {
                    MessageBox.Show("Nothing to save", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // New byte array with only used code 
                byte[] bytes = new byte[end - start + 1];
                for (int i = 0; i < end - start + 1; i++)
                {
                    bytes[i] = assemblerZ80.RAM[start + i];
                }

                // Save binary file
                File.WriteAllBytes(fileDialog.FileName, bytes);

                MessageBox.Show("Binary file saved as\r\n" + fileDialog.FileName, "SAVED", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void openBinary_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select Binary File";
            fileDialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            fileDialog.FileName = "";
            fileDialog.Filter = "Binary|*.bin|All Files|*.*";

            if (fileDialog.ShowDialog() != DialogResult.Cancel)
            {
                sourceFile = fileDialog.FileName;
                byte[] bytes = File.ReadAllBytes(sourceFile);

                FormAddresses formAddresses = new FormAddresses();
                formAddresses.ShowDialog();

                if (formAddresses.loadAddress + bytes.Length > 0x10000)
                {
                    MessageBox.Show("Program is to large (for this start address):\r\nthe end address is 0x" + (formAddresses.loadAddress + bytes.Length).ToString("X"), "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                FormDisAssembler disAssemblerForm = new FormDisAssembler(bytes, formAddresses.loadAddress, formAddresses.startAddress, formAddresses.useLabels);
                DialogResult dialogResult = disAssemblerForm.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    if (timer.Enabled)
                    {
                        timer.Enabled = false;

                        // Enable event handler for updating row/column 
                        richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);
                    }

                    assemblerZ80 = null;
                    UpdateMemoryPanel(0x0000, 0x0000);
                    UpdatePortPanel();
                    UpdateRegisters();
                    UpdateFlags();
                    UpdateInterrupts();
                    ClearDisplay();
                    ClearKeyboard();

                    // Reset color
                    richTextBoxProgram.SelectionStart = 0;
                    richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
                    richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

                    tbSetProgramCounter.Text = "0000";
                    tbMemoryStartAddress.Text = "0000";
                    tbMemoryUpdateByte.Text = "00";
                    numMemoryAddress.Value = 0000;
                    numPort.Value = 0;
                    tbPortUpdateByte.Text = "00";

                    toolStripButtonRun.Enabled = false;
                    toolStripButtonFast.Enabled = false;
                    toolStripButtonStep.Enabled = false;
                    toolStripButtonStop.Enabled = false;

                    lineBreakPoint = -1;

                    Graphics g = pbBreakPoint.CreateGraphics();
                    g.Clear(Color.LightGray);

                    richTextBoxProgram.Text = disAssemblerForm.program;
                }
            }
        }

        private void quit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void resetSimulator_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);
            }

            assemblerZ80 = null;
            UpdateMemoryPanel(0x0000, 0x0000);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();
            ClearDisplay();
            ClearKeyboard();

            // Reset color
            richTextBoxProgram.SelectionStart = 0;
            richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
            richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

            tbSetProgramCounter.Text = "0000";
            tbMemoryStartAddress.Text = "0000";
            tbMemoryUpdateByte.Text = "00";
            numMemoryAddress.Value = 0000;
            numPort.Value = 0;
            tbPortUpdateByte.Text = "00";

            toolStripButtonRun.Enabled = false;
            toolStripButtonFast.Enabled = false;
            toolStripButtonStep.Enabled = false;
            toolStripButtonStop.Enabled = false;

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        private void resetRAM_Click(object sender, EventArgs e)
        {
            assemblerZ80.ClearRam();
            nextInstrAddress = Convert.ToUInt16(tbMemoryStartAddress.Text, 16);
            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
        }

        private void resetPorts_Click(object sender, EventArgs e)
        {
            assemblerZ80.ClearPorts();
            UpdatePortPanel();
        }

        private void new_Click(object sender, EventArgs e)
        {
            assemblerZ80 = null;
            UpdateMemoryPanel(0x0000, 0x0000);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();
            ClearDisplay();
            ClearKeyboard();

            richTextBoxProgram.Clear();
            sourceFile = "";

            tbSetProgramCounter.Text = "0000";
            tbMemoryStartAddress.Text = "0000";
            tbMemoryUpdateByte.Text = "00";
            numMemoryAddress.Value = 0000;
            numPort.Value = 0;
            tbPortUpdateByte.Text = "00";

            toolStripButtonRun.Enabled = false;
            toolStripButtonFast.Enabled = false;
            toolStripButtonStep.Enabled = false;
            toolStripButtonStop.Enabled = false;

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        private void startDebug_Click(object sender, EventArgs e)
        {
            string message;
            nextInstrAddress = 0;
            if (formTerminal != null) formTerminal.tbTerminal.Text = "";

            // Replace all macros with regular code
            try
            {
                message = ProcessMacros(richTextBoxProgram);
                if (message != "OK")
                {
                    MessageBox.Show(this, message, "PROCESS MACROS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Check if a linenumber has been given
                    string[] fields = message.Split(' ');
                    bool result = Int32.TryParse(fields[fields.Length - 1], out int line);
                    if (result)
                    {
                        // Show where the error is (remember the linenumber returned starts with 1 in stead of 0)
                        ChangeColorRTBLine(line - 1, true);
                    }

                    Cursor.Current = Cursors.Arrow;
                    return;
                }
            } catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "startDebug_Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            assemblerZ80 = new AssemblerZ80(richTextBoxProgram.Lines);
            assemblerZ80.skipDelay = chkSkipLoops.Checked;

            // Port 0 (keyboard data) is high by default on MPF-1
            if (chkMPF1.Checked)
            {
                assemblerZ80.PORT[0] = 0xFF;
            }

            try
            {
                // Run the first Pass of assembler
                message = assemblerZ80.FirstPass();
                if (message != "OK")
                {
                    MessageBox.Show(this, message, "FIRSTPASS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Check if a linenumber has been given
                    string[] fields = message.Split(' ');
                    bool result = Int32.TryParse(fields[fields.Length - 1], out int line);
                    if (result)
                    {
                        // Show where the error is (remember the linenumber returned starts with 1 in stead of 0)
                        ChangeColorRTBLine(line - 1, true);
                    }

                    return;
                }

                // Run second pass
                message = assemblerZ80.SecondPass();
                if (message != "OK")
                {
                    MessageBox.Show(this, message, "SECONDPASS", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Show Updated memory
                    UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);

                    // Check if a linenumber has been given
                    string[] fields = message.Split(' ');
                    bool result = Int32.TryParse(fields[fields.Length - 1], out int line);
                    if (result)
                    {
                        // Show where the error is (remember the linenumber returned starts with 1 in stead of 0)
                        ChangeColorRTBLine(line - 1, true);
                    }

                    return;
                }

                // Try to get startadres of program execution
                int startline = -1;
                for (int index = 0; (index < assemblerZ80.RAMprogramLine.Length) && (startline == -1); index++)
                {
                    if (assemblerZ80.RAMprogramLine[index] != -1) startline = index;
                }

                if (startline != -1)
                {
                    tbMemoryStartAddress.Text = startline.ToString("X4");
                    tbSetProgramCounter.Text = startline.ToString("X4");
                }

                // Show Updated memory
                UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);

            } catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "startDebug_Click", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            nextInstrAddress = Convert.ToUInt16(tbSetProgramCounter.Text, 16);
            ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);

            // Insert monitor program if required
            if ((formMPF1 != null) && formMPF1.chkInsertMonitor.Checked)
            {
                // Check if current program overlaps
                bool overlap = false;
                for (int i=0; i<0x0800; i++)
                {
                    if (assemblerZ80.RAMprogramLine[i] >= 0)
                    {
                        overlap = true;
                    }
                }

                if (overlap) MessageBox.Show("The monitor program (0x0000 to 0x0800) will overwrite (some of) the user program", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                byte[] bytes = Resources.mpf;
                int index = 0x0000;
                foreach (byte bt in bytes)
                {
                    assemblerZ80.RAM[index] = bt;

                    // Indicate this is not a regular program line but also not an invalid one (-1)
                    assemblerZ80.RAMprogramLine[index] = -2;

                    index++;
                }
            }

            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
            UpdatePortPanel();
            UpdateRegisters();
            UpdateFlags();
            ClearDisplay();
            ClearKeyboard();

            toolStripButtonRun.Enabled = true;
            toolStripButtonFast.Enabled = true;
            toolStripButtonStep.Enabled = true;
            toolStripButtonStop.Enabled = false;
        }

        private void startRun_Click(object sender, EventArgs e)
        {
            resetSimulatorToolStripMenuItem.Enabled = false;
            toolStripButtonReset.Enabled = false;
            toolStripButtonNew.Enabled = false;
            toolStripButtonRun.Enabled = false;
            toolStripButtonFast.Enabled = false;
            toolStripButtonStep.Enabled = false;
            toolStripButtonStop.Enabled = true;

            // Disable event handler for updating row/column 
            richTextBoxProgram.SelectionChanged -= richTextBoxProgram_SelectionChanged;

            timer.Interval = Convert.ToInt32(numericUpDownDelay.Value);
            timer.Enabled = true;
        }

        private void startStep_Click(object sender, EventArgs e)
        {
            UInt16 currentInstrAddress = nextInstrAddress;
            string error = assemblerZ80.RunInstruction(currentInstrAddress, ref nextInstrAddress);

            UInt16 startViewAddress = Convert.ToUInt16(memoryAddressLabels[0].Text, 16);

            if (!chkLock.Checked)
            {
                if (nextInstrAddress > startViewAddress + 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
                if (nextInstrAddress < startViewAddress)         startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
            }

            UpdateMemoryPanel(startViewAddress, nextInstrAddress);
            UpdateDisplay();
            UpdateKeyboard();
            UpdatePortPanel();
            UpdateTerminal();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();

            if ((error == "") && (assemblerZ80.RAMprogramLine[nextInstrAddress] != -1))
            {
                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);
            } else if (error == "System Halted")
            {
                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;

                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], false);
                MessageBox.Show(error, "SYSTEM HALTED", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else 
            {
                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;

                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], true);
                MessageBox.Show(error, "RUNTIME ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Get index of cursor in current program
            int index = richTextBoxProgram.SelectionStart;

            // Get line number
            int line = richTextBoxProgram.GetLineFromCharIndex(index);
            lblLine.Text = (line + 1).ToString();

            int column = richTextBoxProgram.SelectionStart - richTextBoxProgram.GetFirstCharIndexFromLine(line);
            lblColumn.Text = (column + 1).ToString();
        }

        /// <summary>
        /// Fast run, no updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startFast_Click(object sender, EventArgs e)
        {
            resetSimulatorToolStripMenuItem.Enabled = false;
            toolStripButtonNew.Enabled = false;
            toolStripButtonReset.Enabled = false;
            toolStripButtonRun.Enabled = false;
            toolStripButtonFast.Enabled = false;
            toolStripButtonStep.Enabled = false;
            toolStripButtonStop.Enabled = true;

            ClearColorRTBLine();
            
            string error = "";
            UInt16 currentInstrAddress = nextInstrAddress;

            while (!toolStripButtonFast.Enabled && (error == ""))
            {
                currentInstrAddress = nextInstrAddress;
                error = assemblerZ80.RunInstruction(currentInstrAddress, ref nextInstrAddress);
                if (error == "")
                {
                    UpdateDisplay();
                    UpdateKeyboard();
                    UpdateTerminal();
                    if ((assemblerZ80.RAMprogramLine[nextInstrAddress] == lineBreakPoint) && (lineBreakPoint != -1)) 
                    {
                        resetSimulatorToolStripMenuItem.Enabled = true;
                        toolStripButtonReset.Enabled = true;
                        toolStripButtonNew.Enabled = true;
                        toolStripButtonRun.Enabled = true;
                        toolStripButtonFast.Enabled = true;
                        toolStripButtonStep.Enabled = true;
                        toolStripButtonStop.Enabled = false;
                    } 

                    Application.DoEvents();
                }
            }
            UInt16 startViewAddress = Convert.ToUInt16(memoryAddressLabels[0].Text, 16);

            if (!chkLock.Checked)
            {
                if (nextInstrAddress > startViewAddress + 0x100) startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
                if (nextInstrAddress < startViewAddress)         startViewAddress = (UInt16)(nextInstrAddress & 0xFFF0);
            }

            UpdateMemoryPanel(startViewAddress, nextInstrAddress);
            UpdateDisplay();
            UpdateKeyboard();
            UpdatePortPanel();
            UpdateTerminal();
            UpdateRegisters();
            UpdateFlags();
            UpdateInterrupts();

            if (error == "")
            {
                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);
                toolStripButtonRun.Enabled = true;
                toolStripButtonFast.Enabled = true;
                toolStripButtonStep.Enabled = true;
                toolStripButtonStop.Enabled = false;
            } else if (error == "System Halted")
            {
                resetSimulatorToolStripMenuItem.Enabled = true;
                toolStripButtonReset.Enabled = true;
                toolStripButtonNew.Enabled = true;
                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;

                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], false);
                MessageBox.Show(error, "SYSTEM HALTED", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else
            {
                resetSimulatorToolStripMenuItem.Enabled = true;
                toolStripButtonReset.Enabled = true;
                toolStripButtonNew.Enabled = true;
                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;

                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[currentInstrAddress], true);
                MessageBox.Show(error, "RUNTIME ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Get index of cursor in current program
            int index = richTextBoxProgram.SelectionStart;

            // Get line/column number
            int line = richTextBoxProgram.GetLineFromCharIndex(index);
            lblLine.Text = (line + 1).ToString();

            int column = richTextBoxProgram.SelectionStart - richTextBoxProgram.GetFirstCharIndexFromLine(line);
            lblColumn.Text = (column + 1).ToString();
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                timer.Enabled = false;

                // Enable event handler for updating row/column 
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                ChangeColorRTBLine(assemblerZ80.RAMprogramLine[nextInstrAddress], false);

                resetSimulatorToolStripMenuItem.Enabled = true;
                toolStripButtonReset.Enabled = true;
                toolStripButtonNew.Enabled = true;
                toolStripButtonRun.Enabled = true;
                toolStripButtonFast.Enabled = true;
                toolStripButtonStep.Enabled = true;
                toolStripButtonStop.Enabled = false;
            }
        }

        private void viewHelp_Click(object sender, EventArgs e)
        {
            FormHelp formHelp = new FormHelp();
            formHelp.ShowDialog();
        }

        private void about_Click(object sender, EventArgs e)
        {
            FormAbout formAbout = new FormAbout();
            formAbout.ShowDialog();
        }

        #endregion

        #region EventHandlers (Labels)

        private void labelARegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelARegister);
        }

        private void labelBRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelBRegister);
        }

        private void labelCRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelCRegister);
        }

        private void labelDRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelDRegister);
        }

        private void labelERegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelERegister);
        }

        private void labelHRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelHRegister);
        }

        private void labelLRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelLRegister);
        }

        private void labelPCRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelPCRegister);
        }

        private void labelSPRegister_MouseHover(object sender, EventArgs e)
        {
            RegisterHoverBinary(labelSPRegister);
        }

        #endregion

        #region EventHandlers (Buttons)

        /// <summary>
        /// Command buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCommand_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button.Tag != null)
            {
                Instruction instruction = (Instruction)button.Tag;

                FormCommand command = new FormCommand(instruction.Explanation, instruction.Description, instruction.Mnemonic);
                DialogResult dialogResult = command.ShowDialog();
                
                if (dialogResult == DialogResult.OK)
                {
                    if (richTextBoxProgram.SelectionStart == 0)
                    {
                        richTextBoxProgram.AppendText(command.instruction);
                    } else
                    {
                        richTextBoxProgram.AppendText(Environment.NewLine + command.instruction);
                    }
                }
            }
        }

        /// <summary>
        /// View symbol table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewSymbolTable_Click(object sender, EventArgs e)
        {
            if ((assemblerZ80 != null) && (assemblerZ80.programRun != null))
            {
                string addressSymbolTable = "";

                // Check max length of labels
                int maxLabelSize = 0;
                foreach (KeyValuePair<string, int> keyValuePair in assemblerZ80.addressSymbolTable)
                {
                    if (keyValuePair.Key.Length > maxLabelSize) maxLabelSize = keyValuePair.Key.Length;
                }

                // Add to table
                foreach (KeyValuePair<string, int> keyValuePair in assemblerZ80.addressSymbolTable)
                {
                    addressSymbolTable += keyValuePair.Key;
                    for (int i=keyValuePair.Key.Length; i< maxLabelSize + 1; i++)
                    {
                        addressSymbolTable += " ";
                    }

                    addressSymbolTable += ": " + keyValuePair.Value.ToString("X4") + "\r\n";
                }

                // Create form for display of results                
                Form addressSymbolTableForm = new Form();
                addressSymbolTableForm.Name = "FormSymbolTable";
                addressSymbolTableForm.Text = "SymbolTable";
                addressSymbolTableForm.Icon = Properties.Resources.Z80;
                addressSymbolTableForm.Size = new Size(300, 600);
                addressSymbolTableForm.MinimumSize = new Size(300, 600);
                addressSymbolTableForm.MaximumSize = new Size(300, 600);
                addressSymbolTableForm.MaximizeBox = false;
                addressSymbolTableForm.MinimizeBox = false;
                addressSymbolTableForm.StartPosition = FormStartPosition.CenterScreen;

                // Create button for closing (dialog)form
                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.Location = new Point(204, 530);
                btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                btnOk.Visible = true;
                btnOk.Click += new EventHandler((object o, EventArgs a) =>
                {
                    addressSymbolTableForm.Close();
                });

                Font font = new Font(FontFamily.GenericMonospace, 10.25F);

                // Sort alphabetically
                string[] tempArray = addressSymbolTable.Split('\n');
                Array.Sort(tempArray, StringComparer.InvariantCulture);
                addressSymbolTable = "";
                foreach (string line in tempArray)
                {
                    if (line != "") addressSymbolTable += line + '\n';
                }

                // Add controls to form
                TextBox textBox = new TextBox();
                textBox.Multiline = true;
                textBox.WordWrap = false;
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.ReadOnly = true;
                textBox.BackColor = Color.LightYellow;
                textBox.Size = new Size(268, 510);
                textBox.Text = addressSymbolTable;
                textBox.Font = font;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Location = new Point(10, 10);
                textBox.Select(0, 0);

                textBox.Click += new EventHandler((object o, EventArgs a) =>
                {
                    int position = textBox.GetLineFromCharIndex(textBox.SelectionStart);
                    string line = textBox.Lines[position];
                    textBox.Select(textBox.GetFirstCharIndexFromLine(position), line.Length);
                    string[] parts = line.Split(' ');
                    bool result = Int32.TryParse(parts[parts.Length - 1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int address);
                    if (result) tbMemoryStartAddress.Text = address.ToString("X4");
                    UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
                });

                addressSymbolTableForm.Controls.Add(textBox);
                addressSymbolTableForm.Controls.Add(btnOk);

                // Show form
                addressSymbolTableForm.Show();
            }
        }

        /// <summary>
        /// View program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewProgram_Click(object sender, EventArgs e)
        {
            if ((assemblerZ80 != null) && (assemblerZ80.programRun != null))
            {
                // Create form for display of results                
                Form formProgram = new Form();
                formProgram.Name = "FormProgram";
                formProgram.Text = "Program";
                formProgram.Icon = Properties.Resources.Z80;
                formProgram.Size = new Size(516, 600);
                formProgram.MinimumSize = new Size(500, 600);
                formProgram.MaximizeBox = false;
                formProgram.MinimizeBox = false;
                formProgram.StartPosition = FormStartPosition.CenterScreen;

                // Create button for closing (dialog)form
                Button btnOk = new Button();
                btnOk.Text = "OK";
                btnOk.Location = new Point(400, 530);
                btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                btnOk.Visible = true;
                btnOk.Click += new EventHandler((object o, EventArgs a) =>
                {
                    formProgram.Close();
                });

                string program = "";
                foreach (string line in assemblerZ80.programView)
                {
                    if ((line != null) && (line != "") && (line != "\r") && (line != "\n") && (line != "\r\n")) program += line + "\r\n";
                }

                Font font = new Font(FontFamily.GenericMonospace, 10.25F);

                // Add controls to form
                TextBox textBox = new TextBox();
                textBox.Multiline = true;
                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                textBox.WordWrap = false;
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.ReadOnly = true;
                textBox.BackColor = Color.LightYellow;
                textBox.Size = new Size(486, 510);
                textBox.Text = program;
                textBox.Font = font;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Location = new Point(10, 10);
                textBox.Select(0, 0);

                formProgram.Controls.Add(textBox);
                formProgram.Controls.Add(btnOk);

                // Show form
                formProgram.Show();
            }
        }

        /// <summary>
        /// Set memory start address to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMemoryStartAddress_Click(object sender, EventArgs e)
        {
            UpdateMemoryPanel(GetTextBoxMemoryStartAddress(), nextInstrAddress);
        }

        /// <summary>
        /// Set memory start address to view to Program Counter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewPC_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                UpdateMemoryPanel(assemblerZ80.registerPC, nextInstrAddress);
                tbMemoryStartAddress.Text = assemblerZ80.registerPC.ToString("X4");
            }
        }

        /// <summary>
        /// Set memory start address to view to Stack Pointer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewSP_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                UpdateMemoryPanel(assemblerZ80.registerSP, nextInstrAddress);
                tbMemoryStartAddress.Text = assemblerZ80.registerSP.ToString("X4");
            }
        }

        /// <summary>
        /// Previous memory to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) >= 0x0100)
                {
                    UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) - 0x0100);

                    tbMemoryStartAddress.Text = n.ToString("X4");
                    UpdateMemoryPanel(n, nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Next memory to view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                if (Convert.ToUInt16(memoryAddressLabels[0].Text, 16) < 0xFF00)
                {
                    UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16) + 0x0100);

                    tbMemoryStartAddress.Text = n.ToString("X4");
                    UpdateMemoryPanel(n, nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Write value to memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMemoryWrite_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                assemblerZ80.RAM[(int)numMemoryAddress.Value] = Convert.ToByte(tbMemoryUpdateByte.Text, 16);

                UInt16 n = (UInt16)(Convert.ToUInt16(memoryAddressLabels[0].Text, 16));
                if (
                    (((UInt16)numMemoryAddress.Value) >= n) &&
                    (((UInt16)numMemoryAddress.Value) < n + 0x100)
                   )
                {
                    UpdateMemoryPanel(n, nextInstrAddress);
                }
            }
        }

        /// <summary>
        /// Clear all ports
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearPORT_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                assemblerZ80.ClearPorts();
                UpdatePortPanel();
            }
        }

        /// <summary>
        ///  Write value to port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPortWrite_Click(object sender, EventArgs e)
        {
            if (assemblerZ80 != null)
            {
                assemblerZ80.PORT[(int)numPort.Value] = Convert.ToByte(tbPortUpdateByte.Text, 16);

                UpdatePortPanel();
            }
        }

        /// <summary>
        /// Clear breakpoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearBreakPoint_Click(object sender, EventArgs e)
        {
            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        #endregion

        #region EventHandlers (RichTextBox)

        private void richTextBoxProgram_SelectionChanged(object sender, EventArgs e)
        {
            // Get index of cursor in current program
            int index = richTextBoxProgram.SelectionStart;

            // Get line number
            int line = richTextBoxProgram.GetLineFromCharIndex(index);
            lblLine.Text = (line + 1).ToString();

            int column = richTextBoxProgram.SelectionStart - richTextBoxProgram.GetFirstCharIndexFromLine(line);
            lblColumn.Text = (column + 1).ToString();
        }

        // Program adjusted, remove highlight
        private void richTextBoxProgram_TextChanged(object sender, EventArgs e)
        {
            if (toolStripButtonRun.Enabled)
            {
                int pos = richTextBoxProgram.SelectionStart;

                // Reset color
                richTextBoxProgram.SelectionStart = 0;
                richTextBoxProgram.SelectionLength = richTextBoxProgram.Text.Length;
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;

                richTextBoxProgram.SelectionLength = 0;

                richTextBoxProgram.SelectionStart = pos;

                toolStripButtonRun.Enabled = false;
                toolStripButtonFast.Enabled = false;
                toolStripButtonStep.Enabled = false;
                toolStripButtonStop.Enabled = false;
            }

            lineBreakPoint = -1;

            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);
        }

        /// <summary>
        /// Mouse button clicked in control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseDown(object sender, MouseEventArgs e)
        {
            int x = e.Location.X;
            int y = e.Location.Y;

            int charIndex = richTextBoxProgram.GetCharIndexFromPosition(new Point(x, y));
            int lineIndex = richTextBoxProgram.GetLineFromCharIndex(charIndex);

            if (assemblerZ80 != null)
            {
                bool found = false;
                for (int address = 0; (address < assemblerZ80.RAMprogramLine.Length) && !found; address++)
                {
                    if (assemblerZ80.RAMprogramLine[address] == lineIndex)
                    {
                        found = true;
                        int startAddress = Convert.ToInt32(memoryAddressLabels[0].Text, 16);

                        int row = (address - startAddress) / 16;
                        int col = (address - startAddress) % 16;

                        foreach (Label lbl in memoryTableLabels)
                        {
                            if (lbl.BackColor != Color.LightGreen) lbl.BackColor = SystemColors.Info;
                        }

                        if ((row >= 0) && (col >= 0) && (row < 16) && (col < 16))
                        {
                            if (memoryTableLabels[row, col].BackColor != Color.LightGreen) memoryTableLabels[row, col].BackColor = SystemColors.GradientInactiveCaption;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mouse enters control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseEnter(object sender, EventArgs e)
        {
            toolTip.Active = true;
        }

        /// <summary>
        /// Mouse leaves control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void richTextBoxProgram_MouseLeave(object sender, EventArgs e)
        {
            toolTip.Hide(richTextBoxProgram);
            toolTip.Active = false;
        }

        /// <summary>
        /// Disable tooltip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void richTextBoxProgram_MouseMove(object sender, MouseEventArgs e)
        {
            if ((toolTip != null) && (assemblerZ80 != null))
            {
                int x = e.Location.X;
                int y = e.Location.Y;

                int charIndex = richTextBoxProgram.GetCharIndexFromPosition(new Point(x, y));
                int lineIndex = richTextBoxProgram.GetLineFromCharIndex(charIndex);

                bool found = false;
                for (int index = 0; (index < assemblerZ80.RAMprogramLine.Length) && !found; index++)
                {
                    if (assemblerZ80.RAMprogramLine[index] == lineIndex)
                    {
                        found = true;
                        if (toolTip.GetToolTip(richTextBoxProgram) != index.ToString("X4")) 
                        {
                            toolTip.Show(index.ToString("X4"), richTextBoxProgram, -50, richTextBoxProgram.GetPositionFromCharIndex(charIndex).Y, 50000);
                        } 
                    }
                }
            }
        }

        private void richTextBoxProgram_VScroll(object sender, EventArgs e)
        {
            UpdateBreakPoint(lineBreakPoint);
            toolTip.Hide(richTextBoxProgram);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Replace macros with regular code
        /// </summary>
        /// <returns></returns>
        public string ProcessMacros(RichTextBox richTextBox)
        {
            // Set debug mode to false;
            bool DEBUG = false;

            // Index for new labels from macros to be added at the name
            int indexNewLabel = 0;

            // New dictionary of macros
            macros = new Dictionary<string, MACRO>();

            // Copy program text
            string[] program = richTextBox.Lines;
            List<string> programProcessed = new List<string>();

            // Process all lines
            for (int lineNumber = 0; lineNumber < program.Length; lineNumber++)
            {
                try
                {
                    // Copy line of code to process
                    string line = program[lineNumber];

                    // Remove leading or trailing spaces
                    line = line.Trim();
                    if (line == "")
                    {
                        // Copy empty lines to processed program
                        programProcessed.Add(program[lineNumber]);
                        continue;
                    }

                    // if a comment is found, remove
                    int start_of_comment_pos = line.IndexOf(';');
                    if (start_of_comment_pos != -1)
                    {
                        // Check if really a comment (; could be in a string or char array)
                        int num_quotes = 0;
                        for (int i = 0; i < start_of_comment_pos; i++)
                        {
                            if ((line[i] == '\'') || (line[i] == '\"')) num_quotes++;
                        }

                        if ((num_quotes % 2) == 0)
                        {
                            line = line.Remove(line.IndexOf(';')).Trim();
                            if (line == "")
                            {
                                // Copy comment only lines to processed program
                                programProcessed.Add(program[lineNumber]);
                                continue;
                            }
                        }
                    }

                    // Check if the line starts with an debug define statement
                    if (line.ToLower().StartsWith("debug"))
                    {
                        // If next char is not a colon (for a label) this is a debug define statement
                        if ((line.Length > 5) && (line.Substring(5, 1) != ":"))
                        {
                            string arg = line.Substring(5);
                            if (arg.ToLower().Contains("true")) DEBUG = true;
                            if (arg.ToLower().Contains("1")) DEBUG = true;

                            // Skip line
                            if (lineNumber < program.Length)
                            {
                                lineNumber++;
                                line = program[lineNumber].Trim().ToLower();
                            }
                        }
                    }

                    // Check if the line starts with an debug statement
                    if (line.Trim().ToLower().StartsWith("#debug"))
                    {
                        if (DEBUG)
                        {
                            if (lineNumber < program.Length) lineNumber++;
                            line = program[lineNumber].Trim().ToLower();

                            // Copy lines until the #ENDDEBUG statement line
                            while ((lineNumber < program.Length) && (!line.StartsWith("#enddebug")))
                            {
                                programProcessed.Add(program[lineNumber]);
                                lineNumber++;
                                if (lineNumber < program.Length)
                                {
                                    line = program[lineNumber].Trim().ToLower();
                                }
                            }

                            // Skip #ENDDEBUG line
                            if (lineNumber < program.Length)
                            {
                                lineNumber++;
                                line = program[lineNumber].Trim().ToLower();
                            }
                        } else
                        {
                            // Skip lines until the #ENDDEBUG statement line
                            while ((lineNumber < program.Length) && (!line.StartsWith("#enddebug")))
                            {
                                lineNumber++;
                                if (lineNumber < program.Length)
                                {
                                    line = program[lineNumber].Trim().ToLower();
                                }
                            }

                            // Skip #ENDDEBUG line
                            if (lineNumber < program.Length)
                            {
                                lineNumber++;
                                line = program[lineNumber].Trim().ToLower();
                            }
                        }
                    }

                    // Check if the line starts with a known macro name
                    bool foundMacro = false;
                    foreach (KeyValuePair<string, MACRO> keyValuePair in macros)
                    {
                        if (line.StartsWith(keyValuePair.Key))
                        {
                            // Found macro
                            foundMacro = true;
                            MACRO macro = macros[keyValuePair.Key];

                            string newCode = "";
                            foreach (string ml in macro.lines)
                            {
                                string l = ml;
                                foreach (string local in macro.locals)
                                {
                                    if (l.Contains(local + ":"))
                                    {
                                        string fill = "";
                                        for (int i = 0; i < (7 - local.Length); i++) fill += " ";
                                        l = l.Replace(local + ":", local + indexNewLabel.ToString("D4") + ":\r\n" + fill);
                                        indexNewLabel++;
                                    }
                                }

                                newCode += l + "\r\n";
                            }

                            // Get arguments
                            List<string> args = new List<string>();
                            line = line.Replace(keyValuePair.Key, "").Trim();
                            for (int i = 0; i < line.Length; i++)
                            {
                                bool inStr = false;
                                string arg = "";
                                while ((i < line.Length) && (inStr || (!inStr && (line[i] != ','))))
                                {
                                    arg += line[i];
                                    if ((line[i] == '\'') || (line[i] == '\"')) inStr = !inStr;
                                    i++;
                                }

                                args.Add(arg);
                            }

                            if (args.Count != macro.args.Count)
                            {
                                return ("Invalid number of arguments for macro '" + keyValuePair.Value + "' at line " + (lineNumber + 1).ToString());
                            }

                            // Replace arguments in new code
                            int index = 0;
                            newCode = newCode.ToLower();

                            foreach (string arg in macro.args)
                            {
                                newCode = newCode.Replace(arg.ToLower().Trim(), args[index].ToUpper().Trim());
                                index++;
                            }

                            newCode = newCode.ToLower();
                            programProcessed.Add(newCode);
                        }
                    }

                    // If found process next line
                    if (foundMacro) continue;

                    // Check if there is a label with the 'macro' statement after it (no quotes allowed in line)
                    if ((line.IndexOf(':') != -1) && !line.Contains("\'") && !line.Contains("\""))
                    {
                        int end_of_label_pos = line.IndexOf(':');
                        int start_of_macro_pos = line.ToLower().IndexOf("macro", end_of_label_pos);
                        if (start_of_macro_pos != -1)
                        {
                            // Macro statement found, so process
                            MACRO macro = new MACRO();

                            // Name
                            macro.name = line.Substring(0, end_of_label_pos);

                            // Args
                            List<string> listArgs = new List<string>();
                            string[] args = line.Substring(start_of_macro_pos + 5).Split(',');
                            foreach (string arg in args)
                            {
                                listArgs.Add(arg.Trim());
                            }
                            macro.args = listArgs;

                            // Locals
                            lineNumber++;
                            line = program[lineNumber];

                            List<string> listLocals = new List<string>();

                            while (line.ToLower().Trim().StartsWith("local"))
                            {
                                string local = line.Trim().ToLower().Substring(5);
                                listLocals.Add(local.Trim());

                                lineNumber++;
                                line = program[lineNumber];
                            }
                            macro.locals = listLocals;

                            // Lines
                            List<string> listLines = new List<string>();
                            do
                            {
                                line = program[lineNumber];
                                if (!line.ToLower().Contains("endm")) listLines.Add(line);
                                lineNumber++;
                            } while (!line.ToLower().Contains("endm"));

                            macro.lines = listLines;

                            // Add to dictionary
                            if (!macros.ContainsKey(macro.name))
                            {
                                macros.Add(macro.name, macro);
                            } else
                            {
                                return ("Macro with same same as previous one found at line " + (lineNumber + 1));
                            }
                        } else
                        {
                            // Copy 'non macro lines' to processed program
                            programProcessed.Add(program[lineNumber]);
                        }
                    } else
                    {
                        // Copy 'non macro lines' to processed program
                        programProcessed.Add(program[lineNumber]);
                    }
                } catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "ProcessMacros", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ("EXCEPTION ERROR AT LINE " + (lineNumber + 1));
                }
            }

            string programNew = "";
            for (int i = 0; i < programProcessed.Count; i++)
            {
                programNew += programProcessed[i] + "\r\n";
            }

            richTextBox.Text = programNew;

            return ("OK");
        }

        /// <summary>
        /// Updating the Registers
        /// </summary>
        private void UpdateRegisters()
        {
            if (assemblerZ80 != null)
            {
                labelARegister.Text = assemblerZ80.registerA.ToString("X").PadLeft(2, '0');
                labelBRegister.Text = assemblerZ80.registerB.ToString("X").PadLeft(2, '0');
                labelCRegister.Text = assemblerZ80.registerC.ToString("X").PadLeft(2, '0');
                labelDRegister.Text = assemblerZ80.registerD.ToString("X").PadLeft(2, '0');
                labelERegister.Text = assemblerZ80.registerE.ToString("X").PadLeft(2, '0');
                labelHRegister.Text = assemblerZ80.registerH.ToString("X").PadLeft(2, '0');
                labelLRegister.Text = assemblerZ80.registerL.ToString("X").PadLeft(2, '0');
                labelIRegister.Text = assemblerZ80.registerI.ToString("X").PadLeft(2, '0');
                labelRRegister.Text = assemblerZ80.registerR.ToString("X").PadLeft(2, '0');

                labelAaltRegister.Text = assemblerZ80.registerAalt.ToString("X").PadLeft(2, '0');
                labelBaltRegister.Text = assemblerZ80.registerBalt.ToString("X").PadLeft(2, '0');
                labelCaltRegister.Text = assemblerZ80.registerCalt.ToString("X").PadLeft(2, '0');
                labelDaltRegister.Text = assemblerZ80.registerDalt.ToString("X").PadLeft(2, '0');
                labelEaltRegister.Text = assemblerZ80.registerEalt.ToString("X").PadLeft(2, '0');
                labelHaltRegister.Text = assemblerZ80.registerHalt.ToString("X").PadLeft(2, '0');
                labelLaltRegister.Text = assemblerZ80.registerLalt.ToString("X").PadLeft(2, '0');

                labelIXRegister.Text = assemblerZ80.registerIX.ToString("X").PadLeft(4, '0');
                labelIYRegister.Text = assemblerZ80.registerIY.ToString("X").PadLeft(4, '0');

                labelPCRegister.Text = assemblerZ80.registerPC.ToString("X").PadLeft(4, '0');
                labelSPRegister.Text = assemblerZ80.registerSP.ToString("X").PadLeft(4, '0');
            } else
            {
                labelARegister.Text = "00";
                labelBRegister.Text = "00";
                labelCRegister.Text = "00";
                labelDRegister.Text = "00";
                labelERegister.Text = "00";
                labelHRegister.Text = "00";
                labelLRegister.Text = "00";

                labelAaltRegister.Text = "00";
                labelBaltRegister.Text = "00";
                labelCaltRegister.Text = "00";
                labelDaltRegister.Text = "00";
                labelEaltRegister.Text = "00";
                labelHaltRegister.Text = "00";
                labelLaltRegister.Text = "00";

                labelIXRegister.Text = "0000";
                labelIYRegister.Text = "0000";

                labelPCRegister.Text = "0000";
                labelSPRegister.Text = "0000";

                labelIRegister.Text = "00";
                labelRRegister.Text = "00";
            }
        }

        /// <summary>
        /// Update the Flags
        /// </summary>
        private void UpdateFlags()
        {
            if (assemblerZ80 != null)
            {
                chkFlagC.Checked  = assemblerZ80.flagC;
                chkFlagPV.Checked  = assemblerZ80.flagPV;
                chkFlagH.Checked = assemblerZ80.flagH;
                chkFlagN.Checked  = assemblerZ80.flagN;
                chkFlagZ.Checked  = assemblerZ80.flagZ;
                chkFlagS.Checked  = assemblerZ80.flagS;
                chkFlagCalt.Checked = assemblerZ80.flagCalt;
                chkFlagPValt.Checked = assemblerZ80.flagPValt;
                chkFlagHalt.Checked = assemblerZ80.flagHalt;
                chkFlagNalt.Checked = assemblerZ80.flagNalt;
                chkFlagZalt.Checked = assemblerZ80.flagZalt;
                chkFlagSalt.Checked = assemblerZ80.flagSalt;
            } else
            {
                chkFlagC.Checked  = false;
                chkFlagPV.Checked = false;
                chkFlagH.Checked = false;
                chkFlagN.Checked  = false;
                chkFlagZ.Checked  = false;
                chkFlagS.Checked  = false;
                chkFlagCalt.Checked = false;
                chkFlagPValt.Checked = false;
                chkFlagHalt.Checked = false;
                chkFlagNalt.Checked = false;
                chkFlagZalt.Checked = false;
                chkFlagSalt.Checked = false;
            }
        }

        /// <summary>
        /// Update interrupt masks
        /// </summary>
        private void UpdateInterrupts()
        {
            if (assemblerZ80 != null)
            {
                if (assemblerZ80.intrIE)
                {
                    lblInterrupts.BackColor = Color.LightGreen;
                } else
                {
                    lblInterrupts.BackColor = Color.LightPink;
                }

                if (assemblerZ80.im == AssemblerZ80.IM.im0) lblIM.Text = "Interrupt Mode: im0";
                if (assemblerZ80.im == AssemblerZ80.IM.im1) lblIM.Text = "Interrupt Mode: im1";
                if (assemblerZ80.im == AssemblerZ80.IM.im2) lblIM.Text = "Interrupt Mode: im2";
            }
        }

        /// <summary>
        /// Draw memory panel starting from address startAddress, show nextAddress in green
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="nextAddress"></param>
        private void UpdateMemoryPanel(UInt16 startAddress, UInt16 nextAddress)
        {
            if (assemblerZ80 != null)
            {
                // Boundary at address XXX0
                startAddress = (UInt16)(startAddress & 0xFFF0);

                // Check for overflow in displayig (startaddress + 0xFF larger then 0xFFFF)
                if (startAddress > 0xFF00) startAddress = 0xFF00;

                int i = startAddress;
                int j = 0;

                foreach (Label lbl in memoryAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(4, '0');
                    i += 0x10;
                }

                i = 0;
                j = 0;

                // MemoryTableLabels, display the memory contents
                foreach (Label lbl in memoryTableLabels)
                {
                    int address = startAddress + (16 * i) + j;
                    lbl.Text = assemblerZ80.RAM[address].ToString("X").PadLeft(2, '0');

                    if (address == nextAddress)
                    {
                        lbl.BackColor = Color.LightGreen;
                    } else
                    if (address == assemblerZ80.registerSP)
                    {
                        lbl.BackColor = Color.LightPink;
                    } else
                    {
                        lbl.BackColor = SystemColors.Info;
                    }

                    j++;
                    if (j == 0x10)
                    {
                        j = 0;
                        i++;
                    }
                }
            } else
            {
                int i = 0;

                foreach (Label lbl in memoryAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(4, '0');
                    i += 0x10;
                }

                // MemoryTableLabels, display 00
                foreach (Label lbl in memoryTableLabels)
                {
                    lbl.Text = "00";
                    lbl.BackColor = SystemColors.Info;
                }
            }
        }

        /// <summary>
        /// Port panel Update, we can view 64 Bytes at a time, it will be in form of 8 X 8
        /// </summary>
        private void UpdatePortPanel()
        {
            if (assemblerZ80 != null)
            {
                int i = 0;
                int j = 0;

                foreach (Label lbl in portAddressLabels)
                {
                    lbl.Text = i.ToString("X").PadLeft(2, '0');
                    i += 0x10;
                }

                i = 0;
                j = 0;

                // PortTableLabels, display the port contents
                foreach (Label lbl in portTableLabels)
                {
                    lbl.Text = assemblerZ80.PORT[(16 * i) + j].ToString("X").PadLeft(2, '0');

                    j++;
                    if (j == 0x10)
                    {
                        j = 0x00;
                        i++;
                    }
                }
            } else
            {
                // PortTableLabels, display 00
                foreach (Label lbl in portTableLabels)
                {
                    lbl.Text = "00";
                }
            }
        }

        /// <summary>
        /// Clear all digits of the 7 segment display of MPF-1
        /// </summary>
        private void ClearDisplay()
        {
            if (formMPF1 != null) 
            {
                formMPF1.sevenSegmentDigit0.SegmentsValue = 0x00;
                formMPF1.sevenSegmentDigit1.SegmentsValue = 0x00;
                formMPF1.sevenSegmentDigit2.SegmentsValue = 0x00;
                formMPF1.sevenSegmentDigit3.SegmentsValue = 0x00;
                formMPF1.sevenSegmentDigit4.SegmentsValue = 0x00;
                formMPF1.sevenSegmentDigit5.SegmentsValue = 0x00;
            }
        }

        /// <summary>
        /// Clear all key presses
        /// </summary>
        private void ClearKeyboard()
        {
            if (formMPF1 != null)
            {
                formMPF1.key0.Pressed = false;
                formMPF1.key1.Pressed = false;
                formMPF1.key2.Pressed = false;
                formMPF1.key3.Pressed = false;
                formMPF1.key4.Pressed = false;
                formMPF1.key5.Pressed = false;
                formMPF1.key6.Pressed = false;
                formMPF1.key7.Pressed = false;
                formMPF1.key8.Pressed = false;
                formMPF1.key9.Pressed = false;
                formMPF1.keyA.Pressed = false;
                formMPF1.keyB.Pressed = false;
                formMPF1.keyC.Pressed = false;
                formMPF1.keyD.Pressed = false;
                formMPF1.keyE.Pressed = false;
                formMPF1.keyF.Pressed = false;
                formMPF1.keyAddr.Pressed = false;
                formMPF1.keyCbr.Pressed = false;
                formMPF1.keyData.Pressed = false;
                formMPF1.keyDel.Pressed = false;
                formMPF1.keyGo.Pressed = false;
                formMPF1.keyIns.Pressed = false;
                formMPF1.keyIntr.Pressed = false;
                formMPF1.keyMinus.Pressed = false;
                formMPF1.keyMoni.Pressed = false;
                formMPF1.keyMove.Pressed = false;
                formMPF1.keyPc.Pressed = false;
                formMPF1.keyPlus.Pressed = false;
                formMPF1.keyReg.Pressed = false;
                formMPF1.keyRela.Pressed = false;
                formMPF1.keyReset.Pressed = false;
                formMPF1.keySbr.Pressed = false;
                formMPF1.keyStep.Pressed = false;
                formMPF1.keyTapeRd.Pressed = false;
                formMPF1.keyTapeWr.Pressed = false;
                formMPF1.keyUserKey.Pressed = false;
            }
        }

        /// <summary>
        /// Update 7 segment display of MPF-1 (if active)
        /// </summary>
        private void UpdateDisplay()
        {
            if ((assemblerZ80 != null) && (formMPF1 != null))
            {
                if (!formMPF1.chkDisplayLatch.Checked || (assemblerZ80.PORT[0x01] != 0x00))
                {
                    if ((assemblerZ80.PORT[0x02] & 0b00000001) == 0b00000001) formMPF1.sevenSegmentDigit0.SegmentsValue = assemblerZ80.PORT[0x01];
                    if ((assemblerZ80.PORT[0x02] & 0b00000010) == 0b00000010) formMPF1.sevenSegmentDigit1.SegmentsValue = assemblerZ80.PORT[0x01];
                    if ((assemblerZ80.PORT[0x02] & 0b00000100) == 0b00000100) formMPF1.sevenSegmentDigit2.SegmentsValue = assemblerZ80.PORT[0x01];
                    if ((assemblerZ80.PORT[0x02] & 0b00001000) == 0b00001000) formMPF1.sevenSegmentDigit3.SegmentsValue = assemblerZ80.PORT[0x01];
                    if ((assemblerZ80.PORT[0x02] & 0b00010000) == 0b00010000) formMPF1.sevenSegmentDigit4.SegmentsValue = assemblerZ80.PORT[0x01];
                    if ((assemblerZ80.PORT[0x02] & 0b00100000) == 0b00100000) formMPF1.sevenSegmentDigit5.SegmentsValue = assemblerZ80.PORT[0x01];
                }

                // Check audio output
                bool soundBit = (assemblerZ80.PORT[0x02] & 0b10000000) == 0b00000000;
                if (soundBit)
                {
                    soundActiveCounter = 0;
                    formMPF1.pbSpeaker.Visible = true;
                } else
                {
                    // If slow run (with timer) then timer goes to 100
                    if (timer.Enabled)
                    {
                        if (soundActiveCounter < 100) soundActiveCounter++;
                        if (soundActiveCounter >= 100) formMPF1.pbSpeaker.Visible = false;
                    } else
                    {
                        if (soundActiveCounter < 10000) soundActiveCounter++;
                        if (soundActiveCounter >= 10000) formMPF1.pbSpeaker.Visible = false;
                    }
                }

                // If monitor tone output has been called, output actual tone (if enabled)
                if (formMPF1.chkEnableSound.Checked && (assemblerZ80.registerPC == 0x05E4))
                {
                    Console.Beep(assemblerZ80.registerC * 1000 / 65, 300);
                }
            }
        }

        /// <summary>
        /// Update keyboard of MPF-1 (if active)
        /// </summary>
        private void UpdateKeyboard()
        {
            // Check for MPF-1 active
            if ((assemblerZ80 != null) && (formMPF1 != null))
            {
                switch (assemblerZ80.PORT[0x02] | 0b11000000)
                {
                    case 0b11111110:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.key3.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.key7.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.keyB.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyF.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        break;

                    case 0b11111101:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.key2.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.key6.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.keyA.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyE.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        break;

                    case 0b11111011:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.key1.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.key5.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.key9.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyD.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        if (formMPF1.keyStep.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11101111);
                        if (formMPF1.keyTapeRd.Pressed) assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11011111);
                        break;

                    case 0b11110111:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.key0.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.key4.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.key8.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyC.Pressed)      assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        if (formMPF1.keyGo.Pressed)     assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11101111);
                        if (formMPF1.keyTapeWr.Pressed) assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11011111);
                        break;

                    case 0b11101111:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.keyCbr.Pressed)    assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.keyPc.Pressed)     assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.keyReg.Pressed)    assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyAddr.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        if (formMPF1.keyDel.Pressed)    assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11101111);
                        if (formMPF1.keyRela.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11011111);
                        break;

                    case 0b11011111:
                        assemblerZ80.PORT[0x00] = 0xFF;
                        if (formMPF1.keySbr.Pressed)    assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111110);
                        if (formMPF1.keyMinus.Pressed)  assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111101);
                        if (formMPF1.keyData.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11111011);
                        if (formMPF1.keyPlus.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11110111);
                        if (formMPF1.keyIns.Pressed)    assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11101111);
                        if (formMPF1.keyMove.Pressed)   assemblerZ80.PORT[0x00] = (byte)((assemblerZ80.PORT[0x00] | 0b00111111) & 0b11011111);
                        break;
                }

                // Check for user key
                if (formMPF1.keyUserKey.Pressed)
                {
                    System.Threading.Thread.Sleep(100);
                    assemblerZ80.PORT[0x00] = (byte)(assemblerZ80.PORT[0x00] & 0b10111111);
                    formMPF1.keyUserKey.Pressed = false;
                }

                // Check for moni key (connected to NMI)
                if (formMPF1.keyMoni.Pressed)
                {
                    System.Threading.Thread.Sleep(100);
                    assemblerZ80.registerSP--;
                    assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)((assemblerZ80.registerPC & 0xFF00) >> 8);
                    assemblerZ80.registerSP--;
                    assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)(assemblerZ80.registerPC & 0x00FF);

                    nextInstrAddress = 0x0066;

                    formMPF1.keyMoni.Pressed = false;
                }

                // Check for intr key (connected to INT)
                if (formMPF1.keyIntr.Pressed)
                {
                    System.Threading.Thread.Sleep(100);
                    if (assemblerZ80.intrIE)
                    {
                        // Interrupt mode 0
                        if (assemblerZ80.im == IM.im0)
                        {
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)((assemblerZ80.registerPC & 0xFF00) >> 8);
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)(assemblerZ80.registerPC & 0x00FF);

                            UInt16 address = (UInt16)(0x0100 * assemblerZ80.RAM[0x1FEF] + assemblerZ80.RAM[0x1FEE]);
                            nextInstrAddress = address;
                        }

                        // Interrupt mode 1
                        if (assemblerZ80.im == IM.im1)
                        {
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)((assemblerZ80.registerPC & 0xFF00) >> 8);
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)(assemblerZ80.registerPC & 0x00FF);

                            nextInstrAddress = 0x0038;
                        }

                        // Interrupt mode 2 
                        if (assemblerZ80.im == IM.im2)
                        {
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)((assemblerZ80.registerPC & 0xFF00) >> 8);
                            assemblerZ80.registerSP--;
                            assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)(assemblerZ80.registerPC & 0x00FF);

                            UInt16 address = (UInt16)(0x0100 * assemblerZ80.registerI + 0x00FF);
                            nextInstrAddress = address;
                        }
                    }

                    formMPF1.keyIntr.Pressed = false;
                }

                // Check for reset 
                if (formMPF1.keyReset.Pressed)
                {
                    System.Threading.Thread.Sleep(100);

                    assemblerZ80.intrIE = false;

                    assemblerZ80.registerSP--;
                    assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)(assemblerZ80.registerPC & 0x00FF);
                    assemblerZ80.registerSP--;
                    assemblerZ80.RAM[assemblerZ80.registerSP] = (byte)((assemblerZ80.registerPC & 0xFF00) >> 8);

                    assemblerZ80.im = IM.im0;
                    assemblerZ80.registerI = 0x00;
                    assemblerZ80.registerR = 0x00;
                    assemblerZ80.registerPC = 0x0000;
                    nextInstrAddress = 0x0000;

                    formMPF1.keyReset.Pressed = false;
                }
            }
        }

        /// <summary>
        /// Update terminal (if data available)
        /// </summary>
        private void UpdateTerminal()
        {
            if (assemblerZ80 != null) 
            {
                // Update terminal display if signalled from assembler
                if (assemblerZ80.outTerminal)
                {
                    // Setup terminal screen 
                    if (formTerminal == null)
                    {
                        int x = this.Location.X + this.Width;
                        int y = this.Location.Y;

                        if (x > Screen.PrimaryScreen.WorkingArea.Width)
                        {
                            x = this.Width / 2;
                            y = this.Height / 2;
                        }

                        formTerminal = new FormTerminal(x, y);
                        formTerminal.Show();
                    }

                    char ch = Convert.ToChar(assemblerZ80.PORT[0x80]);
                    formTerminal.tbTerminal.Text += ch;
                    if (ch == '\n')
                    {
                        formTerminal.tbTerminal.Select(formTerminal.tbTerminal.TextLength, 0);
                        formTerminal.tbTerminal.SelectionStart = formTerminal.tbTerminal.TextLength;
                        formTerminal.tbTerminal.ScrollToCaret();
                    }
                }

                assemblerZ80.outTerminal = false;
            }
        }

        /// <summary>
        /// get the memory start address from text box
        /// </summary>
        /// <returns></returns>
        private UInt16 GetTextBoxMemoryStartAddress()
        {
            string txtval = tbMemoryStartAddress.Text;
            UInt16 n = Convert.ToUInt16(txtval, 16);    // convert HEX to INT
            return n;
        }

        /// <summary>
        /// Change colors rich text box
        /// </summary>
        /// <param name="line_number"></param>
        /// <param name="error"></param>
        private void ChangeColorRTBLine(int line_number, bool error)
        {
            if ((line_number >= 0) && (richTextBoxProgram.Lines.Length > line_number))
            {
                // No layout events for now (postpone)
                richTextBoxProgram.SuspendLayout();

                // Disable certain event handlers completely
                richTextBoxProgram.TextChanged -= richTextBoxProgram_TextChanged;
                richTextBoxProgram.SelectionChanged -= richTextBoxProgram_SelectionChanged;

                // No focus so we won't see flicker from selection changes
                lblSetProgramCounter.Focus();

                // Reset color
                richTextBoxProgram.HideSelection = true;
                richTextBoxProgram.SelectAll();
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;
                richTextBoxProgram.DeselectAll();
                richTextBoxProgram.HideSelection = false;

                // Get location in RTB
                int firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(line_number);
                string currentlinetext = richTextBoxProgram.Lines[line_number];

                // Select line and color red/green
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = currentlinetext.Length;
                richTextBoxProgram.SelectionBackColor = System.Drawing.Color.LightGreen;
                if (error) richTextBoxProgram.SelectionBackColor = System.Drawing.Color.LightPink;

                // Reset selection
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = 0;

                // Scroll to line (show lines (focus_line)  before selected line if available)
                if (line_number != 0)
                {
                    int focus_line = line_number - Convert.ToInt32(numFocusLine.Value) + 1;
                    if (focus_line < 1) focus_line = 1;
                    firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(focus_line);
                    richTextBoxProgram.SelectionStart = firstcharindex;
                }

                richTextBoxProgram.ScrollToCaret();

                // Set cursor at selected line
                firstcharindex = richTextBoxProgram.GetFirstCharIndexFromLine(line_number);
                richTextBoxProgram.SelectionStart = firstcharindex;
                richTextBoxProgram.SelectionLength = 0;

                // Set focus again
                richTextBoxProgram.Focus();

                // Enable event handler
                richTextBoxProgram.TextChanged += new EventHandler(richTextBoxProgram_TextChanged);
                richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

                // Resume events 
                richTextBoxProgram.ResumeLayout();
            }
        }

        /// <summary>
        /// Clear colors rich text box
        /// </summary>
        private void ClearColorRTBLine()
        {
            // No layout events for now (postpone)
            richTextBoxProgram.SuspendLayout();

            // Disable certain event handlers completely
            richTextBoxProgram.TextChanged -= richTextBoxProgram_TextChanged;
            richTextBoxProgram.SelectionChanged -= richTextBoxProgram_SelectionChanged;

            // No focus so we won't see flicker from selection changes
            lblSetProgramCounter.Focus();

            // Reset color
            richTextBoxProgram.HideSelection = true;
            richTextBoxProgram.SelectAll();
            richTextBoxProgram.SelectionBackColor = System.Drawing.Color.White;
            richTextBoxProgram.DeselectAll();
            richTextBoxProgram.HideSelection = false;

            // Set focus again
            richTextBoxProgram.Focus();

            // Update breakpoint indicator
            UpdateBreakPoint(lineBreakPoint);

            // Enable event handler
            richTextBoxProgram.TextChanged += new EventHandler(richTextBoxProgram_TextChanged);
            richTextBoxProgram.SelectionChanged += new EventHandler(richTextBoxProgram_SelectionChanged);

            // Resume events 
            richTextBoxProgram.ResumeLayout();
        }

        /// <summary>
        /// show tooltip with string binaryval when we hover mouse over a (register) label 
        /// </summary>
        /// <param name="l"></param>
        private void RegisterHoverBinary(Label l)
        {
            string binaryval;
            binaryval = Convert.ToString(Convert.ToInt32(l.Text, 16), 2);

            // change the HEX string to BINARY string
            binaryval = binaryval.PadLeft(8, '0');
            toolTipRegisterBinary.SetToolTip(l, binaryval);
        }

        /// <summary>
        /// Update picturebox with breakpoint
        /// </summary>
        private void UpdateBreakPoint(int line)
        {
            // Clear other breakpoint
            Graphics g = pbBreakPoint.CreateGraphics();
            g.Clear(Color.LightGray);

            if (line >= 0)
            {
                int index = richTextBoxProgram.GetFirstCharIndexFromLine(line);
                if (index > 0)
                {
                    Point point = richTextBoxProgram.GetPositionFromCharIndex(index);
                    g.FillEllipse(Brushes.Red, new Rectangle(1, richTextBoxProgram.Margin.Top + point.Y, 15, 15));
                }
            }
        }

        /// <summary>
        /// Add info for each instruction button
        /// </summary>
        private void InitButtons()
        {
            // All instructions, sorted by name
            Instructions instructions = new Instructions();
            Array.Sort(instructions.Z80MainInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80BitInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80IXInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80IYInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80MiscInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80IXBitInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));
            Array.Sort(instructions.Z80IYBitInstructions, (x, y) => x.Mnemonic.CompareTo(y.Mnemonic));

            // Add all instruction buttons
            int numInstruction;

            // Main    
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80MainInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80MainInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 68;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(66, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage1.Controls.Add(button);

                    numInstruction++;
                }
            }

            // Bit
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80BitInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80BitInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 68;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(66, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage2.Controls.Add(button);

                    numInstruction++;
                }
            }

            // IX
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80IXInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80IXInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 72;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(70, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage3.Controls.Add(button);

                    numInstruction++;
                }
            }

            // IY
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80IYInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80IYInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 72;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(70, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage4.Controls.Add(button);

                    numInstruction++;
                }
            }

            // Misc.
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80MiscInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80MiscInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 72;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(70, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage5.Controls.Add(button);

                    numInstruction++;
                }
            }

            // BitIX
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80IXBitInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80IXBitInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 72;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(70, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage6.Controls.Add(button);

                    numInstruction++;
                }
            }

            // BitIY
            numInstruction = 0;
            for (int indexZ80Instructions = 0; indexZ80Instructions < instructions.Z80IYBitInstructions.Length; indexZ80Instructions++)
            {
                Instruction instruction = instructions.Z80IYBitInstructions[indexZ80Instructions];

                if ((instruction.Mnemonic != "-") && (instruction.Size != 0))
                {
                    int x = 2 + (numInstruction % 4) * 72;
                    int y = 2 + (numInstruction / 4) * 25;

                    Button button = new Button();
                    button.BackColor = Color.LightGreen;
                    button.Location = new Point(x, y);
                    button.Name = "btn" + instruction.Opcode.ToString();
                    button.Size = new Size(70, 23);
                    button.Text = instruction.Mnemonic;
                    button.UseVisualStyleBackColor = false;
                    button.Click += new EventHandler(this.btnCommand_Click);
                    button.Tag = instruction;
                    button.MouseHover += new EventHandler(Control_MouseHover);

                    tabPage7.Controls.Add(button);

                    numInstruction++;
                }
            }
        }

        #endregion
    }
}
