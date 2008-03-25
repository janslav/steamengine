namespace SteamEngine.RemoteConsole {
	partial class ConnectionForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.chkSavePassword = new System.Windows.Forms.CheckBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.txtAddress = new System.Windows.Forms.TextBox();
			this.endPointSettingBindingSource = new System.Windows.Forms.BindingSource(this.components);
			this.txtPort = new System.Windows.Forms.TextBox();
			this.txtUserName = new System.Windows.Forms.TextBox();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.ddName = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btnConnect = new System.Windows.Forms.Button();
			this.btnNew = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.txtName = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.btnClear = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize) (this.endPointSettingBindingSource)).BeginInit();
			this.SuspendLayout();
			// 
			// chkSavePassword
			// 
			this.chkSavePassword.AutoSize = true;
			this.chkSavePassword.Location = new System.Drawing.Point(108, 146);
			this.chkSavePassword.Name = "chkSavePassword";
			this.chkSavePassword.Size = new System.Drawing.Size(15, 14);
			this.chkSavePassword.TabIndex = 17;
			this.chkSavePassword.CheckedChanged += new System.EventHandler(this.chkSavePassword_CheckedChanged);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(196, 169);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(80, 23);
			this.btnCancel.TabIndex = 19;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 94);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(48, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Address:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(93, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Connection name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// txtAddress
			// 
			this.txtAddress.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.endPointSettingBindingSource, "Address", true));
			this.txtAddress.Location = new System.Drawing.Point(108, 91);
			this.txtAddress.Name = "txtAddress";
			this.txtAddress.Size = new System.Drawing.Size(249, 20);
			this.txtAddress.TabIndex = 9;
			this.txtAddress.TextChanged += new System.EventHandler(this.txtAddress_TextChanged);
			// 
			// endPointSettingBindingSource
			// 
			this.endPointSettingBindingSource.DataSource = typeof(SteamEngine.RemoteConsole.EndPointSetting);
			// 
			// txtPort
			// 
			this.txtPort.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.endPointSettingBindingSource, "Port", true));
			this.txtPort.Location = new System.Drawing.Point(398, 91);
			this.txtPort.Name = "txtPort";
			this.txtPort.Size = new System.Drawing.Size(48, 20);
			this.txtPort.TabIndex = 11;
			this.txtPort.TextChanged += new System.EventHandler(this.txtPort_TextChanged);
			// 
			// txtUserName
			// 
			this.txtUserName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.endPointSettingBindingSource, "UserName", true));
			this.txtUserName.Location = new System.Drawing.Point(108, 117);
			this.txtUserName.Name = "txtUserName";
			this.txtUserName.Size = new System.Drawing.Size(135, 20);
			this.txtUserName.TabIndex = 13;
			this.txtUserName.TextChanged += new System.EventHandler(this.txtUserName_TextChanged);
			// 
			// txtPassword
			// 
			this.txtPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.endPointSettingBindingSource, "Password", true));
			this.txtPassword.Location = new System.Drawing.Point(311, 117);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(135, 20);
			this.txtPassword.TabIndex = 15;
			// 
			// ddName
			// 
			this.ddName.DataSource = this.endPointSettingBindingSource;
			this.ddName.DisplayMember = "Name";
			this.ddName.Location = new System.Drawing.Point(108, 9);
			this.ddName.Name = "ddName";
			this.ddName.Size = new System.Drawing.Size(338, 21);
			this.ddName.TabIndex = 1;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(363, 94);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Port:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 120);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(58, 13);
			this.label4.TabIndex = 12;
			this.label4.Text = "Username:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(249, 120);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(56, 13);
			this.label5.TabIndex = 14;
			this.label5.Text = "Password:";
			// 
			// btnConnect
			// 
			this.btnConnect.Enabled = false;
			this.btnConnect.Location = new System.Drawing.Point(108, 169);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(80, 23);
			this.btnConnect.TabIndex = 18;
			this.btnConnect.Text = "Connect";
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// btnNew
			// 
			this.btnNew.Location = new System.Drawing.Point(108, 36);
			this.btnNew.Name = "btnNew";
			this.btnNew.Size = new System.Drawing.Size(80, 23);
			this.btnNew.TabIndex = 2;
			this.btnNew.Text = "New";
			this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
			// 
			// btnRemove
			// 
			this.btnRemove.Location = new System.Drawing.Point(194, 36);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(80, 23);
			this.btnRemove.TabIndex = 3;
			this.btnRemove.Text = "Remove";
			this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
			// 
			// txtName
			// 
			this.txtName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.endPointSettingBindingSource, "Name", true));
			this.txtName.Location = new System.Drawing.Point(108, 65);
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(338, 20);
			this.txtName.TabIndex = 7;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 68);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(63, 13);
			this.label6.TabIndex = 6;
			this.label6.Text = "New Name:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(12, 146);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(88, 13);
			this.label7.TabIndex = 16;
			this.label7.Text = "Save passwords:";
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(280, 36);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(80, 23);
			this.btnClear.TabIndex = 4;
			this.btnClear.Text = "Clear";
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// btnSave
			// 
			this.btnSave.Location = new System.Drawing.Point(366, 36);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(80, 23);
			this.btnSave.TabIndex = 5;
			this.btnSave.Text = "Save";
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// ConnectionForm
			// 
			this.AcceptButton = this.btnConnect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(459, 205);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.btnClear);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.txtName);
			this.Controls.Add(this.chkSavePassword);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtAddress);
			this.Controls.Add(this.txtPort);
			this.Controls.Add(this.txtUserName);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.ddName);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.btnNew);
			this.Controls.Add(this.btnRemove);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConnectionForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "Connect";
			((System.ComponentModel.ISupportInitialize) (this.endPointSettingBindingSource)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkSavePassword;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtAddress;
		private System.Windows.Forms.TextBox txtPort;
		private System.Windows.Forms.TextBox txtUserName;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.ComboBox ddName;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button btnConnect;
		private System.Windows.Forms.Button btnNew;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.TextBox txtName;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.BindingSource endPointSettingBindingSource;
		private System.Windows.Forms.Button btnSave;
	}
}