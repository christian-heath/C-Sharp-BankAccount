using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace bankaccount.Models
{
    public class User
    {
        // This model is checked when a user registers.
        [Key]
        public int UserId { get; set; }

        // Each user has a list of transactions.

        public List<Transaction> Transactions {get; set;}


        [Required]
        [Display(Name = "First Name:")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name must contain letters only")]
        [MinLength(2, ErrorMessage = "First Name must be 2 characters or longer!")]
        public string FirstName { get; set; }


        [Required]
        [Display(Name = "Last Name:")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last Name must contain letters only")]
        [MinLength(2, ErrorMessage = "Last Name must be 2 characters or longer!")]
        public string LastName { get; set; }


        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address!")]
        [Display(Name = "Email Address:")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password:")]
        [MinLength(8, ErrorMessage = "Password must be 8 characters or longer!")]
        public string Password { get; set; }
        
        public decimal Balance {get; set;}


        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // Confirm password is not saved to the User table.
        [NotMapped]
        [Display(Name = "Password Confirm:")]
        [Compare("Password", ErrorMessage = "Passwords do not match!")]
        [DataType(DataType.Password)]
        public string Confirm { get; set; }
    }
}