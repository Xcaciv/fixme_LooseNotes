using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Services;
using System.Diagnostics;

namespace Xcaciv.LooseNotes.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Insecure: No protection against brute force attacks
            var user = _userService.Authenticate(username, password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            // Insecure: Storing sensitive information in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role ?? "user");

            // Insecure: Verbose error message reveals valid usernames
            if (user.Role == "admin")
            {
                return RedirectToAction("AdminDashboard");
            }
            return RedirectToAction("Index", "Note");
        }

        // GET: /User/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public IActionResult Register(User user)
        {
            // Insecure: No validation or sanitation of inputs
            // No password complexity requirements
            
            // Check if username exists - SQL injection vulnerable method
            var existingUser = _userService.GetByUsername(user.Username);
            if (existingUser != null)
            {
                ViewBag.Error = "Username already exists";
                return View();
            }

            // Create user without hashing password
            _userService.Create(user);
            
            return RedirectToAction("Login");
        }

        // GET: /User/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /User/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // Insecure: Reveals if email exists in database
            var token = _userService.GeneratePasswordResetToken(email);
            if (token == null)
            {
                ViewBag.Error = "Email not found";
                return View();
            }

            // Insecure: Token displayed directly to user instead of sent via email
            ViewBag.Message = $"Password reset token: {token}";
            return View();
        }

        // GET: /User/ResetPassword
        public IActionResult ResetPassword(string email, string token)
        {
            // Insecure: Parameters passed in URL
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // POST: /User/ResetPassword
        [HttpPost]
        public IActionResult ResetPassword(string email, string token, string newPassword)
        {
            // Insecure: No password complexity validation
            var result = _userService.ResetPassword(email, token, newPassword);
            
            if (!result)
            {
                ViewBag.Error = "Invalid or expired token";
                ViewBag.Email = email;
                ViewBag.Token = token;
                return View();
            }

            return RedirectToAction("Login");
        }

        // GET: /User/Profile
        public IActionResult Profile()
        {
            // Insecure: No authentication check
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = _userService.GetById(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            
            // Insecure: Revealing sensitive information including password
            return View(user);
        }

        // POST: /User/Profile
        [HttpPost]
        public IActionResult Profile(User model)
        {
            // Insecure: No authentication check
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            // Insecure: Mass assignment vulnerability
            // User can update any field including Role
            var user = _userService.GetById(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            
            user.Username = model.Username;
            user.Email = model.Email;
            user.Password = model.Password; // Password not hashed
            
            // Potential privilege escalation
            user.Role = model.Role;
            
            _userService.Update(user);
            
            return RedirectToAction("Index", "Note");
        }

        // GET: /User/AdminDashboard
        public IActionResult AdminDashboard()
        {
            // Insecure: Role validation done client-side
            string? role = HttpContext.Session.GetString("Role");
            
            // Still allows access with role spoofing
            ViewBag.IsAdmin = (role == "admin");
            
            var users = _userService.GetAll();
            return View(users);
        }

        // GET: /User/Details/{id}
        public IActionResult Details(int id)
        {
            // Insecure: IDOR vulnerability - any user can see details of other users
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }

        // GET: /User/Edit/{id}
        public IActionResult Edit(int id)
        {
            // Insecure: IDOR vulnerability - any user can edit other users
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }

        // POST: /User/Edit/{id}
        [HttpPost]
        public IActionResult Edit(int id, User model)
        {
            // Insecure: No authorization check
            // IDOR vulnerability - any user can edit other users
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Update user without proper validation
            user.Username = model.Username;
            user.Email = model.Email ?? string.Empty;
            user.Password = model.Password; // Updating password directly without hashing
            user.Role = model.Role; // Privilege escalation possible
            
            _userService.Update(user);
            
            return RedirectToAction("AdminDashboard");
        }

        // GET: /User/Delete/{id}
        public IActionResult Delete(int id)
        {
            // Insecure: IDOR vulnerability - any user can delete other users
            var user = _userService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }

        // POST: /User/DeleteConfirmed/{id}
        [HttpPost, ActionName("DeleteConfirmed")]
        public IActionResult DeleteConfirmed(int id)
        {
            // Insecure: No CSRF protection
            // No authorization check
            _userService.Delete(id);
            
            return RedirectToAction("AdminDashboard");
        }

        // GET: /User/Logout
        public IActionResult Logout()
        {
            // Clear all session variables
            HttpContext.Session.Clear();
            
            return RedirectToAction("Index", "Home");
        }

        // GET: /User/RunCommand
        public IActionResult RunCommand()
        {
            // Super insecure admin feature
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return Forbid();
            }
            
            return View();
        }

        // POST: /User/RunCommand
        [HttpPost]
        public IActionResult RunCommand(string command)
        {
            // Command injection vulnerability
            string? role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return Forbid();
            }
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command ?? ""}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                ViewBag.Output = output;
            }
            catch (Exception ex)
            {
                // Insecure: Detailed exception information exposed to user
                ViewBag.Error = $"Error executing command: {ex.ToString()}";
            }
            
            return View();
        }
    }
}
