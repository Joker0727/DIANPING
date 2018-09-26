using MyTools;
using OpenQA.Selenium;
using SqlLiteHelperDemo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string cityUrl = string.Empty;
        public string workId = "ww-0006";
        public bool isLogin = false;
        public int totalPage = 0;
        public string searchWord,
            cityPinyin,
            searchUrlPrefix,
            searchUrl = string.Empty;
        public SQLiteHelper sh = null;
        public string basePath = AppDomain.CurrentDomain.BaseDirectory;
        public string dataFullPath = AppDomain.CurrentDomain.BaseDirectory + "sqlite3.db";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sel = new SeleniumHelper(1);
            sel.driver.Navigate().GoToUrl(mainUrl);

            MessageBoxButtons message = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("请先登录成功后，再点击确定！", "DIANPING", message);
            if (dr == DialogResult.OK)
                isLogin = true;
            else
                isLogin = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isLogin)
            {
                MessageBox.Show("请退出程序重新你登陆！", "DIANPING");
                return;
            }
            cityPinyin = this.textBox1.Text;
            searchWord = this.textBox2.Text;

            if (string.IsNullOrEmpty(cityPinyin))
            {
                MessageBox.Show("城市拼音不能为空！", "DIANPING");
                return;
            }
            if (string.IsNullOrEmpty(searchWord))
            {
                MessageBox.Show("搜索关键词不能为空！", "DIANPING");
                return;
            }
            cityUrl = mainUrl + "/" + cityPinyin;

            GetShopPages();
        }
        /// <summary>
        /// 获取店铺页数
        /// </summary>
        public void GetShopPages()
        {
            sel.driver.Navigate().GoToUrl(cityUrl);
            ReadOnlyCollection<IWebElement> searchWordList = sel.FindElementsByClassName("search-word");
            if (searchWordList != null && searchWordList.Count > 0)
            {
                searchUrlPrefix = searchWordList[0].GetAttribute("href").Split('_')[0] + "_";
                searchUrl = searchUrlPrefix + searchWord;
                sel.driver.Navigate().GoToUrl(searchUrl);
                ((IJavaScriptExecutor)sel.driver).ExecuteScript("location.reload()");
                ReadOnlyCollection<IWebElement> pageNodeList = sel.FindElementsByClassName("PageLink");
                if (pageNodeList != null && pageNodeList.Count > 0)
                {
                    int pageCount = pageNodeList.Count();
                    string pageUrl = string.Empty;
                    totalPage = int.Parse(pageNodeList[pageCount - 1].Text);
                    for (int i = 1; i < totalPage; i++)
                    {
                        pageUrl = searchUrl + "/p" + i;
                        sel.driver.Navigate().GoToUrl(pageUrl);
                        GetShopUrl();
                    }
                }
            }
        }
        /// <summary>
        /// 获取所有店铺链接
        /// </summary>
        public void GetShopUrl()
        {
            ReadOnlyCollection<IWebElement> shopNodeList = sel.FindElementsByXPath("//div[@id='shop-all-list']/ul/li/div[@class='pic']/a");
            if (shopNodeList != null && shopNodeList.Count > 0)
            {
                string shopUrl, sqlStr = string.Empty;
                sh = new SQLiteHelper(dataFullPath);

                foreach (var shopNode in shopNodeList)
                {
                    try
                    {
                        shopUrl = shopNode.GetAttribute("href");
                        sqlStr = "inster into Shops (ShopUrl,IsPraise,IsUpload) values ('" + shopUrl + "',0,0)";
                        sh.RunSql(sqlStr);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.ToString());
                    }
                }
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
