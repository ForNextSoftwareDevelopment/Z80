using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Z80
{
    public partial class FormMPF1 : Form
    {
        #region Members

        // Led display 
        public SevenSegment sevenSegmentDigit0;
        public SevenSegment sevenSegmentDigit1;
        public SevenSegment sevenSegmentDigit2;
        public SevenSegment sevenSegmentDigit3;
        public SevenSegment sevenSegmentDigit4;
        public SevenSegment sevenSegmentDigit5;

        // Keyboard
        public Key keyReset;
        public Key keyMove;
        public Key keyIns;
        public Key keySbr;
        public Key keyPc;
        public Key keyC;
        public Key keyD;
        public Key keyE;
        public Key keyF;
        public Key keyMoni;
        public Key keyRela;
        public Key keyDel;
        public Key keyCbr;
        public Key keyReg;
        public Key key8;
        public Key key9;
        public Key keyA;
        public Key keyB;
        public Key keyIntr;
        public Key keyTapeWr;
        public Key keyStep;
        public Key keyMinus;
        public Key keyData;
        public Key key4;
        public Key key5;
        public Key key6;
        public Key key7;
        public Key keyUserKey;
        public Key keyTapeRd;
        public Key keyGo;
        public Key keyPlus;
        public Key keyAddr;
        public Key key0;
        public Key key1;
        public Key key2;
        public Key key3;

        // Initial location of window
        int x, y;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public FormMPF1(int x, int y)
        {
            InitializeComponent();

            this.x = x;
            this.y = y;

            // Most left digit
            sevenSegmentDigit5 = new SevenSegment();
            sevenSegmentDigit5.Location = new Point(14, 28);
            Controls.Add(sevenSegmentDigit5);

            sevenSegmentDigit4 = new SevenSegment();
            sevenSegmentDigit4.Location = new Point(sevenSegmentDigit5.Width + 18, 28);
            Controls.Add(sevenSegmentDigit4);

            sevenSegmentDigit3 = new SevenSegment();
            sevenSegmentDigit3.Location = new Point(sevenSegmentDigit5.Width + sevenSegmentDigit4.Width + 22, 28);
            Controls.Add(sevenSegmentDigit3);

            sevenSegmentDigit2 = new SevenSegment();
            sevenSegmentDigit2.Location = new Point(sevenSegmentDigit5.Width + sevenSegmentDigit4.Width + sevenSegmentDigit3.Width + 26, 28);
            Controls.Add(sevenSegmentDigit2);

            sevenSegmentDigit1 = new SevenSegment();
            sevenSegmentDigit1.Location = new Point(sevenSegmentDigit5.Width + sevenSegmentDigit4.Width + sevenSegmentDigit3.Width + sevenSegmentDigit2.Width + 34, 28);
            Controls.Add(sevenSegmentDigit1);

            // Most right digit
            sevenSegmentDigit0 = new SevenSegment();
            sevenSegmentDigit0.Location = new Point(sevenSegmentDigit5.Width + sevenSegmentDigit4.Width + sevenSegmentDigit3.Width + sevenSegmentDigit2.Width + sevenSegmentDigit1.Width + 38, 28);
            Controls.Add(sevenSegmentDigit0);

            // Button positions
            int startX = 34;
            int startY = 28 + sevenSegmentDigit3.Height + 90;

            keyReset = new Key(50, 36,  "RESET");
            keyReset.SetBorderColor(Color.DarkGoldenrod);
            keyReset.Location = new Point(startX, startY);
            Controls.Add(keyReset);

            startX += keyReset.Width + 34;

            keyMove = new Key(50, 36,  "MOVE");
            keyMove.SetBorderColor(Color.DarkKhaki);
            keyMove.Location = new Point(startX, startY);
            Controls.Add(keyMove);

            startX += keyMove.Width + 34;

            keyIns = new Key(50, 36, "INS");
            keyIns.SetBorderColor(Color.DarkKhaki);
            keyIns.Location = new Point(startX, startY);
            Controls.Add(keyIns);

            startX += keyIns.Width + 34;

            keySbr = new Key(50, 36, "SBR");
            keySbr.SetBorderColor(Color.DarkKhaki);
            keySbr.Location = new Point(startX, startY);
            Controls.Add(keySbr);

            startX += keySbr.Width + 34;

            keyPc = new Key(50, 36, "PC");
            keyPc.SetBorderColor(Color.DarkKhaki);
            keyPc.Location = new Point(startX, startY);
            Controls.Add(keyPc);

            startX += keyPc.Width + 34;

            keyC = new Key(50, 36, "C");
            keyC.Location = new Point(startX, startY);
            Controls.Add(keyC);

            startX += keyC.Width + 34;

            keyD = new Key(50, 36, "D");
            keyD.Location = new Point(startX, startY);
            Controls.Add(keyD);

            startX += keyD.Width + 34;

            keyE = new Key(50, 36, "E");
            keyE.Location = new Point(startX, startY);
            Controls.Add(keyE);

            startX += keyE.Width + 34;

            keyF = new Key(50, 36, "F");
            keyF.Location = new Point(startX, startY);
            Controls.Add(keyF);

            startX = 34;
            startY += keyF.Height + 48;

            keyMoni = new Key(50, 36, "MONI");
            keyMoni.SetBorderColor(Color.DarkKhaki);
            keyMoni.Location = new Point(startX, startY);
            Controls.Add(keyMoni);

            startX += keyMoni.Width + 34;

            keyRela = new Key(50, 36, "RELA");
            keyRela.SetBorderColor(Color.DarkKhaki);
            keyRela.Location = new Point(startX, startY);
            Controls.Add(keyRela);

            startX += keyRela.Width + 34;

            keyDel = new Key(50, 36, "DEL");
            keyDel.SetBorderColor(Color.DarkKhaki);
            keyDel.Location = new Point(startX, startY);
            Controls.Add(keyDel);

            startX += keyDel.Width + 34;

            keyCbr = new Key(50, 36, "CBR");
            keyCbr.SetBorderColor(Color.DarkKhaki);
            keyCbr.Location = new Point(startX, startY);
            Controls.Add(keyCbr);

            startX += keyCbr.Width + 34;

            keyReg = new Key(50, 36, "REG");
            keyReg.SetBorderColor(Color.DarkKhaki);
            keyReg.Location = new Point(startX, startY);
            Controls.Add(keyReg);

            startX += keyReg.Width + 34;

            key8 = new Key(50, 36, "8");
            key8.Location = new Point(startX, startY);
            Controls.Add(key8);

            startX += key8.Width + 34;

            key9 = new Key(50, 36, "9");
            key9.Location = new Point(startX, startY);
            Controls.Add(key9);

            startX += key9.Width + 34;

            keyA = new Key(50, 36, "A");
            keyA.Location = new Point(startX, startY);
            Controls.Add(keyA);

            startX += keyA.Width + 34;

            keyB = new Key(50, 36, "B");
            keyB.Location = new Point(startX, startY);
            Controls.Add(keyB);

            startX = 32;
            startY += keyB.Height + 56;

            keyIntr = new Key(50, 36, "INTR");
            keyIntr.SetBorderColor(Color.DarkKhaki);
            keyIntr.Location = new Point(startX, startY);
            Controls.Add(keyIntr);

            startX += keyIntr.Width + 34;

            keyTapeWr = new Key(50, 36, "TAPEW");
            keyTapeWr.SetBorderColor(Color.DarkKhaki);
            keyTapeWr.Location = new Point(startX, startY);
            Controls.Add(keyTapeWr);

            startX += keyTapeWr.Width + 34;

            keyStep = new Key(50, 36, "STEP");
            keyStep.SetBorderColor(Color.DarkKhaki);
            keyStep.Location = new Point(startX, startY);
            Controls.Add(keyStep);

            startX += keyStep.Width + 34;

            keyMinus = new Key(50, 36, "-");
            keyMinus.SetBorderColor(Color.DarkKhaki);
            keyMinus.Location = new Point(startX, startY);
            Controls.Add(keyMinus);

            startX += keyMinus.Width + 34;

            keyData = new Key(50, 36, "DATA");
            keyData.SetBorderColor(Color.DarkKhaki);
            keyData.Location = new Point(startX, startY);
            Controls.Add(keyData);

            startX += keyData.Width + 34;

            key4 = new Key(50, 36, "4");
            key4.Location = new Point(startX, startY);
            Controls.Add(key4);

            startX += key4.Width + 34;

            key5 = new Key(50, 36, "5");
            key5.Location = new Point(startX, startY);
            Controls.Add(key5);

            startX += key5.Width + 34;

            key6 = new Key(50, 36, "6");
            key6.Location = new Point(startX, startY);
            Controls.Add(key6);

            startX += key6.Width + 34;

            key7 = new Key(50, 36, "7");
            key7.Location = new Point(startX, startY);
            Controls.Add(key7);

            startX  = 32;
            startY += key7.Height + 60;

            keyUserKey = new Key(50, 36, "USER");
            keyUserKey.SetBorderColor(Color.DarkKhaki);
            keyUserKey.Location = new Point(startX, startY);
            Controls.Add(keyUserKey);

            startX += keyUserKey.Width + 34;

            keyTapeRd = new Key(50, 36, "TAPER");
            keyTapeRd.SetBorderColor(Color.DarkKhaki);
            keyTapeRd.Location = new Point(startX, startY);
            Controls.Add(keyTapeRd);

            startX += keyTapeRd.Width + 34;

            keyGo = new Key(50, 36, "GO");
            keyGo.SetBorderColor(Color.DarkKhaki);
            keyGo.Location = new Point(startX, startY);
            Controls.Add(keyGo);

            startX += keyGo.Width + 34;

            keyPlus = new Key(50, 36, "+");
            keyPlus.SetBorderColor(Color.DarkKhaki);
            keyPlus.Location = new Point(startX, startY);
            Controls.Add(keyPlus);

            startX += keyPlus.Width + 34;

            keyAddr = new Key(50, 36, "ADDR");
            keyAddr.SetBorderColor(Color.DarkKhaki);
            keyAddr.Location = new Point(startX, startY);
            Controls.Add(keyAddr);

            startX += keyAddr.Width + 34;

            key0 = new Key(50, 36, "0");
            key0.Location = new Point(startX, startY);
            Controls.Add(key0);

            startX += key0.Width + 34;

            key1 = new Key(50, 36, "1");
            key1.Location = new Point(startX, startY);
            Controls.Add(key1);

            startX += key1.Width + 34;

            key2 = new Key(50, 36, "2");
            key2.Location = new Point(startX, startY);
            Controls.Add(key2);

            startX += key2.Width + 34;

            key3 = new Key(50, 36, "3");
            key3.Location = new Point(startX, startY);
            Controls.Add(key3);
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Form loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMPF1_Load(object sender, EventArgs e)
        {
            // Set location of window
            this.Location = new Point(x, y); 
        }

        #endregion
    }
}
