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
using System.Threading;
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
        public SQLiteHelper sh = null;
        public string basePath = AppDomain.CurrentDomain.BaseDirectory;
        public string dataFullPath = AppDomain.CurrentDomain.BaseDirectory + "sqlite3.db";
        public string searchWord,
                      cityPinyin,
                      searchUrlPrefix,
                      searchUrl = string.Empty;
        public Thread praiseTh,
                      upLoadTh = null;
        public string picFolderPath = string.Empty;
        public List<string> picList = new List<string>();
        public bool isOk = true;
        public int praiseCount = 0;
        public int upLoadCount = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;
            if (!File.Exists(dataFullPath))
            {
                MessageBox.Show("本地数据库文件存在！", "DIANPING");
                isOk = false;
            }
            sh = new SQLiteHelper(dataFullPath);
            sel = new SeleniumHelper(1);
            sel.driver.Navigate().GoToUrl(mainUrl);

            MessageBoxButtons message = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("请先登录成功后，再点击确定！", "DIANPING", message);
            if (dr == DialogResult.OK)
                isLogin = true;
            else
                isLogin = false;
        }
        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.button2.Text = "上传图片";
            string btnText = this.button1.Text;
            if (!isOk)
                return;
            if (btnText == "开始点赞")
            {
                if (!IsAuthorised())
                {
                    MessageBox.Show("请检查网络！", "DIANPING");
                    return;
                }
                if (!isLogin)
                {
                    MessageBox.Show("请退出程序重新你登陆！", "DIANPING");
                    return;
                }
                if (upLoadTh != null)
                {
                    upLoadTh.Abort();
                    upLoadTh = null;
                    this.button2.Enabled = false;
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

                this.button1.Text = "暂停";
                praiseTh = new Thread(GetShopPages);
                praiseTh.IsBackground = true;
                praiseTh.Start();
            }
            else
            {
                this.button2.Enabled = true;
                if (praiseTh != null)
                {
                    praiseTh.Abort();
                    praiseTh = null;
                    this.button1.Text = "开始点赞";
                }
            }
        }
        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.button1.Text = "开始点赞";
            string btn2Text = this.button2.Text;
            if (!isOk)
                return;
            if (btn2Text == "上传图片")
            {
                if (!IsAuthorised())
                {
                    MessageBox.Show("请检查网络！", "DIANPING");
                    return;
                }
                if (!isLogin)
                {
                    MessageBox.Show("请退出程序重新你登陆！", "DIANPING");
                    return;
                }
                if (praiseTh != null)
                {
                    praiseTh.Abort();
                    praiseTh = null;
                    this.button1.Enabled = false;
                }
                picFolderPath = this.textBox3.Text;
                if (string.IsNullOrEmpty(picFolderPath))
                {
                    MessageBox.Show("待上传图片所在文件夹路径不能为空！", "DIANPING");
                    return;
                }
                this.button2.Text = "暂停";
                if (!ReadAllPicPath())
                    return;
                upLoadTh = new Thread(ToUpLoadPage);
                upLoadTh.IsBackground = true;
                upLoadTh.Start();
            }
            else
            {
                this.button1.Enabled = true;
                if (upLoadTh != null)
                {
                    upLoadTh.Abort();
                    upLoadTh = null;
                    this.button2.Text = "上传图片";
                }
            }
        }
        /// <summary>
        /// 点击选择待上传图片文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择待上传图片所在文件夹";
            if (dialog.ShowDialog() == DialogResult.OK)
                this.textBox3.Text = dialog.SelectedPath;
        }
        /// <summary>
        /// 获取店铺页数
        /// </summary>
        public void GetShopPages()
        {
            try
            {
                sel.driver.Navigate().GoToUrl(cityUrl);
                ReadOnlyCollection<IWebElement> searchWordList = sel.FindElementsByClassName("search-word");
                if (searchWordList != null && searchWordList.Count > 0)
                {
                    searchUrlPrefix = searchWordList[0].GetAttribute("href").Split('_')[0] + "_";
                    searchUrl = searchUrlPrefix + searchWord;
                    sel.driver.Navigate().GoToUrl(searchUrl);

                    ReadOnlyCollection<IWebElement> pageNodeList = sel.FindElementsByClassName("PageLink");
                    if (pageNodeList != null && pageNodeList.Count > 0)
                    {
                        int pageCount = pageNodeList.Count();
                        string pageUrl = string.Empty;
                        totalPage = int.Parse(pageNodeList[pageCount - 1].Text);
                        for (int i = 1; i < totalPage; i++)
                        {
                            try
                            {
                                pageUrl = searchUrl + "/p" + i;
                                sel.driver.Navigate().GoToUrl(pageUrl);
                                GetShopUrl();
                            }
                            catch (Exception ex)
                            {
                                WriteLog(ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }
        /// <summary>
        /// 获取所有店铺链接
        /// </summary>
        public void GetShopUrl()
        {
            try
            {
                ReadOnlyCollection<IWebElement> shopNodeList = sel.FindElementsByXPath("//div[@id='shop-all-list']/ul/li/div[@class='pic']/a");
                if (shopNodeList != null && shopNodeList.Count > 0)
                {
                    string shopUrl, sqlStr = string.Empty;
                    List<string> tempUrlList = new List<string>();
                    foreach (var shopNode in shopNodeList)
                    {
                        try
                        {
                            shopUrl = shopNode.GetAttribute("href");
                            tempUrlList.Add(shopUrl);
                            sqlStr = "insert into Shops (ShopUrl,IsUpLoad) values ('" + shopUrl + "',0)";
                            int urlId = sh.ExeSqlOut(sqlStr);
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex.ToString());
                        }
                    }
                    foreach (var tempUrl in tempUrlList)
                    {
                        ToPraise(tempUrl);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog(e.ToString());
            }
        }
        /// <summary>
        /// 点赞
        /// </summary>
        public bool ToPraise(string shopUrl)
        {
            bool isAllPraise = false;
            try
            {
                sel.driver.Navigate().GoToUrl(shopUrl);
                ReadOnlyCollection<IWebElement> praiseNodeList = sel.FindElementsByCss(".item.J-praise");
                if (praiseNodeList != null && praiseNodeList.Count > 0)
                {
                    for (int i = 0; i < praiseNodeList.Count; i++)
                    {
                        try
                        {
                            string jsStr = "document.getElementsByClassName('item J-praise')[" + i + "].click();";
                            ((IJavaScriptExecutor)sel.driver).ExecuteScript(jsStr); //需要将driver强制转换成JS执行器类型
                            praiseCount++;
                            this.label6.Invoke(new Action(() =>
                            {
                                this.label6.Text = praiseCount.ToString();
                            }));
                        }
                        catch (Exception ex)
                        {
                            isAllPraise = false;
                            WriteLog(ex.ToString());
                        }
                    }
                    isAllPraise = true;
                }
            }
            catch (Exception e)
            {
                isAllPraise = false;
                WriteLog(e.ToString());
            }
            return isAllPraise;
        }

        /// <summary>
        /// 读取所有待上传图片路径
        /// </summary>
        public bool ReadAllPicPath()
        {
            if (!Directory.Exists(picFolderPath))
            {
                MessageBox.Show("文件夹路径不存在！", "DIANPING");
                return false;
            }

            DirectoryInfo dir = new DirectoryInfo(picFolderPath);
            FileInfo[] fileInfo = dir.GetFiles();

            foreach (FileInfo subinfo in fileInfo)
            {
                try
                {
                    if (subinfo.Extension.ToLower() == ".jpg" ||
                   subinfo.Extension.ToLower() == ".png" ||
                   subinfo.Extension.ToLower() == ".bmp" ||
                   subinfo.Extension.ToLower() == ".jpeg")
                    {
                        string fullPath = subinfo.FullName;
                        picList.Add(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }

            return true;
        }
        /// <summary>
        /// 获取所有需要上传图片的店铺的链接
        /// </summary>
        public List<string> GetAllUpLoadShopUrl()
        {
            string sqlStr = "select ShopUrl from Shops where IsUpLoad = 0";
            string shopUrl = string.Empty;
            List<string> urlList = new List<string>();
            object[] objArr = sh.GetField(sqlStr);
            foreach (var obj in objArr)
            {
                try
                {
                    shopUrl = obj.ToString();
                    urlList.Add(shopUrl);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
            return urlList;
        }
        /// <summary>
        /// 进入上传页面
        /// </summary>
        public void ToUpLoadPage()
        {
            List<string> upLoadShopUrlList = GetAllUpLoadShopUrl();
            string tempStr = "http://www.dianping.com/shop/";
            string shopIdStr, upLoadUrl = string.Empty;

            if (upLoadShopUrlList != null && upLoadShopUrlList.Count > 0)
            {
                foreach (var url in upLoadShopUrlList)
                {
                    try
                    {
                        shopIdStr = url.Replace(tempStr, "").Trim();
                        if (!IsNumeric(shopIdStr))
                            continue;
                        upLoadUrl = "http://www.dianping.com/upload/shop/" + shopIdStr;
                        sel.driver.Navigate().GoToUrl(upLoadUrl);
                        foreach (var pic in picList)
                        {
                            IWebElement addPicBtnNode = sel.FindElementByName("file");
                            addPicBtnNode.SendKeys(pic);
                            Thread.Sleep(2000);
                        }
                        IWebElement btn_save = sel.FindElementByClassName("btn_save");
                        if (btn_save != null)
                        {
                            btn_save.Click();
                            break;
                        }
                        upLoadCount++;
                        this.label7.Invoke(new Action(() =>
                        {
                            this.label7.Text = upLoadCount.ToString();
                        }));

                        string sqlStr = "UPDATE  Shops SET IsUpLoad = 1 where ShopUrl = '" + url + "'";
                        sh.RunSql(sqlStr);

                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.ToString());
                    }
                }
            }
        }
        /// <summary>
        /// 随机获取图片路径
        /// </summary>
        /// <returns></returns>
        public string GetPicPath()
        {
            string picPath = string.Empty;
            try
            {
                int length = picList.Count();
                Random rd = new Random();
                int sub = rd.Next(0, length);
                picPath = picList[sub];
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
            return picPath;
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
        public bool IsAuthorised()
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
