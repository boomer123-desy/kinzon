using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KingZon.OLCM.Base.WebApi.Controllers
{
    [Route("api/base/[controller]/[action]")]
    [ApiController]
    public class CraftController:KingZonControllerBase
    {
        private readonly CommonService<Base_Craft> _craftService;
        public CraftController(CommonService<Base_Craft> craftService)
        {
            _craftService = craftService;
        }
        /// <summary>
        /// 获取全部工艺数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<Base_Craft>> GetCraftsList()
        {
            var crafts = await _craftService.GetAll().ToListAsync();
            return Ok(crafts);
        }
    }
}
