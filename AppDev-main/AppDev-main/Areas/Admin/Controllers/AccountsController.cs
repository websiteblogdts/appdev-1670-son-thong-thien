using AppDev.Areas.Admin.ViewModels;
using AppDev.Data;
using AppDev.Helpers;
using AppDev.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AppDev.Areas.Admin.Controllers
{
    //chỉ có thể được truy cập bởi những người dùng đã đăng nhập và có vai trò "Admin",
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public AccountsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }
        public ActionResult Index()
        {
            return View();
        }


        public async Task<ActionResult> Customers()
        {
            var users = await userManager.GetUsersInRoleAsync(Roles.User);
            //Đây là phương thức của userManager để lấy tất cả (getuser) các người dùng có vai trò "User".
            //Sau khi lấy danh sách người dùng, dữ liệu được gán vào biến users.
            ViewData["Title"] = "Customer Accounts";
            ViewData["ReturnUrl"] = HttpContext.Request.Path;
            return View("Users", users);
        }

        public async Task<ActionResult> StoreOwners()
        {
            var users = await userManager.GetUsersInRoleAsync(Roles.StoreOwner);

            ViewData["Title"] = "Store Owner Accounts";
            ViewData["ReturnUrl"] = HttpContext.Request.Path;
            return View("Users", users);
        }


        //Phương thức này được sử dụng để hiển thị giao diện đặt lại mật khẩu cho một người dùng có ID cụ thể.
        public async Task<ActionResult> ResetPassword(string? id, string? returnUrl)
        {
            //Dòng code if (id is null) kiểm tra xem có thông tin về ID người dùng được cung cấp hay không.
            //Nếu không có ID, nghĩa là không có người dùng cụ thể để đặt lại mật khẩu,
            //và phương thức sẽ trả về view mặc định.
            if (id is null)
                return View();

            var user = await userManager.FindByIdAsync(id);
            //Nếu có ID người dùng được cung cấp, tiếp theo,
            //phương thức sử dụng userManager.FindByIdAsync(id) để tìm kiếm người dùng theo ID.
            //Nếu người dùng không tồn tại(user == null),
            //phương thức sẽ trả về một trạng thái "Not Found", chỉ ra rằng không tìm thấy người dùng.
            if (user == null)
                return NotFound();

            var model = new ResetPasswordViewModel()
            {
                ReturnUrl = returnUrl,
                Email = user.Email,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        //Phương thức này xử lý yêu cầu đặt lại mật khẩu
        //sau khi dữ liệu đã được gửi đi từ form trang đặt lại mật khẩu
        public async Task<ActionResult> ResetPassword(string? id, string? returnUrl, ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // model(dữ liệu từ form được gửi lên, chứa thông tin đặt lại mật khẩu mới).
            //if (!ModelState.IsValid) kiểm tra xem dữ liệu từ form có hợp lệ không.
            //Nếu không hợp lệ, nghĩa là có lỗi trong dữ liệu nhập,
            //phương thức trả về view đặt lại mật khẩu với model để hiển thị các thông báo lỗi.


            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("Email", "Cannot find user with email.");
                return View(model);
            }
            //FindByEmailAsync(model.Email): Đây là phương thức của userManager
            //để tìm kiếm người dùng bằng địa chỉ email.


            var permission = await userManager.IsInRoleAsync(user, Roles.User)
                || await userManager.IsInRoleAsync(user, Roles.StoreOwner);
            //var permission = ... kiểm tra xem người dùng có quyền đặt lại mật khẩu hay không. 
            if (!permission)
            {
                ModelState.AddModelError("", "Cannot reset password,. Permission denied.");
                return View(model);
            }

            var code = await userManager.GeneratePasswordResetTokenAsync(user);

            var result = await userManager.ResetPasswordAsync(user, code, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            if (returnUrl != null)
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
