﻿using PagedList;
using SQLServerBackupTool.Lib.Annotations;
using SQLServerBackupTool.Web.Lib.Mvc;
using SQLServerBackupTool.Web.ViewModels;
using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace SQLServerBackupTool.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : ApplicationController
    {
        private const int NumberItemsPerPage = 30;

        private static MembershipProvider Provider
        {
            get { return Membership.Provider; }
        }

        protected override void Initialize(RequestContext r)
        {
            base.Initialize(r);

            if (!Provider.EnablePasswordReset || Provider.RequiresQuestionAndAnswer)
            {
                throw new ConfigurationErrorsException(
                    "UsersController requires membership configuration options : enablePasswordReset -> true and requiresQuestionAndAnswer -> false"
                );
            }
        }

        /**
         * Index
         */

        public ActionResult Index()
        {
            int pageIndex;

            if (!int.TryParse(Request.Params["page"], out pageIndex))
            {
                pageIndex = 1;
            }

            int totalRecords;

            var users = Provider.GetAllUsers(pageIndex - 1, NumberItemsPerPage, out totalRecords);

            var list = users.Cast<MembershipUser>();

            return View(list.ToPagedList(pageIndex, NumberItemsPerPage));
        }

        /**
         * Create
         */

        public ActionResult Create()
        {
            return View(new MembershipEditViewModel
            {
                IsApproved = true,
            });
        }

        [ValidateAntiForgeryToken, HttpPost]
        public ActionResult Create(MembershipUser u)
        {
            return RedirectToAction("Edit", new { id=u.UserName });
        }

        /**
         * Edit
         */

        public ActionResult Edit(string id)
        {
            var u = Membership.GetUser(id);

            if (u == null)
            {
                return HttpNotFound(string.Format("User : {0} not found", id));
            }

            return View(new MembershipEditViewModel(u)
            {
                ForEdit = true,
            });
        }

        [ValidateAntiForgeryToken, HttpPost]
        public ActionResult Edit(MembershipEditViewModel u)
        {
            var username = u.UserName;
            var mem = Membership.GetUser(username);

            if (mem == null)
            {
                return HttpNotFound(string.Format("User : {0} not found", username));
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(u.Password) && u.Password == u.PasswordConfirmation)
                {
                    mem.ChangePassword(mem.ResetPassword(), u.Password);
                }

                if (mem.Comment != u.Comment)
                {
                    mem.Comment = u.Comment;
                    Provider.UpdateUser(mem);
                }

                HandleRoles(u);

                AddFlashMessage("User successfully modified", FlashMessageType.Success);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                AddFlashMessage("An error occurred during user modification", FlashMessageType.Error);
                Logger.ErrorException("An error occurred", ex);
            }

            u.ForEdit = true;
            return View(u);
        }

        /**
         * User delete
         */

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            try
            {
                if (Provider.DeleteUser(id, true))
                {
                    AddFlashMessage(string.Format("User '{0}' successfully deleted", id), FlashMessageType.Success);
                }
                else
                {
                    AddFlashMessage(string.Format("Unable to delete user '{0}'", id), FlashMessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                AddFlashMessage(string.Format("An error occurred while deleting user {0}", id), FlashMessageType.Error);
                Logger.ErrorException("An error occurred", ex);
            }

            return RedirectToAction("Index");
        }

        /**
         * Random password generator
         */

        public ActionResult GeneratePassword()
        {
            return Content(
                Membership.GeneratePassword(
                    Membership.MinRequiredPasswordLength,
                    Membership.MinRequiredNonAlphanumericCharacters
                ),
                "text/plain"
            );
        }

        /// <summary>
        /// When roles are enabled adds/removes roles
        /// </summary>
        /// <param name="u"><see cref="MembershipEditViewModel"/> to get the roles from</param>
        protected static void HandleRoles([NotNull] MembershipEditViewModel u)
        {
            if (u == null)
            {
                throw new ArgumentNullException("u");
            }

            if (!Roles.Enabled)
            {
                return;
            }

            Roles.RemoveUserFromRoles(u.UserName, Roles.GetRolesForUser(u.UserName));

            if (u.Roles != null && u.Roles.Any())
            {
                Roles.AddUserToRoles(u.UserName, u.Roles.ToArray());
            }
        }
    }
}
