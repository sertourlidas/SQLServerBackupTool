﻿using NLog;
using SQLServerBackupTool.Web.Models;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Routing;

namespace SQLServerBackupTool.Web.Lib.Mvc
{
    [Authorize]
    public class ApplicationController : Controller, IFlashMessageProvider
    {
        protected Logger Logger
        {
            get;
            set;
        }

        protected SSBTDbContext DbContext
        {
            get;
            set;
        }

        public void AddFlashMessage(string message, FlashMessageType t)
        {
            FlashMessagesHelper.AddFlashMessage(this, message, t);
        }

        public void FlashInfo(string message)
        {
            AddFlashMessage(message, FlashMessageType.Info);
        }

        public void FlashSuccess(string message)
        {
            AddFlashMessage(message, FlashMessageType.Success);
        }

        public void FlashWarning(string message)
        {
            AddFlashMessage(message, FlashMessageType.Warning);
        }

        public void FlashError(string message)
        {
            AddFlashMessage(message, FlashMessageType.Error);
        }

        protected override void Initialize(RequestContext r)
        {
            var controllerName = r.RouteData.GetRequiredString("controller");
            var actionName     = r.RouteData.GetRequiredString("action");

            ViewBag.ControllerName = controllerName;
            Logger                 = LogManager.GetLogger(string.Format("{0}_{1}", controllerName, actionName));
            DbContext              = new SSBTDbContext();

            base.Initialize(r);
        }

        protected override void Dispose(bool disposing)
        {
            DbContext.Dispose();
            base.Dispose(disposing);
        }

        protected static string GetBackupsConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["BackupConnection"].ConnectionString;
        }
    }
}
