﻿using Berry.Entity.SystemManage;
using Berry.Extension;
using Berry.IService.SystemManage;
using Berry.Service.Base;
using Berry.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using Berry.Entity.CommonEntity;
using Newtonsoft.Json.Linq;

namespace Berry.Service.SystemManage
{
    /// <summary>
    /// 系统日志
    /// </summary>
    public class LogService : BaseService<LogEntity>, ILogService
    {
        /// <summary>
        /// 日志列表
        /// </summary>
        /// <param name="pagination">分页</param>
        /// <param name="queryJson">查询参数</param>
        /// <returns></returns>
        public IEnumerable<LogEntity> GetPageList(PaginationEntity pagination, string queryJson)
        {
            IEnumerable<LogEntity> res = null;
            IDbTransaction tran = null;
            Logger(this.GetType(), "GetMemberList-获取成员列表", () =>
            {
                using (var conn = this.BaseRepository().GetBaseConnection())
                {
                    tran = conn.BeginTransaction();

                    var expression = LambdaExtension.True<LogEntity>();
                    JObject queryParam = queryJson.TryToJObject();
                    if (queryParam != null)
                    {
                        //日志分类
                        if (!queryParam["Category"].IsEmpty())
                        {
                            int categoryId = queryParam["CategoryId"].ToInt();
                            expression = expression.And(t => t.CategoryId == categoryId);
                        }
                        //操作时间
                        if (!queryParam["StartTime"].IsEmpty() && !queryParam["EndTime"].IsEmpty())
                        {
                            DateTime startTime = queryParam["StartTime"].TryToDateTime();
                            DateTime endTime = queryParam["EndTime"].TryToDateTime().AddDays(1);
                            expression = expression.And(t => t.OperateTime >= startTime && t.OperateTime <= endTime);
                        }
                        //操作用户Id
                        if (!queryParam["OperateUserId"].IsEmpty())
                        {
                            string OperateUserId = queryParam["OperateUserId"].ToString();
                            expression = expression.And(t => t.OperateUserId == OperateUserId);
                        }
                        //操作用户账户
                        if (!queryParam["OperateAccount"].IsEmpty())
                        {
                            string OperateAccount = queryParam["OperateAccount"].ToString();
                            expression = expression.And(t => t.OperateAccount.Contains(OperateAccount));
                        }
                        //操作类型
                        if (!queryParam["OperateType"].IsEmpty())
                        {
                            string operateType = queryParam["OperateType"].ToString();
                            expression = expression.And(t => t.OperateType == operateType);
                        }
                        //功能模块
                        if (!queryParam["Module"].IsEmpty())
                        {
                            string module = queryParam["Module"].ToString();
                            expression = expression.And(t => t.Module.Contains(module));
                        }
                    }
                    Tuple<IEnumerable<LogEntity>, int> tuple = this.BaseRepository().FindList<LogEntity>(conn, expression, pagination.sidx, pagination.sord.ToLower() == "asc", pagination.rows, pagination.page, tran);
                    pagination.records = tuple.Item2;
                    res = tuple.Item1;

                    tran.Commit();
                }
            }, e =>
            {
                Trace.WriteLine(e.Message);
            });
            return res;
        }

        /// <summary>
        /// 日志实体
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public LogEntity GetEntity(string keyValue)
        {
            LogEntity res = null;
            IDbTransaction tran = null;
            Logger(this.GetType(), "GetEntity-日志实体", () =>
            {
                using (var conn = this.BaseRepository().GetBaseConnection())
                {
                    tran = conn.BeginTransaction();
                    res = this.BaseRepository().FindEntity<LogEntity>(conn, keyValue, tran);
                    tran.Commit();
                }
            }, e =>
            {
                Trace.WriteLine(e.Message);
            });
            return res;
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        /// <param name="categoryId">日志分类Id</param>
        /// <param name="keepTime">保留时间段内</param>
        public void RemoveLog(int categoryId, string keepTime)
        {
            IDbTransaction tran = null;
            Logger(this.GetType(), "RemoveLog-清空日志", () =>
            {
                using (var conn = this.BaseRepository().GetBaseConnection())
                {
                    tran = conn.BeginTransaction();

                    DateTime operateTime = DateTime.Now;
                    if (keepTime == "7")//保留近一周
                    {
                        operateTime = DateTime.Now.AddDays(-7);
                    }
                    else if (keepTime == "1")//保留近一个月
                    {
                        operateTime = DateTime.Now.AddMonths(-1);
                    }
                    else if (keepTime == "3")//保留近三个月
                    {
                        operateTime = DateTime.Now.AddMonths(-3);
                    }

                    Expression<Func<LogEntity, bool>> expression = LambdaExtension.True<LogEntity>();

                    expression = expression.And(l => l.CategoryId == categoryId);
                    expression = expression.And(l => l.OperateTime <= operateTime);

                    int res = this.BaseRepository().Delete<LogEntity>(conn, expression, tran);

                    tran.Commit();
                }
            }, e =>
            {
                Trace.WriteLine(e.Message);
            });
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="logEntity">对象</param>
        public void WriteLog(LogEntity logEntity)
        {
            IDbTransaction tran = null;
            Logger(this.GetType(), "WriteLog-写日志", () =>
            {
                using (var conn = this.BaseRepository().GetBaseConnection())
                {
                    tran = conn.BeginTransaction();

                    logEntity.Id = CommonHelper.GetGuid().ToString();
                    logEntity.OperateTime = DateTime.Now;
                    logEntity.DeleteMark = false;
                    logEntity.EnabledMark = true;
                    logEntity.IPAddress = NetHelper.Ip;
                    logEntity.Host = NetHelper.Host;
                    logEntity.Browser = NetHelper.Browser;

                    int res = this.BaseRepository().Insert<LogEntity>(conn, logEntity, tran);

                    tran.Commit();
                }
            }, e =>
            {
                Trace.WriteLine(e.Message);
            });
        }
    }
}