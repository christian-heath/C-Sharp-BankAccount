// Adding all requirements.
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
            // Link the variable dbContext to my model variable "context" so I can communicate with MySQL.
            dbContext = context;
        }

        [HttpGet("")]
        [HttpGet("Register")]
        
        // User will first come to this page and be prompted to register or redirect to the login page.
        public IActionResult Register()
        {
            // Render the Cshtml file titled "Register."
            return View("Register");
        }

        [HttpPost("Register")]

        // Post request sent when user submits the registration form.
        public IActionResult Register(User user)
        {
            // First check to see whether the form submitted was valid and meets all Model requirements.

            if (ModelState.IsValid)
            {
                // If the form was valid but the email submitted is already in the Db, this will catch the error.
                
                if (dbContext.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    // Return the "Register" cshtml file with error messages displayed.
                    return View("Register");
                }

                // If the form is valid and email is not already in the Db. A new user will be created.
                else
                {
                    // First the user's password will be hashed for security purposes.
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    user.Password = Hasher.HashPassword(user, user.Password);
                    // Open the Db connection and add the user object to the database.
                    dbContext.Add(user);
                    // Save the changes to the Db and close the connection.
                    dbContext.SaveChanges();
                    // Set the User's custom UserID in session thus allowing them to enter the app without having to login each time.
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    // Redirect to the User's custom account method with their UserID as the parameter.
                    return RedirectToAction($"Account/{user.UserId}");
                }
            }
            // If the model state is invalid the "Register" cshtml file will be rendered with error messages displayed.
            else
            {
                return View("Register");
            }
        }

        [HttpGet("Login")]

        // The "Login" cshtml file will render a login page.
        public IActionResult Login()
        {
            return View("Login");
        }

        [HttpPost("Login")]

        // Post request sent when user submits the Login form.
        public IActionResult Login(LoginUser user)
        {
            // First check to see whether the form submitted was valid and meets all Model requirements.
            if (ModelState.IsValid)
            {
                // Create a variable called userInDB to check if the submitted email is in the Db.
                var userInDb = dbContext.Users.FirstOrDefault(u => u.Email == user.Email);
                if (userInDb == null)
                {
                    // If the email submitted is not in the Db "userInDb == null" then it will render the "Login" view with errors displayed.
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                // Hash the submitted password.
                var hasher = new PasswordHasher<LoginUser>();
                // Check the submitted password to the password belonging to the submitted email.
                var result = hasher.VerifyHashedPassword(user, userInDb.Password, user.Password);
                if (result == 0)
                {
                    // If result == 0, the passwords do not match and the "Login" view will be rendered with error messages.
                    ModelState.AddModelError("Password", "Password does not exist!");
                    return View("Login");
                }
                else
                {
                    // If the password submitted matches the Db. The user's UserID will be stored in session and they will be granted access to their account.
                    HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                    return Redirect($"Account/{userInDb.UserId}");
                }
            }
            // Else if the model state is invalid the "Login" page will be rendered with error messages.
            else
            {
                return View("Login");
            }
        }

        [HttpGet("Account/{User_Id}")]

        // Account info page of the user. Takes a User's UserId as the parameter.
        public IActionResult Account(int User_Id)
        {
            // If the user's UserID is not in session, this means they are not logged in and do not have access to the account. They will be redirected to the Login page. 
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login");
            }
            // If the user tires to access someone else's account page. This will check the UserId in session and deny access to them before redirecting them to the Login page.
            if (HttpContext.Session.GetInt32("UserId") != User_Id)
            {
                int? UserId = HttpContext.Session.GetInt32("UserId");
                return RedirectToAction("Login");
            }
            // Db query which assigns a single user and their transactions to a User Object called "Current User"
            User CurrentUser = dbContext.Users.Include(user => user.Transactions).Where(user => user.UserId == User_Id).SingleOrDefault();
            if (CurrentUser.Transactions != null)
            {
                // If the user has any posted transactions, they will be listed in descending order.
                CurrentUser.Transactions = CurrentUser.Transactions.OrderByDescending(trans => trans.CreatedAt).ToList();
            }
            // Store the Current User object in a ViewBag and return the "Account" cshtml view.
            ViewBag.User = CurrentUser;
            return View("Account");
        }

        [HttpPost("Transaction")]

        // Post request when a user submits a transaction. This method takes a decimal amount as a parameter to deposit.
        public IActionResult Transaction(decimal amount)
        {
            // Retrieve the Current User from the Db using their UserID which is stored in session.
            User CurrentUser = dbContext.Users.SingleOrDefault(u => u.UserId == HttpContext.Session.GetInt32("UserId"));
            if (amount > 0)
            {
                // If the amount deposited is greater than 0. The amount will be added to the user's balance.
                CurrentUser.Balance += amount;
                // Create a new transaction object.
                Transaction NewTransaction = new Transaction
                {
                    Amount = amount,
                    CreatedAt = DateTime.Now,
                    UserId = CurrentUser.UserId
                };
                // Add and save the transaction to the Db.
                dbContext.Add(NewTransaction);
                dbContext.SaveChanges();
                // Notify the user that their deposit was successful.
                ModelState.AddModelError("Amount", "Your deposit was successful!");
                // Redirect the user to their custom account page.
                return Redirect($"Account/{CurrentUser.UserId}");
            }
            
            // If the amount deposited is less than 0 (i.e. a negative number), this is handled as a withdrawal.
            else
            {
                // If the User's balance PLUS the withdrawal amount (a negative number) is less than 0 then the user does not have sufficient funds to withdraw.
                if (CurrentUser.Balance + amount < 0)
                {
                    // The user will be redirected to their account page with this error message displayed.
                    ModelState.AddModelError("Amount", "Balance is insufficient");
                    return Redirect($"Account/{CurrentUser.UserId}");
                }
                else
                {
                    // If the user's balance is sufficient, the amount will be subtracted (added because it is a negative) from their balance.
                    CurrentUser.Balance += amount;
                    // A new transaction is created.
                    Transaction NewTransaction = new Transaction
                    {
                        Amount = amount,
                        CreatedAt = DateTime.Now,
                        UserId = CurrentUser.UserId
                    };
                    // The transaction is added and saved to the Db.
                    dbContext.Add(NewTransaction);
                    dbContext.SaveChanges();
                    // Notify the user that they have successfully withdrawn, and redirect them to their account page.
                    ModelState.AddModelError("Amount", "Your withdrawal was a success!");
                    return Redirect($"Account/{CurrentUser.UserId}");
                }
            }
        }

        [HttpGet("Logout")]
        // logout get method, no information is submited via post request.
        public IActionResult Logout()
        {
            // The UserID stored in session is cleared.
            HttpContext.Session.Clear();
            // The user is redirected to the Login page.
            return RedirectToAction("Login");
        }
    }
}