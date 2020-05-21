using data_context.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace webapi_core_using_azure_tablestorage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminConfigurationController : ControllerBase
    {
        private readonly IAdminConfigRepo _adminRepo;
        public AdminConfigurationController(IAdminConfigRepo adminRepo)
        {
            _adminRepo = adminRepo;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllConfiguration()
        {
            return new OkObjectResult(await _adminRepo.GetAll());
        }

        [HttpGet]
        [Route("getbyname")]
        public async Task<IActionResult> GetByName(string configName)
        {
            return new OkObjectResult(await _adminRepo.Get(configName));
        }

        [HttpPost]
        public async Task<IActionResult> InsertAdminConfig([FromBody] AdminConfigResponse command)
        {
            var data = await _adminRepo.Insert(command);
            return new OkObjectResult(data);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAdminConfig([FromBody] AdminConfigResponse command)
        {
            var data = await _adminRepo.Upsert(command);
            return new OkObjectResult(data);
        }

        [HttpDelete]
        [Route("DeleteAdminConfig")]
        public async Task<IActionResult> DeleteAdminConfig(string configName)
        {
            await _adminRepo.Delete(configName);
            return new OkResult();
        }
    }
}
