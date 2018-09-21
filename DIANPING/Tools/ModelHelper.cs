using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyTools
{

    public class ModelHelper<T> where T : new()  // 此处一定要加上new()
    {
        /// <summary>
        /// DataTable转List
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> DataTableToModel(DataTable dt)
        {
            List<T> list = new List<T>();// 定义集合
            Type type = typeof(T); // 获得此模型的类型
            string tempName = "";
            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                PropertyInfo[] propertys = t.GetType().GetProperties();// 获得此模型的公共属性
                foreach (PropertyInfo pro in propertys)
                {
                    tempName = pro.Name;
                    if (dt.Columns.Contains(tempName))
                    {
                        if (!pro.CanWrite) continue;
                        object value = dr[tempName];
                        if (value != DBNull.Value)
                            pro.SetValue(t, value, null);
                    }
                }
                list.Add(t);
            }
            return list; ;
        }
    }
}
