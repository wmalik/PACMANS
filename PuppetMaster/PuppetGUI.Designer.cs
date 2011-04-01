namespace PuppetMaster
{
    partial class PuppetGUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Servers", 2, 2);
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Clients", 8, 8);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PuppetGUI));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.rightClickStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.disconnectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.readCalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.createRes = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.southPanel = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.consoleBox = new System.Windows.Forms.TextBox();
            this.loadEventsButton = new System.Windows.Forms.Button();
            this.slotsBox = new System.Windows.Forms.TextBox();
            this.usersBox = new System.Windows.Forms.TextBox();
            this.descBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.rightClickStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.southPanel.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.southPanel);
            this.splitContainer1.Panel2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.splitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer1.Size = new System.Drawing.Size(784, 564);
            this.splitContainer1.SplitterDistance = 335;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.loadEventsButton);
            this.splitContainer2.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer2.Size = new System.Drawing.Size(784, 335);
            this.splitContainer2.SplitterDistance = 236;
            this.splitContainer2.TabIndex = 0;
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.rightClickStrip;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            treeNode3.ImageIndex = 2;
            treeNode3.Name = "Servers";
            treeNode3.SelectedImageIndex = 2;
            treeNode3.Text = "Servers";
            treeNode4.ImageIndex = 8;
            treeNode4.Name = "Clients";
            treeNode4.SelectedImageIndex = 8;
            treeNode4.Text = "Clients";
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3,
            treeNode4});
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.Size = new System.Drawing.Size(236, 335);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // rightClickStrip
            // 
            this.rightClickStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.disconnectMenuItem,
            this.connectMenuItem,
            this.readCalMenuItem});
            this.rightClickStrip.Name = "rightClickStrip";
            this.rightClickStrip.Size = new System.Drawing.Size(151, 70);
            this.rightClickStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // disconnectMenuItem
            // 
            this.disconnectMenuItem.Name = "disconnectMenuItem";
            this.disconnectMenuItem.Size = new System.Drawing.Size(150, 22);
            this.disconnectMenuItem.Text = "Disconnect";
            this.disconnectMenuItem.Click += new System.EventHandler(this.disconnectMenuItem_Click);
            // 
            // connectMenuItem
            // 
            this.connectMenuItem.Name = "connectMenuItem";
            this.connectMenuItem.Size = new System.Drawing.Size(150, 22);
            this.connectMenuItem.Text = "Connect";
            this.connectMenuItem.Click += new System.EventHandler(this.connectMenuItem_Click);
            // 
            // readCalMenuItem
            // 
            this.readCalMenuItem.Name = "readCalMenuItem";
            this.readCalMenuItem.Size = new System.Drawing.Size(150, 22);
            this.readCalMenuItem.Text = "Read Calendar";
            this.readCalMenuItem.Click += new System.EventHandler(this.readCalMenuItem_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Button White Check.png");
            this.imageList1.Images.SetKeyName(1, "Stop.png");
            this.imageList1.Images.SetKeyName(2, "Database.png");
            this.imageList1.Images.SetKeyName(3, "iPhone.png");
            this.imageList1.Images.SetKeyName(4, "colloquy.ico");
            this.imageList1.Images.SetKeyName(5, "delete.ico");
            this.imageList1.Images.SetKeyName(6, "plus.ico");
            this.imageList1.Images.SetKeyName(7, "tick.ico");
            this.imageList1.Images.SetKeyName(8, "user.ico");
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.descBox);
            this.groupBox1.Controls.Add(this.usersBox);
            this.groupBox1.Controls.Add(this.slotsBox);
            this.groupBox1.Controls.Add(this.createRes);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(6, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(217, 128);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Create Reservation";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // createRes
            // 
            this.createRes.Location = new System.Drawing.Point(68, 98);
            this.createRes.Name = "createRes";
            this.createRes.Size = new System.Drawing.Size(100, 23);
            this.createRes.TabIndex = 2;
            this.createRes.Text = "Create";
            this.createRes.UseVisualStyleBackColor = true;
            this.createRes.Click += new System.EventHandler(this.createRes_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Slots";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Users";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Description";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // southPanel
            // 
            this.southPanel.AutoSize = true;
            this.southPanel.Controls.Add(this.tabControl1);
            this.southPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.southPanel.Location = new System.Drawing.Point(0, 0);
            this.southPanel.Name = "southPanel";
            this.southPanel.Size = new System.Drawing.Size(784, 225);
            this.southPanel.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(784, 225);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.consoleBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(776, 199);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Console";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // consoleBox
            // 
            this.consoleBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.consoleBox.Location = new System.Drawing.Point(3, 3);
            this.consoleBox.Multiline = true;
            this.consoleBox.Name = "consoleBox";
            this.consoleBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.consoleBox.Size = new System.Drawing.Size(770, 193);
            this.consoleBox.TabIndex = 0;
            // 
            // loadEventsButton
            // 
            this.loadEventsButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.loadEventsButton.Location = new System.Drawing.Point(0, 312);
            this.loadEventsButton.Name = "loadEventsButton";
            this.loadEventsButton.Size = new System.Drawing.Size(236, 23);
            this.loadEventsButton.TabIndex = 1;
            this.loadEventsButton.Text = "Load events from file";
            this.loadEventsButton.UseVisualStyleBackColor = true;
            this.loadEventsButton.Click += new System.EventHandler(this.loadEventsButton_Click);
            // 
            // slotsBox
            // 
            this.slotsBox.Location = new System.Drawing.Point(72, 71);
            this.slotsBox.Name = "slotsBox";
            this.slotsBox.Size = new System.Drawing.Size(100, 20);
            this.slotsBox.TabIndex = 3;
            this.slotsBox.Text = "1,2,3";
            // 
            // usersBox
            // 
            this.usersBox.Location = new System.Drawing.Point(72, 45);
            this.usersBox.Name = "usersBox";
            this.usersBox.Size = new System.Drawing.Size(100, 20);
            this.usersBox.TabIndex = 3;
            this.usersBox.Text = "Client5,Client2";
            // 
            // descBox
            // 
            this.descBox.Location = new System.Drawing.Point(72, 19);
            this.descBox.Name = "descBox";
            this.descBox.Size = new System.Drawing.Size(100, 20);
            this.descBox.TabIndex = 3;
            this.descBox.Text = "Birthday Party";
            // 
            // PuppetGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(784, 564);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PuppetGUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PACMANS - Puppet Master";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.PuppetGUI_Shown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.rightClickStrip.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.southPanel.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button createRes;
        private System.Windows.Forms.Panel southPanel;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TextBox consoleBox;
        private System.Windows.Forms.ContextMenuStrip rightClickStrip;
        private System.Windows.Forms.ToolStripMenuItem disconnectMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectMenuItem;
        private System.Windows.Forms.ToolStripMenuItem readCalMenuItem;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button loadEventsButton;
        private System.Windows.Forms.TextBox descBox;
        private System.Windows.Forms.TextBox usersBox;
        private System.Windows.Forms.TextBox slotsBox;
    }
}

