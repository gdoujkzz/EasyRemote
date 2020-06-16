﻿using AxMSTSCLib;
using ComponentOwl.BetterListView;
using NPOI.Extension;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EasyRemoteDesktop
{
    public partial class FormMain : Form
    {
        private AxMsRdpClient7NotSafeForScripting axMsRdpc = null;
        private bool isFullScreen = false;
        private List<string> axMsRdpcArray = null;

        public FormMain()
        {
            InitializeComponent();
            axMsRdpcArray = new List<string>();
        }

        #region 全局定义
        /// <summary>
        /// 创建远程桌面连接
        /// </summary>
        /// <param name="args">参数数组 new string[] { ServerIp, UserName, Password }</param>
        private void CreateAxMsRdpClient(string[] args)
        {
            string[] ServerIps = args[0].Split(':');

            Form axMsRdpcForm = new Form();
            axMsRdpcForm.ShowIcon = false;
            //axMsRdpcForm.StartPosition = FormStartPosition.Manual;
            axMsRdpcForm.Name = string.Format("Form_{0}", ServerIps[0].Replace(".", ""));
            axMsRdpcForm.Text = string.Format("{0} ({1})", args[3], ServerIps[0]);
            axMsRdpcForm.Size = new Size(1024, 768);
            axMsRdpcForm.FormClosed += new FormClosedEventHandler(this.axMsRdpcForm_Closed);

            //Rectangle ScreenArea = Screen.PrimaryScreen.Bounds;
            // 给axMsRdpc取个名字
            string _axMsRdpcName = string.Format("axMsRdpc_{0}", ServerIps[0].Replace(".", ""));
            if (axMsRdpcArray.Contains(_axMsRdpcName))
            {
                Global.WinMessage("此远程已经连接，请勿重复连接！"); return;
            }
            else
            {
                axMsRdpc = new AxMsRdpClient7NotSafeForScripting();
            }
            // 添加到当前缓存
            axMsRdpcArray.Add(_axMsRdpcName);

            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).BeginInit();
            axMsRdpc.Dock = DockStyle.Fill;
            axMsRdpc.Enabled = true;

            // 绑定连接与释放事件
            axMsRdpc.OnConnecting += new EventHandler(this.axMsRdpc_OnConnecting);
            axMsRdpc.OnDisconnected += new IMsTscAxEvents_OnDisconnectedEventHandler(this.axMsRdpc_OnDisconnected);

            axMsRdpcForm.Controls.Add(axMsRdpc);
            axMsRdpcForm.WindowState = FormWindowState.Maximized;
            axMsRdpcForm.Show();
            ((System.ComponentModel.ISupportInitialize)(axMsRdpc)).EndInit();

            // RDP名字
            axMsRdpc.Name = _axMsRdpcName;
            // 服务器地址
            axMsRdpc.Server = ServerIps[0];
            // 远程登录账号
            axMsRdpc.UserName = args[1];
            // 远程端口号
            axMsRdpc.AdvancedSettings7.RDPPort = ServerIps.Length == 1 ? 3389 : Convert.ToInt32(ServerIps[1]);
            //axMsRdpc.AdvancedSettings7.ContainerHandledFullScreen = 1;
            // 自动控制屏幕显示尺寸
            //axMsRdpc.AdvancedSettings7.SmartSizing = true;
            // 启用CredSSP身份验证（有些服务器连接没有反应，需要开启这个）
            axMsRdpc.AdvancedSettings7.EnableCredSspSupport = true;
            // 远程登录密码
            axMsRdpc.AdvancedSettings7.ClearTextPassword = args[2];
            // 禁用公共模式
            //axMsRdpc.AdvancedSettings7.PublicMode = false;
            // 颜色位数 8,16,24,32
            axMsRdpc.ColorDepth = 32;
            // 开启全屏 true|flase
            axMsRdpc.FullScreen = this.isFullScreen;
            // 设置远程桌面宽度为显示器宽度
            //axMsRdpc.DesktopWidth = ScreenArea.Width;
            axMsRdpc.DesktopWidth = axMsRdpcForm.ClientRectangle.Width;
            // 设置远程桌面宽度为显示器高度
            //axMsRdpc.DesktopHeight = ScreenArea.Height;
            axMsRdpc.DesktopHeight = axMsRdpcForm.ClientRectangle.Height;
            // 远程连接
            axMsRdpc.Connect();
        }

        /// <summary>
        /// 启动选中列表行的数据进行远程服务器连接
        /// </summary>
        private void SelectListViewRunRdpc()
        {
            if (this.betterListView1.SelectedItems.Count == 0) return;

            for (int i = 0; i < this.betterListView1.SelectedItems.Count; i++)
            {
                CreateAxMsRdpClient(new string[] {
                    this.betterListView1.SelectedItems[i].SubItems[0].Text,
                    this.betterListView1.SelectedItems[i].SubItems[1].Text,
                    this.betterListView1.SelectedItems[i].SubItems[2].Text,
                    this.betterListView1.SelectedItems[i].SubItems[3].Text
                });
            }
        }

        enum OperType
        {
            Add,
            Edit
        }

        /// <summary>
        /// 编辑数据
        /// </summary>
        private void AddOrEditListViewDataSource(OperType type)
        {
            AddServer frm = new AddServer();
            if (type == OperType.Edit)
            {
                if (this.betterListView1.SelectedItems.Count == 0) return;
                string serverIp = this.betterListView1.SelectedItems[0].SubItems[0].Text;
                frm._Action = "EDIT";
                frm._ServerIp = serverIp;
            }
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                BindsListViewDataSource();
            }
        }

        /// <summary>
        /// 根据服务器IP删除数据
        /// </summary>
        private void DeleteListViewDataSource()
        {
            if (this.betterListView1.SelectedItems.Count == 0) return;

            DialogResult result = MessageBox.Show("已选中 1 项数据，是否确认删除？", "删除提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                string serverIp = this.betterListView1.SelectedItems[0].SubItems[0].Text;
                string where = string.Format("c0='{0}'", serverIp);
                new TXTClass().txtDelete(Global.dbFile, '|', where);
                // 重新绑定数据源
                BindsListViewDataSource();
            }
        }

        /// <summary>
        /// 绑定数据源
        /// </summary>
        public void BindsListViewDataSource()
        {
            this.betterListView1.Items.Clear();
            DataTable dt = new TXTClass().txtRead(Global.dbFile, '|');
            foreach (DataRow row in dt.Rows)
            {
                BetterListViewItem item = new BetterListViewItem(new string[] {
                    row[0].ToString(),
                    row[1].ToString(),
                    row[2].ToString(),
                    row[3].ToString()
                });
                item.ImageIndex = 0;
                this.betterListView1.Items.Add(item);

            }
            this.tsItemLabel.Text = string.Format("共 {0} 项", dt.Rows.Count);
        }
        #endregion

        #region 主窗体
        // 主窗体-窗体加载
        private void FormMain_Load(object sender, EventArgs e)
        {
            BindsListViewDataSource();
        }
        // 主窗体-恢复窗体
        private void FormMain_SizeChanged(object sender, EventArgs e)
        {
            // 隐藏任务栏图标
            this.ShowInTaskbar = false;
        }
        #endregion

        #region ListView
        // ListView-双击打开远程连接
        private void betterListView1_DoubleClick(object sender, EventArgs e)
        {
            SelectListViewRunRdpc();
        }
        // ListView-至少选中一条数据才显示可用菜单项
        private void betterListView1_SelectedItemsChanged(object sender, BetterListViewSelectedItemsChangedEventArgs eventArgs)
        {
            if (this.betterListView1.SelectedItems.Count == 0)
            {
                this.tsbConnect.Enabled = false;
                this.tsbEdit.Enabled = false;
                this.tsbDel.Enabled = false;
                this.tsMenuConnect.Enabled = false;
                this.tsMenuEdit.Enabled = false;
                this.tsMenuDel.Enabled = false;
            }
            else
            {
                this.tsbConnect.Enabled = true;
                this.tsbEdit.Enabled = true;
                this.tsbDel.Enabled = true;
                this.tsMenuConnect.Enabled = true;
                this.tsMenuEdit.Enabled = true;
                this.tsMenuDel.Enabled = true;
            }
        }
        #endregion

        #region 菜单栏
        // 菜单栏-连接
        private void tsbConnect_Click(object sender, EventArgs e)
        {
            SelectListViewRunRdpc();
        }
        // 菜单栏-添加
        private void tsbAddData_Click(object sender, EventArgs e)
        {
            AddOrEditListViewDataSource(OperType.Add);
        }
        // 菜单栏-编辑
        private void tsbEdit_Click(object sender, EventArgs e)
        {
            AddOrEditListViewDataSource(OperType.Edit);
        }
        // 菜单栏-删除
        private void tsbDel_Click(object sender, EventArgs e)
        {
            DeleteListViewDataSource();
        }
        // 菜单栏-全屏模式
        private void tsbFullScreen_Click(object sender, EventArgs e)
        {
            this.isFullScreen = !this.isFullScreen;
            this.WindowState = !this.isFullScreen ? FormWindowState.Normal : FormWindowState.Maximized;
            if (this.isFullScreen)
            {
                this.tsbFullScreen.Text = "取消全屏";
                this.tsbFullScreen.ForeColor = Color.Gray;
            }
            else
            {
                this.tsbFullScreen.Text = "全屏模式";
                this.tsbFullScreen.ForeColor = Color.OliveDrab;
            }
        }
        // 菜单栏-关于
        private void tsbAbout_Click(object sender, EventArgs e)
        {
            FormAbout frm = new FormAbout();
            frm.ShowDialog();
        }
        #endregion

        #region 右键菜单
        // 右键菜单-连接
        private void tsMenuConnect_Click(object sender, EventArgs e)
        {
            SelectListViewRunRdpc();
        }
        // 右键菜单-编辑
        private void tsMenuEdit_Click(object sender, EventArgs e)
        {
            AddOrEditListViewDataSource(OperType.Edit);
        }
        // 右键菜单-删除
        private void tsMenuDel_Click(object sender, EventArgs e)
        {
            DeleteListViewDataSource();
        }
        #endregion

        #region 托盘菜单
        // 托盘菜单-双击托盘图标
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.tsMenuNotifyShow_Click(sender, e);
        }
        // 托盘菜单-显示窗口
        private void tsMenuNotifyShow_Click(object sender, EventArgs e)
        {
            // 还原窗口
            WindowState = FormWindowState.Normal;
            // 显示任务栏图标
            this.ShowInTaskbar = true;
        }
        // 托盘菜单-退出
        private void tsMenuNotifyExit_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Application.Exit();
        }
        #endregion

        #region 远程桌面组件axMsRdpc
        // 远程桌面-连接
        private void axMsRdpc_OnConnecting(object sender, EventArgs e)
        {
            var _axMsRdp = sender as AxMsRdpClient7NotSafeForScripting;
            _axMsRdp.ConnectingText = _axMsRdp.GetStatusText(Convert.ToUInt32(_axMsRdp.Connected));
            _axMsRdp.FindForm().WindowState = FormWindowState.Normal;
        }
        // 远程桌面-连接断开
        private void axMsRdpc_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            var _axMsRdp = sender as AxMsRdpClient7NotSafeForScripting;
            string disconnectedText = string.Format("远程桌面 {0} 连接已断开！", _axMsRdp.Server);
            _axMsRdp.DisconnectedText = disconnectedText;
            _axMsRdp.FindForm().Close();
            Global.WinMessage(disconnectedText, "远程连接");

        }
        #endregion

        #region 远程桌面窗体axMsRdpcForm
        // 远程桌面窗体-关闭
        private void axMsRdpcForm_Closed(object sender, FormClosedEventArgs e)
        {
            Form frm = (Form)sender;
            //MessageBox.Show(frm.Controls[0].GetType().ToString());
            foreach (Control ctrl in frm.Controls)
            {
                // 找到当前打开窗口下面的远程桌面
                if (ctrl.GetType().ToString() == "AxMSTSCLib.AxMsRdpClient7NotSafeForScripting")
                {
                    // 释放缓存
                    if (axMsRdpcArray.Contains(ctrl.Name)) axMsRdpcArray.Remove(ctrl.Name);
                    // 断开连接
                    var _axMsRdp = ctrl as AxMsRdpClient7NotSafeForScripting;
                    if (_axMsRdp.Connected != 0)
                    {
                        _axMsRdp.Disconnect();
                        _axMsRdp.Dispose();
                    }
                }
            }
        }
        #endregion

        private void btn_export_Click(object sender, EventArgs e)
        {
            try
            {
                //选择文件的功能做一下。
                string excelPath = null;
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Title = "请选择文件";
                fileDialog.Filter = "所有文件(*xls*)|*.xls*";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    excelPath = fileDialog.FileName;
                }
                if (excelPath == null)
                {
                    MessageBox.Show("清选择文件");
                    return;
                }
                var serverInfos = Excel.Load<ServerInfo>(excelPath);
                foreach (var server in serverInfos)
                {
                    string userName = server.UserName.Split('/')[0].ToString();
                    string pwd = server.Password.Split('/')[1].ToString();
                    BetterListViewItem item = new BetterListViewItem(new string[] {
                    server.Ip.ToString(),
                    userName,
                    pwd,
                    server.Remark
                    });
                    item.ImageIndex = 0;
                    string data = string.Format("{0}|{1}|{2}|{3}", server.Ip, userName, pwd, server.Remark);
                    new TXTClass().txtWrite(Global.dbFile, data);
                    //this.betterListView1.Items.Add(item);
                }
                BindsListViewDataSource();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tsbBatchDel_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("是否确认全部删除？", "删除提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                new TXTClass().txtBatchDelete(Global.dbFile);
                // 重新绑定数据源
                BindsListViewDataSource();
            }

        }
    }


    public class ServerInfo
    {
        [Column(Index = 1, Title = "账号密码")]
        public string UserName { get; set; }


        [Column(Index = 0, Title = "ip")]
        public string Ip { get; set; }


        [Column(Index = 1, Title = "账号密码")]
        public string Password { get; set; }


        [Column(Index = 2, Title = "备注")]
        public string Remark { get; set; }

        public override string ToString()
        {
            return Ip + UserName + Remark + Password;
        }

    }
}
