using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using final_belt.Models;
using DbConnection;
using Microsoft.AspNetCore.Http;

namespace final_belt.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [Route("register")]
        public IActionResult NewUser(User user)
        {
            TryValidateModel(user);
            
            if(ModelState.IsValid)
            {
                string query = $"INSERT INTO `users` (`name`,  `email`, `password`, `description`,`created_at`, `updated_at`) VALUES ('{user.Name}', '{user.Email}', '{user.Password}', '{user.Description}', NOW(), NOW());";
                DbConnector.Query(query);
                string queryID = $"SELECT * FROM users WHERE users.email = '{user.Email}'";
                List<Dictionary<string,object>> getID = DbConnector.Query(queryID);
                HttpContext.Session.SetInt32("UserId", (int)getID[0]["id"]);
                HttpContext.Session.SetString("UserEmail", (string)getID[0]["email"]);
                return RedirectToAction("Welcome");
            }
            //Front end validation
            return View("Index");
        }
        [HttpPost]
        [Route("login")]
        public IActionResult Login(string loginEmail, string loginPass)
        {
            string query = $"SELECT * FROM users WHERE users.email = '{loginEmail}'";
            List<Dictionary<string,object>> user = DbConnector.Query(query);
            if (user.Count > 0) {
                string query2 = $"SELECT * FROM users WHERE users.email = '{loginEmail}' AND users.password = '{loginPass}'";
                List<Dictionary<string,object>> checkPass = DbConnector.Query(query2);
                if (checkPass.Count == 1) {
                    HttpContext.Session.SetString("UserEmail", (string)checkPass[0]["email"]);
                    HttpContext.Session.SetInt32("UserId", (int)checkPass[0]["id"]);
                    return RedirectToAction("Welcome");
                } 
                else 
                {
                    ViewBag.error = "Wrong Email/Password combination";
                    return View("Index");
                }
            } 
            else 
            {
                ViewBag.error = "This Email is not in use";
                return View("Index");

            }
        }
        [HttpGet]
        [Route("welcome")]
        public IActionResult Welcome()
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                Console.WriteLine("WELCOMEE");
                string query = $"SELECT * FROM users WHERE users.id = '{authorizedid}'";
                Dictionary<string,object> user = DbConnector.Query(query).SingleOrDefault();
                ViewBag.User = user;

                string friendQuery = $"SELECT users.id AS user_id, users.name AS user_name, friends.status, friend.id AS friend_id, friend.name AS friend_name FROM users JOIN friends ON users.id = friends.user_id JOIN users as friend ON friends.friend_id = friend.id WHERE user_id = {authorizedid};";

                // string friendQuery = $"SELECT users.id AS user_id, users.name AS user_name, friends.status, friends.friend_id, friendlist.name as friend_name FROM users LEFT JOIN friends ON user_id = friends.user_id LEFT JOIN users as friendlist ON  friendlist.id = friends.friend_id WHERE users.id = {authorizedid} AND friend_id != {authorizedid};";
                List<Dictionary<string,object>> friendships = DbConnector.Query(friendQuery);
                ViewBag.friendships= friendships;

                return View();
            }
           
        }
        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index"); 
        }

        [HttpGet]
        [Route("users")]
        public IActionResult Users()
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                int? UserId = HttpContext.Session.GetInt32("UserId");
                string query = $"SELECT * FROM users WHERE users.id != '{UserId}'";
                List<Dictionary<string,object>> otherusers = DbConnector.Query(query);
                ViewBag.OtherUsers = otherusers;

                string friendQuery = $"SELECT users.id AS user_id, users.name AS user_name, friends.status, friend.id AS friend_id, friend.name AS friend_name FROM users JOIN friends ON users.id = friends.user_id JOIN users as friend ON friends.friend_id = friend.id WHERE user_id = {UserId};";
                List<Dictionary<string,object>> friendships = DbConnector.Query(friendQuery);
                ViewBag.friendships= friendships;
                return View(); 
            }
        }

        [HttpGet]
        [Route("user/{user_id}")]
        public IActionResult UserId(int user_id)
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                string query = $"SELECT * FROM users WHERE users.id = '{user_id}'";
                Dictionary<string,object> user = DbConnector.Query(query).SingleOrDefault();
                ViewBag.User = user;
                return View();
            }
        }
        [HttpPost]
        [Route("accept")]
        public IActionResult Add(int id)
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                string find1 = $"SELECT * FROM friends WHERE `user_id`={id} AND `friend_id`={authorizedid};";
                Dictionary<string,object> friendship = DbConnector.Query(find1).SingleOrDefault();
                string update = $"UPDATE `friends` SET `status`=1 WHERE `id`={friendship["id"]};";
                DbConnector.Query(update);
                string find2 = $"SELECT * FROM friends WHERE `user_id`={authorizedid} AND `friend_id`={id};";
                Dictionary<string,object> friendship2 = DbConnector.Query(find2).SingleOrDefault();
                string update2 = $"UPDATE `friends` SET `status`=1 WHERE `id`={friendship2["id"]};";
                DbConnector.Query(update2);
                return RedirectToAction("Welcome");
            }
        }
        [HttpPost]
        [Route("ignore")]
        public IActionResult Ignore(int id)
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                string find1 = $"SELECT * FROM friends WHERE `user_id`={id} AND `friend_id`={authorizedid};";
                Dictionary<string,object> friendship = DbConnector.Query(find1).SingleOrDefault();
                string update = $"DELETE FROM friends WHERE `id`={friendship["id"]};";
                DbConnector.Query(update);
                string find2 = $"SELECT * FROM friends WHERE `user_id`={authorizedid} AND `friend_id`={id};";
                Dictionary<string,object> friendship2 = DbConnector.Query(find2).SingleOrDefault();
                string update2 = $"DELETE FROM friends WHERE `id`={friendship2["id"]};";
                DbConnector.Query(update2);
                return RedirectToAction("Welcome");
            }
        }
        [HttpPost]
        [Route("connect")]
        public IActionResult Connect(int id)
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {
                string query1 = $"INSERT INTO `friends` (`user_id`,  `friend_id`, `status`) VALUES ('{authorizedid}', '{id}', 2 );";
                DbConnector.Query(query1);
                string query2 = $"INSERT INTO `friends` (`user_id`,  `friend_id`, `status`) VALUES ('{id}', '{authorizedid}', 3 );";
                DbConnector.Query(query2);
                return RedirectToAction("Welcome");
            }
        }
        [HttpPost]
        [Route("remove")]
        public IActionResult Remove(int id)
        {
            int? authorizedid = HttpContext.Session.GetInt32("UserId");
            if (authorizedid is null)
            {
                TempData["error"]= "Must login first";
                return Redirect("/");
            } 
            else
            {            
                Console.WriteLine("AUTHORIZED ID  " + authorizedid);            
                string find1 = $"SELECT * FROM friends WHERE `user_id`={id} AND `friend_id`={authorizedid};";
                Dictionary<string,object> friendship = DbConnector.Query(find1).SingleOrDefault();
                string update = $"DELETE FROM friends WHERE `id`={friendship["id"]};";
                DbConnector.Query(update);
                string find2 = $"SELECT * FROM friends WHERE `user_id`={authorizedid} AND `friend_id`={id};";
                Dictionary<string,object> friendship2 = DbConnector.Query(find2).SingleOrDefault();
                string update2 = $"DELETE FROM friends WHERE `id`={friendship2["id"]};";
                DbConnector.Query(update2);
                return RedirectToAction("Welcome");
            }
        }
    }
}
