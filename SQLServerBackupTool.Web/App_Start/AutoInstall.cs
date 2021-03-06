﻿using SQLServerBackupTool.Lib.Annotations;
using SQLServerBackupTool.Web;
using SQLServerBackupTool.Web.Lib;
using SQLServerBackupTool.Web.Models;
using System.ComponentModel;
using System.Data.Entity;
using System.Web.Security;

[assembly: WebActivator.PostApplicationStartMethod(typeof(AutoInstall), "PostStart")]

namespace SQLServerBackupTool.Web
{
    [Localizable(false)]
    public static class AutoInstall
    {
        [UsedImplicitly]
        public static void PostStart()
        {
            BasicMembershipAuthHttpModule.Realm = "SSBT.web";

            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<SSBTDbContext>());

            using (var ddb = new SSBTDbContext())
            {
                ddb.Database.Initialize(false);
            }

            MembershipUser defaultUser = null;
            if (Membership.GetAllUsers().Count == 0)
            {
                defaultUser = Membership.CreateUser("admin", "password");
            }

            if (!Roles.RoleExists("Admin"))
            {
                Roles.CreateRole("Admin");

                if (defaultUser != null)
                {
                    Roles.AddUserToRole(defaultUser.UserName, "Admin");
                }
            }

            if (!Roles.RoleExists("Operator"))
            {
                Roles.CreateRole("Operator");
            }
        }
    }
}
