using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Extensions;
using IntelligentAttendanceSystem.Helper;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using IntelligentAttendanceSystem.Services;
using IntelligentAttendanceSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetSDKCS;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace IntelligentAttendanceSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IDahuaService_One dahuaService;
        private const int m_WaitTime = 3000;
        private IntPtr m_LoginID = IntPtr.Zero;
        private NET_FACERECONGNITION_GROUP_INFO m_GroupInfo;
        private NET_FACERECONGNITION_GROUP_INFO[] m_Groups;

        private NET_DEVICEINFO_Ex m_DevInfo = new NET_DEVICEINFO_Ex();
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            IDahuaService_One _dahuaService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            dahuaService = _dahuaService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user != null && !user.IsActive)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Your account has been deactivated.");
                        return View(model);
                    }

                    // Redirect based on user type
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            ViewBag.Countries = Countries.GetCountrySelectList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, IFormFile picture)
        {
            if (ModelState.IsValid)
            {
                if (m_LoginID == IntPtr.Zero)
                {
                    SystemDevice systemDevice = _context.SystemDevices.FirstOrDefault();
                    if (systemDevice != null)
                    {
                        systemDevice.IsInit = dahuaService.Init();
                        if (systemDevice.IsInit)
                        {
                            TempData["SuccessMessage"] = "Dahua SDK initialized successfully.";
                            m_LoginID = NETClient.LoginWithHighLevelSecurity(systemDevice.IPAddress.Trim(), systemDevice.Port, systemDevice.UserName.Trim(), systemDevice.Password, EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref m_DevInfo);
                            if (m_LoginID != IntPtr.Zero)
                            {
                                m_Groups = FindGroups();
                                bool uINd = addUserInDevice(model, picture?.OpenReadStream().ReadFully());
                                if (uINd)
                                {
                                    await AddUserInDB(model);
                                }
                            }
                            else
                            {
                                systemDevice.Status = "Connection Failed";
                                TempData["ErrorMessage"] = "Dahua SDK initialized successfully. But Login Faild";
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to initialize Dahua SDK.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No Device Found in Db Kindly Register Some.";
                    }
                }
                else
                {

                    m_Groups = FindGroups();
                    // Add in Device 
                    bool uINd = addUserInDevice(model, picture?.OpenReadStream().ReadFully());
                    if (uINd)
                    {
                        await AddUserInDB(model);
                    }
                }
            }

            // If we got this far, something failed; redisplay form
            ViewBag.Countries = Countries.GetCountrySelectList();
            return View(model);
        }
        private NET_FACERECONGNITION_GROUP_INFO[] FindGroups()
        {
            int nMax = 20;
            bool bRet = false;
            NET_IN_FIND_GROUP_INFO stuIn = new NET_IN_FIND_GROUP_INFO();
            NET_OUT_FIND_GROUP_INFO stuOut = new NET_OUT_FIND_GROUP_INFO();
            stuIn.dwSize = (uint)Marshal.SizeOf(stuIn);
            stuOut.dwSize = (uint)Marshal.SizeOf(stuOut);
            stuOut.nMaxGroupNum = nMax;
            try
            {
                stuOut.pGroupInfos = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NET_FACERECONGNITION_GROUP_INFO)) * nMax);
                NET_FACERECONGNITION_GROUP_INFO stuGroup = new NET_FACERECONGNITION_GROUP_INFO();
                stuGroup.dwSize = (uint)Marshal.SizeOf(stuGroup);
                for (int i = 0; i < nMax; i++)
                {
                    IntPtr pAdd = IntPtr.Add(stuOut.pGroupInfos, (int)stuGroup.dwSize * i);
                    Marshal.StructureToPtr(stuGroup, pAdd, true);
                }

                bRet = NETClient.FindGroupInfo(m_LoginID, ref stuIn, ref stuOut, m_WaitTime);
                if (bRet)
                {
                    NET_FACERECONGNITION_GROUP_INFO[] stuGroups = new NET_FACERECONGNITION_GROUP_INFO[stuOut.nRetGroupNum];
                    for (int i = 0; i < stuOut.nRetGroupNum; i++)
                    {
                        IntPtr pAdd = IntPtr.Add(stuOut.pGroupInfos, (int)Marshal.SizeOf(typeof(NET_FACERECONGNITION_GROUP_INFO)) * i);
                        stuGroups[i] = (NET_FACERECONGNITION_GROUP_INFO)Marshal.PtrToStructure(pAdd, typeof(NET_FACERECONGNITION_GROUP_INFO));
                    }
                    return stuGroups;
                }
                else
                {
                    TempData["ErrorMessage"] = NETClient.GetLastError();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            finally
            {
                Marshal.FreeHGlobal(stuOut.pGroupInfos);
            }
            return null;
        }

        private async Task<IActionResult> AddUserInDB(RegisterViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Gender = model.Gender,
                Birthday = model.Birthday,
                Address = model.Address,
                CredentialType = model.CredentialType,
                CredentialNumber = model.CredentialNumber,
                Region = model.Region, // This will be the country code like "US"
                RollNumber = model.RollNumber,
                Class = model.Class,
                EmployeeId = model.EmployeeId,
                Department = model.Department,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                string role = model.UserType.ToString();
                await _userManager.AddToRoleAsync(user, role);

                _logger.LogInformation("User created a new account with password.");

                TempData["SuccessMessage"] = "User registered successfully!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return null;
        }
        private bool addUserInDevice(RegisterViewModel model, byte[]? picture)
        {
            bool ret = false;
            NET_IN_OPERATE_FACERECONGNITIONDB stuInParam = new NET_IN_OPERATE_FACERECONGNITIONDB();
            try
            {
                stuInParam.dwSize = (uint)Marshal.SizeOf(typeof(NET_IN_OPERATE_FACERECONGNITIONDB));
                stuInParam.emOperateType = EM_OPERATE_FACERECONGNITIONDB_TYPE.ADD;//operate
                stuInParam.stPersonInfo.szPersonNameEx = model.FullName.Trim();
                stuInParam.stPersonInfo.szID = model.CredentialNumber.Trim();
                stuInParam.stPersonInfo.bySex = (byte)(model.Gender);
                stuInParam.stPersonInfo.pszGroupID = Marshal.StringToHGlobalAnsi(m_GroupInfo.szGroupId);
                stuInParam.stPersonInfo.bGroupIdLen = (byte)m_GroupInfo.szGroupId.Length;
                stuInParam.stPersonInfo.pszGroupName = Marshal.StringToHGlobalAnsi(m_GroupInfo.szGroupName);
                stuInParam.stPersonInfo.byIDType = (byte)(model.CredentialType);
                stuInParam.stPersonInfo.wYear = (ushort)model.Birthday.Year;
                stuInParam.stPersonInfo.byMonth = (byte)model.Birthday.Month;
                stuInParam.stPersonInfo.byDay = (byte)model.Birthday.Day;
                if (null != picture)
                {
                    stuInParam.stPersonInfo.wFacePicNum = 1;
                    byte[] data = picture;
                    stuInParam.stPersonInfo.szFacePicInfo = new NET_PIC_INFO[48];
                    for (int i = 0; i < 48; i++)
                    {
                        stuInParam.stPersonInfo.szFacePicInfo[i] = new NET_PIC_INFO();
                    }
                    stuInParam.stPersonInfo.szFacePicInfo[0].dwFileLenth = (uint)data.Length;
                    stuInParam.stPersonInfo.szFacePicInfo[0].dwOffSet = 0;
                    stuInParam.nBufferLen = data.Length;
                    if (0 == stuInParam.nBufferLen)
                    {
                        stuInParam.pBuffer = IntPtr.Zero;
                    }
                    else
                    {
                        stuInParam.pBuffer = Marshal.AllocHGlobal(stuInParam.nBufferLen);
                        Marshal.Copy(data, 0, stuInParam.pBuffer, stuInParam.nBufferLen);
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please upload picture!";
                }


                NET_OUT_OPERATE_FACERECONGNITIONDB stuOutParam = new NET_OUT_OPERATE_FACERECONGNITIONDB();
                stuOutParam.dwSize = (uint)Marshal.SizeOf(typeof(NET_OUT_OPERATE_FACERECONGNITIONDB));

                ret = NETClient.OperateFaceRecognitionDB(m_LoginID, ref stuInParam, ref stuOutParam, m_WaitTime);
                if (!ret)
                {
                    TempData["ErrorMessage"] = NETClient.GetLastError();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            finally
            {
                Marshal.FreeHGlobal(stuInParam.stPersonInfo.pszGroupID);
                Marshal.FreeHGlobal(stuInParam.stPersonInfo.pszGroupName);
                Marshal.FreeHGlobal(stuInParam.pBuffer);
            }
            return ret;
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                // Here you would typically send an email with the reset link
                // For now, we'll just redirect to confirmation page

                return RedirectToAction("ForgotPasswordConfirmation");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}