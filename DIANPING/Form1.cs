using MyTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DIANPING
{
    public partial class Form1 : Form
    {
        public SeleniumHelper sel = null;
        public string mainUrl = "http://www.dianping.com";
        public string workId = "ww-0006";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            sel = new SeleniumHelper();
            sel.driver.Navigate().GoToUrl(mainUrl);

            MessageBoxButtons message = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("请先登录成功后，再点击确定！", "DIANPING", message);
            if (dr == DialogResult.OK)
            {

            }
        }       
        /// <summary>
        /// 判断字符串是不是数字类型的 true是数字
        /// </summary>
        /// <param name="value">需要检测的字符串</param>
        /// <returns>true是数字</returns>
        public bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^\d(\.\d+)?|[1-9]\d+(\.\d+)?$");
        }
        /// <summary>
        /// 日志打印
        /// </summary>
        /// <param name="log"></param>
        public void WriteLog(string log)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "log\\";//日志文件夹
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)//判断文件夹是否存在
                    dir.Create();//不存在则创建

                FileInfo[] subFiles = dir.GetFiles();//获取该文件夹下的所有文件
                foreach (FileInfo f in subFiles)
                {
                    string fname = Path.GetFileNameWithoutExtension(f.FullName); //获取文件名，没有后缀
                    DateTime start = Convert.ToDateTime(fname);//文件名转换成时间
                    DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
                    TimeSpan sp = end.Subtract(start);//计算时间差
                    if (sp.Days > 30)//大于30天删除
                        f.Delete();
                }

                string logName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";//日志文件名称，按照当天的日期命名
                string fullPath = path + logName;//日志文件的完整路径
                string contents = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -> " + log + "\r\n";//日志内容

                File.AppendAllText(fullPath, contents, Encoding.UTF8);//追加日志

            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// 授权
        /// </summary>
        /// <param name="workId"></param>
        /// <returns></returns>
        public bool IsAuthorised(string workId)
        {
            string conStr = "Server=111.230.149.80;DataBase=MyDB;uid=sa;pwd=1add1&one";
            bool bo = false;
            try
            {
                using (SqlConnection con = new SqlConnection(conStr))
                {
                    string sql = string.Format("select count(*) from MyWork Where PassState = 1 and WorkId ='{0}'", workId);
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        con.Open();
                        int count = int.Parse(cmd.ExecuteScalar().ToString());
                        if (count > 0)
                            bo = true;
                    }
                }
            }
            catch (Exception)
            {
                bo = false;
            }

            return bo;
        }
    }
}
