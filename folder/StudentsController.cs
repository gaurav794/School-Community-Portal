using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lab4.Data;
using Lab4.Models;
using Lab4.Models.ViewModels;

namespace Lab4.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SchoolCommunityContext _context;

        public StudentsController(SchoolCommunityContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(int ID)
        {

            var communityViewModel = new CommunityViewModel();

            //LINQ query to fetch students
            communityViewModel.Students = await (from s in _context.Students select s).ToListAsync();

            if (ID != 0)
            {
                ViewData["ID"] = ID;
                //LINQ query to fetch members of the community
                communityViewModel.Communities = await (from s in _context.Students
                                                     join cm in _context.CommunityMemberships on s.ID equals cm.StudentID
                                                     join c in _context.Communities on cm.CommunityID equals c.ID
                                                     where s.ID == ID
                                                     select c).ToListAsync();
            }
            return View(communityViewModel);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,LastName,EnrollmentDate")] Models.Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,FirstName,LastName,EnrollmentDate")] Models.Student student)
        {
            if (id != student.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }


        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.ID == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            _context.Students.Remove(student);

            //Delete Community related with Student
            //LINQ query to fetch communities
            var communities = await (from c in _context.Communities select c).ToListAsync();
            //Students linked with communities
            var studentCommunities = await (from s in _context.Students
                                            join cm in _context.CommunityMemberships on s.ID equals cm.StudentID
                                            join c in _context.Communities on cm.CommunityID equals c.ID
                                            where s.ID == id
                                            select c).ToListAsync();

            foreach (var item in studentCommunities)
            {
                if ((studentCommunities.FirstOrDefault(s => s.ID == item.ID)) != null)
                {
                    var removeCommunity = await _context.CommunityMemberships.FindAsync(id, item.ID);
                    _context.CommunityMemberships.Remove(removeCommunity);
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.ID == id);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> EditMemberships(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            ViewData["FullName"] = student.FullName;
            //LINQ query to fetch communities
            var communities = await (from c in _context.Communities select c).ToListAsync();
            ViewData["Communities"] = communities;

            //Students linked with communities
            var studentCommunities = await (from s in _context.Students
                                                    join cm in _context.CommunityMemberships on s.ID equals cm.StudentID
                                                    join c in _context.Communities on cm.CommunityID equals c.ID
                                                    where s.ID == id
                                                    select c).ToListAsync();
            //Store data into ViewData
            ViewData["StudentCommunities"] = studentCommunities;

            return View(student);
        }

        public async Task<IActionResult> AddMemberships(int studentId, string communityId) 
        {
            if (studentId == 0 || communityId == null)
            {
                return NotFound();
            }
            //Add community to CommunityMemberships Table          
            var addCommunity = new CommunityMembership{StudentID=studentId,CommunityID=communityId};
            _context.CommunityMemberships.Add(addCommunity);
            await _context.SaveChangesAsync();

            //Redirect to /Students/EditMemberships/id
            return RedirectToAction("EditMemberships", new { id = studentId });
        }

        public async Task<IActionResult> RemoveMemberships(int studentId, string communityId)
        {

            if (studentId == 0 || communityId == null)
            {
                return NotFound();
            }
            //Remove community to CommunityMemberships Table  
            var removeCommunity = await _context.CommunityMemberships.FindAsync(studentId,communityId);
            _context.CommunityMemberships.Remove(removeCommunity);
            await _context.SaveChangesAsync();

            //Redirect to /Students/EditMemberships/id
            return RedirectToAction("EditMemberships", new { id = studentId });
        }



    }
}
