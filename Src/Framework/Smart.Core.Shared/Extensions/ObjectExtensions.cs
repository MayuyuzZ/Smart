﻿using Newtonsoft.Json.Linq;
using Smart.Core.Data;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Smart.Core.Extensions
{
    /// <summary>
    /// 对象扩展方法
    /// </summary>
    public static class ObjectExtensions
    {
        #region As

        /// <summary>将值转换为指定的数据类型的值。</summary>
        /// <typeparam name="TValue">转换的数据类型。</typeparam>
        /// <param name="value">要转换的值。</param>
        /// <returns>转换后的值。</returns>
        public static TValue As<TValue>(this object value)
        {
            return value.As(default(TValue));
        }

        /// <summary>将值转换为指定的数据类型和指定默认值。</summary>
        /// <typeparam name="TValue">转换的数据类型。</typeparam>
        /// <param name="value">要转换的值。</param>
        /// <param name="defaultValue">如果 <paramref name="value" /> 为空或是一个无效的值则返回该值,。</param>
        /// <returns>转换后的值。</returns>
        public static TValue As<TValue>(this object value, TValue defaultValue)
        {
            if (value == null || value is DBNull) return defaultValue;
            try
            {
                var targetType = typeof(TValue);
                var valueType = value.GetType();

                if (targetType.IsAssignableFrom(valueType))
                {
                    return (TValue)value;
                }
                else if (value is JToken jtoken)
                {
                    return jtoken.Value<TValue>();
                }
                else if (valueType.IsEnum)
                {
                    return (TValue)value;
                }
                else if (targetType.IsEnum)
                {
                    if (valueType == typeof(int))
                    {
                        return (TValue)Enum.ToObject(targetType, value);
                    }
                    else
                    {
                        return (TValue)Enum.Parse(targetType, value.ToString());
                    }
                }
                // 类型是否可以从对象转换
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(value.GetType()))
                {
                    TValue result = (TValue)converter.ConvertFrom(value);
                    return result;
                }

                // 值是否可以转换为指定的类型
                converter = TypeDescriptor.GetConverter(value.GetType());
                if (converter.CanConvertTo(targetType))
                {
                    TValue result = (TValue)converter.ConvertTo(value, targetType);
                    return result;
                }
            }
            catch
            {
            }
            return defaultValue;
        }

        /// <summary>
        /// 将值转换为字符串
        /// </summary>
        /// <param name="value">要转换的值。</param>
        /// <returns>返回表示当前对象的字符串，如果 <paramref name="value" /> 为null,则返回string.Empty</returns>
        public static string AsString(this object value)
        {
            return value == null || value is DBNull ? string.Empty : value.ToString();
        }
        #endregion

        /// <summary>
        /// 深拷贝对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Copy<T>(this T obj) where T : class
        {
            if (obj == null) return null;
            return (T)obj.ToJson().JsonTo(obj.GetType());
        }

        #region Is
        /// <summary>
        /// 判断对象是否为 null 或 DBNull
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNull(this object value)
        {
            return value == null || value is DBNull;
        }
        /// <summary>检查一个值是否可以转换为指定的数据类型。</summary>
        /// <typeparam name="TValue">要转换的数据类型。</typeparam>
        /// <returns>true 如果 <paramref name="value" /> 可以转换为指定的类型; 否则, false.</returns>
        /// <param name="value">检查字符串值。</param>
        /// <returns></returns>
        public static bool Is<TValue>(this object value)
        {
            if (value == null && typeof(TValue).IsClass) return true;
            var converter = TypeDescriptor.GetConverter(typeof(TValue));
            if (converter != null)
            {
                try
                {
                    if (value == null || converter.CanConvertFrom(null, value.GetType()))
                    {
                        converter.ConvertFrom(null, CultureInfo.CurrentCulture, value);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        #endregion

        /// <summary>
        /// 获取对象属性名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="selectorMember"></param>
        /// <returns></returns>
        public static string[] GetPropertyNames<T>(this T entity, Expression<Func<T, object>> selectorMember) where T : class
        {
            if (selectorMember == null) return null;
            string[] properties = null;
            if (selectorMember.Body is MemberExpression)
            {
                properties = new string[] { (selectorMember.Body as MemberExpression).Member.Name };
            }
            else if (selectorMember.Body is UnaryExpression) // 可空类型成员
            {
                if ((selectorMember.Body as UnaryExpression).Operand is MemberExpression me)
                {
                    properties = new string[] { me.Member.Name };
                }
            }
            else
            {
                var pis = selectorMember.Compile()(entity).GetType().GetProperties();
                properties = new string[pis.Length];
                for (int i = 0; i < pis.Length; i++)
                {
                    properties[i] = pis[i].Name;
                }
            }
            return properties;
        }

        #region GetDisplayName

        /// <summary>
        ///获取对象属性的显示名称 ,依次从 DescriptionAttribute ，DisplayNameAttribute，DisplayAttribute 特性获取
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些特性。</param>
        /// <returns></returns>
        public static string GetDisplayName<T>(this T obj, bool inherit = true)
        {
            Type type = typeof(T);
            if (type.IsEnum)
            {
                var filedInfo = type.GetField(Enum.GetName(type, obj));
                return filedInfo.GetDisplayName();
            }
            else if (type.IsNullableType())
            {
                type = Nullable.GetUnderlyingType(type);
            }
            if (type.IsEnum)
            {
                if (obj == null)
                {
                    return null;
                }
                else
                {
                    var filedInfo = type.GetField(Enum.GetName(type, obj));
                    return filedInfo.GetDisplayName();
                }
            }
            return type.GetDisplayName(inherit);
        }

        /// <summary>
        /// 获取对象属性的显示名称 ,依次从 DescriptionAttribute ，DisplayNameAttribute，DisplayAttribute 特性获取
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="selectorMember"></param>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些特性。</param>
        /// <returns></returns>
        public static string GetDisplayName<T>(this T obj, Expression<Func<T, object>> selectorMember, bool inherit = true) where T : class
        {
            return GetDisplayName(obj, obj.GetPropertyNames(selectorMember), inherit);
        }

        /// <summary>
        /// 获取对象属性的显示名称 ,依次从 DescriptionAttribute ，DisplayNameAttribute，DisplayAttribute 特性获取
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyNames"></param>
        /// <param name="inherit">指定是否搜索该成员的继承链以查找这些特性。</param>
        /// <returns></returns>
        public static string GetDisplayName<T>(this T obj, string[] propertyNames, bool inherit = true) where T : class
        {
            var type = typeof(T);
            var displayNames = new StringBuilder();
            foreach (var item in propertyNames)
            {
                if (displayNames.Length > 0) displayNames.Append(",");
                var pi = type.GetProperty(item);
                displayNames.Append(pi.GetDisplayName());
            }
            return displayNames.ToString();
        }

        #endregion

        /// <summary>
        /// 通过属性名称获取属性值。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name">属性名称</param>
        /// <returns></returns>
        public static object Get(this IEntity obj, string name)
        {
            var cache = SmartContext.Current.Resolve<Caching.ICache>(SmartContext.DEFAULT_CACHE_KEY);
            var type = obj.GetType();
            var pis = cache.Get(type.FullName, () => type.GetProperties());
            var pi = pis.FirstOrDefault(p => p.Name == name);
            if (pi == null)
            {
                throw new ArgumentException("name", $"{name}属性不存在。");
            }
            return pi.GetValue(obj, null);
        }

        /// <summary>
        /// 通过属性名称动态给属性赋值
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
        public static void Set<TValue>(this IEntity obj, string name, TValue value)
        {
            var cache = SmartContext.Current.Resolve<Caching.ICache>(SmartContext.DEFAULT_CACHE_KEY);
            var type = obj.GetType();
            var pis = cache.Get(type.FullName, () => type.GetProperties());
            var pi = pis.FirstOrDefault(p => p.Name == name);
            if (pi == null)
            {
                throw new ArgumentException("name", $"{name}属性不存在。");
            }
            var valueType = value.GetType();
            // 类型相同，或和可空类型中的类型相同，不需要类型转换
            if (valueType == pi.PropertyType)// || Nullable.GetUnderlyingType(pi.PropertyType) == valueType
            {
                pi.SetValue(obj, value, null);
                return;
            }
            // 类型是否可以从对象转换
            var converter = TypeDescriptor.GetConverter(pi.PropertyType);
            if (converter.CanConvertFrom(value.GetType()))
            {
                pi.SetValue(obj, converter.ConvertFrom(value), null);
                return;
            }
            // 值是否可以转换为指定的类型
            converter = TypeDescriptor.GetConverter(value.GetType());
            if (converter.CanConvertTo(pi.PropertyType))
            {
                pi.SetValue(obj, converter.ConvertTo(value, pi.PropertyType), null);
                return;
            }
            throw new ArgumentException("value", $"{value.GetType().FullName}和{pi.PropertyType.FullName}类型不一致。");
        }

        /// <summary>
        /// 将对象序列化为JSON字符串，循环引用的对象将被忽略
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToJson(this object value)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore, // 忽略循环引用
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(value, settings);
        }

        /// <summary>
        /// 将对象序列化为JSON字符串，循环引用的对象将被忽略
        /// </summary>
        /// <param name="value"></param>
        /// <param name="formatting"></param>
        /// <param name="settings"></param>
        /// <param name="nullValue"></param>
        /// <param name="referenceLoop"></param>
        /// <returns></returns>
        public static string ToJson(this object value,
             Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None,
             Newtonsoft.Json.NullValueHandling nullValue = Newtonsoft.Json.NullValueHandling.Ignore,
             Newtonsoft.Json.ReferenceLoopHandling referenceLoop = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
             Newtonsoft.Json.JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = new Newtonsoft.Json.JsonSerializerSettings()
                {
                    ReferenceLoopHandling = referenceLoop, // 忽略循环引用
                    NullValueHandling = nullValue,
                };
            }
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(value, formatting, settings);
            return json;
        }
    }
}
