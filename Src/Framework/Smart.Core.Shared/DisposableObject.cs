﻿using System;

namespace Smart.Core
{
    /// <summary>
    /// 实现可释放的对象基础类
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        /// <summary>
        /// 获取是否已经释放或重置非托管资源
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        ~DisposableObject() { Dispose(false); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
