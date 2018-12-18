using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using bankaccount.Models;

namespace bankaccount.Controllers
{
    public class HomeController : Controller
    {
        private BankContext dbContext;

        public HomeController(BankContext context)
        {
            dbContext = context;
        }

        [HttpGet("")]
        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                if (dbContext.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Register");
                }
                else
                {
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    user.Password = Hasher.HashPassword(user, user.Password);
                    dbContext.Add(user);
                    dbContext.SaveChanges();
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    return RedirectToAction($"Account/{user.UserId}");
                }
            }
            else
            {
                return View("Register");
            }
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public IActionResult Login(LoginUser user)
        {
            if (ModelState.IsValid)
            {
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == user.Email);
                if (userInDb == null)
                {
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(user, userInDb.Password, user.Password);
                if (result == 0)
                {
                    ModelState.AddModelError("Password", "Password does not exist!");
                    return View("Login");
                }
                else
                {
                    HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                    return Redirect($"Account/{userInDb.UserId}");
                }
            }
            else
            {
                return View("Login");
            }
        }

        [HttpGet("Account/{User_Id}")]
        public IActionResult Account(int User_Id)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Register");
            }
            if (HttpContext.Session.GetInt32("UserId") != User_Id)
            {
                int? UserId = HttpContext.Session.GetInt32("UserId");
                return Redirect($"/Account/{UserId}");
            }
            User CurrentUser = dbContext.Users.Include(user => user.Transactions).Where(user => user.UserId == User_Id).SingleOrDefault();
            if (CurrentUser.Transactions != null)
            {
                CurrentUser.Transactions = CurrentUser.Transactions.OrderByDescending(trans => trans.CreatedAt).ToList();
            }
            ViewBag.User = CurrentUser;
            return View();
        }

        [HttpPost("Transaction")]
        public IActionResult Transaction(decimal amount)
        {
            User CurrentUser = dbContext.Users.SingleOrDefault(u => u.UserId == HttpContext.Session.GetInt32("UserId"));
            if (amount > 0)
            {
                CurrentUser.Balance += amount;
                Transaction NewTransaction = new Transaction
                {
                    Amount = amount,
                    CreatedAt = DateTime.Now,
                    UserId = CurrentUser.UserId
                };
                dbContext.Add(NewTransaction);
                dbContext.SaveChanges();
                ModelState.AddModelError("Amount", "Success!");
                return Redirect($"Account/{CurrentUser.UserId}");
            }
            else
            {
                if (CurrentUser.Balance + amount < 0)
                {
                    ModelState.AddModelError("Amount", "Balance is insufficient");
                    return Redirect($"Account/{CurrentUser.UserId}");
                }
                else
                {
                    CurrentUser.Balance += amount;
                    Transaction NewTransaction = new Transaction
                    {
                        Amount = amount,
                        CreatedAt = DateTime.Now,
                        UserId = CurrentUser.UserId
                    };
                    dbContext.Add(NewTransaction);
                    dbContext.SaveChanges();
                    ModelState.AddModelError("Amount", "Success!");
                    return Redirect($"Account/{CurrentUser.UserId}");
                }
            }
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}